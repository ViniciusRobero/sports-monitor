import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { CompetitionGroup, Divergence, GoogleSearchSnapshot, LiveMatchGroup, MatchGroup, MatchSnapshot, VerificationUpdate, ProviderStatus } from './models';

@Injectable({ providedIn: 'root' })
export class AlertService {
  readonly divergences = signal<Divergence[]>([]);

  readonly competitionGroups = computed<CompetitionGroup[]>(() => {
    const byMatch = new Map<string, MatchGroup>();
    for (const snapList of this.liveMatches()) {
      if (!snapList.length) continue;
      const first = snapList[0];
      const competition = snapList.map(s => s.competition).find(c => c && !/^\d+$/.test(c)) ?? first.competition ?? '';
      const group: MatchGroup = {
        matchId: first.matchId,
        homeTeam: first.homeTeam,
        awayTeam: first.awayTeam,
        competition,
        kickOff: first.kickOff,
        status: first.status,
        sources: {}
      };
      for (const snap of snapList) group.sources[snap.source] = snap;
      byMatch.set(first.matchId, group);
    }
    const byComp = new Map<string, MatchGroup[]>();
    for (const match of byMatch.values()) {
      const comp = match.competition || 'Sem competição';
      if (!byComp.has(comp)) byComp.set(comp, []);
      byComp.get(comp)!.push(match);
    }
    return [...byComp.entries()]
      .map(([competition, matches]) => ({ competition, matches }))
      .filter(g => !/^\d+$/.test(g.competition))
      .sort((a, b) => a.competition.localeCompare(b.competition));
  });
  readonly liveMatches = signal<LiveMatchGroup[]>([]);
  readonly googleSnapshots = signal<GoogleSearchSnapshot[]>([]);
  readonly providerStatuses = signal<ProviderStatus[]>([]);
  readonly connected = signal(false);
  readonly soundEnabled = signal(true);

  private googlePollTimer?: ReturnType<typeof setInterval>;
  private matchPollTimer?: ReturnType<typeof setInterval>;

  private hub = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/alerts')
    .withAutomaticReconnect()
    .build();

  constructor(private http: HttpClient) {}

  async init(): Promise<void> {
    const recent = await this.http.get<Divergence[]>('/api/divergences?limit=50').toPromise();
    this.divergences.set(recent ?? []);

    this.hub.on('ReceiveAlert', (d: Divergence) => {
      this.divergences.update(list => [d, ...list]);
      this.playAlert();
    });

    this.hub.onreconnected(() => this.connected.set(true));
    this.hub.onclose(() => this.connected.set(false));

    await this.hub.start();
    this.connected.set(true);

    this.fetchMatches();
    this.fetchProviderStatuses();
    this.matchPollTimer = setInterval(() => {
      this.fetchMatches();
      this.fetchProviderStatuses();
    }, 20_000);

    this.fetchGoogleResults();
    this.googlePollTimer = setInterval(() => this.fetchGoogleResults(), 120_000);
  }

  fetchMatches(): void {
    this.http.get<LiveMatchGroup[]>('/api/matches/live').subscribe({
      next: groups => this.liveMatches.set(groups),
      error: () => {}
    });
  }

  fetchProviderStatuses(): void {
    this.http.get<ProviderStatus[]>('/api/providers/status').subscribe({
      next: statuses => this.providerStatuses.set(statuses),
      error: () => {}
    });
  }

  divergencesForMatch(matchId: string): Divergence[] {
    return this.divergences().filter(d => d.matchId === matchId);
  }

  divergencesForSourceAndMatch(source: string, matchId: string): Divergence[] {
    return this.divergences().filter(
      d => d.matchId === matchId &&
           this.isActive(d) &&
           (d.sourceA === source || d.sourceB === source)
    );
  }

  snapshotsBySource(): Map<string, MatchSnapshot[]> {
    const map = new Map<string, MatchSnapshot[]>();
    for (const group of this.liveMatches()) {
      for (const snap of group) {
        if (!map.has(snap.source)) map.set(snap.source, []);
        map.get(snap.source)!.push(snap);
      }
    }
    return map;
  }

  liveMatchIds(): Set<string> {
    return new Set(this.liveMatches().flatMap(g => g.map(s => s.matchId)));
  }

  ignoreDivergence(id: string): void {
    this.verify(id, { status: 'Ignored', replayLink: null, analystNotes: null, manualActionStatus: null });
  }

  ignoreAll(): Promise<void[]> {
    const pending = this.divergences().filter(d => this.isActive(d));
    return Promise.all(pending.map(d =>
      this.verify(d.id, { status: 'Ignored', replayLink: null, analystNotes: null, manualActionStatus: null })
    ));
  }

  toggleSound(): void {
    this.soundEnabled.update(enabled => !enabled);
  }

  isActive(d: Divergence): boolean {
    return !['Confirmed', 'FalsePositive', 'Ignored'].includes(d.verificationStatus);
  }

  private fetchGoogleResults(): void {
    this.http.get<GoogleSearchSnapshot[]>('/api/google-results').subscribe({
      next: snaps => this.googleSnapshots.set(snaps),
      error: () => {}
    });
  }

  googleForMatch(matchId: string): GoogleSearchSnapshot | null {
    return this.googleSnapshots().find(s => s.matchId === matchId) ?? null;
  }

  async verify(id: string, update: VerificationUpdate): Promise<void> {
    await this.http.post(`/api/divergences/${id}/verify`, update).toPromise();
    this.divergences.update(list =>
      list.map(d => d.id === id ? { ...d, ...update, verificationStatus: update.status } : d)
    );
  }

  private playAlert(): void {
    if (!this.soundEnabled()) return;

    const ctx = new AudioContext();

    // Three sharp whistle blasts (referee style)
    const blastAt = (startTime: number) => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      const filter = ctx.createBiquadFilter();
      osc.connect(filter);
      filter.connect(gain);
      gain.connect(ctx.destination);

      filter.type = 'bandpass';
      filter.frequency.value = 2800;
      filter.Q.value = 8;

      osc.type = 'square';
      osc.frequency.setValueAtTime(2700, startTime);
      osc.frequency.linearRampToValueAtTime(3100, startTime + 0.05);
      osc.frequency.linearRampToValueAtTime(2800, startTime + 0.18);

      gain.gain.setValueAtTime(0, startTime);
      gain.gain.linearRampToValueAtTime(0.35, startTime + 0.01);
      gain.gain.setValueAtTime(0.33, startTime + 0.18);
      gain.gain.exponentialRampToValueAtTime(0.001, startTime + 0.24);

      osc.start(startTime);
      osc.stop(startTime + 0.24);
    };

    blastAt(ctx.currentTime);
    blastAt(ctx.currentTime + 0.3);
    blastAt(ctx.currentTime + 0.6);
  }
}

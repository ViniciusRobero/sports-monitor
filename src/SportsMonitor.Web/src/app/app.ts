import { Component, OnInit, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AlertService } from './alert.service';
import { SourcePanel } from './source-panel';
import { MatchSnapshot, Severity } from './models';

const SOURCE_ORDER = ['bet365', 'google', 'sofascore', 'api_football', '365scores'];
const SEVERITY_ORDER: Severity[] = ['Critical', 'High', 'Medium', 'Low'];

@Component({
  selector: 'app-root',
  imports: [SourcePanel],
  template: `
    <header>
      <div class="header-main">
        <span class="title">Sports Monitor</span>
        <span class="conn" [class.ok]="alerts.connected()">
          {{ alerts.connected() ? 'Ao vivo' : 'Desconectado' }}
        </span>
        <span class="count">{{ totalMatches() }} partida{{ totalMatches() !== 1 ? 's' : '' }}</span>
        @if (activeDivergenceCount() > 0) {
          <span class="alert-count">! {{ activeDivergenceCount() }} alerta{{ activeDivergenceCount() !== 1 ? 's' : '' }}</span>
          <button class="btn-ghost danger" (click)="ignoreAll()">Ignorar todos</button>
        }
      </div>

      <div class="filters">
        <input
          class="search"
          type="search"
          placeholder="Time ou competicao"
          [value]="searchText()"
          (input)="searchText.set($any($event.target).value)" />
        <span class="result-count">{{ filteredMatchCount() }} resultado{{ filteredMatchCount() !== 1 ? 's' : '' }}</span>
        <button class="btn-ghost" [class.muted]="!showHalfTime()" (click)="showHalfTime.update(value => !value)">
          HT {{ showHalfTime() ? 'on' : 'off' }}
        </button>
        <button
          class="btn-icon"
          [title]="alerts.soundEnabled() ? 'Desligar som' : 'Ligar som'"
          (click)="alerts.toggleSound()">
          @if (alerts.soundEnabled()) { <span>&#128276;</span> } @else { <span>&#128263;</span> }
        </button>
        <button class="btn-refresh" (click)="refresh()" [disabled]="refreshing()">
          {{ refreshing() ? '...' : 'Atualizar' }}
        </button>
      </div>
    </header>

    @if (activeDivergenceCount() > 0) {
      <section class="alerts-panel">
        <div class="alerts-panel-header">
          <button class="panel-toggle" (click)="alertsPanelOpen.update(value => !value)">
            {{ alertsPanelOpen() ? 'v' : '>' }}
          </button>
          <span class="panel-title">Alertas ativos</span>
          <span class="panel-total">{{ activeDivergenceCount() }}</span>
          <button class="btn-ghost danger" (click)="ignoreAll()">Ignorar todos</button>
        </div>

        @if (alertsPanelOpen()) {
          <div class="alerts-list">
            @for (group of severityGroups(); track group.severity) {
              <div class="severity-group">
                <div class="severity-title">{{ group.severity }}</div>
                @for (d of group.items; track d.id) {
                  <div
                    class="global-alert"
                    [class.Critical]="d.severity === 'Critical'"
                    [class.High]="d.severity === 'High'"
                    [class.Medium]="d.severity === 'Medium'"
                    [class.Low]="d.severity === 'Low'">
                    <div class="global-alert-main">
                      <span class="severity-dot"></span>
                      <span class="global-teams">{{ d.homeTeam }} x {{ d.awayTeam }}</span>
                      <span class="global-type">{{ labelType(d.type) }}</span>
                    </div>
                    <div class="global-values">
                      {{ labelSource(d.sourceA) }}: {{ d.sourceAValue }}
                      <span class="sep">!=</span>
                      {{ labelSource(d.sourceB) }}: {{ d.sourceBValue }}
                    </div>
                    <div class="global-actions">
                      <button class="btn-confirm" (click)="confirm(d.id)">Confirmar</button>
                      <button class="btn-false-pos" (click)="falsePositive(d.id)">Falso positivo</button>
                      <button class="btn-ignore" (click)="alerts.ignoreDivergence(d.id)">Ignorar</button>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        }
      </section>
    }

    <div class="grid">
      @for (source of sources(); track source.key) {
        <app-source-panel
          [source]="source.key"
          [snapshots]="source.snapshots"
          [divergences]="alerts.divergences()"
          [googleSnapshots]="alerts.googleSnapshots()" />
      }
    </div>
  `,
  styles: [`
    :host { display: flex; flex-direction: column; height: 100vh; overflow: hidden; }
    header { display: flex; align-items: center; gap: 12px; padding: 10px 16px; background: #11111b; border-bottom: 1px solid #313244; flex-shrink: 0; flex-wrap: wrap; }
    .header-main, .filters { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
    .header-main { min-width: 260px; }
    .filters { margin-left: auto; }
    .title { font-size: 15px; font-weight: bold; color: #cdd6f4; }
    .conn { font-size: 11px; padding: 2px 8px; border-radius: 3px; background: #f38ba8; color: #1e1e2e; }
    .conn.ok { background: #a6e3a1; }
    .count { font-size: 12px; color: #6c7086; }
    .alert-count { font-size: 12px; color: #f38ba8; font-weight: 600; }
    .result-count { font-size: 12px; color: #6c7086; }
    .search { width: min(260px, 38vw); min-width: 180px; height: 30px; padding: 0 10px; background: #181825; border: 1px solid #313244; color: #cdd6f4; border-radius: 4px; font-size: 12px; outline: none; }
    .btn-refresh, .btn-ghost, .btn-icon { height: 30px; background: #313244; border: 1px solid #45475a; color: #cdd6f4; border-radius: 4px; cursor: pointer; font-size: 12px; }
    .btn-refresh { padding: 4px 12px; }
    .btn-refresh:disabled { opacity: 0.5; cursor: default; }
    .btn-ghost { padding: 4px 10px; }
    .btn-ghost.muted { color: #6c7086; }
    .btn-ghost.danger { color: #f38ba8; border-color: rgba(243,139,168,0.45); }
    .btn-icon { width: 34px; display: inline-flex; align-items: center; justify-content: center; font-size: 14px; }
    .alerts-panel { flex-shrink: 0; background: #181825; border-bottom: 1px solid #313244; }
    .alerts-panel-header { display: flex; align-items: center; gap: 10px; padding: 9px 16px; }
    .panel-toggle { width: 26px; height: 26px; background: #313244; border: 1px solid #45475a; color: #cdd6f4; border-radius: 4px; cursor: pointer; }
    .panel-title { color: #cdd6f4; font-size: 13px; font-weight: 700; }
    .panel-total { font-size: 11px; background: rgba(243,139,168,0.15); color: #f38ba8; padding: 1px 7px; border-radius: 10px; font-weight: 700; margin-right: auto; }
    .alerts-list { max-height: 260px; overflow-y: auto; padding: 0 16px 12px; display: grid; gap: 10px; }
    .severity-group { display: grid; gap: 6px; }
    .severity-title { font-size: 10px; color: #6c7086; text-transform: uppercase; font-weight: 700; }
    .global-alert { display: grid; grid-template-columns: minmax(230px, 1.2fr) minmax(180px, 1fr) auto; align-items: center; gap: 10px; padding: 8px 10px; background: #1e1e2e; border: 1px solid #313244; border-left: 3px solid #a6e3a1; border-radius: 6px; }
    .global-alert.Critical { border-left-color: #f38ba8; }
    .global-alert.High { border-left-color: #fab387; }
    .global-alert.Medium { border-left-color: #f9e2af; }
    .global-alert-main { display: flex; align-items: center; gap: 8px; min-width: 0; }
    .severity-dot { width: 7px; height: 7px; border-radius: 50%; background: #a6e3a1; flex: 0 0 auto; }
    .global-alert.Critical .severity-dot { background: #f38ba8; }
    .global-alert.High .severity-dot { background: #fab387; }
    .global-alert.Medium .severity-dot { background: #f9e2af; }
    .global-teams { color: #cdd6f4; font-size: 12px; font-weight: 700; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .global-type, .global-values { color: #a6adc8; font-size: 11px; }
    .global-values { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .sep { color: #f38ba8; font-weight: 700; padding: 0 4px; }
    .global-actions { display: flex; gap: 5px; justify-content: flex-end; }
    .grid { display: grid; grid-template-columns: minmax(260px, 2fr) minmax(260px, 2fr) repeat(3, minmax(220px, 1.35fr)); gap: 10px; padding: 10px; flex: 1; overflow: auto; min-height: 0; }
    @media (max-width: 1200px) { .grid { grid-template-columns: repeat(3, minmax(240px, 1fr)); } }
    @media (max-width: 900px) {
      .filters { margin-left: 0; width: 100%; }
      .grid { grid-template-columns: 1fr 1fr; overflow-y: auto; }
      .global-alert { grid-template-columns: 1fr; align-items: stretch; }
      .global-actions { justify-content: flex-start; }
    }
    @media (max-width: 520px) {
      header { align-items: stretch; }
      .header-main { width: 100%; }
      .filters { width: 100%; align-items: stretch; }
      .count, .alert-count, .result-count { display: none; }
      .search { width: 100%; min-width: 0; flex: 1 1 100%; }
      .btn-refresh { width: 100%; }
      .btn-ghost { flex: 1; }
      .grid { grid-template-columns: 1fr; overflow-y: auto; }
      .alerts-list { max-height: 220px; padding: 0 10px 10px; }
      .alerts-panel-header { padding: 8px 10px; }
      .global-actions { flex-direction: column; }
      .global-actions button { width: 100%; min-height: 30px; }
    }
  `]
})
export class App implements OnInit {
  refreshing = signal(false);
  searchText = signal('');
  showHalfTime = signal(true);
  alertsPanelOpen = signal(true);

  sources = computed(() => {
    const bySource = this.alerts.snapshotsBySource();
    const allKeys = [...new Set([...SOURCE_ORDER, ...bySource.keys()])];
    return allKeys
      .map(key => ({ key, snapshots: this.filterSnapshots(this.snapshotsForSource(key, bySource)) }))
      .filter(s => SOURCE_ORDER.includes(s.key) || s.snapshots.length > 0);
  });

  totalMatches = computed(() => this.alerts.liveMatches().length);

  filteredMatchCount = computed(() => {
    const ids = new Set<string>();
    for (const source of this.sources()) {
      if (source.key === 'google') continue;
      for (const snap of source.snapshots) ids.add(snap.matchId);
    }
    return ids.size;
  });

  activeDivergences = computed(() =>
    this.alerts.divergences()
      .filter(d => this.alerts.isActive(d))
      .sort((a, b) => SEVERITY_ORDER.indexOf(a.severity) - SEVERITY_ORDER.indexOf(b.severity))
  );

  activeDivergenceCount = computed(() => this.activeDivergences().length);

  severityGroups = computed(() =>
    SEVERITY_ORDER
      .map(severity => ({
        severity,
        items: this.activeDivergences().filter(d => d.severity === severity)
      }))
      .filter(group => group.items.length > 0)
  );

  constructor(public alerts: AlertService, private http: HttpClient) {}

  ngOnInit() { this.alerts.init(); }

  refresh(): void {
    this.refreshing.set(true);
    this.http.post('/api/refresh', {}).subscribe({
      next: () => { this.alerts.fetchMatches(); this.refreshing.set(false); },
      error: () => this.refreshing.set(false)
    });
  }

  ignoreAll(): void {
    this.alerts.ignoreAll().catch(console.error);
  }

  confirm(id: string): void {
    this.alerts.verify(id, { status: 'Confirmed', replayLink: null, analystNotes: null, manualActionStatus: null })
      .catch(console.error);
  }

  falsePositive(id: string): void {
    this.alerts.verify(id, { status: 'FalsePositive', replayLink: null, analystNotes: null, manualActionStatus: null })
      .catch(console.error);
  }

  labelType(t: string): string {
    return ({
      ScoreMismatch: 'Placar divergente',
      GoalScorerMismatch: 'Marcador divergente',
      MissingGoalEvent: 'Gol ausente',
      YellowCardMismatch: 'Cartao amarelo divergente',
      RedCardMismatch: 'Cartao vermelho divergente',
      MatchStatusMismatch: 'Status divergente',
    } as Record<string, string>)[t] ?? t;
  }

  labelSource(s: string): string {
    return ({
      sofascore: 'SofaScore',
      bet365: 'Bet365',
      api_football: 'API Football',
      '365scores': '365Scores',
      google: 'Google',
    } as Record<string, string>)[s] ?? s;
  }

  private snapshotsForSource(key: string, bySource: Map<string, MatchSnapshot[]>): MatchSnapshot[] {
    if (key !== 'google') return bySource.get(key) ?? [];

    return this.alerts.liveMatches()
      .map(group => group[0])
      .filter((snap): snap is MatchSnapshot => Boolean(snap))
      .map(snap => ({ ...snap, source: 'google' }));
  }

  private filterSnapshots(snapshots: MatchSnapshot[]): MatchSnapshot[] {
    const text = this.searchText().trim().toLowerCase();

    return snapshots.filter(snap => {
      const matchesText = !text ||
        snap.homeTeam.toLowerCase().includes(text) ||
        snap.awayTeam.toLowerCase().includes(text) ||
        snap.competition.toLowerCase().includes(text);
      const matchesStatus = this.showHalfTime() || snap.status !== 'HalfTime';
      return matchesText && matchesStatus;
    });
  }
}

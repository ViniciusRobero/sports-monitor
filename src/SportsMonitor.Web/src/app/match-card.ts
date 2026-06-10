import { Component, computed, effect, input, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Divergence, MatchGroup, MatchSnapshot } from './models';
import { DivergenceCard } from './divergence-card';
import { AlertService } from './alert.service';

const SOURCE_ORDER = ['sofascore', 'bet365', 'api_football', '365scores'];
const SOURCE_LABELS: Record<string, string> = {
  sofascore: 'SofaScore', bet365: 'Bet365',
  api_football: 'API Football', '365scores': '365Scores', google: 'Google'
};

@Component({
  selector: 'app-match-card',
  imports: [DivergenceCard, NgClass],
  template: `
    <div class="match-card" [class.has-alert]="activeDivergences().length > 0" [class.alert-mode]="alertMode()">

      <div class="status-row">
        <span class="status-chip" [ngClass]="statusClass()">
          @if (isLive()) { <span class="live-dot"></span> }
          {{ statusLabel() }}
        </span>
        @if (latestMinute()) {
          <span class="minute">{{ latestMinute() }}</span>
        }
        @if (activeDivergences().length > 0) {
          <span class="div-badge">⚠ {{ activeDivergences().length }}</span>
        }
        @if (!alertMode()) {
          <button class="expand-btn" (click)="expanded.update(v => !v)" [title]="expanded() ? 'Recolher' : 'Expandir'">
            {{ expanded() ? '▲' : '▼' }}
          </button>
        }
      </div>

      <div class="score-row">
        <span class="team home">{{ match().homeTeam }}</span>
        <span class="score-number">{{ consensus().home }} – {{ consensus().away }}</span>
        <span class="team away">{{ match().awayTeam }}</span>
      </div>

      <div class="sources-row">
        @for (entry of sourceEntries(); track entry.source) {
          <div class="source-badge" [class.divergent]="isDivergent(entry.source)">
            <span class="src-name">{{ labelSource(entry.source) }}</span>
            <span class="src-score">{{ entry.snap.homeScore }}-{{ entry.snap.awayScore }}</span>
            @if (isDivergent(entry.source)) { <span class="src-warn">⚠</span> }
          </div>
        }
      </div>

      @if (expanded()) {
        <div class="match-detail">
          <table class="detail-table">
            <thead>
              <tr>
                <th>Fonte</th>
                <th>Placar</th>
                <th>Status</th>
                <th>Eventos</th>
              </tr>
            </thead>
            <tbody>
              @for (entry of sourceEntries(); track entry.source) {
                <tr [class.divergent]="isDivergent(entry.source)">
                  <td class="src-cell">
                    {{ labelSource(entry.source) }}
                    @if (isDivergent(entry.source)) { <span class="cell-warn">⚠</span> }
                  </td>
                  <td class="score-cell">{{ entry.snap.homeScore }}-{{ entry.snap.awayScore }}</td>
                  <td class="status-cell">{{ labelStatus(entry.snap.status) }}</td>
                  <td class="events-cell">{{ formatEvents(entry.snap) }}</td>
                </tr>
              }
            </tbody>
          </table>
          @for (d of activeDivergences(); track d.id) {
            <app-divergence-card [d]="d" />
          }
        </div>
      }

    </div>
  `,
  styles: [`
    .match-card {
      background: #1a1a24;
      border-radius: 12px;
      border: 1px solid #2a2a3a;
      overflow: hidden;
      transition: border-color 0.2s;
    }
    .match-card.has-alert { border-color: rgba(255,71,87,0.45); }
    .match-card.alert-mode {
      background: rgba(255,71,87,0.06);
      border-color: #ff4757;
      border-width: 2px;
      animation: alertPulse 2s ease-in-out infinite;
    }
    @keyframes alertPulse {
      0%, 100% { box-shadow: 0 0 0 0 rgba(255,71,87,0); }
      50% { box-shadow: 0 0 0 6px rgba(255,71,87,0.15); }
    }
    .match-card.alert-mode .score-number { color: #ff8b96; }
    .match-card.alert-mode .team { color: #ffd0d4; }

    /* Status row */
    .status-row {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 14px 6px;
    }
    .status-chip {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      font-size: 10px;
      font-weight: 700;
      letter-spacing: 0.8px;
      text-transform: uppercase;
      padding: 3px 9px;
      border-radius: 20px;
    }
    .status-chip.live {
      background: rgba(255,71,87,0.12);
      color: #ff4757;
      border: 1px solid rgba(255,71,87,0.3);
    }
    .status-chip.ht {
      background: rgba(255,165,2,0.12);
      color: #ffa502;
      border: 1px solid rgba(255,165,2,0.3);
    }
    .status-chip.finished {
      background: rgba(112,112,160,0.1);
      color: #7070a0;
      border: 1px solid rgba(112,112,160,0.25);
    }
    .status-chip.not-started {
      background: rgba(124,106,245,0.1);
      color: #7c6af5;
      border: 1px solid rgba(124,106,245,0.25);
    }
    .status-chip.default {
      background: #22222e;
      color: #7070a0;
      border: 1px solid #2a2a3a;
    }
    .live-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #ff4757;
      animation: livePulse 1.5s ease-in-out infinite;
      flex-shrink: 0;
    }
    @keyframes livePulse {
      0%, 100% { opacity: 1; transform: scale(1); }
      50% { opacity: 0.5; transform: scale(0.7); }
    }
    .minute {
      font-size: 11px;
      color: #7070a0;
    }
    .div-badge {
      margin-left: auto;
      font-size: 11px;
      font-weight: 600;
      color: #ff4757;
      background: rgba(255,71,87,0.1);
      padding: 2px 8px;
      border-radius: 10px;
    }
    .expand-btn {
      background: none;
      border: none;
      color: #45455a;
      cursor: pointer;
      padding: 3px 6px;
      font-size: 11px;
      line-height: 1;
      border-radius: 4px;
      transition: color 0.15s;
    }
    .expand-btn:hover { color: #7070a0; }
    .status-row:not(:has(.div-badge)) .expand-btn { margin-left: auto; }

    /* Score row */
    .score-row {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 4px 14px 12px;
    }
    .team {
      flex: 1;
      font-size: 14px;
      font-weight: 600;
      color: #e2e2f0;
      line-height: 1.3;
    }
    .team.home { text-align: right; }
    .team.away { text-align: left; }
    .score-number {
      flex: 0 0 auto;
      font-size: 26px;
      font-weight: 700;
      font-variant-numeric: tabular-nums;
      color: #e2e2f0;
      letter-spacing: -1px;
      min-width: 72px;
      text-align: center;
    }

    /* Source badges */
    .sources-row {
      display: flex;
      flex-wrap: wrap;
      gap: 5px;
      padding: 0 14px 14px;
    }
    .source-badge {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      padding: 3px 10px;
      border-radius: 6px;
      background: #22222e;
      border: 1px solid #2a2a3a;
      font-size: 12px;
      transition: border-color 0.15s;
    }
    .source-badge.divergent {
      border-color: rgba(255,71,87,0.4);
      background: rgba(255,71,87,0.05);
    }
    .src-name { color: #7070a0; font-size: 10px; }
    .src-score { color: #e2e2f0; font-weight: 600; font-variant-numeric: tabular-nums; }
    .src-warn { color: #ff4757; font-size: 10px; }

    /* Expanded detail */
    .match-detail {
      border-top: 1px solid #2a2a3a;
      padding: 12px 14px;
      background: #13131d;
    }
    .detail-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 12px;
      margin-bottom: 4px;
    }
    .detail-table thead tr { background: transparent; }
    .detail-table th {
      padding: 5px 10px;
      text-align: left;
      font-size: 10px;
      color: #45455a;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      border-bottom: 1px solid #2a2a3a;
    }
    .detail-table td { padding: 7px 10px; border-bottom: 1px solid #1e1e2a; }
    .detail-table tr:last-child td { border-bottom: none; }
    .detail-table tr.divergent { background: rgba(255,71,87,0.04); }
    .src-cell { color: #89b4fa; font-weight: 500; display: flex; align-items: center; gap: 5px; }
    .cell-warn { color: #ff4757; font-size: 11px; }
    .score-cell { color: #e2e2f0; font-weight: 600; font-variant-numeric: tabular-nums; white-space: nowrap; }
    .status-cell { font-size: 11px; color: #7070a0; white-space: nowrap; }
    .events-cell { color: #a6adc8; font-size: 11px; }

    @media (max-width: 520px) {
      .score-number { font-size: 22px; min-width: 60px; }
      .team { font-size: 13px; }
      .detail-table { font-size: 11px; }
      .events-cell { display: none; }
    }
  `]
})
export class MatchCard {
  match = input.required<MatchGroup>();
  divergences = input<Divergence[]>([]);
  alertMode = input<boolean>(false);

  expanded = signal(false);

  constructor(private alerts: AlertService) {
    effect(() => {
      if (this.alertMode()) this.expanded.set(true);
    });
  }

  activeDivergences = computed(() =>
    this.divergences().filter(d => this.alerts.isActive(d))
  );

  private divergentSources = computed(() => {
    const active = this.activeDivergences();
    return new Set([...active.map(d => d.sourceA), ...active.map(d => d.sourceB)]);
  });

  isDivergent(source: string): boolean {
    return this.divergentSources().has(source);
  }

  sourceEntries = computed(() =>
    SOURCE_ORDER
      .filter(s => this.match().sources[s])
      .map(s => ({ source: s, snap: this.match().sources[s] }))
  );

  consensus = computed(() => {
    const entries = this.sourceEntries();
    if (!entries.length) return { home: '-', away: '-' };
    // Keep numeric scores intact — do NOT build a "h-a" string and split('-'),
    // which corrupts negative scores (e.g. "-1--1" -> ['','1','','1']).
    const counts = new Map<string, { home: number; away: number; count: number }>();
    for (const { snap } of entries) {
      const key = `${snap.homeScore}|${snap.awayScore}`;
      const cur = counts.get(key);
      if (cur) cur.count++;
      else counts.set(key, { home: snap.homeScore, away: snap.awayScore, count: 1 });
    }
    const best = [...counts.values()].sort((a, b) => b.count - a.count)[0];
    const fmt = (n: number) => (n < 0 ? '-' : `${n}`);
    return { home: fmt(best.home), away: fmt(best.away) };
  });

  statusClass = computed(() => {
    const s = this.match().status;
    if (s === 'Live') return 'live';
    if (s === 'HalfTime') return 'ht';
    if (s === 'Finished') return 'finished';
    if (s === 'NotStarted') return 'not-started';
    return 'default';
  });

  isLive = computed(() => this.match().status === 'Live');

  statusLabel = computed(() => {
    const labels: Record<string, string> = {
      Live: 'Ao Vivo', HalfTime: 'Intervalo', Finished: 'Encerrada',
      NotStarted: 'Não iniciada', Postponed: 'Adiada', Cancelled: 'Cancelada'
    };
    return labels[this.match().status] ?? this.match().status;
  });

  latestMinute = computed(() => {
    const entries = this.sourceEntries();
    if (!entries.length) return '';
    const latest = entries.reduce((a, b) =>
      new Date(a.snap.collectedAt) > new Date(b.snap.collectedAt) ? a : b
    );
    const events = latest.snap.events ?? [];
    if (!events.length) return '';
    const last = events.reduce((a: any, b) => b.minute > a.minute ? b : a, { minute: 0 });
    return last.minute > 0 ? `${last.minute}'` : '';
  });

  formatEvents(snap: MatchSnapshot): string {
    if (!snap.events?.length) return '—';
    const icons: Record<string, string> = {
      Goal: '⚽', OwnGoal: '⚽(CG)', Penalty: '⚽(P)',
      YellowCard: '🟡', RedCard: '🔴'
    };
    return snap.events
      .filter(e => e.type !== 'Substitution')
      .map(e => `${icons[e.type] ?? e.type} ${e.playerName} (${e.minute}')`)
      .join(' · ') || '—';
  }

  labelSource(s: string): string { return SOURCE_LABELS[s] ?? s; }

  labelStatus(s: string): string {
    const labels: Record<string, string> = {
      Live: 'Ao vivo', HalfTime: 'Intervalo', Finished: 'Encerrada',
      NotStarted: 'Não iniciada', Postponed: 'Adiada', Cancelled: 'Cancelada'
    };
    return labels[s] ?? s;
  }

}

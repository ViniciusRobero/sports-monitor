import { Component, computed, input } from '@angular/core';
import { Divergence, LiveMatchGroup, MatchSnapshot } from './models';
import { DivergenceCard } from './divergence-card';
import { AlertService } from './alert.service';
import { LocalTimePipe } from './local-time.pipe';

@Component({
  selector: 'app-match-card',
  imports: [DivergenceCard, LocalTimePipe],
  template: `
    <div class="match-card" [class.has-alert]="divergences().length > 0">

      <div class="match-header">
        <div class="match-title">
          <span class="teams">{{ first().homeTeam }} × {{ first().awayTeam }}</span>
          <span class="competition">{{ first().competition }}</span>
        </div>
        <div class="match-meta">
          <span class="minute">{{ latestMinute() }}</span>
          @if (divergences().length > 0) {
            <span class="alert-badge">⚠ {{ divergences().length }} divergência{{ divergences().length > 1 ? 's' : '' }}</span>
          }
          <span class="updated">{{ first().collectedAt | localTime }}</span>
        </div>
      </div>

      <table class="sources-table">
        <thead>
          <tr>
            <th>Fonte</th>
            <th>Placar</th>
            <th>Eventos</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          @for (snap of snapshots(); track snap.source) {
            <tr [class.divergent]="isDivergent(snap.source)">
              <td class="source-name">
                {{ labelSource(snap.source) }}
                @if (isDivergent(snap.source)) { <span class="warn">⚠</span> }
              </td>
              <td class="score">{{ snap.homeScore }}-{{ snap.awayScore }}</td>
              <td class="events">{{ formatEvents(snap) }}</td>
              <td class="status">{{ labelStatus(snap.status) }}</td>
            </tr>
          }
        </tbody>
      </table>

      @if (divergences().length > 0) {
        <div class="divergences-section">
          <div class="div-section-title">Divergências detectadas</div>
          @for (d of divergences(); track d.id) {
            <app-divergence-card [d]="d" />
          }
        </div>
      }

    </div>
  `,
  styles: [`
    .match-card { background: #1e1e2e; border-radius: 6px; margin-bottom: 14px; overflow: hidden; border: 1px solid #313244; }
    .match-card.has-alert { border-color: #f38ba8; }

    .match-header { display: flex; justify-content: space-between; align-items: flex-start; padding: 12px 14px 8px; border-bottom: 1px solid #313244; gap: 12px; flex-wrap: wrap; }
    .match-title { display: flex; flex-direction: column; gap: 2px; }
    .teams { font-size: 15px; font-weight: 600; color: #cdd6f4; }
    .competition { font-size: 11px; color: #6c7086; text-transform: uppercase; letter-spacing: 0.5px; }
    .match-meta { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
    .minute { font-size: 12px; color: #89b4fa; background: #313244; padding: 2px 8px; border-radius: 10px; }
    .alert-badge { font-size: 11px; color: #1e1e2e; background: #f38ba8; padding: 2px 8px; border-radius: 10px; font-weight: 600; }
    .updated { font-size: 11px; color: #45475a; }

    .sources-table { width: 100%; border-collapse: collapse; font-size: 13px; }
    .sources-table thead tr { background: #181825; }
    .sources-table th { padding: 6px 14px; text-align: left; font-size: 11px; color: #6c7086; font-weight: 500; text-transform: uppercase; letter-spacing: 0.5px; }
    .sources-table td { padding: 8px 14px; border-top: 1px solid #313244; }
    .sources-table tr.divergent { background: rgba(243,139,168,0.06); }
    .sources-table tr.divergent td { border-top-color: rgba(243,139,168,0.2); }

    .source-name { color: #89b4fa; font-weight: 500; display: flex; align-items: center; gap: 6px; }
    .warn { color: #f38ba8; font-size: 12px; }
    .score { color: #cdd6f4; font-weight: 600; font-size: 15px; font-variant-numeric: tabular-nums; }
    .events { color: #a6adc8; font-size: 12px; max-width: 260px; }
    .status { font-size: 11px; color: #6c7086; }

    .divergences-section { padding: 10px 14px 14px; border-top: 1px solid #313244; background: #181825; }
    .div-section-title { font-size: 11px; color: #6c7086; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }
  `]
})
export class MatchCard {
  snapshots = input.required<LiveMatchGroup>();
  divergences = input<Divergence[]>([]);

  first = computed(() => this.snapshots()[0]);

  private divergentSources = computed(() => {
    const active = this.divergences().filter(
      d => !['Confirmed', 'FalsePositive', 'Ignored'].includes(d.verificationStatus)
    );
    return new Set([...active.map(d => d.sourceA), ...active.map(d => d.sourceB)]);
  });

  isDivergent(source: string): boolean {
    return this.divergentSources().has(source);
  }

  latestMinute(): string {
    const snap = this.snapshots().reduce((a, b) =>
      new Date(a.collectedAt) > new Date(b.collectedAt) ? a : b
    );
    const status = snap.status;
    if (status === 'HalfTime') return 'Intervalo';
    if (status === 'Finished') return 'Encerrada';
    if (status === 'NotStarted') return 'Não iniciada';
    if (status === 'Postponed') return 'Adiada';
    const lastGoal = snap.events.filter(e => e.type === 'Goal' || e.type === 'OwnGoal' || e.type === 'Penalty');
    const lastEvent = snap.events.reduce((a, b) => b.minute > a.minute ? b : a, { minute: 0 } as any);
    return lastEvent.minute > 0 ? `${lastEvent.minute}'` : 'Ao vivo';
  }

  formatEvents(snap: MatchSnapshot): string {
    if (!snap.events || snap.events.length === 0) return '—';
    const icons: Record<string, string> = {
      Goal: '⚽', OwnGoal: '⚽(CG)', Penalty: '⚽(P)',
      YellowCard: '🟡', RedCard: '🔴', Substitution: '🔄', VAR: 'VAR'
    };
    return snap.events
      .filter(e => e.type !== 'Substitution')
      .map(e => `${icons[e.type] ?? e.type} ${e.playerName} (${e.minute}')`)
      .join(' · ') || '—';
  }

  labelSource(s: string): string {
    return ({ sofascore: 'SofaScore', bet365: 'Bet365', api_football: 'API Football', '365scores': '365Scores', google: 'Google' } as any)[s] ?? s;
  }

  labelStatus(s: string): string {
    return ({ Live: 'Ao vivo', HalfTime: 'Intervalo', Finished: 'Encerrada', NotStarted: 'Não iniciada', Postponed: 'Adiada', Cancelled: 'Cancelada' } as any)[s] ?? s;
  }
}

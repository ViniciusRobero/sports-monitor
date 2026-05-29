import { Component, computed, input } from '@angular/core';
import { Divergence, GoogleSearchResult, MatchSnapshot } from './models';
import { AlertService } from './alert.service';

@Component({
  selector: 'app-source-match-card',
  imports: [],
  template: `
    <div class="smc" [class.has-alert]="activeDivergences().length > 0">

      <div class="smc-header">
        <span class="teams">{{ snap().homeTeam }} × {{ snap().awayTeam }}</span>
        <span class="minute">{{ matchMinute() }}</span>
      </div>

      <div class="smc-score">
        <span class="score-home">{{ snap().homeScore }}</span>
        <span class="score-sep">-</span>
        <span class="score-away">{{ snap().awayScore }}</span>
      </div>

      @if (!isGoogle()) {
        <div class="smc-events">
          @if (keyEvents().length === 0) {
            <span class="no-events">Sem eventos</span>
          }
          @for (ev of keyEvents(); track $index) {
            <div class="event">{{ eventIcon(ev.type) }} {{ ev.playerName }}<span class="ev-min">({{ ev.minute }}')</span></div>
          }
        </div>
      }

      @if (isGoogle() && googleSnippets().length > 0) {
        <div class="smc-snippets">
          @for (r of googleSnippets(); track r.url) {
            <a [href]="r.url" target="_blank" class="snippet">
              <span class="snippet-title">{{ r.title }}</span>
              <span class="snippet-text">{{ r.snippet }}</span>
            </a>
          }
        </div>
      }

      @for (d of activeDivergences(); track d.id) {
        <div class="alert-bar">
          <div class="alert-row">
            <span class="alert-icon">⚠</span>
            <span class="alert-type">{{ labelType(d.type) }}</span>
          </div>
          <div class="alert-values">
            <span class="val-a">{{ labelSource(d.sourceA) }}: {{ d.sourceAValue }}</span>
            <span class="sep">≠</span>
            <span class="val-b">{{ labelSource(d.sourceB) }}: {{ d.sourceBValue }}</span>
          </div>
          <button class="btn-ignore" (click)="ignore(d.id)">Ignorar</button>
        </div>
      }

    </div>
  `,
  styles: [`
    .smc { background: #181825; border-radius: 6px; padding: 10px 12px; margin-bottom: 8px; border: 1px solid #313244; transition: border-color 0.3s; }
    .smc.has-alert { border-color: #f38ba8; animation: pulse-alert 1.8s ease-in-out infinite; }
    @keyframes pulse-alert {
      0%, 100% { box-shadow: 0 0 0 0 rgba(243,139,168,0.4); }
      50%       { box-shadow: 0 0 0 6px rgba(243,139,168,0); }
    }

    .smc-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
    .teams { font-size: 12px; font-weight: 600; color: #cdd6f4; }
    .minute { font-size: 11px; color: #89b4fa; background: #313244; padding: 1px 6px; border-radius: 8px; }

    .smc-score { display: flex; align-items: center; gap: 6px; margin-bottom: 8px; }
    .score-home, .score-away { font-size: 28px; font-weight: 700; color: #cdd6f4; font-variant-numeric: tabular-nums; line-height: 1; }
    .score-sep { font-size: 20px; color: #45475a; }

    .smc-events { font-size: 11px; margin-bottom: 6px; display: flex; flex-direction: column; gap: 3px; }
    .event { color: #a6adc8; display: flex; align-items: center; gap: 4px; }
    .ev-min { color: #45475a; margin-left: 2px; }
    .no-events { color: #45475a; font-style: italic; }

    .smc-snippets { display: flex; flex-direction: column; gap: 4px; margin-bottom: 6px; }
    .snippet { display: flex; flex-direction: column; background: #1e1e2e; border-radius: 4px; padding: 5px 8px; text-decoration: none; }
    .snippet:hover { background: #313244; }
    .snippet-title { font-size: 11px; color: #89b4fa; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .snippet-text { font-size: 10px; color: #6c7086; margin-top: 2px; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }

    .alert-bar { background: rgba(243,139,168,0.08); border: 1px solid rgba(243,139,168,0.3); border-radius: 4px; padding: 7px 10px; margin-top: 8px; display: flex; flex-direction: column; gap: 4px; }
    .alert-row { display: flex; align-items: center; gap: 6px; }
    .alert-icon { color: #f38ba8; font-size: 12px; }
    .alert-type { color: #f38ba8; font-size: 11px; font-weight: 600; }
    .alert-values { font-size: 11px; color: #a6adc8; display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .val-a, .val-b { color: #cdd6f4; }
    .sep { color: #f38ba8; font-weight: bold; }
    .btn-ignore { align-self: flex-end; padding: 3px 10px; background: #313244; border: 1px solid #45475a; color: #6c7086; border-radius: 4px; cursor: pointer; font-size: 11px; margin-top: 2px; }
    .btn-ignore:hover { background: #45475a; color: #cdd6f4; }
  `]
})
export class SourceMatchCard {
  snap = input.required<MatchSnapshot>();
  divergences = input<Divergence[]>([]);
  isGoogle = input(false);
  googleSnippets = input<GoogleSearchResult[]>([]);

  constructor(private alerts: AlertService) {}

  activeDivergences = computed(() =>
    this.divergences().filter(d => d.verificationStatus !== 'Ignored')
  );

  keyEvents = computed(() =>
    (this.snap().events ?? []).filter(e => e.type !== 'Substitution')
  );

  matchMinute(): string {
    const s = this.snap().status;
    if (s === 'HalfTime') return 'Intervalo';
    if (s === 'Finished') return 'Encerrada';
    if (s === 'NotStarted') return 'Não iniciada';
    if (s === 'Postponed') return 'Adiada';
    const events = this.snap().events ?? [];
    const last = events.reduce((a, b) => b.minute > a.minute ? b : a, { minute: 0 } as any);
    return last.minute > 0 ? `${last.minute}'` : 'Ao vivo';
  }

  eventIcon(type: string): string {
    const icons: Record<string, string> = {
      Goal: '⚽', OwnGoal: '⚽', Penalty: '⚽', YellowCard: '🟡', RedCard: '🔴', VAR: '📺'
    };
    return icons[type] ?? '•';
  }

  labelType(t: string): string {
    return ({
      ScoreMismatch: 'Placar divergente', GoalScorerMismatch: 'Marcador divergente',
      MissingGoalEvent: 'Gol ausente', YellowCardMismatch: 'Cartão amarelo divergente',
      RedCardMismatch: 'Cartão vermelho divergente', MatchStatusMismatch: 'Status divergente',
    } as any)[t] ?? t;
  }

  labelSource(s: string): string {
    return ({ sofascore: 'SofaScore', bet365: 'Bet365', api_football: 'API Football', '365scores': '365Scores' } as any)[s] ?? s;
  }

  ignore(id: string): void {
    this.alerts.ignoreDivergence(id);
  }
}

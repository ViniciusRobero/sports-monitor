import { Component, input } from '@angular/core';
import { Divergence } from './models';
import { AlertService } from './alert.service';
import { LocalTimePipe } from './local-time.pipe';

// Inline alert component — shown inside source-match-card when a divergence is active.
@Component({
  selector: 'app-divergence-card',
  imports: [LocalTimePipe],
  template: `
    @if (d().verificationStatus !== 'Ignored') {
      <div class="alert-bar" [class]="d().severity">
        <div class="alert-row">
          <span class="alert-icon">⚠</span>
          <span class="alert-type">{{ labelType(d().type) }}</span>
          <span class="alert-time">{{ d().detectedAt | localTime }}</span>
        </div>
        <div class="alert-values">
          <span class="val">{{ labelSource(d().sourceA) }}: <strong>{{ d().sourceAValue }}</strong></span>
          <span class="sep">≠</span>
          <span class="val">{{ labelSource(d().sourceB) }}: <strong>{{ d().sourceBValue }}</strong></span>
        </div>
        <button class="btn-ignore" (click)="ignore()">Ignorar</button>
      </div>
    }
  `,
  styles: [`
    .alert-bar { border-radius: 4px; padding: 7px 10px; margin-top: 6px; display: flex; flex-direction: column; gap: 4px; border-left: 3px solid; }
    .alert-bar.Critical { background: rgba(243,139,168,0.08); border-color: #f38ba8; }
    .alert-bar.High { background: rgba(250,179,135,0.08); border-color: #fab387; }
    .alert-bar.Medium { background: rgba(249,226,175,0.08); border-color: #f9e2af; }
    .alert-bar.Low { background: rgba(166,227,161,0.08); border-color: #a6e3a1; }
    .alert-row { display: flex; align-items: center; gap: 6px; }
    .alert-icon { font-size: 12px; }
    .alert-bar.Critical .alert-icon, .alert-bar.Critical .alert-type { color: #f38ba8; }
    .alert-bar.High .alert-icon, .alert-bar.High .alert-type { color: #fab387; }
    .alert-bar.Medium .alert-icon, .alert-bar.Medium .alert-type { color: #f9e2af; }
    .alert-bar.Low .alert-icon, .alert-bar.Low .alert-type { color: #a6e3a1; }
    .alert-type { font-size: 11px; font-weight: 600; flex: 1; }
    .alert-time { font-size: 10px; color: #45475a; }
    .alert-values { font-size: 11px; color: #a6adc8; display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .val { color: #a6adc8; }
    .val strong { color: #cdd6f4; }
    .sep { color: #f38ba8; font-weight: bold; }
    .btn-ignore { align-self: flex-end; padding: 3px 10px; background: #313244; border: 1px solid #45475a; color: #6c7086; border-radius: 4px; cursor: pointer; font-size: 11px; }
    .btn-ignore:hover { background: #45475a; color: #cdd6f4; }
  `]
})
export class DivergenceCard {
  d = input.required<Divergence>();

  constructor(private alerts: AlertService) {}

  ignore(): void { this.alerts.ignoreDivergence(this.d().id); }

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
}

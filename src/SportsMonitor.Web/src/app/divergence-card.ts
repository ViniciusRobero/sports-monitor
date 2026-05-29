import { Component, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Divergence, GoogleSearchSnapshot, VerificationStatus } from './models';
import { AlertService } from './alert.service';
import { LocalTimePipe } from './local-time.pipe';

@Component({
  selector: 'app-divergence-card',
  imports: [FormsModule, LocalTimePipe],
  template: `
    <div class="card" [class]="severityClass()">
      <div class="card-header">
        <span class="badge">{{ labelSeverity(d().severity) }}</span>
        <span class="type">{{ labelType(d().type) }}</span>
        <span class="match">{{ d().homeTeam }} vs {{ d().awayTeam }}</span>
        <span class="time">{{ d().detectedAt | localTime }}</span>
        <span class="status" [class]="statusClass()">{{ labelStatus(d().verificationStatus) }}</span>
      </div>

      <div class="sources">
        <div class="source">
          <strong>{{ labelSource(d().sourceA) }}</strong>
          <span>{{ d().sourceAValue }}</span>
        </div>
        <div class="vs">vs</div>
        <div class="source">
          <strong>{{ labelSource(d().sourceB) }}</strong>
          <span>{{ d().sourceBValue }}</span>
        </div>
        @if (d().officialSourceValue) {
          <div class="official">Oficial: {{ d().officialSourceValue }}</div>
        }
      </div>

      <div class="quick-links">
        <a [href]="googleUrl()" target="_blank" class="google-link">🔍 Google</a>
      </div>

      @if (googleSnapshot()) {
        <div class="google-panel">
          <div class="google-panel-header">Google — {{ googleSnapshot()!.fetchedAt | localTime }}</div>
          @for (r of googleSnapshot()!.results; track r.url) {
            <a [href]="r.url" target="_blank" class="google-result">
              <span class="g-title">{{ r.title }}</span>
              <span class="g-snippet">{{ r.snippet }}</span>
            </a>
          }
        </div>
      }

      @if (!verified()) {
        <div class="verify-form">
          <input [(ngModel)]="replayLink" placeholder="Link do replay (YouTube, etc.)" />
          <textarea [(ngModel)]="notes" placeholder="Notas do analista..." rows="2"></textarea>
          <div class="actions">
            <button class="btn-confirm" (click)="verify('Confirmed')">Confirmado</button>
            <button class="btn-false" (click)="verify('FalsePositive')">Falso Positivo</button>
            <button class="btn-ignore" (click)="verify('Ignored')">Ignorar</button>
          </div>
        </div>
      } @else {
        <div class="verified-info">
          @if (d().replayLink) { <a [href]="d().replayLink" target="_blank">Replay</a> }
          @if (d().analystNotes) { <span class="notes">{{ d().analystNotes }}</span> }
        </div>
      }
    </div>
  `,
  styles: [`
    .card { border-left: 4px solid; padding: 12px; margin-bottom: 10px; background: #1e1e2e; border-radius: 4px; }
    .card.Critical { border-color: #f38ba8; }
    .card.High { border-color: #fab387; }
    .card.Medium { border-color: #f9e2af; }
    .card.Low { border-color: #a6e3a1; }
    .card-header { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; margin-bottom: 8px; font-size: 13px; }
    .badge { background: #f38ba8; color: #1e1e2e; padding: 2px 6px; border-radius: 3px; font-weight: bold; font-size: 11px; }
    .card.High .badge { background: #fab387; }
    .card.Medium .badge { background: #f9e2af; }
    .card.Low .badge { background: #a6e3a1; }
    .type { color: #cdd6f4; font-weight: 500; }
    .match { color: #89b4fa; flex: 1; }
    .time { color: #6c7086; font-size: 11px; }
    .status { font-size: 11px; padding: 2px 6px; border-radius: 3px; }
    .status.Pending { background: #45475a; color: #cdd6f4; }
    .status.Confirmed { background: #a6e3a1; color: #1e1e2e; }
    .status.FalsePositive { background: #6c7086; color: #cdd6f4; }
    .status.Ignored { background: #313244; color: #6c7086; }
    .sources { display: flex; gap: 12px; align-items: center; font-size: 13px; margin-bottom: 8px; }
    .source { display: flex; flex-direction: column; gap: 2px; }
    .source strong { color: #89b4fa; font-size: 11px; text-transform: uppercase; }
    .source span { color: #cdd6f4; font-size: 14px; font-weight: 500; }
    .vs { color: #6c7086; font-size: 11px; }
    .official { color: #a6e3a1; font-size: 12px; margin-left: auto; }
    .verify-form { display: flex; flex-direction: column; gap: 6px; margin-top: 8px; }
    input, textarea { background: #313244; border: 1px solid #45475a; color: #cdd6f4; padding: 6px 8px; border-radius: 4px; font-size: 12px; resize: none; }
    .actions { display: flex; gap: 6px; }
    button { padding: 5px 12px; border-radius: 4px; border: none; cursor: pointer; font-size: 12px; font-weight: 500; }
    .btn-confirm { background: #a6e3a1; color: #1e1e2e; }
    .btn-false { background: #6c7086; color: #cdd6f4; }
    .btn-ignore { background: #313244; color: #6c7086; }
    .quick-links { margin-bottom: 6px; }
    .google-link { font-size: 12px; padding: 3px 8px; background: #313244; border-radius: 3px; color: #89b4fa; text-decoration: none; }
    .google-link:hover { background: #45475a; }
    .verified-info { display: flex; gap: 10px; font-size: 12px; color: #6c7086; margin-top: 4px; }
    .verified-info a { color: #89b4fa; }
    .notes { font-style: italic; }
    .google-panel { margin-top: 10px; border-top: 1px solid #313244; padding-top: 8px; }
    .google-panel-header { font-size: 11px; color: #6c7086; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 6px; }
    .google-result { display: flex; flex-direction: column; gap: 2px; padding: 6px 8px; margin-bottom: 4px; background: #181825; border-radius: 4px; text-decoration: none; }
    .google-result:hover { background: #313244; }
    .g-title { color: #89b4fa; font-size: 12px; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .g-snippet { color: #a6adc8; font-size: 11px; line-height: 1.4; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
  `]
})
export class DivergenceCard {
  d = input.required<Divergence>();
  googleSnapshot = input<GoogleSearchSnapshot | null>(null);
  replayLink = '';
  notes = '';

  constructor(private alerts: AlertService) {}

  googleUrl(): string {
    const q = encodeURIComponent(`${this.d().homeTeam} ${this.d().awayTeam} placar ao vivo`);
    return `https://www.google.com/search?q=${q}`;
  }

  verified(): boolean {
    return ['Confirmed', 'FalsePositive', 'Ignored'].includes(this.d().verificationStatus);
  }

  severityClass(): string { return this.d().severity; }
  statusClass(): string { return this.d().verificationStatus; }

  labelSeverity(s: string): string {
    return ({ Critical: 'Crítico', High: 'Alto', Medium: 'Médio', Low: 'Baixo' } as any)[s] ?? s;
  }

  labelType(t: string): string {
    return ({
      ScoreMismatch:        'Placar divergente',
      GoalScorerMismatch:   'Marcador divergente',
      MissingGoalEvent:     'Gol ausente',
      YellowCardMismatch:   'Cartão amarelo divergente',
      RedCardMismatch:      'Cartão vermelho divergente',
      MatchStatusMismatch:  'Status da partida divergente',
      SubstitutionMismatch: 'Substituição divergente',
      VARDecisionMismatch:  'Decisão de VAR divergente',
    } as any)[t] ?? t;
  }

  labelStatus(s: string): string {
    return ({
      Pending:      'Pendente',
      InAnalysis:   'Em análise',
      Confirmed:    'Confirmado',
      FalsePositive:'Falso positivo',
      Ignored:      'Ignorado',
    } as any)[s] ?? s;
  }

  labelSource(s: string): string {
    return ({
      sofascore:   'SofaScore',
      bet365:      'Bet365',
      api_football:'API Football',
      '365scores': '365Scores',
      google:      'Google',
    } as any)[s] ?? s;
  }

  async verify(status: VerificationStatus): Promise<void> {
    await this.alerts.verify(this.d().id, {
      status,
      replayLink: this.replayLink || null,
      analystNotes: this.notes || null,
      manualActionStatus: null
    });
  }
}

import { Component, computed, input } from '@angular/core';
import { Divergence, GoogleSearchSnapshot, MatchSnapshot } from './models';
import { AlertService } from './alert.service';
import { SourceMatchCard } from './source-match-card';

const SOURCE_META: Record<string, { label: string; icon: string }> = {
  bet365:      { label: 'Bet365',       icon: '🟢' },
  google:      { label: 'Google',       icon: '🔍' },
  sofascore:   { label: 'SofaScore',    icon: '📊' },
  api_football:{ label: 'API Football', icon: '⚽' },
  '365scores': { label: '365Scores',    icon: '📡' },
};

@Component({
  selector: 'app-source-panel',
  imports: [SourceMatchCard],
  template: `
    <div class="panel">
      <div class="panel-header">
        <span class="panel-icon">{{ meta().icon }}</span>
        <span class="panel-label">{{ meta().label }}</span>
        @if (snapshots().length > 0) {
          <span class="panel-count">{{ snapshots().length }}</span>
        }
        @if (alertCount() > 0) {
          <span class="panel-alerts">⚠ {{ alertCount() }}</span>
        }
      </div>

      <div class="panel-body">
        @if (snapshots().length === 0) {
          <div class="empty">Sem partidas ao vivo</div>
        }
        @for (snap of snapshots(); track snap.matchId) {
          <app-source-match-card
            [snap]="snap"
            [divergences]="divForSnap(snap)"
            [isGoogle]="isGoogle()"
            [googleSnippets]="snippetsForSnap(snap.matchId)" />
        }
      </div>
    </div>
  `,
  styles: [`
    .panel { display: flex; flex-direction: column; background: #1e1e2e; border-radius: 8px; border: 1px solid #313244; height: 100%; min-height: 0; }

    .panel-header { display: flex; align-items: center; gap: 8px; padding: 10px 14px; border-bottom: 1px solid #313244; background: #181825; border-radius: 8px 8px 0 0; flex-shrink: 0; }
    .panel-icon { font-size: 14px; }
    .panel-label { font-size: 13px; font-weight: 700; color: #cdd6f4; text-transform: uppercase; letter-spacing: 0.5px; flex: 1; }
    .panel-count { font-size: 11px; background: #313244; color: #89b4fa; padding: 1px 7px; border-radius: 10px; font-weight: 600; }
    .panel-alerts { font-size: 11px; background: rgba(243,139,168,0.15); color: #f38ba8; padding: 1px 7px; border-radius: 10px; font-weight: 600; }

    .panel-body { padding: 10px; overflow-y: auto; flex: 1; }
    .empty { color: #45475a; font-size: 12px; text-align: center; padding: 24px 0; font-style: italic; }
  `]
})
export class SourcePanel {
  source = input.required<string>();
  snapshots = input<MatchSnapshot[]>([]);
  divergences = input<Divergence[]>([]);
  googleSnapshots = input<GoogleSearchSnapshot[]>([]);

  constructor(private alerts: AlertService) {}

  isGoogle = computed(() => this.source() === 'google');

  meta = computed(() => SOURCE_META[this.source()] ?? { label: this.source(), icon: '📌' });

  alertCount = computed(() =>
    this.divergences().filter(d => d.verificationStatus !== 'Ignored').length
  );

  divForSnap(snap: MatchSnapshot): Divergence[] {
    return this.alerts.divergencesForSourceAndMatch(this.source(), snap.matchId);
  }

  snippetsForSnap(matchId: string): import('./models').GoogleSearchResult[] {
    const snap = this.googleSnapshots().find(s => s.matchId === matchId);
    return snap?.results ?? [];
  }
}

import { Component, computed, input, signal } from '@angular/core';
import { CompetitionGroup, Divergence } from './models';
import { AlertService } from './alert.service';
import { MatchCard } from './match-card';

@Component({
  selector: 'app-competition-section',
  imports: [MatchCard],
  template: `
    <div class="comp-section">
      <button class="comp-header" (click)="expanded.update(v => !v)">
        <span class="comp-name">{{ group().competition }}</span>
        <span class="match-count">{{ group().matches.length }} partida{{ group().matches.length !== 1 ? 's' : '' }}</span>
        @if (alertCount() > 0) {
          <span class="alert-chip">⚠ {{ alertCount() }}</span>
        }
        <span class="chevron" [class.open]="expanded()">▶</span>
      </button>
      @if (expanded()) {
        <div class="matches-list">
          @for (match of group().matches; track match.matchId) {
            <app-match-card
              [match]="match"
              [divergences]="divForMatch(match.matchId)" />
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .comp-section {
      border-radius: 12px;
      border: 1px solid #2a2a3a;
      overflow: hidden;
    }

    .comp-header {
      width: 100%;
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 12px 16px;
      background: #1a1a24;
      border: none;
      cursor: pointer;
      text-align: left;
      color: inherit;
      transition: background 0.15s;
    }
    .comp-header:hover { background: #20202c; }

    .comp-name {
      font-size: 12px;
      font-weight: 700;
      color: #e2e2f0;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      flex: 1;
      text-align: left;
    }
    .match-count {
      font-size: 11px;
      color: #7070a0;
      background: #22222e;
      padding: 2px 8px;
      border-radius: 10px;
    }
    .alert-chip {
      font-size: 11px;
      color: #ff4757;
      background: rgba(255,71,87,0.1);
      padding: 2px 8px;
      border-radius: 10px;
      font-weight: 600;
    }
    .chevron {
      font-size: 10px;
      color: #45455a;
      transition: transform 0.2s ease;
      display: inline-block;
    }
    .chevron.open { transform: rotate(90deg); }

    .matches-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 8px;
      background: #11111b;
      border-top: 1px solid #2a2a3a;
    }
  `]
})
export class CompetitionSection {
  group = input.required<CompetitionGroup>();
  divergences = input<Divergence[]>([]);

  expanded = signal(true);

  alertCount = computed(() => {
    const matchIds = new Set(this.group().matches.map(m => m.matchId));
    return this.divergences()
      .filter(d => this.alerts.isActive(d) && matchIds.has(d.matchId))
      .length;
  });

  divForMatch(matchId: string): Divergence[] {
    return this.divergences().filter(d => d.matchId === matchId);
  }

  constructor(private alerts: AlertService) {}
}

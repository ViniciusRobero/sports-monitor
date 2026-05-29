import { Component, OnInit, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AlertService } from './alert.service';
import { MatchCard } from './match-card';
import { DivergenceCard } from './divergence-card';

@Component({
  selector: 'app-root',
  imports: [MatchCard, DivergenceCard],
  template: `
    <header>
      <span class="title">Sports Monitor</span>
      <span class="conn" [class.ok]="alerts.connected()">
        {{ alerts.connected() ? 'Ao vivo' : 'Desconectado' }}
      </span>
      <span class="count">{{ alerts.liveMatches().length }} partida{{ alerts.liveMatches().length !== 1 ? 's' : '' }}</span>
      @if (activeDivergences() > 0) {
        <span class="alert-count">⚠ {{ activeDivergences() }} divergência{{ activeDivergences() !== 1 ? 's' : '' }}</span>
      }
      <button class="btn-refresh" (click)="refresh()" [disabled]="refreshing()">
        {{ refreshing() ? '...' : '↻ Atualizar' }}
      </button>
    </header>

    <main>
      @if (alerts.liveMatches().length === 0) {
        <div class="empty">Nenhuma partida ao vivo monitorada no momento.</div>
      }

      @for (group of alerts.liveMatches(); track group[0].matchId) {
        <app-match-card
          [snapshots]="group"
          [divergences]="alerts.divergencesForMatch(group[0].matchId)" />
      }

      @if (orphanDivergences().length > 0) {
        <div class="section-title">Alertas recentes</div>
        @for (d of orphanDivergences(); track d.id) {
          <app-divergence-card [d]="d" [googleSnapshot]="alerts.googleForMatch(d.matchId)" />
        }
      }
    </main>
  `,
  styles: [`
    header { display: flex; align-items: center; gap: 12px; padding: 12px 20px;
      background: #181825; border-bottom: 1px solid #313244; position: sticky; top: 0; z-index: 10; flex-wrap: wrap; }
    .title { font-size: 16px; font-weight: bold; color: #cdd6f4; }
    .conn { font-size: 12px; padding: 3px 8px; border-radius: 3px; background: #f38ba8; color: #1e1e2e; }
    .conn.ok { background: #a6e3a1; }
    .count { font-size: 12px; color: #6c7086; }
    .alert-count { font-size: 12px; color: #f38ba8; font-weight: 600; }
    .btn-refresh { margin-left: auto; padding: 5px 12px; background: #313244; border: 1px solid #45475a; color: #cdd6f4; border-radius: 4px; cursor: pointer; font-size: 12px; }
    .btn-refresh:hover:not(:disabled) { background: #45475a; }
    .btn-refresh:disabled { opacity: 0.5; cursor: default; }
    main { padding: 16px 20px; max-width: 960px; margin: 0 auto; }
    .empty { color: #6c7086; text-align: center; padding: 60px 20px; font-size: 14px; }
    .section-title { font-size: 11px; color: #6c7086; text-transform: uppercase; letter-spacing: 0.5px; margin: 20px 0 8px; }
  `]
})
export class App implements OnInit {
  refreshing = signal(false);

  activeDivergences = computed(() =>
    this.alerts.divergences().filter(
      d => !['Confirmed', 'FalsePositive', 'Ignored'].includes(d.verificationStatus)
    ).length
  );

  orphanDivergences = computed(() => {
    const liveIds = this.alerts.liveMatchIds();
    return this.alerts.divergences().filter(d => !liveIds.has(d.matchId));
  });

  constructor(public alerts: AlertService, private http: HttpClient) {}

  ngOnInit() { this.alerts.init(); }

  refresh(): void {
    this.refreshing.set(true);
    this.http.post('/api/refresh', {}).subscribe({
      next: () => {
        this.alerts.fetchMatches();
        this.refreshing.set(false);
      },
      error: () => this.refreshing.set(false)
    });
  }
}

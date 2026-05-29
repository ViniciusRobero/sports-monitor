import { Component, OnInit, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AlertService } from './alert.service';
import { DivergenceCard } from './divergence-card';

@Component({
  selector: 'app-root',
  imports: [DivergenceCard],
  template: `
    <header>
      <span class="title">Sports Monitor</span>
      <span class="conn" [class.ok]="alerts.connected()">
        {{ alerts.connected() ? 'Live' : 'Disconnected' }}
      </span>
      <span class="count">{{ alerts.divergences().length }} divergences</span>
      <button class="btn-refresh" (click)="refresh()" [disabled]="refreshing()">
        {{ refreshing() ? '...' : '↻ Refresh' }}
      </button>
    </header>

    <main>
      @if (alerts.divergences().length === 0) {
        <div class="empty">No divergences detected. Monitoring live matches...</div>
      }
      @for (d of alerts.divergences(); track d.id) {
        <app-divergence-card [d]="d" [googleSnapshot]="alerts.googleForMatch(d.matchId)" />
      }
    </main>
  `,
  styles: [`
    header { display: flex; align-items: center; gap: 16px; padding: 12px 20px;
      background: #181825; border-bottom: 1px solid #313244; position: sticky; top: 0; z-index: 10; }
    .title { font-size: 16px; font-weight: bold; color: #cdd6f4; }
    .conn { font-size: 12px; padding: 3px 8px; border-radius: 3px; background: #f38ba8; color: #1e1e2e; }
    .conn.ok { background: #a6e3a1; }
    .count { font-size: 12px; color: #6c7086; margin-left: auto; }
    main { padding: 16px 20px; max-width: 900px; margin: 0 auto; }
    .empty { color: #6c7086; text-align: center; padding: 60px 20px; font-size: 14px; }
    .btn-refresh { padding: 5px 12px; background: #313244; border: 1px solid #45475a; color: #cdd6f4; border-radius: 4px; cursor: pointer; font-size: 12px; }
    .btn-refresh:hover:not(:disabled) { background: #45475a; }
    .btn-refresh:disabled { opacity: 0.5; cursor: default; }
  `]
})
export class App implements OnInit {
  refreshing = signal(false);

  constructor(public alerts: AlertService, private http: HttpClient) {}

  ngOnInit() { this.alerts.init(); }

  refresh(): void {
    this.refreshing.set(true);
    this.http.post('/api/refresh', {}).subscribe({
      next: () => this.refreshing.set(false),
      error: () => this.refreshing.set(false)
    });
  }
}

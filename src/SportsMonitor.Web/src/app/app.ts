import { Component, OnInit } from '@angular/core';
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
  `]
})
export class App implements OnInit {
  constructor(public alerts: AlertService) {}
  ngOnInit() { this.alerts.init(); }
}

import { Component, OnInit, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AlertService } from './alert.service';
import { SourcePanel } from './source-panel';

const SOURCE_ORDER = ['bet365', 'google', 'sofascore', 'api_football', '365scores'];

@Component({
  selector: 'app-root',
  imports: [SourcePanel],
  template: `
    <header>
      <span class="title">Sports Monitor</span>
      <span class="conn" [class.ok]="alerts.connected()">
        {{ alerts.connected() ? 'Ao vivo' : 'Desconectado' }}
      </span>
      <span class="count">{{ totalMatches() }} partida{{ totalMatches() !== 1 ? 's' : '' }}</span>
      @if (activeDivergences() > 0) {
        <span class="alert-count">⚠ {{ activeDivergences() }} alerta{{ activeDivergences() !== 1 ? 's' : '' }}</span>
      }
      <button class="btn-refresh" (click)="refresh()" [disabled]="refreshing()">
        {{ refreshing() ? '...' : '↻ Atualizar' }}
      </button>
    </header>

    <div class="grid">
      @for (source of sources(); track source.key) {
        <app-source-panel
          [source]="source.key"
          [snapshots]="source.snapshots"
          [divergences]="alerts.divergences()"
          [googleSnapshots]="alerts.googleSnapshots()" />
      }
    </div>
  `,
  styles: [`
    :host { display: flex; flex-direction: column; height: 100vh; overflow: hidden; }
    header { display: flex; align-items: center; gap: 12px; padding: 10px 16px;
      background: #11111b; border-bottom: 1px solid #313244; flex-shrink: 0; flex-wrap: wrap; }
    .title { font-size: 15px; font-weight: bold; color: #cdd6f4; }
    .conn { font-size: 11px; padding: 2px 8px; border-radius: 3px; background: #f38ba8; color: #1e1e2e; }
    .conn.ok { background: #a6e3a1; }
    .count { font-size: 12px; color: #6c7086; }
    .alert-count { font-size: 12px; color: #f38ba8; font-weight: 600; }
    .btn-refresh { margin-left: auto; padding: 4px 12px; background: #313244; border: 1px solid #45475a; color: #cdd6f4; border-radius: 4px; cursor: pointer; font-size: 12px; }
    .btn-refresh:hover:not(:disabled) { background: #45475a; }
    .btn-refresh:disabled { opacity: 0.5; cursor: default; }
    .grid { display: grid; grid-template-columns: 2fr 2fr 1.5fr 1.5fr; gap: 10px; padding: 10px; flex: 1; overflow: hidden; min-height: 0; }
    @media (max-width: 900px) { .grid { grid-template-columns: 1fr 1fr; overflow-y: auto; } }
    @media (max-width: 520px) { .grid { grid-template-columns: 1fr; overflow-y: auto; } }
  `]
})
export class App implements OnInit {
  refreshing = signal(false);

  sources = computed(() => {
    const bySource = this.alerts.snapshotsBySource();
    // Show sources in fixed order; include any extra sources at the end
    const allKeys = [...new Set([...SOURCE_ORDER, ...bySource.keys()])];
    return allKeys
      .filter(k => k !== 'google') // Google is always shown but uses its own data
      .concat(['google'])
      .filter((k, i, arr) => arr.indexOf(k) === i) // dedupe
      .map(key => ({ key, snapshots: bySource.get(key) ?? [] }))
      .filter(s => SOURCE_ORDER.includes(s.key) || s.snapshots.length > 0);
  });

  totalMatches = computed(() => this.alerts.liveMatches().length);

  activeDivergences = computed(() =>
    this.alerts.divergences().filter(d => d.verificationStatus !== 'Ignored').length
  );

  constructor(public alerts: AlertService, private http: HttpClient) {}

  ngOnInit() { this.alerts.init(); }

  refresh(): void {
    this.refreshing.set(true);
    this.http.post('/api/refresh', {}).subscribe({
      next: () => { this.alerts.fetchMatches(); this.refreshing.set(false); },
      error: () => this.refreshing.set(false)
    });
  }
}

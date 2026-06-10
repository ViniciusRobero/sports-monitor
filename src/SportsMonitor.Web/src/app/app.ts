import { Component, OnInit, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AlertService } from './alert.service';
import { CompetitionSection } from './competition-section';
import { MatchCard } from './match-card';
import { Divergence, MatchGroup, Severity } from './models';

const SEVERITY_ORDER: Severity[] = ['Critical', 'High', 'Medium', 'Low'];

@Component({
  selector: 'app-root',
  imports: [CompetitionSection, MatchCard],
  template: `
    <header>
      <div class="header-main">
        <span class="logo">⚽</span>
        <span class="title">Sports Monitor</span>
        <span class="conn" [class.ok]="alerts.connected()">
          {{ alerts.connected() ? '● Ao vivo' : '○ Off' }}
        </span>
        <span class="spacer"></span>
        <span class="stats">{{ totalMatches() }} partida{{ totalMatches() !== 1 ? 's' : '' }}</span>
        @if (activeDivergenceCount() > 0) {
          <span class="alert-badge">⚠ {{ activeDivergenceCount() }}</span>
          <button class="btn-ghost danger" (click)="ignoreAll()">Ignorar todos</button>
        }
        <button class="btn-icon" [title]="alerts.soundEnabled() ? 'Desligar som' : 'Ligar som'"
                (click)="alerts.toggleSound()">
          @if (alerts.soundEnabled()) { <span>&#128276;</span> } @else { <span>&#128263;</span> }
        </button>
        <button class="btn-refresh" (click)="refresh()" [disabled]="refreshing()">
          {{ refreshing() ? '...' : '↻' }}
        </button>
      </div>

      <div class="provider-bar">
        @for (s of alerts.providerStatuses(); track s.name) {
          @if (s.enabled || s.consecutiveFailures > 0) {
          <span class="provider-badge" [class]="s.health" [title]="s.lastError || ''">
            <span class="p-name">{{ s.name }}</span>
            <span class="p-time">{{ formatTime(s.lastSuccessUtc) }}</span>
            @if (s.consecutiveFailures > 0) {
              <span class="p-fail">×{{ s.consecutiveFailures }}</span>
            }
          </span>
          }
        }
      </div>
    </header>

    <div class="filter-bar">
      <input
        class="search-input"
        type="search"
        placeholder="Buscar time..."
        [value]="searchText()"
        (input)="searchText.set($any($event.target).value)" />
      <div class="bar-sep"></div>
      @for (group of alerts.competitionGroups(); track group.competition) {
        <button
          class="chip"
          [class.active]="isCompSelected(group.competition)"
          (click)="toggleComp(group.competition)">
          {{ group.competition }}
          <span class="chip-count">{{ group.matches.length }}</span>
        </button>
      }
      @if (selectedCompetitions().size > 0) {
        <button class="chip clear-chip" (click)="clearFilters()">✕ Limpar</button>
      }
      <div class="bar-end">
        <span class="result-count">{{ filteredMatchCount() }} resultado{{ filteredMatchCount() !== 1 ? 's' : '' }}</span>
        <button class="chip" [class.active]="!showHalfTime()" (click)="showHalfTime.update(v => !v)"
                title="Ocultar/exibir partidas no intervalo">HT</button>
      </div>
    </div>

    @if (activeDivergenceCount() > 0) {
      <section class="alerts-panel">
        <div class="alerts-panel-header">
          <button class="panel-toggle" (click)="alertsPanelOpen.update(v => !v)">
            {{ alertsPanelOpen() ? '▼' : '▶' }}
          </button>
          <span class="panel-title">Alertas ativos</span>
          <span class="panel-total">{{ activeDivergenceCount() }}</span>
          <button class="btn-ghost danger" (click)="ignoreAll()">Ignorar todos</button>
        </div>
        @if (alertsPanelOpen()) {
          <div class="alerts-list">
            @for (group of severityGroups(); track group.severity) {
              <div class="severity-group">
                <div class="severity-title">{{ group.severity }}</div>
                @for (d of group.items; track d.id) {
                  <div class="global-alert"
                       [class.Critical]="d.severity === 'Critical'"
                       [class.High]="d.severity === 'High'"
                       [class.Medium]="d.severity === 'Medium'"
                       [class.Low]="d.severity === 'Low'">
                    <div class="global-alert-main">
                      <span class="severity-dot"></span>
                      <span class="global-teams">{{ d.homeTeam }} × {{ d.awayTeam }}</span>
                      <span class="global-type">{{ labelType(d.type) }}</span>
                    </div>
                    <div class="global-values">
                      {{ labelSource(d.sourceA) }}: {{ d.sourceAValue }}
                      <span class="sep">≠</span>
                      {{ labelSource(d.sourceB) }}: {{ d.sourceBValue }}
                    </div>
                    <div class="global-actions">
                      <button class="btn-confirm" (click)="confirm(d.id)">Confirmar</button>
                      <button class="btn-false-pos" (click)="falsePositive(d.id)">Falso positivo</button>
                      <button class="btn-ignore" (click)="alerts.ignoreDivergence(d.id)">Ignorar</button>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        }
      </section>
    }

    <main>
      <div class="content">
        @if (divergingMatches().length > 0) {
          <section class="active-alerts-section">
            <div class="active-alerts-header">
              <span class="aa-dot"></span>
              <span class="aa-title">DIVERGÊNCIAS ATIVAS</span>
              <span class="aa-count">{{ divergingMatches().length }}</span>
            </div>
            <div class="aa-cards">
              @for (match of divergingMatches(); track match.matchId) {
                <app-match-card
                  [match]="match"
                  [divergences]="divForMatch(match.matchId)"
                  [alertMode]="true" />
              }
            </div>
          </section>
        }

        @if (filteredGroups().length === 0) {
          <div class="empty-state">
            <span class="empty-icon">⚽</span>
            <span>Nenhuma partida ao vivo</span>
            @if (selectedCompetitions().size > 0 || searchText()) {
              <button class="btn-ghost" (click)="clearFilters(); searchText.set('')">Limpar filtros</button>
            }
          </div>
        }
        @for (group of filteredGroups(); track group.competition) {
          <app-competition-section
            [group]="group"
            [divergences]="alerts.divergences()" />
        }
      </div>
    </main>
  `,
  styles: [`
    :host { display: flex; flex-direction: column; height: 100vh; overflow: hidden; font-family: 'Inter', system-ui, -apple-system, sans-serif; }

    /* ── Header ── */
    header {
      flex-shrink: 0;
      background: #11111b;
      border-bottom: 1px solid #2a2a3a;
      padding: 10px 16px 0;
    }
    .header-main {
      display: flex;
      align-items: center;
      gap: 10px;
      padding-bottom: 10px;
      flex-wrap: wrap;
    }
    .logo { font-size: 18px; line-height: 1; }
    .title { font-size: 15px; font-weight: 700; color: #e2e2f0; }
    .conn {
      font-size: 11px;
      padding: 2px 9px;
      border-radius: 20px;
      background: rgba(255,71,87,0.15);
      color: #ff4757;
      border: 1px solid rgba(255,71,87,0.3);
    }
    .conn.ok {
      background: rgba(46,213,115,0.12);
      color: #2ed573;
      border-color: rgba(46,213,115,0.3);
    }
    .spacer { flex: 1; }
    .stats { font-size: 12px; color: #7070a0; }
    .alert-badge {
      font-size: 12px;
      font-weight: 600;
      color: #ff4757;
      background: rgba(255,71,87,0.1);
      padding: 2px 9px;
      border-radius: 10px;
    }
    .btn-ghost {
      height: 28px;
      background: #22222e;
      border: 1px solid #2a2a3a;
      color: #a0a0c0;
      border-radius: 6px;
      cursor: pointer;
      font-size: 12px;
      padding: 0 10px;
    }
    .btn-ghost:hover { background: #2a2a3a; color: #e2e2f0; }
    .btn-ghost.danger { color: #ff4757; border-color: rgba(255,71,87,0.35); }
    .btn-icon {
      width: 32px;
      height: 28px;
      background: #22222e;
      border: 1px solid #2a2a3a;
      color: #e2e2f0;
      border-radius: 6px;
      cursor: pointer;
      font-size: 14px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
    .btn-refresh {
      height: 28px;
      padding: 0 14px;
      background: #22222e;
      border: 1px solid #2a2a3a;
      color: #a0a0c0;
      border-radius: 6px;
      cursor: pointer;
      font-size: 13px;
      font-weight: 600;
    }
    .btn-refresh:hover { background: #2a2a3a; color: #e2e2f0; }
    .btn-refresh:disabled { opacity: 0.4; cursor: default; }

    /* Provider status bar */
    .provider-bar {
      display: flex;
      gap: 6px;
      flex-wrap: wrap;
      padding-bottom: 10px;
      border-top: 1px solid #1e1e2a;
      padding-top: 8px;
    }
    .provider-badge {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      font-size: 10px;
      padding: 3px 8px;
      border-radius: 4px;
      background: #1a1a24;
      border: 1px solid #2a2a3a;
      color: #7070a0;
    }
    .provider-badge.Healthy { border-color: rgba(46,213,115,0.4); color: #2ed573; }
    .provider-badge.Degraded { border-color: rgba(255,165,2,0.4); color: #ffa502; }
    .provider-badge.Down { border-color: rgba(255,71,87,0.4); color: #ff4757; }
    .p-name { font-weight: 700; }
    .p-time { color: #45455a; }
    .p-fail { font-weight: 600; }

    /* ── Filter bar ── */
    .filter-bar {
      flex-shrink: 0;
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 8px 16px;
      background: #0f0f13;
      border-bottom: 1px solid #2a2a3a;
      overflow-x: auto;
      scrollbar-width: none;
      -webkit-overflow-scrolling: touch;
    }
    .filter-bar::-webkit-scrollbar { display: none; }

    .search-input {
      flex: 0 0 auto;
      height: 30px;
      padding: 0 12px;
      background: #1a1a24;
      border: 1px solid #2a2a3a;
      color: #e2e2f0;
      border-radius: 20px;
      font-size: 12px;
      outline: none;
      width: 180px;
      font-family: inherit;
      transition: border-color 0.15s;
    }
    .search-input:focus { border-color: #7c6af5; }

    .bar-sep {
      width: 1px;
      height: 18px;
      background: #2a2a3a;
      flex-shrink: 0;
    }

    .chip {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      height: 30px;
      padding: 0 12px;
      border-radius: 20px;
      border: 1px solid #2a2a3a;
      background: #1a1a24;
      color: #7070a0;
      font-size: 12px;
      cursor: pointer;
      white-space: nowrap;
      flex-shrink: 0;
      font-family: inherit;
      transition: all 0.15s;
    }
    .chip:hover { border-color: #7c6af5; color: #e2e2f0; }
    .chip.active {
      background: rgba(124,106,245,0.15);
      border-color: #7c6af5;
      color: #7c6af5;
      font-weight: 600;
    }
    .chip-count {
      font-size: 10px;
      background: #22222e;
      padding: 1px 5px;
      border-radius: 8px;
    }
    .chip.active .chip-count { background: rgba(124,106,245,0.2); }
    .clear-chip { color: #ff4757; border-color: rgba(255,71,87,0.3); }
    .clear-chip:hover { background: rgba(255,71,87,0.08); }

    .bar-end {
      display: flex;
      align-items: center;
      gap: 6px;
      margin-left: auto;
      flex-shrink: 0;
    }
    .result-count { font-size: 11px; color: #45455a; white-space: nowrap; }

    /* ── Alerts panel ── */
    .alerts-panel { flex-shrink: 0; background: #0f0f13; border-bottom: 1px solid #2a2a3a; }
    .alerts-panel-header {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 9px 16px;
    }
    .panel-toggle {
      width: 24px;
      height: 24px;
      background: #22222e;
      border: 1px solid #2a2a3a;
      color: #7070a0;
      border-radius: 4px;
      cursor: pointer;
      font-size: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .panel-title { color: #e2e2f0; font-size: 13px; font-weight: 700; }
    .panel-total {
      font-size: 11px;
      background: rgba(255,71,87,0.12);
      color: #ff4757;
      padding: 1px 7px;
      border-radius: 10px;
      font-weight: 700;
      margin-right: auto;
    }
    .alerts-list { max-height: 240px; overflow-y: auto; padding: 0 16px 12px; display: flex; flex-direction: column; gap: 8px; }
    .severity-group { display: flex; flex-direction: column; gap: 5px; }
    .severity-title { font-size: 10px; color: #45455a; text-transform: uppercase; font-weight: 700; }
    .global-alert {
      display: grid;
      grid-template-columns: minmax(200px, 1.2fr) minmax(160px, 1fr) auto;
      align-items: center;
      gap: 10px;
      padding: 8px 10px;
      background: #1a1a24;
      border: 1px solid #2a2a3a;
      border-left: 3px solid #2ed573;
      border-radius: 8px;
      font-size: 12px;
    }
    .global-alert.Critical { border-left-color: #ff4757; }
    .global-alert.High { border-left-color: #ffa502; }
    .global-alert.Medium { border-left-color: #f9e2af; }
    .global-alert-main { display: flex; align-items: center; gap: 8px; min-width: 0; }
    .severity-dot { width: 7px; height: 7px; border-radius: 50%; background: #2ed573; flex: 0 0 auto; }
    .global-alert.Critical .severity-dot { background: #ff4757; }
    .global-alert.High .severity-dot { background: #ffa502; }
    .global-alert.Medium .severity-dot { background: #f9e2af; }
    .global-teams { color: #e2e2f0; font-weight: 700; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .global-type { color: #7070a0; font-size: 11px; white-space: nowrap; }
    .global-values { color: #7070a0; font-size: 11px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .sep { color: #ff4757; font-weight: 700; padding: 0 3px; }
    .global-actions { display: flex; gap: 5px; justify-content: flex-end; }

    /* ── Main content ── */
    main { flex: 1; overflow-y: auto; min-height: 0; }
    .content {
      max-width: 900px;
      margin: 0 auto;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    /* ── Active alerts top section ── */
    .active-alerts-section {
      border-radius: 12px;
      border: 2px solid rgba(255,71,87,0.6);
      background: rgba(255,71,87,0.04);
      overflow: hidden;
      animation: alertSectionPulse 2.5s ease-in-out infinite;
    }
    @keyframes alertSectionPulse {
      0%, 100% { border-color: rgba(255,71,87,0.6); }
      50% { border-color: rgba(255,71,87,1); }
    }
    .active-alerts-header {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 10px 16px;
      background: rgba(255,71,87,0.08);
      border-bottom: 1px solid rgba(255,71,87,0.25);
    }
    .aa-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #ff4757;
      animation: livePulse 1s ease-in-out infinite;
      flex-shrink: 0;
    }
    @keyframes livePulse {
      0%, 100% { opacity: 1; transform: scale(1); }
      50% { opacity: 0.4; transform: scale(0.6); }
    }
    .aa-title {
      font-size: 11px;
      font-weight: 800;
      letter-spacing: 1.5px;
      color: #ff4757;
      text-transform: uppercase;
      flex: 1;
    }
    .aa-count {
      font-size: 11px;
      font-weight: 700;
      color: #ff4757;
      background: rgba(255,71,87,0.15);
      padding: 2px 8px;
      border-radius: 10px;
    }
    .aa-cards {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 8px;
    }

    /* Empty state */
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 10px;
      padding: 60px 20px;
      color: #45455a;
      font-size: 14px;
    }
    .empty-icon { font-size: 36px; opacity: 0.3; }

    /* Responsive */
    @media (max-width: 768px) {
      .header-main { gap: 8px; }
      .provider-bar { display: none; }
      .search-input { width: 140px; }
    }
    @media (max-width: 520px) {
      header { padding: 8px 12px 0; }
      .header-main { padding-bottom: 8px; }
      .stats, .result-count { display: none; }
      .content { padding: 10px; }
      .alerts-list { max-height: 180px; }
      .global-alert { grid-template-columns: 1fr; }
      .global-actions { justify-content: flex-start; flex-wrap: wrap; }
    }
  `]
})
export class App implements OnInit {
  refreshing = signal(false);
  searchText = signal('');
  selectedCompetitions = signal<Set<string>>(new Set());
  showHalfTime = signal(true);
  alertsPanelOpen = signal(true);

  filteredGroups = computed(() => {
    const selected = this.selectedCompetitions();
    const text = this.searchText().trim().toLowerCase();
    const showHT = this.showHalfTime();

    return this.alerts.competitionGroups()
      .filter(g => !selected.size || selected.has(g.competition))
      .map(g => ({
        ...g,
        matches: g.matches.filter(m => {
          const textMatch = !text ||
            m.homeTeam.toLowerCase().includes(text) ||
            m.awayTeam.toLowerCase().includes(text);
          const htMatch = showHT || m.status !== 'HalfTime';
          return textMatch && htMatch;
        })
      }))
      .filter(g => g.matches.length > 0);
  });

  totalMatches = computed(() =>
    this.alerts.competitionGroups().reduce((sum, g) => sum + g.matches.length, 0)
  );

  filteredMatchCount = computed(() =>
    this.filteredGroups().reduce((sum, g) => sum + g.matches.length, 0)
  );

  activeDivergences = computed(() =>
    this.alerts.divergences()
      .filter(d => this.alerts.isActive(d))
      .sort((a, b) => SEVERITY_ORDER.indexOf(a.severity) - SEVERITY_ORDER.indexOf(b.severity))
  );

  divergingMatches = computed<MatchGroup[]>(() => {
    const activeIds = new Set(
      this.alerts.divergences()
        .filter(d => this.alerts.isActive(d))
        .map(d => d.matchId)
    );
    const result: MatchGroup[] = [];
    for (const group of this.filteredGroups()) {
      for (const match of group.matches) {
        if (activeIds.has(match.matchId)) result.push(match);
      }
    }
    return result;
  });

  activeDivergenceCount = computed(() => this.activeDivergences().length);

  severityGroups = computed(() =>
    SEVERITY_ORDER
      .map(severity => ({
        severity,
        items: this.activeDivergences().filter(d => d.severity === severity)
      }))
      .filter(group => group.items.length > 0)
  );

  constructor(public alerts: AlertService, private http: HttpClient) {}

  ngOnInit() { this.alerts.init(); }

  isCompSelected(comp: string): boolean {
    return this.selectedCompetitions().has(comp);
  }

  toggleComp(comp: string): void {
    this.selectedCompetitions.update(set => {
      const next = new Set(set);
      if (next.has(comp)) next.delete(comp);
      else next.add(comp);
      return next;
    });
  }

  divForMatch(matchId: string): Divergence[] {
    return this.alerts.divergences().filter(d => d.matchId === matchId);
  }

  clearFilters(): void {
    this.selectedCompetitions.set(new Set<string>());
  }

  refresh(): void {
    this.refreshing.set(true);
    this.http.post('/api/refresh', {}).subscribe({
      next: () => { this.alerts.fetchMatches(); this.refreshing.set(false); },
      error: () => this.refreshing.set(false)
    });
  }

  ignoreAll(): void {
    this.alerts.ignoreAll().catch(console.error);
  }

  confirm(id: string): void {
    this.alerts.verify(id, { status: 'Confirmed', replayLink: null, analystNotes: null, manualActionStatus: null })
      .catch(console.error);
  }

  falsePositive(id: string): void {
    this.alerts.verify(id, { status: 'FalsePositive', replayLink: null, analystNotes: null, manualActionStatus: null })
      .catch(console.error);
  }

  labelType(t: string): string {
    return ({
      ScoreMismatch: 'Placar divergente',
      GoalScorerMismatch: 'Marcador divergente',
      MissingGoalEvent: 'Gol ausente',
      YellowCardMismatch: 'Cartão amarelo divergente',
      RedCardMismatch: 'Cartão vermelho divergente',
      MatchStatusMismatch: 'Status divergente',
    } as Record<string, string>)[t] ?? t;
  }

  labelSource(s: string): string {
    return ({
      sofascore: 'SofaScore', bet365: 'Bet365',
      api_football: 'API Football', '365scores': '365Scores', google: 'Google',
    } as Record<string, string>)[s] ?? s;
  }

  formatTime(utcString: string | null): string {
    if (!utcString) return 'Nunca';
    return new Date(utcString).toLocaleTimeString('pt-BR', {
      hour: '2-digit', minute: '2-digit', second: '2-digit'
    });
  }
}

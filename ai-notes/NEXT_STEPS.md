# Next Steps

## Phase 01 Status: COMPLETE

Toda a pesquisa de fontes foi concluída. A arquitetura pode ser iniciada.

---

## Atual: Phase 04 — MVP Implementation Plan

Phase 01, Phase 02 (requisitos em PHASE_02_PLUS_PLANNING_UPDATE.md) e Phase 03 (arquitetura em PHASE_03_TECHNICAL_ARCHITECTURE.md) estão completos.

### Deliverable principal

Criar o arquivo:
```
PHASE_04_MVP_IMPLEMENTATION_PLAN.md
```

Conteúdo esperado (tarefas em ordem):
1. Scaffold da solution (projetos, referências, DI base)
2. Domain: entidades e interfaces
3. Infrastructure: JSONL repository + InMemorySnapshotStore
4. Provider: ApiFootballProvider (primeiro — API documentada)
5. DivergenceEngine + ScoreMismatchRule
6. AlertWorker + SignalRAlertChannel
7. BFF: endpoints + SignalR hub
8. Angular: dashboard mínimo + som de alerta
9. Desktop shell: WPF + WebView2
10. SofaScoreProvider + Api365ScoresProvider
11. Demais regras de divergência

### Referências de arquitetura

- `PHASE_03_TECHNICAL_ARCHITECTURE.md` — design patterns, DI, solution structure
- `PHASE_02_PLUS_PLANNING_UPDATE.md` — requisitos operacionais

---

## Pesquisas ainda pendentes

1. **FIFA.com** — Copa do Mundo 2026 (#64, Very High priority); verificar endpoints de partidas ao vivo antes da implementação

---

## Validações manuais ainda pendentes

1. **BetsAPI pricing** — acessar betsapi.com/mm/pricing_table (requer login) para confirmar custo e coberturas
2. **The Odds API suspension status** — testar free tier (500 créditos gratuitos) e verificar se o payload de odds ao vivo inclui campo de status de suspensão de mercado
3. **Betfair do Brasil** — testar se é possível criar conta Betfair a partir de IP/documentos brasileiros
4. **API-Football payload** — testar free tier (100 req/dia) para confirmar estrutura de evento ao vivo (gol, artilheiro, minuto, tipo)

---

## Tensão de fontes: RESOLVIDA

- **SofaScore**: integrar via `api.sofascore.com/api/v1` (GET /events/live + /event/{id}/incidents)
- **365Scores**: integrar via `webws.365scores.com/web/` (GET /game/?gameId={id})
- **Google**: não automatizado — dashboard gera link de busca para verificação manual
- Detalhes: `PHASE_01_COMPARISON_SOURCES_RESEARCH.md`

---

## Fontes MVP confirmadas

| Fonte | Papel | Custo |
|---|---|---|
| API-Football (Pro $19/mo) | Dados esportivos ao vivo — primário | $19/mo |
| BetsAPI | Bet365 live odds + suspensão de mercado | Verificar |
| The Odds API (Business $99/mo) | Odds agregadas multi-bookmaker | $99/mo |
| Betfair Exchange API | Exchange live odds + market status | Grátis |
| OpenLigaDB | Bundesliga/DFB-Pokal grátis (suplementar) | Grátis |
| football-data.org (free) | Brasileirão + PL + UCL (suplementar) | Grátis |
| API Futebol (avaliar) | Futebol brasileiro específico (suplementar) | TBD |

---

## Stack tecnológica confirmada

```
Backend:     ASP.NET Core (.NET 8+)
Workers:     .NET Worker Services
Dashboard:   Angular
Real-time:   SignalR
Database:    SQLite (MVP)
Desktop:     WPF/WinForms + WebView2 (thin shell)
Local URL:   http://localhost:5000
```

---

## Não iniciar ainda

- Automação de apostas
- Login automático em bookmakers
- Bypass de captcha/anti-bot
- Scraping de sites protegidos
- Cloud deployment

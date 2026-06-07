## Latest Production Note - 2026-06-07

SportsMonitor is deployed at `http://34.151.245.70/` on GCP project `sportsmonitor-prod`, VM `sportsmonitor-vm`, zone `southamerica-east1-b`. The VM disk was cleaned after 365Scores history filled the 20GB disk; `Scores365Provider` now does not persist `RawJson`, keeping `/opt/sportsmonitor/data` small. Production validation: HTTP 200, `/api/matches/live` returns live groups, `sportsmonitor` and `nginx` active, disk about 20% used. User wants GCP/account focus on SportsMonitor only; no active Compute/Run/SQL/GKE/Functions resources were found in other accessible projects, only old buckets that were not deleted automatically because broad bucket deletion is irreversible.

---
# Project Context - Sports Data Divergence Monitor

## 1. Project Summary

This is a **local-first, desktop-first sports data divergence monitoring system** built in .NET.

The system monitors live football/soccer matches and compares data across sources such as 365Scores, SofaScore, Google, and official competition websites. When a divergence is detected, it triggers an audible alert ("apito") and displays a divergence card in the dashboard.

The system does **not** place bets automatically. After an alert, the analyst manually verifies the event by checking the relevant source and searching for replay/video evidence. The analyst manually decides whether to act outside the system.

The official competition website is the preferred reference source whenever available.

FIFA World Cup 2026 is a very high-priority competition and must be explicitly included in research and implementation.

AI handoff is mandatory. `PROJECT_CONTEXT.md` and `/ai-notes/` files must be kept updated so any AI model can continue the work without losing context.

Important continuity rule: because this project may move between different AI models/agents, any important information shared by the user during a session must be stored in repository handoff files before ending the turn. Do not rely on chat memory alone. At minimum, update `PROJECT_CONTEXT.md` and the relevant files under `/ai-notes/`.

---

## 2. Current Phase

**Phase 01 â€” Data Source Research: COMPLETE**
**Phase 03 â€” Technical Architecture: COMPLETE** (Phase 02 requirements jÃ¡ capturados em PHASE_02_PLUS_PLANNING_UPDATE.md)

**Phase 04 â€” MVP Implementation: COMPLETE**
**Phase 08 â€” Local Packaging: COMPLETE**
**Phase 09 â€” Real Providers + Linux Deploy Prep: IMPLEMENTED / MANUAL VALIDATION PENDING**

### O que estÃ¡ implementado (2026-06-07)

- Domain, Application, Infrastructure completos
- 5 providers: SofaScore, 365Scores, ApiFootball, BetsAPI, Google Custom Search
- 5 regras de divergÃªncia: ScoreMismatch, GoalScorerMismatch, MissingGoal, CardMismatch, MatchStatusMismatch
- DemoWorker: modo demo rico sem API keys, com Bet365 e Google como fontes principais, 5 partidas mockadas, divergÃªncia rÃ¡pida de resultado em ~5s e Flamengo x Palmeiras avanÃ§ando em fases a cada 10s. Demo agora Ã© opcional; operaÃ§Ã£o real usa `Demo.Enabled: false`
- WPF + WebView2 shell com wait-for-ready e logging em arquivo
- Dashboard Angular com botÃ£o de refresh manual, painÃ©is por fonte, Google na segunda coluna, filtros por texto/status, painel global de alertas, toggle de som, "Ignorar todos" e layout mobile melhorado
- publish.ps1: pacote single-exe para Windows
- `publish-linux.sh`, `deploy.sh`, `sportsmonitor.service` e `nginx-sportsmonitor.conf` para deploy Linux/GCP
- `publish-linux.ps1`, `setup-gcp-vm.ps1` e `deploy-gcp.ps1` para fluxo GCP via CLI no Windows sem depender de WSL/rsync
- `setup-google-search-key.ps1` para habilitar Custom Search API, criar API key restrita e gerar `appsettings.Production.json` gitignored via CLI
- SofaScore/365Scores habilitados; Google habilitado com intervalo 300s e placeholders de credencial
- Delay sequencial de 200ms no SofaScore incidents para reduzir risco de rate limit
- 78 testes passando

### PendÃªncias
- Criar/fornecer Google Custom Search API key e Search Engine ID reais
- Search Engine ID recuperado de histÃ³rico local: `25c69f98aa10d4ba0`. API key antiga encontrada no histÃ³rico falhou com 403; criar nova key via `setup-google-search-key.ps1` no projeto GCP escolhido.
- Provider token/status check em 2026-06-07:
  - SofaScore external API docs check em 2026-06-07: usuario compartilhou `https://api.sofascore.com/api/docs/external#tag/Betting-Odds/operation/get_sofascore_app_external_api_v1_bettingodds_list`. A documentacao externa, `openapi.json`, `swagger.json` e o provavel endpoint `https://api.sofascore.com/api/v1/betting-odds/list` retornaram HTTP 403 via CLI. A operacao citada e de Betting Odds, nao de placar/incidentes ao vivo. Nao assumir formato de token/header ate obter o Swagger `Authorize` ou um cURL gerado pela documentacao.
  - SofaScore nÃ£o usa token no cÃ³digo, mas o endpoint `https://api.sofascore.com/api/v1/sport/football/events/live` retornou HTTP 403 nos testes locais mesmo com headers de navegador. Isso indica bloqueio/anti-bot/IP, nÃ£o falta de token.
  - 365Scores nÃ£o usa token e respondeu HTTP 200 via `curl.exe` no endpoint `/web/games/?...&onlyLive=true`.
  - Google usa API key + Search Engine ID. `SearchEngineId=25c69f98aa10d4ba0` estÃ¡ disponÃ­vel; API key antiga falhou com 403 e deve ser recriada via CLI.
- Latency requirement em 2026-06-07: usuÃ¡rio quer atualizaÃ§Ã£o dos dados no tempo mais rÃ¡pido possÃ­vel. Perfil agressivo atual: 365Scores 10s, SofaScore 10s se o acesso funcionar, Google 300s por quota/custo. Google Ã© fonte de verificaÃ§Ã£o, nÃ£o placar estruturado de baixa latÃªncia. Para sub-10s confiÃ¡vel, avaliar fonte paga/licenciada.
- Criar VM no Compute Engine via CLI (`gcloud compute instances create`), instalar runtime/infra e configurar systemd + Nginx via SSH
- Criar `appsettings.Production.json` na VM ou variÃ¡veis de ambiente com credenciais reais via linha de comando
- Validar em horÃ¡rio com partidas ao vivo se SofaScore e 365Scores agrupam corretamente via `FuzzyMatchResolver`
- Validar real BetsAPI payloads, especialmente campo `LA`, se/ quando BetsAPI entrar no escopo
- API-Football key ($19/mÃªs â€” api-sports.io), se a fonte oficial/comercial for habilitada

---

## 3. Core Requirements

- **Desktop-first**: Windows desktop app (WPF/WinForms shell + WebView2) that is trivially migratable to web
- **Local-first execution**: runs on the user's machine, no mandatory cloud infrastructure
- **Local server model**: ASP.NET Core at `http://localhost:5000`, Angular dashboard served as static files
- **Easy web migration**: core is already a standard ASP.NET Core app â€” deploy to server = web app
- **.NET stack**: ASP.NET Core, .NET Worker Services, SignalR, SQLite, Angular
- **Historical data**: all source readings must be persisted locally (SQLite) â€” one row per poll per source, full JSON payload + parsed fields
- Prefer official APIs and commercial/licensed providers
- Do not automate betting, do not bypass captchas, anti-bot protections, login restrictions, paywalls, fingerprinting, or access controls

---

## 4. Confirmed Operational Workflow (Josias)

1. System monitors a live match
2. Compares data from 365Scores, SofaScore, Google, and official competition website
3. If divergence detected â†’ triggers audible alert ("apito")
4. Dashboard shows exactly where the divergence occurred
5. Analyst manually opens the relevant match/source pages
6. Analyst manually searches for replay/video evidence
7. Analyst confirms whether the event was real and the data is correct
8. Analyst manually decides whether to act in the betting platform
9. Betting action is manual and depends on available bookmaker limit

The system supports this operation â€” it does **not** replace it.

---

## 5. Source Priority

### Primary comparison group

| Source | Role | Access | Status |
|---|---|---|---|
| SofaScore | Primary comparison | API interna `api.sofascore.com/api/v1` | **Researched** â€” viÃ¡vel |
| 365Scores | Primary comparison | API interna `webws.365scores.com/web/` | **Researched** â€” viÃ¡vel |
| Google | Primary verification | Custom Search JSON API + mock de snippets no demo | **Integrado** â€” painel de verificaÃ§Ã£o no dashboard |
| Official competition website | **Preferred reference/truth** | Via APIs comerciais (API-Football) | Via aggregators |

### Reference source rule

When available: **Official competition website = preferred truth/reference source**

If official source is delayed/missing/inconsistent: divergence is marked for manual verification.

### Access strategy (decided 2026-05-26 â€” RESOLVED)

**SofaScore:** `GET https://api.sofascore.com/api/v1/sport/football/events/live` + `/event/{id}/incidents`. User-Agent browser + 25-30s polling. Sem auth.

**SofaScore External API:** official docs URL shared by user points to a Betting Odds operation, but local CLI tests returned HTTP 403 for docs/openapi/swagger and probable betting odds list endpoint. Treat formal external access as pending/partner-gated until the Swagger auth scheme or generated cURL command is available.

**365Scores:** `GET https://webws.365scores.com/web/game/?appTypeId=5&langId=31&timezoneName=America/Sao_Paulo&userCountryId=-1&gameId={id}`. Sem auth.

**Google:** NÃ£o hÃ¡ endpoint pÃºblico de live score estruturado. O MVP usa Google Custom Search JSON API para snippets/links de verificaÃ§Ã£o, e o modo demo injeta resultados mockados por partida.

**Boundary:** dados pÃºblicos apenas, sem login bypass, sem CAPTCHA solving, sem fingerprint spoofing.

Detalhes completos: `PHASE_01_COMPARISON_SOURCES_RESEARCH.md`

---

## 6. Technology Stack (Confirmed)

```text
Backend:     ASP.NET Core (.NET 8+)
Workers:     .NET Worker Services (IHostedService)
Dashboard:   Angular (served by ASP.NET Core as static files)
Real-time:   SignalR
Database:    SQLite (MVP) â†’ PostgreSQL if needed
Desktop:     WPF or WinForms host + WebView2 (thin shell)
Packaging:   Single-file .exe or MSIX installer (Phase 08)
Local URL:   http://localhost:5000
Optional LAN: http://192.168.x.x:5000
```

---

## 7. Important Files

| File | Purpose |
|---|---|
| `PHASE_01_DATA_SOURCE_RESEARCH.md` | Phase 01 research document â€” templates, criteria, competition list (63+1) |
| `PHASE_01_RESEARCH_RESULTS.md` | Phase 01 results: APIs, odds APIs, live score apps, bookmakers |
| `PHASE_01_OFFICIAL_SITES_RESEARCH.md` | Phase 01 results: 63 official competition websites |
| `PHASE_01_COMPARISON_SOURCES_RESEARCH.md` | **SofaScore + 365Scores + Google endpoints**: endpoints internos, schema histÃ³rico JSONL, estratÃ©gia .NET |
| `PHASE_02_PLUS_PLANNING_UPDATE.md` | **Key requirements doc**: operational workflow, sources, World Cup, phase roadmap, desktop-first |
| `PHASE_03_TECHNICAL_ARCHITECTURE.md` | **Arquitetura tÃ©cnica aprovada**: solution structure, design patterns, workers, DI, fluxo completo |
| `PROJECT_CONTEXT.md` | Current project state and AI handoff file |
| `/ai-notes/SESSION_LOG.md` | Chronological log of research/implementation sessions |
| `/ai-notes/NEXT_STEPS.md` | Immediate next tasks |
| `/ai-notes/DECISIONS.md` | Technical and product decisions already made |
| `/ai-notes/SOURCE_RESEARCH_STATUS.md` | Status by source/provider/bookmaker |
| `/ai-notes/RELEASE_WORKFLOW.md` | Required correction -> tests -> code review -> commit -> GCP publication workflow |
| `/ai-notes/CREDENTIALS_GUIDE.md` | How to obtain/confirm provider credentials and what does not require tokens |

---

## 8. Decisions Already Made

| Date | Decision | Reason |
|---|---|---|
| 2026-05-26 | Project must be local-first | Avoid recurring cloud costs |
| 2026-05-26 | Desktop-first (WPF/WinForms + WebView2 shell) | User requirement; Angular core ensures trivial web migration |
| 2026-05-26 | Phase 01 must be research-only | Architecture depends on how each source can actually be accessed |
| 2026-05-26 | Architecture starts only after source viability report | Avoid designing around assumptions |
| 2026-05-26 | Maintain continuity files for AI handoff | Allow switching between ChatGPT, Claude, Codex, or other agents |
| 2026-05-26 | Live score apps (SofaScore, Flashscore, FotMob) excluded as direct API sources | No official APIs; scrapers violate ToS and are fragile |
| 2026-05-26 | Sportradar and OpticOdds excluded from MVP | Enterprise pricing ($10k+/mo) incompatible with local small system |
| 2026-05-26 | Bookmakers without public API accessed via BetsAPI or The Odds API | Bet365, Betano, Sportingbet, Pinnacle have no public APIs |
| 2026-05-26 | Betting action is fully manual | System alerts; human decides and acts |
| 2026-05-26 | Audible alert ("apito") is MVP-mandatory | Confirmed by Josias via WhatsApp |
| 2026-05-26 | Manual verification workflow required in dashboard | Analyst must record replay links, notes, confirmation status |
| 2026-05-26 | Official competition website = preferred reference source | Confirmed by Josias as the truth/reference |
| 2026-05-26 | FIFA World Cup 2026 added as competition #64, Very High priority | Tournament June 11â€“July 19 2026; 48 teams, 104 matches |

---

## 9. Current Research Status

| Source/Group | Status | Notes |
|---|---|---|
| Sports data APIs | **Done** | API-Football (best MVP), Sportmonks (alternative), football-data.org (secondary) |
| Odds APIs | **Done** | The Odds API ($29-99/mo), BetsAPI (Bet365 live+suspension) |
| Live score apps | **Done** | All excluded as direct sources â€” no official APIs |
| Bookmakers | **Done** | Betfair Exchange (free API), others via BetsAPI/The Odds API |
| Official competition websites (63) | **Done** | None have public API; all depend on commercial APIs; OpenLigaDB (Bundesliga) is the only exception |
| 365Scores | **Done** | API interna `webws.365scores.com/web/` integrada como comparaÃ§Ã£o |
| FIFA.com (World Cup 2026) | **Pending** | Competition #64; needs source profile before real-data implementation |

---

## 10. MVP Candidate Sources (Ranked)

| Rank | Source | Role | Cost/mo |
|---|---|---|---|
| 1 | API-Football | Primary sports data (events, live, coverage) | $19-39/mo |
| 2 | BetsAPI | Bet365 live odds + suspension status | Verify pricing |
| 3 | The Odds API | Odds aggregation (multiple bookmakers) | $29-99/mo |
| 4 | Betfair Exchange API | Live exchange odds + market status | Free |
| 5 | Sportmonks | Alternative/backup sports data | â‚¬129/mo |
| 6 | football-data.org | Supplementary (BrasileirÃ£o free) | â‚¬0-29/mo |
| 7 | OpenLigaDB | Bundesliga/2.Bundesliga/DFB-Pokal free | Free |
| 8 | API Futebol | Brazilian football supplement (evaluate) | TBD |

**Estimated MVP cost:** ~$50-150/mo depending on plans chosen.

---

## 11. Competition Scope (64 competitions)

- 63 competitions researched in Phase 01 (see `PHASE_01_DATA_SOURCE_RESEARCH.md`)
- Competition #64: **FIFA World Cup 2026** â€” Very High priority

| # | Competition | Official URL | Priority |
|---:|---|---|---|
| 64 | FIFA World Cup 2026 | https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026 | Very High |

---

## 12. MVP Divergence Types (Priority Order)

1. ScoreMismatch
2. GoalScorerMismatch
3. MissingGoalEvent
4. YellowCardMismatch
5. RedCardMismatch
6. MatchStatusMismatch

Full list in `PHASE_02_PLUS_PLANNING_UPDATE.md` section 8.

---

## 13. Phase Roadmap

| Phase | Name | Status |
|---|---|---|
| 01 | Data Source Research | **COMPLETE** |
| 02 | Functional Requirements and Operational Workflow | **CAPTURED** |
| 03 | Technical Architecture | **COMPLETE** |
| 04 | MVP Implementation Plan | **COMPLETE** |
| 05 | Provider Integration | **MVP COMPLETE** |
| 06 | Divergence Engine | **COMPLETE** |
| 07 | Dashboard and Manual Verification | **MVP COMPLETE** |
| 08 | Local Packaging and Handoff | **COMPLETE** |
| 09 | Real Providers, Frontend UX, Linux/GCP Deploy Prep | **IMPLEMENTED - MANUAL VALIDATION PENDING** |

---

## 14. Last Session Summary

Date: 2026-06-07

Summary:
- Enabled real-provider configuration path: Demo off, SofaScore on, 365Scores on, Google on with 300s polling and credential placeholders.
- Added sequential 200ms delay between SofaScore incident calls.
- Improved Angular dashboard with text filtering, HalfTime toggle, sound toggle, active-alert panel, ignore-all action, source-specific alert counts, Google verification column derived from live matches, and mobile layout fixes.
- Preserved existing Confirmed/FalsePositive card actions and aligned "active alert" logic to exclude Confirmed, FalsePositive, and Ignored divergences.
- Added Linux/GCP deploy artifacts: `publish-linux.sh`, `deploy.sh`, `sportsmonitor.service`, `nginx-sportsmonitor.conf`.
- Updated `.gitignore` for `appsettings.Production.json` and `publish-linux/`.
- Rebuilt Angular static assets into `src/SportsMonitor.Bff/wwwroot`.
- Validated Angular production build and Linux publish.
- Test status: `dotnet test src\SportsMonitor.slnx` => 78 passed.

Files changed:
- `.gitignore`
- `src/SportsMonitor.Bff/appsettings.json`
- `src/SportsMonitor.Infrastructure/Providers/SofaScoreProvider.cs`
- `src/SportsMonitor.Web/src/app/*`
- `src/SportsMonitor.Web/src/styles.css`
- `src/SportsMonitor.Bff/wwwroot/*`
- `publish-linux.sh`
- `deploy.sh`
- `publish-linux.ps1`
- `setup-gcp-vm.ps1`
- `deploy-gcp.ps1`
- `sportsmonitor.service`
- `nginx-sportsmonitor.conf`
- `README.md`
- `PROJECT_CONTEXT.md`
- `ai-notes/*`

Remaining:
- Provide real Google credentials and VM information.
- Configure production secrets outside Git.
- Run real-provider smoke test during live matches.
- Deploy to VM and verify systemd/Nginx startup using CLI-only workflow (`gcloud`, `ssh`, `scp`/`rsync`, repo scripts).

---

Date: 2026-05-29

Summary:
- Expanded `DemoWorker` with Bet365 and Google as primary demo sources.
- Added 5 demo matches, including a quick Botafogo x Corinthians score mismatch after ~5 seconds, plus a phased Flamengo x Palmeiras simulation that updates every 10 seconds.
- Mocked Google verification snippets for every demo match.
- Fixed dashboard layout so Google appears in the second column and the source grid can scroll.
- Rebuilt Angular static assets into `src/SportsMonitor.Bff/wwwroot`.
- Generated `publish\` and verified the published BFF returns 5 match groups and 5 Google snapshots.
- Test status: `dotnet test src\SportsMonitor.slnx` => 68 passed.

Files changed:
- `src/SportsMonitor.Workers/DemoWorker.cs`
- `src/SportsMonitor.Web/src/app/app.ts`
- `src/SportsMonitor.Bff/appsettings.json`
- `src/SportsMonitor.Bff/wwwroot/*`
- `README.md`
- `PROJECT_CONTEXT.md`
- `ai-notes/*`

---

## 15. Open Questions

- Is Betfair account creation feasible from Brazil?
- What is BetsAPI's exact pricing? (requires login to see pricing table)
- Does The Odds API Business plan include market suspension status field?
- Does FIFA.com expose live match data accessible without login/anti-bot?
- What is the minimum acceptable odds update frequency for divergence detection?
- Which competitions are highest priority for MVP?
- Which alert channel first: dashboard sound only, or Telegram/Discord in Phase 02?
- **Source tension**: SofaScore/365Scores use internal endpoints; Google is verification/snippet source, not structured live-score truth.

---

## 16. Technical Unknowns Requiring Manual Validation

| Unknown | Source | Method |
|---|---|---|
| BetsAPI exact pricing and coverage | BetsAPI | Visit betsapi.com/mm/pricing_table |
| The Odds API suspension status field | The Odds API | API trial (free 500 credits) |
| Betfair account from Brazil | Betfair | Manual registration test |
| API-Football live event payload structure | API-Football | Free tier (100 req/day) |
| FIFA.com live data accessibility | FIFA.com | Research session |

---

## 17. Next Steps

1. Smoke test `publish\SportsMonitor.Desktop.exe` on a clean Windows user machine with WebView2 Runtime installed.
2. Validate real BetsAPI payloads, especially event/player fields used by `BetsApiProvider`.
3. Configure real Google Custom Search credentials and tune polling to stay within quota.
4. Research FIFA.com as source for World Cup 2026.
5. Manual validations: BetsAPI pricing, The Odds API suspension field, Betfair Brazil, API-Football payload.

---

## 18. Notes for Any AI Agent

Before doing any work:

1. Read this file first.
2. Read `PHASE_02_PLUS_PLANNING_UPDATE.md` for the full operational requirements, workflow, and phase planning.
3. Read `PHASE_01_RESEARCH_RESULTS.md` for source research results.
4. Check `/ai-notes/NEXT_STEPS.md` for immediate tasks.
5. MVP, local packaging, real-provider configuration, frontend UX updates, and Linux/GCP deploy prep are implemented; next work should focus on real credentials, VM deploy, live-match validation, and source hardening.
6. The app is desktop-first (.NET, WPF/WinForms + WebView2), but architecturally web-migratable.
7. No automated betting â€” ever.
8. GCP configuration and deployment must be done via command line, not via Console web as the primary path.
9. Production changes should follow the documented release flow: correction -> tests -> code review -> commit -> GCP publication -> validation -> handoff update.
10. Store important user-shared information in repository handoff files whenever models/agents may change; do not rely only on conversation history.
11. Update this file before ending the session.


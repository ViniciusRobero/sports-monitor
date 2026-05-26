# Project Context - Sports Data Divergence Monitor

## 1. Project Summary

This is a **local-first, desktop-first sports data divergence monitoring system** built in .NET.

The system monitors live football/soccer matches and compares data across sources such as 365Scores, SofaScore, Google, and official competition websites. When a divergence is detected, it triggers an audible alert ("apito") and displays a divergence card in the dashboard.

The system does **not** place bets automatically. After an alert, the analyst manually verifies the event by checking the relevant source and searching for replay/video evidence. The analyst manually decides whether to act outside the system.

The official competition website is the preferred reference source whenever available.

FIFA World Cup 2026 is a very high-priority competition and must be explicitly included in research and implementation.

AI handoff is mandatory. `PROJECT_CONTEXT.md` and `/ai-notes/` files must be kept updated so any AI model can continue the work without losing context.

---

## 2. Current Phase

**Phase 01 — Data Source Research: COMPLETE**
**Phase 03 — Technical Architecture: COMPLETE** (Phase 02 requirements já capturados em PHASE_02_PLUS_PLANNING_UPDATE.md)

**Next: Phase 04 — MVP Implementation Plan**

Phase 04 deliverable: `PHASE_04_MVP_IMPLEMENTATION_PLAN.md`

---

## 3. Core Requirements

- **Desktop-first**: Windows desktop app (WPF/WinForms shell + WebView2) that is trivially migratable to web
- **Local-first execution**: runs on the user's machine, no mandatory cloud infrastructure
- **Local server model**: ASP.NET Core at `http://localhost:5000`, Angular dashboard served as static files
- **Easy web migration**: core is already a standard ASP.NET Core app — deploy to server = web app
- **.NET stack**: ASP.NET Core, .NET Worker Services, SignalR, SQLite, Angular
- **Historical data**: all source readings must be persisted locally (SQLite) — one row per poll per source, full JSON payload + parsed fields
- Prefer official APIs and commercial/licensed providers
- Do not automate betting, do not bypass captchas, anti-bot protections, login restrictions, paywalls, fingerprinting, or access controls

---

## 4. Confirmed Operational Workflow (Josias)

1. System monitors a live match
2. Compares data from 365Scores, SofaScore, Google, and official competition website
3. If divergence detected → triggers audible alert ("apito")
4. Dashboard shows exactly where the divergence occurred
5. Analyst manually opens the relevant match/source pages
6. Analyst manually searches for replay/video evidence
7. Analyst confirms whether the event was real and the data is correct
8. Analyst manually decides whether to act in the betting platform
9. Betting action is manual and depends on available bookmaker limit

The system supports this operation — it does **not** replace it.

---

## 5. Source Priority

### Primary comparison group

| Source | Role | Access | Status |
|---|---|---|---|
| SofaScore | Primary comparison | API interna `api.sofascore.com/api/v1` | **Researched** — viável |
| 365Scores | Primary comparison | API interna `webws.365scores.com/web/` | **Researched** — viável |
| Google | Primary comparison | Sem endpoint JSON acessível | **Not viable** — link manual no dashboard |
| Official competition website | **Preferred reference/truth** | Via APIs comerciais (API-Football) | Via aggregators |

### Reference source rule

When available: **Official competition website = preferred truth/reference source**

If official source is delayed/missing/inconsistent: divergence is marked for manual verification.

### Access strategy (decided 2026-05-26 — RESOLVED)

**SofaScore:** `GET https://api.sofascore.com/api/v1/sport/football/events/live` + `/event/{id}/incidents`. User-Agent browser + 25-30s polling. Sem auth.

**365Scores:** `GET https://webws.365scores.com/web/game/?appTypeId=5&langId=31&timezoneName=America/Sao_Paulo&userCountryId=-1&gameId={id}`. Sem auth.

**Google:** Não viável como fonte automatizada. Dashboard gera link de busca para verificação manual do analista.

**Boundary:** dados públicos apenas, sem login bypass, sem CAPTCHA solving, sem fingerprint spoofing.

Detalhes completos: `PHASE_01_COMPARISON_SOURCES_RESEARCH.md`

---

## 6. Technology Stack (Confirmed)

```text
Backend:     ASP.NET Core (.NET 8+)
Workers:     .NET Worker Services (IHostedService)
Dashboard:   Angular (served by ASP.NET Core as static files)
Real-time:   SignalR
Database:    SQLite (MVP) → PostgreSQL if needed
Desktop:     WPF or WinForms host + WebView2 (thin shell)
Packaging:   Single-file .exe or MSIX installer (Phase 08)
Local URL:   http://localhost:5000
Optional LAN: http://192.168.x.x:5000
```

---

## 7. Important Files

| File | Purpose |
|---|---|
| `PHASE_01_DATA_SOURCE_RESEARCH.md` | Phase 01 research document — templates, criteria, competition list (63+1) |
| `PHASE_01_RESEARCH_RESULTS.md` | Phase 01 results: APIs, odds APIs, live score apps, bookmakers |
| `PHASE_01_OFFICIAL_SITES_RESEARCH.md` | Phase 01 results: 63 official competition websites |
| `PHASE_01_COMPARISON_SOURCES_RESEARCH.md` | **SofaScore + 365Scores + Google endpoints**: endpoints internos, schema histórico JSONL, estratégia .NET |
| `PHASE_02_PLUS_PLANNING_UPDATE.md` | **Key requirements doc**: operational workflow, sources, World Cup, phase roadmap, desktop-first |
| `PHASE_03_TECHNICAL_ARCHITECTURE.md` | **Arquitetura técnica aprovada**: solution structure, design patterns, workers, DI, fluxo completo |
| `PROJECT_CONTEXT.md` | Current project state and AI handoff file |
| `/ai-notes/SESSION_LOG.md` | Chronological log of research/implementation sessions |
| `/ai-notes/NEXT_STEPS.md` | Immediate next tasks |
| `/ai-notes/DECISIONS.md` | Technical and product decisions already made |
| `/ai-notes/SOURCE_RESEARCH_STATUS.md` | Status by source/provider/bookmaker |

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
| 2026-05-26 | FIFA World Cup 2026 added as competition #64, Very High priority | Tournament June 11–July 19 2026; 48 teams, 104 matches |

---

## 9. Current Research Status

| Source/Group | Status | Notes |
|---|---|---|
| Sports data APIs | **Done** | API-Football (best MVP), Sportmonks (alternative), football-data.org (secondary) |
| Odds APIs | **Done** | The Odds API ($29-99/mo), BetsAPI (Bet365 live+suspension) |
| Live score apps | **Done** | All excluded as direct sources — no official APIs |
| Bookmakers | **Done** | Betfair Exchange (free API), others via BetsAPI/The Odds API |
| Official competition websites (63) | **Done** | None have public API; all depend on commercial APIs; OpenLigaDB (Bundesliga) is the only exception |
| 365Scores | **Pending** | Desired primary source; never researched; API status unknown |
| FIFA.com (World Cup 2026) | **Pending** | Competition #64; needs source profile before implementation |

---

## 10. MVP Candidate Sources (Ranked)

| Rank | Source | Role | Cost/mo |
|---|---|---|---|
| 1 | API-Football | Primary sports data (events, live, coverage) | $19-39/mo |
| 2 | BetsAPI | Bet365 live odds + suspension status | Verify pricing |
| 3 | The Odds API | Odds aggregation (multiple bookmakers) | $29-99/mo |
| 4 | Betfair Exchange API | Live exchange odds + market status | Free |
| 5 | Sportmonks | Alternative/backup sports data | €129/mo |
| 6 | football-data.org | Supplementary (Brasileirão free) | €0-29/mo |
| 7 | OpenLigaDB | Bundesliga/2.Bundesliga/DFB-Pokal free | Free |
| 8 | API Futebol | Brazilian football supplement (evaluate) | TBD |

**Estimated MVP cost:** ~$50-150/mo depending on plans chosen.

---

## 11. Competition Scope (64 competitions)

- 63 competitions researched in Phase 01 (see `PHASE_01_DATA_SOURCE_RESEARCH.md`)
- Competition #64: **FIFA World Cup 2026** — Very High priority

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
| 02 | Functional Requirements and Operational Workflow | **Next** |
| 03 | Technical Architecture | Pending |
| 04 | MVP Implementation Plan | Pending |
| 05 | Provider Integration | Pending |
| 06 | Divergence Engine | Pending |
| 07 | Dashboard and Manual Verification | Pending |
| 08 | Local Packaging and Handoff | Pending |

---

## 14. Last Session Summary

Date: 2026-05-26

Summary:
- Received `PHASE_02_PLUS_PLANNING_UPDATE.md` from user (Josias operational clarifications)
- Confirmed desktop-first requirement (.NET, WPF/WinForms + WebView2 shell)
- Confirmed operational workflow: system alerts → analyst verifies manually → analyst acts manually
- Confirmed primary comparison sources: 365Scores, SofaScore, Google, official competition website
- Added FIFA World Cup 2026 as competition #64 (Very High priority)
- Defined phase roadmap 02–08
- Saved PHASE_02_PLUS_PLANNING_UPDATE.md to project folder
- Updated PROJECT_CONTEXT.md to reflect all new requirements

Files changed:
- `PHASE_02_PLUS_PLANNING_UPDATE.md` (created)
- `PROJECT_CONTEXT.md` (updated — this file)
- `ai-notes/NEXT_STEPS.md` (updated)
- `ai-notes/SESSION_LOG.md` (updated)

---

## 15. Open Questions

- Is Betfair account creation feasible from Brazil?
- What is BetsAPI's exact pricing? (requires login to see pricing table)
- Does The Odds API Business plan include market suspension status field?
- Does 365Scores have an official or unofficial API? (never researched)
- Does FIFA.com expose live match data accessible without login/anti-bot?
- What is the minimum acceptable odds update frequency for divergence detection?
- Which competitions are highest priority for MVP?
- Which alert channel first: dashboard sound only, or Telegram/Discord in Phase 02?
- **Source tension**: SofaScore/365Scores/Google have no known official APIs — how do we get their data for comparison?

---

## 16. Technical Unknowns Requiring Manual Validation

| Unknown | Source | Method |
|---|---|---|
| BetsAPI exact pricing and coverage | BetsAPI | Visit betsapi.com/mm/pricing_table |
| The Odds API suspension status field | The Odds API | API trial (free 500 credits) |
| Betfair account from Brazil | Betfair | Manual registration test |
| API-Football live event payload structure | API-Football | Free tier (100 req/day) |
| 365Scores API status | 365Scores | Research session |
| FIFA.com live data accessibility | FIFA.com | Research session |

---

## 17. Next Steps

1. **Phase 02**: Create `PHASE_02_FUNCTIONAL_REQUIREMENTS_AND_OPERATIONAL_WORKFLOW.md`
2. Research 365Scores (API status, coverage, access method)
3. Research FIFA.com as source for World Cup 2026
4. Resolve source tension: how to access SofaScore/365Scores/Google data without ToS violation
5. Manual validations: BetsAPI pricing, The Odds API suspension field, Betfair Brazil, API-Football payload

---

## 18. Notes for Any AI Agent

Before doing any work:

1. Read this file first.
2. Read `PHASE_02_PLUS_PLANNING_UPDATE.md` for the full operational requirements, workflow, and phase planning.
3. Read `PHASE_01_RESEARCH_RESULTS.md` for source research results.
4. Check `/ai-notes/NEXT_STEPS.md` for immediate tasks.
5. Phase 01 is complete. Start Phase 02: create `PHASE_02_FUNCTIONAL_REQUIREMENTS_AND_OPERATIONAL_WORKFLOW.md`.
6. The app is desktop-first (.NET, WPF/WinForms + WebView2), but architecturally web-migratable.
7. No automated betting — ever.
8. Update this file before ending the session.

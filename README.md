# Sports Data Divergence Monitor

A **local-first, desktop-first** system that monitors live football/soccer matches across multiple data sources, detects divergences in real time, and alerts the analyst with an audible sound so they can manually verify and act.

> **Status:** Planning phase — architecture defined, implementation starting.

---

## What it does

The system watches live matches simultaneously on sources like SofaScore, 365Scores, and official competition websites. When the sources disagree — different goal scorer, different score, missing event — it triggers an audible alert and shows a divergence card in the dashboard.

The analyst then manually opens the relevant pages, searches for replay evidence, confirms whether the event was real, and decides whether to act on a betting platform. **The system never places bets automatically.**

### Example

```
Match: Flamengo vs Palmeiras — 32'

SofaScore:    Goal by Pedro
365Scores:    Goal by Arrascaeta
API-Football: Goal by Pedro

→ CRITICAL ALERT: GoalScorerMismatch
  Official source indicates Arrascaeta.
  SofaScore and API-Football indicate Pedro.
  Manual replay verification required.
```

---

## Architecture overview

```
User Machine
│
├── Desktop Shell (WPF + WebView2)
│   └── Opens http://localhost:5000 in a native window
│
├── ASP.NET Core (localhost:5000)
│   ├── REST API (BFF)
│   ├── SignalR Hub  ←── real-time push to Angular
│   └── Angular Dashboard (served as static files)
│
├── .NET Worker Services
│   ├── SofaScoreWorker        (configurable interval, default 60s)
│   ├── Api365ScoresWorker     (configurable interval, default 45s)
│   ├── ApiFootballWorker      (configurable interval, default 30s)
│   ├── BetfairStreamWorker    (WebSocket — push, no polling)
│   └── AlertWorker            (consumes divergence queue)
│
├── In-Memory Snapshot Store   ← triggers divergence detection on every update
├── Divergence Engine          ← reactive, runs on every snapshot change
│
└── Storage
    ├── JSONL files per source per day  (MVP history)
    └── SQLite (divergences + verification records)
```

### Key design patterns

| Pattern | Where | Purpose |
|---|---|---|
| Adapter | `IMatchDataProvider` | Each data source is a self-contained adapter. Add a new source = add one class. |
| Strategy | `IDivergenceRule` | Each divergence type is an independent rule. Add a rule = add one class. |
| Strategy | `IAlertChannel` | Each alert destination is independent. Add Telegram = add one class. |
| Repository | `IMatchHistoryRepository` | Storage is swappable. JSONL now, SQLite/Postgres later — one line change in DI. |
| Observer | `ISnapshotStore.SnapshotUpdated` | Divergence detection fires immediately when any source updates — no polling loop. |
| Options | `IOptionsMonitor<T>` | Polling intervals are hot-reloadable from `appsettings.json` while the app runs. |

### Migration to web

The core is already a standard ASP.NET Core app. To deploy as a web application:
- Remove the `Desktop/` project
- Deploy `Bff/` + `Web/` to any server
- Zero code changes required

---

## Data sources

### Primary comparison sources (live match data)

| Source | Method | Endpoint | Auth |
|---|---|---|---|
| SofaScore | Internal API | `api.sofascore.com/api/v1` | None |
| 365Scores | Internal API | `webws.365scores.com/web/` | None |
| API-Football | Official API | `v3.football.api-sports.io` | API key |
| Betfair Exchange | Official WebSocket | Betfair Streaming API | Account required |

### Supplementary sources

| Source | Role | Cost |
|---|---|---|
| API-Football Pro | Primary sports data aggregator | $19/mo |
| The Odds API | Multi-bookmaker odds | $99/mo (Business) |
| BetsAPI | Bet365 live odds + suspension status | TBD |
| OpenLigaDB | Bundesliga/DFB-Pokal (free) | Free |
| football-data.org | Brasileirão + PL (free tier) | Free |

### Source access policy

- Official APIs and licensed providers are preferred
- SofaScore and 365Scores are accessed via their internal (unofficial) JSON endpoints using standard HTTP GET requests with a browser User-Agent — no login bypass, no CAPTCHA solving, no fingerprint spoofing
- Google is not integrated as an automated source; the dashboard provides a one-click Google search link for manual verification

---

## Divergence types

| Type | Priority | Description |
|---|---|---|
| ScoreMismatch | 1 — Critical | Sources report different scores |
| GoalScorerMismatch | 2 — Critical | Sources attribute goal to different players |
| MissingGoalEvent | 3 — High | One source has a goal the other doesn't |
| YellowCardMismatch | 4 — High | Card attributed to different player |
| RedCardMismatch | 5 — High | Red card attribution differs |
| MatchStatusMismatch | 6 — Medium | Sources disagree on match status |
| SubstitutionMismatch | — | Future |
| VARDecisionMismatch | — | Future |

---

## Solution structure

```
SportsMonitor.sln
├── SportsMonitor.Domain/          # Entities, interfaces — no external dependencies
├── SportsMonitor.Application/     # Divergence engine, rules, use cases
├── SportsMonitor.Infrastructure/  # HTTP providers, JSONL/SQLite storage, alert channels
├── SportsMonitor.Workers/         # .NET Worker Services (background polling)
├── SportsMonitor.Bff/             # ASP.NET Core API + SignalR hub
├── SportsMonitor.Web/             # Angular dashboard
└── SportsMonitor.Desktop/         # WPF + WebView2 thin shell
```

---

## Configuration

Polling intervals are configurable in `appsettings.json` and support **hot-reload** — change the value while the app is running and it takes effect on the next poll cycle:

```json
{
  "Providers": {
    "SofaScore": {
      "Enabled": true,
      "PollingIntervalSeconds": 60
    },
    "Api365Scores": {
      "Enabled": true,
      "PollingIntervalSeconds": 45
    },
    "ApiFootball": {
      "Enabled": true,
      "PollingIntervalSeconds": 30
    },
    "Betfair": {
      "Enabled": false
    }
  }
}
```

A settings page in the dashboard (Phase 07) will allow editing these values without touching the file.

---

## Historical data

Every polling cycle writes a line to a JSONL file:

```
/data/
  2026-05-26/
    sofascore.jsonl
    365scores.jsonl
    api_football.jsonl
```

Each line is a complete snapshot:
```json
{ "ts": "2026-05-26T21:32:00Z", "matchId": "...", "homeScore": 1, "awayScore": 0, "events": [...], "raw": "..." }
```

This enables full audit of what each source was reporting at any point in time. A SQLite database stores parsed divergences and analyst verification records.

---

## Manual verification workflow

When a divergence alert fires, the analyst:

1. Sees the divergence card in the dashboard
2. Clicks the quick-links to open the relevant match page on each source
3. Clicks the Google search link to find replay/highlight evidence
4. Pastes the replay link into the dashboard
5. Confirms or dismisses the divergence
6. Adds analyst notes if needed
7. Manually acts on the betting platform if applicable

The dashboard tracks: verification status, replay link, analyst notes, manual action status.

---

## Competitions monitored (64)

Includes all major European leagues, South American competitions, and:

| Priority | Competitions |
|---|---|
| Very High | FIFA World Cup 2026, UEFA Champions League, UEFA Europa League, Brasileirão A |
| High | Premier League, LaLiga, Bundesliga, Serie A, Ligue 1, Copa Libertadores, Copa do Brasil |
| Medium | Championship, Scottish Prem, MLS, Serie B italiana, Eredivisie, Primeira Liga, J-League |
| Lower | Regional cups, Nordic leagues, South American second tiers, Asian leagues |

Full list in [PHASE_01_DATA_SOURCE_RESEARCH.md](PHASE_01_DATA_SOURCE_RESEARCH.md).

---

## Project documentation

| File | Description |
|---|---|
| [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) | Master handoff file — current status, decisions, open questions |
| [PHASE_01_RESEARCH_RESULTS.md](PHASE_01_RESEARCH_RESULTS.md) | Source research: APIs, odds APIs, bookmakers |
| [PHASE_01_OFFICIAL_SITES_RESEARCH.md](PHASE_01_OFFICIAL_SITES_RESEARCH.md) | Research on 63 official competition websites |
| [PHASE_01_COMPARISON_SOURCES_RESEARCH.md](PHASE_01_COMPARISON_SOURCES_RESEARCH.md) | SofaScore + 365Scores internal endpoints, JSONL schema, .NET strategy |
| [PHASE_02_PLUS_PLANNING_UPDATE.md](PHASE_02_PLUS_PLANNING_UPDATE.md) | Operational requirements: analyst workflow, alert behavior, World Cup scope |
| [PHASE_03_TECHNICAL_ARCHITECTURE.md](PHASE_03_TECHNICAL_ARCHITECTURE.md) | Full technical architecture: patterns, DI setup, data flow |
| [ai-notes/DECISIONS.md](ai-notes/DECISIONS.md) | All architectural and product decisions |
| [ai-notes/NEXT_STEPS.md](ai-notes/NEXT_STEPS.md) | Immediate next tasks |
| [ai-notes/SESSION_LOG.md](ai-notes/SESSION_LOG.md) | Chronological session log |
| [ai-notes/SOURCE_RESEARCH_STATUS.md](ai-notes/SOURCE_RESEARCH_STATUS.md) | Per-source research status table |

---

## Important constraints

- **No automated betting** — the system alerts; the human decides and acts
- **No login bypass** — sources accessed only via public, unauthenticated endpoints
- **No anti-bot bypass** — no CAPTCHA solving, no fingerprint spoofing, no Playwright stealth
- **No cloud required** — runs entirely on the local machine
- **Local access:** `http://localhost:5000`
- **Optional LAN access:** `http://192.168.x.x:5000`

---

## Planned phases

| Phase | Name | Status |
|---|---|---|
| 01 | Data Source Research | Complete |
| 02 | Functional Requirements | Complete (in PHASE_02_PLUS_PLANNING_UPDATE.md) |
| 03 | Technical Architecture | Complete |
| 04 | MVP Implementation Plan | Next |
| 05 | Provider Integration | Pending |
| 06 | Divergence Engine | Pending |
| 07 | Dashboard and Manual Verification | Pending |
| 08 | Local Packaging and Handoff | Pending |

---

## Tech stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 8+) |
| Background workers | .NET Worker Services (`IHostedService`) |
| Real-time | SignalR |
| Frontend | Angular |
| Database | SQLite (MVP) → PostgreSQL (if needed) |
| Desktop shell | WPF + WebView2 |
| Packaging | Single-file `.exe` / MSIX (Phase 08) |

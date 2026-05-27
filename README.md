# Sports Data Divergence Monitor

A **local-first, desktop-first** system that monitors live football/soccer matches across multiple data sources, detects divergences in real time, and alerts the analyst with an audible sound so they can manually verify and act on Bet365.

> **Status:** MVP implemented — 4 providers, 4 divergence rules, Angular dashboard, WPF desktop shell. 61 tests passing.

---

## What it does

The system watches live matches simultaneously on **Bet365** (via BetsAPI), **SofaScore**, **API-Football** and **365Scores**. When the sources disagree — different goal scorer, different score, missing event, card on the wrong player — it triggers an audible alert ("apito") and shows a divergence card in the dashboard.

The analyst then searches for replay evidence on **Google**, confirms whether the event was real, and decides whether to act manually on **Bet365**. **The system never places bets automatically.**

### Example

```
Match: Flamengo vs Palmeiras — 32'

SofaScore:    Goal by Pedro
Bet365:       Goal by Arrascaeta
API-Football: Goal by Pedro

→ CRITICAL ALERT: GoalScorerMismatch
  SofaScore + API-Football: Pedro
  Bet365: Arrascaeta
  → Search on Google → confirm replay → act manually on Bet365
```

---

## Architecture overview

```
User Machine
│
├── Desktop Shell (WPF + WebView2)
│   └── Auto-starts BFF and opens http://localhost:5000
│
├── ASP.NET Core BFF (localhost:5000)
│   ├── REST API  GET /api/matches/live
│   │            GET /api/divergences
│   │            POST /api/divergences/{id}/verify
│   ├── SignalR Hub /hubs/alerts  ←── real-time push to dashboard
│   └── Angular Dashboard
│
├── .NET Worker Services (background polling)
│   ├── ApiFootballWorker   (default 30s — official API)
│   ├── BetsApiWorker       (default 30s — Bet365 via BetsAPI)
│   ├── SofaScoreWorker     (default 30s — SofaScore internal API)
│   ├── Scores365Worker     (default 20s — 365Scores internal API)
│   └── AlertWorker         (consumes divergence queue → SignalR)
│
├── In-Memory Snapshot Store   ← fires SnapshotUpdated on every update
├── Divergence Engine          ← reactive, evaluates all rules on each snapshot
│
└── Storage (JSONL per source per day)
    data/2026-05-27/
      snapshots/api_football.jsonl
      snapshots/bet365.jsonl
      snapshots/sofascore.jsonl
      snapshots/365scores.jsonl
      divergences.jsonl
```

### Key design patterns

| Pattern | Where | Purpose |
|---|---|---|
| Adapter | `IMatchDataProvider` | Add a new source = add one class |
| Strategy | `IDivergenceRule` | Add a divergence type = add one class |
| Strategy | `IAlertChannel` | Add Telegram/SMS alert = add one class |
| Repository | `IMatchHistoryRepository` | JSONL now, SQLite later — one DI line change |
| Observer | `ISnapshotStore.SnapshotUpdated` | Detection fires instantly on update, no polling loop |
| Options | `IOptionsMonitor<T>` | Polling intervals hot-reloadable from `appsettings.json` |

---

## Data sources

| Source | Events (goals, cards) | Scores | Method | Cost |
|---|---|---|---|---|
| **Bet365** (via BetsAPI) | ✅ goal scorers, cards | ✅ | Licensed API — `api.b365api.com` | Paid |
| **SofaScore** | ✅ full incidents | ✅ | Internal API — `api.sofascore.com/api/v1` | Free |
| **API-Football** | ✅ full events | ✅ | Official API — `v3.football.api-sports.io` | $19/mo (Pro) |
| **365Scores** | ❌ score only | ✅ | Internal API — `webws.365scores.com/web/` | Free |

### Verification sources (manual)

| Source | Role |
|---|---|
| **Google** | Dashboard generates a one-click search link for replay/highlight evidence. Not automated — analyst verifies manually. |
| **Bet365** | Primary platform where the analyst manually acts after confirming a divergence. |

### Source access policy

- Official APIs (API-Football, BetsAPI) are accessed with API keys
- SofaScore and 365Scores are accessed via internal (unofficial) JSON endpoints with a standard browser User-Agent — no login bypass, no CAPTCHA solving, no fingerprint spoofing
- Google is never automated; the dashboard generates a search link that opens in the browser

---

## Divergence rules implemented

| Rule | Severity | What it detects |
|---|---|---|
| `ScoreMismatchRule` | Critical | Sources report different scores |
| `GoalScorerMismatchRule` | Critical | Goal at same minute attributed to different players |
| `MissingGoalRule` | High | One source has a goal the other doesn't |
| `CardMismatchRule` | High | Yellow or red card attributed to different player |

---

## Running locally

**Requirements:** .NET 10 SDK, Node 18+, Angular CLI 21+

```bash
# 1 — start the BFF (API + workers + SignalR)
dotnet run --project src/SportsMonitor.Bff/SportsMonitor.Bff.csproj

# 2 — start the Angular dashboard (dev mode, proxies to localhost:5000)
cd src/SportsMonitor.Web && ng serve

# OR launch the WPF desktop shell (auto-starts BFF)
dotnet run --project src/SportsMonitor.Desktop/SportsMonitor.Desktop.csproj
```

Dashboard available at `http://localhost:4200` (dev) or `http://localhost:5000` (BFF serving static files).

---

## Configuration (`src/SportsMonitor.Bff/appsettings.json`)

All providers are disabled by default. Enable and configure API keys before running:

```json
{
  "Providers": {
    "ApiFootball": {
      "Enabled": true,
      "PollingIntervalSeconds": 30,
      "ApiKey": "YOUR_API_FOOTBALL_KEY"
    },
    "BetsApi": {
      "Enabled": true,
      "PollingIntervalSeconds": 30,
      "Token": "YOUR_BETSAPI_TOKEN"
    },
    "SofaScore": {
      "Enabled": true,
      "PollingIntervalSeconds": 30
    },
    "Scores365": {
      "Enabled": true,
      "PollingIntervalSeconds": 20
    }
  }
}
```

---

## Solution structure

```
src/
├── SportsMonitor.Domain/          # Entities, interfaces — zero external deps
├── SportsMonitor.Application/     # DivergenceEngine + 4 rules
├── SportsMonitor.Infrastructure/  # 4 providers, JSONL repository, resolver, store
├── SportsMonitor.Workers/         # 5 hosted workers (4 polling + 1 alert)
├── SportsMonitor.Bff/             # ASP.NET Core host — REST API + SignalR hub
├── SportsMonitor.Web/             # Angular 21 dashboard
├── SportsMonitor.Desktop/         # WPF + WebView2 thin shell
└── SportsMonitor.Tests/           # 61 xUnit tests
```

---

## Important constraints

- **No automated betting** — the system alerts; the human decides and acts on Bet365
- **No login bypass** — no CAPTCHA solving, no fingerprint spoofing, no Playwright stealth
- **No cloud required** — runs entirely on the local machine
- **Local access:** `http://localhost:5000`

---

## Tech stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 10) |
| Workers | .NET `IHostedService` / `BackgroundService` |
| Real-time | SignalR |
| Frontend | Angular 21 |
| Storage | JSONL files (MVP) |
| Desktop shell | WPF + WebView2 |
| Tests | xUnit + FluentAssertions |

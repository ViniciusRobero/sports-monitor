# Phase 02+ Planning Update - Operational Flow, Manual Verification and World Cup Scope

> This document is an update package to be added after the Phase 01 data source research already performed in Claude Code.
>
> It captures the latest operational clarifications from Josias' WhatsApp audios/screenshots and adds the FIFA World Cup as an explicit monitoring target.

---

## 1. Purpose of this update

This document updates the project planning with three important decisions:

1. The system is a **local-first sports data divergence monitor**.
2. The system must **alert divergences**, but the final investigation and betting action remain **manual**.
3. The FIFA World Cup must be included as a high-priority competition/source scope.

This file should be used as input for planning the next implementation phases.

---

## 2. Confirmed product understanding

The project is not primarily a betting bot.

The project is a **live sports data divergence monitoring system** that compares multiple information sources and alerts the user when relevant discrepancies are detected.

The expected monitored sources mentioned by Josias are:

- 365Scores
- SofaScore
- Google
- Official competition website

The official competition website should be treated as the preferred reference source whenever available.

---

## 3. Confirmed operational flow from Josias

Based on the latest WhatsApp screenshots/audio transcripts, the expected workflow is:

1. The system monitors a live match.
2. The system compares data from 365Scores, SofaScore, Google and the official competition website.
3. If a divergence appears, the system triggers an alert.
4. The alert should be audible, described by Josias as an "apito".
5. The dashboard should show exactly where the divergence happened.
6. The analyst/user manually opens the relevant match/source.
7. The analyst manually checks the official source.
8. The analyst manually searches for replay/video evidence of the play.
9. The analyst confirms whether the event was real and whether the player/event data is correct.
10. The analyst manually decides whether to act in the betting platform.
11. The betting action is manual and depends on the available limit released by the bookmaker.

The system should support this operation, not replace it.

---

## 4. Important boundary: no automated betting

The system must not:

- place bets automatically;
- automate bookmaker account actions;
- bypass anti-bot systems;
- bypass captcha;
- bypass login restrictions;
- bypass fingerprinting protections;
- bypass paywalls or access controls.

The system may:

- monitor data sources;
- compare sports data;
- alert divergences;
- organize links and evidence;
- provide a dashboard for manual analysis;
- store analyst notes;
- store verification status;
- store replay links manually inserted by the analyst.

---

## 5. Updated high-level workflow

```text
Live match
↓
Collect data from multiple sources
↓
Normalize data
↓
Compare data across sources
↓
Detect divergence
↓
Trigger audible alert
↓
Show divergence in dashboard
↓
Analyst opens official/source pages manually
↓
Analyst searches replay/video manually
↓
Analyst confirms or rejects the divergence
↓
Analyst manually acts outside the system if applicable
```

---

## 6. Practical example

```text
Match:
Flamengo vs Palmeiras

Event:
Goal at 32'

365Scores:
Goal by Pedro

SofaScore:
Goal by Arrascaeta

Google:
Goal by Pedro

Official website:
Goal by Arrascaeta

Detected divergence:
GoalScorerMismatch

System action:
Trigger critical alert.

Dashboard message:
Critical divergence detected in goal scorer.
Official source indicates Arrascaeta.
365Scores and Google indicate Pedro.
Manual replay verification required.
```

---

## 7. Manual verification feature requirement

The dashboard should include a manual verification workflow.

Suggested fields:

| Field | Description |
|---|---|
| Match | Match name |
| Competition | Competition/league/cup |
| Match minute | Minute of the event |
| Event type | Goal, card, substitution, penalty, own goal, VAR, etc. |
| Source A value | Data from first source |
| Source B value | Data from second source |
| Google value | Data from Google, when available |
| Official source value | Data from official competition website |
| Divergence type | Type of detected mismatch |
| Severity | Low, Medium, High, Critical |
| Official source link | Direct link to the official match page, when available |
| 365Scores link | Direct link to the match page, when available |
| SofaScore link | Direct link to the match page, when available |
| Google link/search | Google match/search link, when available |
| Replay link | Manual field to paste replay/video evidence |
| Verification status | Pending, In analysis, Confirmed, False positive, Ignored |
| Analyst notes | Free text |
| Manual action status | Not checked, Checked, Acted manually, Not actionable |

---

## 8. Divergence types to support

Initial divergence types:

```text
ScoreMismatch
MatchStatusMismatch
GoalScorerMismatch
GoalMinuteMismatch
MissingGoalEvent
YellowCardMismatch
RedCardMismatch
CardPlayerMismatch
SubstitutionMismatch
PenaltyMismatch
OwnGoalMismatch
VARDecisionMismatch
SourceDelay
OfficialSourceMismatch
```

Priority for MVP:

1. ScoreMismatch
2. GoalScorerMismatch
3. MissingGoalEvent
4. YellowCardMismatch
5. RedCardMismatch
6. MatchStatusMismatch

---

## 9. Alert behavior

The alert system should support:

- audible alert in the dashboard;
- visible highlighted card;
- alert severity;
- timestamp;
- source involved;
- event involved;
- optional desktop notification;
- optional Telegram/Discord notification in later phases.

For MVP, the minimum acceptable alert is:

```text
Dashboard card + audible sound.
```

---

## 10. Local-first requirement remains mandatory

The project must run on the user's machine.

This remains a core constraint for every phase.

The application should be planned as a local server/application:

```text
User Machine
└── .NET Workers
└── ASP.NET Core Local API/BFF
└── Angular Dashboard
└── SQLite local database
└── Local logs
└── Local configuration files
└── Local dashboard accessible via browser
```

Expected local access:

```text
http://localhost:5000
```

Optional LAN access:

```text
http://192.168.x.x:5000
```

The reason is to avoid cloud infrastructure costs during the MVP and early versions.

---

## 11. Source priority update

Based on the latest clarification, the source comparison priority is now:

### Primary comparison group

```text
365Scores
SofaScore
Google
Official competition website
```

### Reference source rule

Whenever available:

```text
Official competition website = preferred truth/reference source
```

If the official source is delayed, missing or inconsistent:

```text
The divergence should be marked as requiring manual verification.
```

---

## 12. World Cup scope addition

The FIFA World Cup must be added as a high-priority monitoring target.

Official source:

```text
https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026
```

Important known context:

- The FIFA World Cup 2026 is the 23rd edition of the tournament.
- It is the first FIFA World Cup with 48 teams.
- It is hosted by three countries: Canada, Mexico and the United States.
- The tournament is scheduled for June 11 to July 19, 2026.
- It includes 104 matches.

The official FIFA website should be researched as a priority source for:

- fixtures;
- live match pages;
- match status;
- score;
- goal scorers;
- cards;
- substitutions;
- match events;
- lineups;
- official match reports;
- possible live commentary or match centre endpoints;
- replay/highlight availability, if any.

---

## 13. World Cup source research checklist

For FIFA World Cup 2026, Phase 01 research or follow-up research should answer:

| Question | Status |
|---|---|
| Does FIFA provide public match pages for each fixture? | TBD |
| Does FIFA expose live match data in JSON? | TBD |
| Does the website use internal API endpoints? | TBD |
| Are those endpoints public, documented, or private? | TBD |
| Are match events available during live matches? | TBD |
| Are goal scorers available during live matches? | TBD |
| Are yellow/red cards available during live matches? | TBD |
| Are substitutions available during live matches? | TBD |
| Are event timestamps/minutes available? | TBD |
| Is there a live commentary feed? | TBD |
| Is there a match centre page? | TBD |
| Does it require login? | TBD |
| Does it use anti-bot protection? | TBD |
| Does it work from a normal local machine? | TBD |
| Is polling allowed or technically realistic? | TBD |
| Does it expose replay/highlight links? | TBD |
| Is it stable enough to be the official reference source? | TBD |

---

## 14. World Cup as a priority source profile

```text
Source:
FIFA World Cup 2026 official website

Category:
Official competition source

URL:
https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026

Priority:
Very High

Role in system:
Preferred official reference source for World Cup matches.

Desired data:
- Fixtures
- Live score
- Match status
- Goals
- Goal scorer
- Cards
- Substitutions
- Penalties
- Own goals
- VAR decisions
- Match events
- Official match reports
- Replay/highlight links, if available

Initial recommendation:
Research immediately before World Cup-related implementation.

MVP impact:
If FIFA live data is accessible and stable, it should be used as the reference source for World Cup divergence detection.

Risk:
TBD after technical research.
```

---

## 15. Competition scope update

The previously provided list of competitions remains valid and should be monitored/researched as official source candidates.

The World Cup must be added explicitly to the competition list:

| # | Competition | Official URL | Priority |
|---:|---|---|---|
| 64 | FIFA World Cup 2026 | https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026 | Very High |

---

## 16. Implementation phase planning implications

Since Phase 01 has already been executed in Claude Code, the next phases should not jump directly into full architecture.

The next planning should use this order:

### Phase 02 - Functional Requirements and Operational Workflow

Define exactly what the system must do from the user's perspective.

Deliverables:

- user workflow;
- analyst workflow;
- dashboard requirements;
- alert requirements;
- manual verification workflow;
- source comparison rules;
- divergence types;
- MVP use cases.

### Phase 03 - Technical Architecture Based on Phase 01

Design architecture using the actual collection methods discovered in Phase 01.

Deliverables:

- .NET solution structure;
- worker strategy;
- provider adapter strategy;
- local database strategy;
- dashboard/BFF strategy;
- local deployment strategy;
- configuration strategy;
- logging strategy.

### Phase 04 - MVP Implementation Plan

Break the work into small buildable tasks.

Deliverables:

- epics;
- user stories;
- technical tasks;
- acceptance criteria;
- MVP cutline;
- mocked vs real providers;
- first playable/local version.

### Phase 05 - Provider Integration

Implement only the providers selected by the research.

Deliverables:

- provider adapters;
- rate limiting;
- provider health checks;
- normalization;
- error handling;
- retry/circuit breaker.

### Phase 06 - Divergence Engine

Implement comparison and alert logic.

Deliverables:

- divergence detection rules;
- severity engine;
- duplicate alert prevention;
- stale source detection;
- manual verification state.

### Phase 07 - Dashboard and Manual Verification

Implement the dashboard for real operation.

Deliverables:

- live matches page;
- match detail page;
- divergence cards;
- manual verification checklist;
- replay link field;
- analyst notes;
- status transitions;
- audible alerts.

### Phase 08 - Local Packaging and Handoff

Make it easy to run locally.

Deliverables:

- local start script;
- appsettings template;
- SQLite database initialization;
- README;
- PROJECT_CONTEXT.md;
- SESSION_LOG.md;
- troubleshooting guide.

---

## 17. Suggested prompt for Claude Code: next phase planning

Use this prompt inside Claude Code:

```text
We have completed Phase 01 research for a local-first sports data divergence monitoring system.

Now I want to plan the next phases based on the updated operational requirements below.

Project summary:
The system must run locally on the user's machine to avoid cloud infrastructure costs. It should monitor live football/soccer matches, compare data from sources such as 365Scores, SofaScore, Google, and official competition websites, detect divergences, trigger an audible alert, and show the divergence in a dashboard.

Important:
The system must not place bets automatically.
The betting action is manual.
After an alert, the analyst manually checks the source, opens the match, searches replay/video evidence, confirms whether the event really happened and whether the player/event data is correct, then manually decides whether to act outside the system.

Confirmed target sources:
- 365Scores
- SofaScore
- Google
- Official competition websites

The official competition website should be treated as the preferred reference source when available.

World Cup addition:
FIFA World Cup 2026 must be included as a very high-priority competition/source:
https://www.fifa.com/en/tournaments/mens/worldcup/canadamexicousa2026

Please create a detailed planning document for:
1. Phase 02 - Functional Requirements and Operational Workflow
2. Phase 03 - Technical Architecture
3. Phase 04 - MVP Implementation Plan
4. Phase 05 - Provider Integration
5. Phase 06 - Divergence Engine
6. Phase 07 - Dashboard and Manual Verification
7. Phase 08 - Local Packaging and AI Handoff

For each phase, include:
- objective
- scope
- deliverables
- technical decisions
- user stories
- acceptance criteria
- risks
- dependencies
- what should not be implemented yet

Preserve the local-first requirement.
Preserve the AI handoff standard using PROJECT_CONTEXT.md and /ai-notes files.
Avoid overengineering, but make the plan realistic enough for implementation.
```

---

## 18. AI handoff requirement reminder

The project must keep AI handoff files updated.

Required files:

```text
PROJECT_CONTEXT.md
/ai-notes/SESSION_LOG.md
/ai-notes/NEXT_STEPS.md
/ai-notes/DECISIONS.md
/ai-notes/SOURCE_RESEARCH_STATUS.md
```

Every relevant session must update:

```text
PROJECT_CONTEXT.md
```

and, when applicable:

```text
/ai-notes/SESSION_LOG.md
/ai-notes/NEXT_STEPS.md
```

This is required because the user may switch between Claude Code, Codex, ChatGPT or another model depending on context window, token usage or implementation needs.

---

## 19. Updated project summary for PROJECT_CONTEXT.md

Use this block to update the project context file:

```text
The project is a local-first sports data divergence monitoring system.

It must run on the user's machine to avoid cloud infrastructure costs.

The system monitors live football/soccer matches and compares data across sources such as 365Scores, SofaScore, Google and official competition websites.

When a divergence is detected, the system should trigger an audible alert and display a clear divergence card in the dashboard.

The system does not place bets automatically. After an alert, the analyst manually verifies the event by checking the relevant source and searching for replay/video evidence. The analyst manually decides whether to act outside the system.

The official competition website should be treated as the preferred reference source whenever available.

FIFA World Cup 2026 is now a very high-priority competition/source and should be explicitly included in the research and implementation planning.

AI handoff is mandatory. PROJECT_CONTEXT.md and /ai-notes files must be updated so another model can continue the work without losing context.
```

---

## 20. Updated MVP direction

The MVP should focus on:

1. Local app running on user's machine.
2. Dashboard with live monitored matches.
3. At least one real or mocked official source.
4. At least one real or mocked comparison source.
5. Divergence detection for score and goal scorer.
6. Audible alert.
7. Manual verification checklist.
8. Analyst notes and replay link.
9. Persistent local storage.
10. AI handoff files.

The MVP should not focus yet on:

- automatic betting;
- bookmaker account automation;
- complex odds strategy;
- cloud deployment;
- mobile app;
- advanced replay discovery;
- machine learning;
- large-scale multi-user support;
- full competition coverage from day one.

---

## 21. Desktop-first requirement (added 2026-05-26)

The application must be a **desktop app first**, but must be architecturally trivial to migrate to web if needed.

### Target platform
Windows desktop application.

### Recommended approach
Since the backend is already a local web server (ASP.NET Core + Angular), the desktop experience is achieved by adding a thin native shell:

```text
User Machine
└── .NET Desktop Shell (WPF or WinForms + WebView2)
    ├── System tray icon
    ├── Start/Stop server control
    ├── "Open Dashboard" button (opens WebView2 window → localhost:5000)
    └── Optional: package everything as a single .exe / installer

└── ASP.NET Core local server (localhost:5000)
    ├── .NET Workers (background polling/streaming)
    ├── REST API / SignalR BFF
    └── Angular Dashboard (served as static files)

└── SQLite (local database file)
└── Local logs
└── Local config (appsettings.json)
```

### Migration to web
Because the core is already a standard ASP.NET Core app:
- Migration to web = deploy the ASP.NET Core app to a server (zero code changes)
- The Angular dashboard is already browser-native
- Workers can be converted to cloud-hosted background services
- SQLite can be replaced with PostgreSQL

### What NOT to do
Do not build the entire UI in WPF or WinForms. The UI lives in Angular (web-first). The desktop shell is only a host/launcher — it should be as thin as possible.

### Technology stack (confirmed)
```text
Backend:    ASP.NET Core (.NET 8+)
Workers:    .NET Worker Services (IHostedService)
Dashboard:  Angular (served by ASP.NET Core as static files)
Real-time:  SignalR
Database:   SQLite (MVP) → PostgreSQL if needed
Desktop:    WPF/WinForms host + WebView2 (thin shell)
Packaging:  Single-file .exe or MSIX installer (Phase 08)
```

---

## 22. Immediate next step

Start Phase 02 planning in Claude Code using this document.

The next phase should produce:

```text
PHASE_02_FUNCTIONAL_REQUIREMENTS_AND_OPERATIONAL_WORKFLOW.md
```

Then proceed to:

```text
PHASE_03_TECHNICAL_ARCHITECTURE.md
PHASE_04_MVP_IMPLEMENTATION_PLAN.md
```

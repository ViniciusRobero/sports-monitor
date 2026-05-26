# Phase 01 - Data Source Research

## 1. Objective

This document defines the research phase for a local-first sports data monitoring system.

The goal of this phase is **not to implement the application yet**. The goal is to investigate, source by source, how live sports data can be accessed technically, reliably, legally, and cost-effectively.

The system should monitor football/soccer competitions, collect data from multiple sources, compare them, detect divergences, and alert the user when relevant inconsistencies appear.

Desired monitored data includes:

- Live matches
- Match status
- Score
- Goal events
- Goal scorer
- Yellow/red cards
- Substitutions
- Event minute
- Official competition data
- Odds
- Live odds
- Bookmaker markets
- Market suspension status
- Data divergence between official sources, score apps, APIs, and bookmakers

The architecture phase will only start after this research phase produces a clear source viability report.

---

## 2. Core Requirement: Local-First Execution

The system must run on the user's local machine as a local application or local server.

The first version should avoid mandatory cloud infrastructure in order to reduce recurring costs.

Initial target execution model:

```text
User Machine
│
├── .NET Worker Services
├── ASP.NET Core local API/BFF
├── Angular dashboard
├── SQLite local database
├── Local logs
├── Local configuration files
└── Telegram/Discord/local sound alerts
```

Expected local access:

```text
http://localhost:5000
```

Optional LAN access:

```text
http://192.168.x.x:5000
```

Cloud may be considered later only if there is a real need for:

- External availability
- Higher uptime
- Remote access
- Heavy workloads
- Centralized deployment
- Multiple users
- Better resilience

For this research phase, every data source must be evaluated with this question in mind:

> Can this source be consumed reliably by an application running 24/7 on a normal user's local machine?

---

## 3. Local Execution Compatibility Checklist

For each source, evaluate:

| Item | Result | Notes |
|---|---|---|
| Works from normal residential IP | TBD | TBD |
| Requires fixed IP | TBD | TBD |
| Requires IP whitelisting | TBD | TBD |
| Requires cloud/server infrastructure | TBD | TBD |
| Requires API key | TBD | TBD |
| Requires OAuth/browser login | TBD | TBD |
| Requires persistent browser session | TBD | TBD |
| Has aggressive rate limit by IP | TBD | TBD |
| Blocks datacenter IPs | TBD | TBD |
| Blocks residential IPs | TBD | TBD |
| Suitable for local polling | TBD | TBD |
| Suitable for 24/7 local execution | TBD | TBD |
| Local credential storage possible | TBD | TBD |
| Works without browser automation | TBD | TBD |
| Works without bypassing access controls | TBD | TBD |

Local compatibility classification:

| Classification | Meaning |
|---|---|
| A | Good for local execution |
| B | Works locally with limitations |
| C | Better suited for server/cloud execution |
| D | Not recommended for local execution |
| E | Not viable |


---

## 4. Project Continuity and AI Handoff Standard

This project may be worked on using different AI models or tools, such as ChatGPT, Claude, Codex, local agents, or IDE-based agents.

Because of token limits, context loss, model switching, or long research sessions, the project must always maintain a local continuity file inside the project repository.

Recommended file:

```text
PROJECT_CONTEXT.md
```

Optional supporting files:

```text
/ai-notes/SESSION_LOG.md
/ai-notes/NEXT_STEPS.md
/ai-notes/DECISIONS.md
/ai-notes/SOURCE_RESEARCH_STATUS.md
```

### 4.1 Purpose

The continuity file exists so that any model, agent, or developer can quickly understand:

- What the project is about
- What phase the project is currently in
- What was already researched
- What decisions were made
- What sources were evaluated
- What remains pending
- What the next task is
- Which files are important
- Which assumptions must not be forgotten

This prevents losing progress when switching models or when a conversation reaches token limits.

### 4.2 Mandatory Update Rule

At the end of every meaningful work session, update `PROJECT_CONTEXT.md` with:

- Current phase
- What was done
- What changed
- Decisions made
- Open questions
- Next recommended step
- Relevant files touched
- Research sources added or updated

No implementation or research task should be considered complete until the continuity file is updated.

### 4.3 Required `PROJECT_CONTEXT.md` Template

```md
# Project Context - Sports Data Divergence Monitor

## 1. Project Summary

This is a local-first sports data monitoring and divergence detection system.

The goal is to monitor football/soccer competitions, collect data from official competition websites, sports data APIs, odds APIs, live score apps, and bookmaker-related providers, then compare the information to detect divergences in live match data.

The system must initially run on the user's local machine to avoid mandatory cloud costs.

## 2. Current Phase

Current phase: Phase 01 - Data Source Research

Architecture and implementation are blocked until Phase 01 produces a clear source viability report.

## 3. Core Requirements

- Local-first execution
- Run on the user's machine as a local server/application
- Avoid mandatory cloud infrastructure in the MVP
- Prefer official APIs and commercial/licensed providers
- Do not automate betting
- Do not bypass captchas, anti-bot protections, login restrictions, paywalls, fingerprinting, or access controls
- Evaluate each source for cost, latency, rate limits, data quality, legal/terms risk, and local execution compatibility

## 4. Current Scope

The current scope is to research technical access methods for:

- Official competition websites
- Sports data APIs
- Odds APIs
- Live score apps/sites
- Bookmaker-related data providers

The initial list includes 63 competitions and official websites documented in `PHASE_01_DATA_SOURCE_RESEARCH.md`.

## 5. Important Files

| File | Purpose |
|---|---|
| `PHASE_01_DATA_SOURCE_RESEARCH.md` | Main Phase 01 research document |
| `PROJECT_CONTEXT.md` | Current project state and handoff file |
| `/ai-notes/SESSION_LOG.md` | Chronological log of research/implementation sessions |
| `/ai-notes/NEXT_STEPS.md` | Immediate next tasks |
| `/ai-notes/DECISIONS.md` | Technical and product decisions already made |
| `/ai-notes/SOURCE_RESEARCH_STATUS.md` | Status by source/provider/bookmaker |

## 6. Decisions Already Made

| Date | Decision | Reason |
|---|---|---|
| TBD | Project must be local-first | Avoid recurring cloud costs |
| TBD | Phase 01 must be research-only | Architecture depends on how each source can actually be accessed |
| TBD | Architecture starts only after source viability report | Avoid designing around assumptions |

## 7. Current Research Status

| Source/Group | Status | Notes |
|---|---|---|
| Official competition websites | Pending | Initial list documented |
| Sports data APIs | Pending | API-Football, Sportmonks, etc. to be researched |
| Odds APIs | Pending | The Odds API, OpticOdds, etc. to be researched |
| Live score apps | Pending | SofaScore, 365Scores, Flashscore, etc. to be researched |
| Bookmakers | Pending | Bet365 and others to be researched cautiously |

## 8. Last Session Summary

Date: TBD

Summary:
- TBD

Files changed:
- TBD

Important findings:
- TBD

## 9. Open Questions

- Which bookmakers/bets must be mapped first?
- Which data is mandatory for the MVP: score, cards, scorer, odds, market suspension, or all of them?
- Which competitions are highest priority?
- What minimum update frequency is acceptable?
- Which alert channel should be used first: dashboard sound, Telegram, Discord, or another option?

## 10. Next Steps

1. Receive or confirm the final list of bookmakers/bets/apps/providers to map.
2. Prioritize the sources for research.
3. Research each source using the Phase 01 template.
4. Fill the source viability matrix.
5. Decide which sources are eligible for MVP.
6. Only then start system architecture.

## 11. Notes for Any AI Agent

Before doing any work:

1. Read this file first.
2. Read `PHASE_01_DATA_SOURCE_RESEARCH.md`.
3. Check `/ai-notes/NEXT_STEPS.md`, if it exists.
4. Do not start architecture or implementation unless Phase 01 has enough research data.
5. Update this file before ending the session.
```

### 4.4 Recommended Session Log Template

File:

```text
/ai-notes/SESSION_LOG.md
```

Template:

```md
# Session Log

## YYYY-MM-DD - Short Session Title

### Goal

TBD

### Work Done

- TBD

### Findings

- TBD

### Decisions

- TBD

### Files Changed

- TBD

### Next Step

- TBD
```

### 4.5 Recommended Source Research Status Template

File:

```text
/ai-notes/SOURCE_RESEARCH_STATUS.md
```

Template:

```md
# Source Research Status

| Source | Category | Status | Best Access Method | Local Compatible | MVP Candidate | Notes |
|---|---|---|---|---|---|---|
| API-Football | Sports data API | Pending | TBD | TBD | TBD | TBD |
| Sportmonks | Sports data API | Pending | TBD | TBD | TBD | TBD |
| The Odds API | Odds API | Pending | TBD | TBD | TBD | TBD |
| Bet365 | Bookmaker | Pending | TBD | TBD | TBD | TBD |
| SofaScore | Live score app | Pending | TBD | TBD | TBD | TBD |
```

### 4.6 Handoff Prompt for a New AI Model

When switching models, start the new session with this prompt:

```text
You are continuing work on a local-first sports data divergence monitoring system.

Before doing anything, read the project continuity files:

1. PROJECT_CONTEXT.md
2. PHASE_01_DATA_SOURCE_RESEARCH.md
3. /ai-notes/NEXT_STEPS.md, if available
4. /ai-notes/SESSION_LOG.md, if available
5. /ai-notes/SOURCE_RESEARCH_STATUS.md, if available

The project is currently in Phase 01: Data Source Research.
Do not start architecture or implementation until the source research produces a clear viability matrix.

Your job is to continue from the latest documented state, preserve decisions already made, and update PROJECT_CONTEXT.md before ending the session.
```

## 5. Scope of Phase 01

This phase focuses on technical discovery and source feasibility.

In scope:

- Researching official APIs
- Researching commercial sports data APIs
- Researching odds APIs
- Researching official competition websites
- Researching live score apps/sites
- Researching bookmaker data availability
- Evaluating live data access methods
- Evaluating cost, latency, rate limits, and risk
- Documenting how each source can or cannot be integrated
- Producing a source viability matrix
- Producing MVP source recommendations

Out of scope for this phase:

- System architecture definition
- Database schema finalization
- Worker implementation
- Dashboard implementation
- Betting automation
- Account automation
- Login automation
- Captcha bypass
- Anti-bot bypass
- Fingerprint bypass
- Scraping protected/private data

---

## 6. Safety and Technical Boundaries

The project should be treated as a **sports data monitoring and divergence detection system**.

The system must not:

- Automate betting
- Place bets automatically
- Bypass captchas
- Bypass anti-bot systems
- Bypass login restrictions
- Bypass paywalls
- Circumvent fingerprinting protections
- Evade access controls
- Abuse undocumented/private endpoints

Acceptable approaches:

- Official APIs
- Commercial/licensed data providers
- Public documentation
- Public widgets, when allowed
- Manual validation using browser DevTools for research only
- Local polling that respects rate limits
- Provider fallback
- Circuit breaker
- Caching
- Health checks

Risky or non-MVP approaches:

- Direct bookmaker scraping
- Browser automation requiring persistent logged-in sessions
- Unofficial APIs with unclear terms
- HTML scraping of fragile pages
- Undocumented endpoints with unstable behavior
- Any access method requiring bypassing protections

---

## 7. Initial Competitions and Official Websites to Monitor

These are the initial competitions/sites provided by the user. They represent the official competition layer that may be used for official reference data, validation, or comparison.

| # | Competition | Official Website | Category | Priority | Research Status | Local Compatibility | MVP Candidate |
|---:|---|---|---|---|---|---|---|
| 1 | 2ª Divisão Alemã | https://www.bundesliga.com/en/2bundesliga | Official competition site | TBD | Pending | TBD | TBD |
| 2 | 2ª Divisão Dinamarquesa | https://divisionsforeningen.dk/ | Official competition site | TBD | Pending | TBD | TBD |
| 3 | 2ª Divisão Escocesa | https://spfl.co.uk/ | Official competition site | TBD | Pending | TBD | TBD |
| 4 | 2ª Divisão Espanhola | https://www.laliga.com/en-GB/laliga-hypermotion | Official competition site | TBD | Pending | TBD | TBD |
| 5 | 2ª Divisão Italiana | https://www.legab.it/ | Official competition site | TBD | Pending | TBD | TBD |
| 6 | 2ª Divisão Norueguesa | https://www.fotball.no/ | Official competition site | TBD | Pending | TBD | TBD |
| 7 | 2ª Divisão Sueca | https://superettan.se/ | Official competition site | TBD | Pending | TBD | TBD |
| 8 | 2ª Divisão Inglesa | https://www.efl.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 9 | 3ª Divisão Inglesa | https://www.efl.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 10 | 4ª Divisão Inglesa | https://www.efl.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 11 | 5ª Divisão Inglesa | https://www.thenationalleague.org.uk/ | Official competition site | TBD | Pending | TBD | TBD |
| 12 | América do Sul - Elim. da Copa | https://www.conmebol.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 13 | Brasileirão - Série A | https://www.cbf.com.br/futebol-brasileiro/competicoes/campeonato-brasileiro-serie-a | Official competition site | TBD | Pending | TBD | TBD |
| 14 | Brasileirão - Série B | https://www.cbf.com.br/futebol-brasileiro/competicoes/campeonato-brasileiro-serie-b | Official competition site | TBD | Pending | TBD | TBD |
| 15 | Campeonato Alemão | https://www.bundesliga.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 16 | Campeonato Argentino | https://www.ligaprofesional.ar/ | Official competition site | TBD | Pending | TBD | TBD |
| 17 | Campeonato Australiano | https://aleagues.com.au/ | Official competition site | TBD | Pending | TBD | TBD |
| 18 | Campeonato Austríaco | https://www.bundesliga.at/ | Official competition site | TBD | Pending | TBD | TBD |
| 19 | Campeonato Belga | https://www.proleague.be/ | Official competition site | TBD | Pending | TBD | TBD |
| 20 | Campeonato Búlgaro | https://efbetleague.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 21 | Campeonato Chileno | https://campeonatochileno.cl/ | Official competition site | TBD | Pending | TBD | TBD |
| 22 | Campeonato Chinês | https://en.csl-china.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 23 | Campeonato Colombiano | https://dimayor.com.co/ | Official competition site | TBD | Pending | TBD | TBD |
| 24 | Campeonato Dinamarquês | https://superliga.dk/ | Official competition site | TBD | Pending | TBD | TBD |
| 25 | Campeonato Equatoriano | https://ligapro.ec/ | Official competition site | TBD | Pending | TBD | TBD |
| 26 | Campeonato Escocês | https://spfl.co.uk/ | Official competition site | TBD | Pending | TBD | TBD |
| 27 | Campeonato Espanhol | https://www.laliga.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 28 | Campeonato Francês | https://www.ligue1.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 29 | Campeonato Grego | https://www.slgr.gr/ | Official competition site | TBD | Pending | TBD | TBD |
| 30 | Campeonato Holandês | https://eredivisie.eu/ | Official competition site | TBD | Pending | TBD | TBD |
| 31 | Campeonato Húngaro | https://nb1.hu/ | Official competition site | TBD | Pending | TBD | TBD |
| 32 | Campeonato Inglês | https://www.premierleague.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 33 | Campeonato Inglês (F) | https://www.wslfootball.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 34 | Campeonato Irlandês | https://www.leagueofireland.ie/ | Official competition site | TBD | Pending | TBD | TBD |
| 35 | Campeonato Italiano | https://www.legaseriea.it/ | Official competition site | TBD | Pending | TBD | TBD |
| 36 | Campeonato Japonês | https://www.jleague.co/ | Official competition site | TBD | Pending | TBD | TBD |
| 37 | Campeonato Mexicano | https://ligamx.net/ | Official competition site | TBD | Pending | TBD | TBD |
| 38 | Campeonato Norueguês | https://www.eliteserien.no/ | Official competition site | TBD | Pending | TBD | TBD |
| 39 | Campeonato Peruano | https://liga1.pe/ | Official competition site | TBD | Pending | TBD | TBD |
| 40 | Campeonato Português | https://www.ligaportugal.pt/ | Official competition site | TBD | Pending | TBD | TBD |
| 41 | Campeonato Saudita | https://spl.com.sa/en | Official competition site | TBD | Pending | TBD | TBD |
| 42 | Campeonato Sueco | https://allsvenskan.se/ | Official competition site | TBD | Pending | TBD | TBD |
| 43 | Campeonato Turco | https://www.tff.org/ | Official competition site | TBD | Pending | TBD | TBD |
| 44 | Copa da Alemanha | https://www.dfb.de/dfb-pokal/ | Official competition site | TBD | Pending | TBD | TBD |
| 45 | Copa da Liga da Argentina | https://www.ligaprofesional.ar/ | Official competition site | TBD | Pending | TBD | TBD |
| 46 | Copa da Liga Inglesa | https://www.efl.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 47 | Copa das Ligas | https://www.leaguescup.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 48 | Copa do Brasil | https://www.cbf.com.br/futebol-brasileiro/competicoes/copa-do-brasil | Official competition site | TBD | Pending | TBD | TBD |
| 49 | Copa do Rei | https://rfef.es/ | Official competition site | TBD | Pending | TBD | TBD |
| 50 | Copa Libertadores | https://libertadores.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 51 | Copa Sul-Americana | https://www.conmebol.com/sudamericana/ | Official competition site | TBD | Pending | TBD | TBD |
| 52 | Copinha | https://copinha.com.br/ | Official competition site | TBD | Pending | TBD | TBD |
| 53 | Eliminatórias da Eurocopa | https://www.uefa.com/european-qualifiers/ | Official competition site | TBD | Pending | TBD | TBD |
| 54 | Liga Canadense | https://canpl.ca/ | Official competition site | TBD | Pending | TBD | TBD |
| 55 | Liga Conferência | https://www.uefa.com/uefaconferenceleague/ | Official competition site | TBD | Pending | TBD | TBD |
| 56 | Liga das Nações (Concacaf) | https://www.concacaf.com/nations-league/ | Official competition site | TBD | Pending | TBD | TBD |
| 57 | Liga dos Campeões | https://www.uefa.com/uefachampionsleague/ | Official competition site | TBD | Pending | TBD | TBD |
| 58 | Liga dos Campeões da Ásia | https://www.the-afc.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 59 | Liga dos Campeões (F) | https://www.uefa.com/womenschampionsleague/ | Official competition site | TBD | Pending | TBD | TBD |
| 60 | Liga dos Campeões Q. | https://www.uefa.com/uefachampionsleague/ | Official competition site | TBD | Pending | TBD | TBD |
| 61 | Liga Europa | https://www.uefa.com/uefaeuropaleague/ | Official competition site | TBD | Pending | TBD | TBD |
| 62 | MLS | https://www.mlssoccer.com/ | Official competition site | TBD | Pending | TBD | TBD |
| 63 | Taça de Portugal | https://www.fpf.pt/ | Official competition site | TBD | Pending | TBD | TBD |

---

## 8. Source Categories to Research

### 7.1 Official Competition Websites

Purpose:

- Validate official match data
- Confirm official goal scorers
- Confirm cards and event attribution
- Compare official data against score apps and betting sources

Examples:

- CBF
- CONMEBOL
- UEFA
- Bundesliga
- LaLiga
- EFL
- SPFL
- MLS
- National league websites

Potential access methods:

| Method | Description | MVP Suitability |
|---|---|---|
| Official documented API | Best option when available | High |
| Public structured endpoints | Possible but must be validated | Medium/Low |
| RSS/feed/widgets | Useful for partial data | Medium |
| HTML scraping | Fragile and risky | Low |
| Manual fallback | Useful for validation only | Medium |

### 7.2 Commercial Sports Data APIs

Purpose:

- Get normalized match data
- Get live fixtures
- Get live events
- Avoid custom integration per official website
- Reduce maintenance complexity

Initial candidates:

- API-Football
- API-Sports
- Sportmonks
- football-data.org
- LiveScore API
- Sportradar
- SportsData.io
- TheSports

### 7.3 Odds APIs

Purpose:

- Get odds
- Get live odds if available
- Compare bookmaker markets
- Track odds movement
- Detect market anomalies

Initial candidates:

- The Odds API
- OpticOdds
- SportsGameOdds
- Odds-API.io
- BetsAPI / b365api
- Other providers with bookmaker coverage

### 7.4 Live Score Apps and Sites

Purpose:

- Compare popular public-facing live data
- Detect divergence between score apps
- Validate latency and event differences

Initial candidates:

- SofaScore
- 365Scores
- Flashscore
- FotMob
- OneFootball
- Google Sports
- ESPN

### 7.5 Bookmakers

Purpose:

- Evaluate whether odds and in-play market data can be accessed through legitimate means
- Identify third-party providers that expose bookmaker data
- Avoid direct automation when risky

Initial candidates:

- Bet365
- Betano
- Sportingbet
- KTO
- Superbet
- Betfair
- Pinnacle
- Stake
- Betway
- Betsson

---

## 9. Evaluation Criteria

Each source must be evaluated using the following criteria.

### 8.1 Access Method

| Question | Answer |
|---|---|
| Does the source have an official public API? | TBD |
| Is the API paid, free, trial-based, or partner-only? | TBD |
| Does it require authentication? | TBD |
| Does it require login? | TBD |
| Does it require browser session? | TBD |
| Does it expose REST endpoints? | TBD |
| Does it expose WebSocket/SSE? | TBD |
| Does it expose widgets only? | TBD |
| Does it require scraping? | TBD |
| Does it require browser automation? | TBD |

### 8.2 Data Availability

| Data Type | Available | Notes |
|---|---|---|
| Live matches | TBD | TBD |
| Score | TBD | TBD |
| Match status | TBD | TBD |
| Match clock | TBD | TBD |
| Goal events | TBD | TBD |
| Goal scorer | TBD | TBD |
| Own goals | TBD | TBD |
| Penalties | TBD | TBD |
| VAR events | TBD | TBD |
| Yellow cards | TBD | TBD |
| Red cards | TBD | TBD |
| Substitutions | TBD | TBD |
| Corners | TBD | TBD |
| Lineups | TBD | TBD |
| Match statistics | TBD | TBD |
| Odds | TBD | TBD |
| Live odds | TBD | TBD |
| Bookmaker markets | TBD | TBD |
| Market suspension status | TBD | TBD |

### 8.3 Coverage

| Coverage Item | Result | Notes |
|---|---|---|
| Football/soccer support | TBD | TBD |
| Brazil competitions | TBD | TBD |
| South America competitions | TBD | TBD |
| European competitions | TBD | TBD |
| Lower divisions | TBD | TBD |
| Women's competitions | TBD | TBD |
| Cup competitions | TBD | TBD |
| World Cup qualifiers | TBD | TBD |
| Historical data | TBD | TBD |
| Pre-match data | TBD | TBD |
| In-play data | TBD | TBD |

### 8.4 Operational Characteristics

| Item | Result | Notes |
|---|---|---|
| Latency/update frequency | TBD | TBD |
| Rate limits | TBD | TBD |
| Pricing | TBD | TBD |
| Free tier/trial | TBD | TBD |
| Required plan for live data | TBD | TBD |
| Required plan for odds | TBD | TBD |
| Required plan for bookmaker coverage | TBD | TBD |
| Reliability signals | TBD | TBD |
| Documentation quality | TBD | TBD |
| SDK availability | TBD | TBD |

### 8.5 Risk Assessment

| Risk Type | Level | Notes |
|---|---|---|
| Technical risk | TBD | TBD |
| Legal/terms risk | TBD | TBD |
| Stability risk | TBD | TBD |
| Cost risk | TBD | TBD |
| Data quality risk | TBD | TBD |
| Local execution risk | TBD | TBD |
| Maintenance risk | TBD | TBD |

Risk levels:

- Low
- Medium
- High
- Critical

---

## 10. Technical Access Method Classification

Each source must be classified into one of the following access methods.

| Code | Method | Description | MVP Recommendation |
|---|---|---|---|
| A | Official documented API | Public or paid documented API from the source itself | Strong candidate |
| B | Commercial third-party API | Licensed provider/API that exposes the data | Strong candidate |
| C | Widget/embed only | Public embed or widget, usually not ideal for structured data | Secondary |
| D | Public but undocumented endpoint | Endpoint observed publicly but not officially documented | Experimental only |
| E | HTML scraping | Data extracted from page HTML | Avoid for MVP |
| F | Browser automation | Requires browser/session automation | Avoid for MVP |
| G | Not viable | No acceptable access method found | Exclude |

Initial MVP should prioritize:

```text
A. Official documented API
B. Commercial third-party API
```

Experimental or later phases may evaluate:

```text
C. Widget/embed only
D. Public but undocumented endpoint
```

Avoid for the first MVP:

```text
E. HTML scraping
F. Browser automation
G. Not viable
```

---

## 11. Source Research Template

Use this template for each source.

```md
### Source Name

#### Basic Information

| Field | Value |
|---|---|
| Source name | TBD |
| Category | TBD |
| Official website | TBD |
| Documentation URL | TBD |
| Source priority | TBD |
| Research status | Pending |

#### Access Summary

| Item | Result | Notes |
|---|---|---|
| Official API exists | TBD | TBD |
| API type | TBD | REST/WebSocket/SSE/Other |
| Public access | TBD | TBD |
| Paid access | TBD | TBD |
| Partner-only access | TBD | TBD |
| Requires API key | TBD | TBD |
| Requires login | TBD | TBD |
| Requires browser session | TBD | TBD |
| Requires fixed IP/IP whitelist | TBD | TBD |
| Local machine compatible | TBD | TBD |

#### Available Data

| Data Type | Available | Notes |
|---|---|---|
| Live matches | TBD | TBD |
| Score | TBD | TBD |
| Match status | TBD | TBD |
| Goal events | TBD | TBD |
| Goal scorer | TBD | TBD |
| Cards | TBD | TBD |
| Substitutions | TBD | TBD |
| Event minute | TBD | TBD |
| Odds | TBD | TBD |
| Live odds | TBD | TBD |
| Bookmaker markets | TBD | TBD |
| Bet365 coverage | TBD | TBD |

#### Coverage

| Item | Result | Notes |
|---|---|---|
| Football/soccer | TBD | TBD |
| Brazil competitions | TBD | TBD |
| South America | TBD | TBD |
| Europe | TBD | TBD |
| Lower divisions | TBD | TBD |
| Women's competitions | TBD | TBD |
| Cups | TBD | TBD |

#### Local Execution Compatibility

| Item | Result | Notes |
|---|---|---|
| Works from normal residential IP | TBD | TBD |
| Requires cloud/server | TBD | TBD |
| Requires fixed IP | TBD | TBD |
| Requires browser login | TBD | TBD |
| Supports local polling | TBD | TBD |
| Suitable for 24/7 local execution | TBD | TBD |
| Stores credentials locally | TBD | TBD |

#### Costs and Limits

| Item | Result | Notes |
|---|---|---|
| Free tier | TBD | TBD |
| Trial | TBD | TBD |
| Monthly cost | TBD | TBD |
| Request limit | TBD | TBD |
| Live data limit | TBD | TBD |
| Odds limit | TBD | TBD |

#### Risks

| Risk | Level | Notes |
|---|---|---|
| Technical risk | TBD | TBD |
| Legal/terms risk | TBD | TBD |
| Stability risk | TBD | TBD |
| Cost risk | TBD | TBD |
| Data quality risk | TBD | TBD |
| Local execution risk | TBD | TBD |

#### Recommended Access Method

TBD

#### MVP Recommendation

Options:

- Use in MVP
- Use later
- Research more
- Avoid
- Not viable

Decision: TBD

#### Conclusion

TBD
```

---

## 12. Official Competition Site Research Template

Use this template for each official competition website.

```md
### Competition Name

| Field | Value |
|---|---|
| Competition | TBD |
| Official website | TBD |
| Country/region | TBD |
| Division/type | TBD |
| Research status | Pending |

#### Data Needed

| Data Type | Needed | Available | Notes |
|---|---|---|---|
| Fixtures | Yes | TBD | TBD |
| Live score | Yes | TBD | TBD |
| Match events | Yes | TBD | TBD |
| Goal scorer | Yes | TBD | TBD |
| Cards | Yes | TBD | TBD |
| Substitutions | Optional | TBD | TBD |
| Official match report | Yes | TBD | TBD |
| Lineups | Optional | TBD | TBD |
| Statistics | Optional | TBD | TBD |

#### Technical Discovery

| Item | Result | Notes |
|---|---|---|
| Official API found | TBD | TBD |
| Public JSON endpoints found | TBD | TBD |
| WebSocket/SSE found | TBD | TBD |
| RSS/feed found | TBD | TBD |
| Widgets found | TBD | TBD |
| HTML-only data | TBD | TBD |
| Requires login | TBD | TBD |
| Anti-bot protection observed | TBD | TBD |

#### Local-First Suitability

| Item | Result | Notes |
|---|---|---|
| Can be queried locally | TBD | TBD |
| Stable for 24/7 monitoring | TBD | TBD |
| Low-cost/free | TBD | TBD |
| Requires cloud | TBD | TBD |

#### Recommendation

TBD
```

---

## 13. Deep Research Prompt - General Phase 01

Use this prompt with ChatGPT, Claude, or another research-capable AI tool.

```text
I want to perform a deep technical research phase for a local-first sports data divergence monitoring system.

The goal is NOT to implement the app yet. The goal is to investigate, source by source, how live sports data can be accessed technically and reliably.

Project context:
We want to monitor live football/soccer matches and compare data across multiple sources to detect divergences. Desired data includes:
- live matches
- score
- match status
- match clock
- goal events
- goal scorer
- own goals
- penalties
- VAR events
- yellow/red cards
- substitutions
- event minute
- official competition data when available
- odds
- live odds
- bookmaker markets
- market suspension status
- Bet365-related odds if available through legitimate providers

Critical requirement:
The system must run on the user's local machine, not initially in the cloud.

For each source, evaluate:
- whether it can be consumed reliably from a normal local machine
- whether it requires fixed IP, server IP, IP whitelisting, VPN, proxy, or cloud deployment
- whether it requires browser login or persistent session
- whether local polling from a residential connection is realistic
- whether API keys can be safely configured locally
- whether the source is suitable for a Windows desktop/local-server MVP
- whether Docker is required or optional
- whether 24/7 local execution is realistic

Important boundaries:
- Do not automate betting.
- Do not bypass captcha, anti-bot systems, fingerprinting, login restrictions, paywalls, or access controls.
- Prefer official APIs, licensed providers, documented APIs, or commercial feeds.
- If a source has no public API, classify the access method and risk, but do not provide bypass instructions.
- If scraping or browser automation would be required, describe the limitation, risk, and why it may not be suitable for MVP.

Research the following categories:
1. Official/commercial sports data APIs
2. Odds APIs
3. Popular live score apps/sites
4. Bookmakers
5. Official competition websites

For each source, produce a structured report with:
- Source name
- Category
- Official website/documentation URL
- Whether an official public API exists
- Whether access requires partnership, paid plan, login, token, or API key
- Available data types
- Live match support
- Match events support
- Goal scorer support
- Cards support
- Odds support
- Live odds support
- Bookmaker coverage
- Bet365 coverage, if applicable
- Supported sports and football competitions
- Latency/update frequency, if documented
- Rate limits
- Pricing/free trial
- Technical integration method:
  A. Official documented API
  B. Commercial third-party API
  C. Widget/embed only
  D. Public but undocumented endpoint
  E. HTML scraping
  F. Browser automation
  G. Not viable
- Local execution compatibility:
  A. Good for local execution
  B. Works locally with limitations
  C. Better suited for server/cloud execution
  D. Not recommended for local execution
  E. Not viable
- Risks:
  - technical risk
  - legal/terms risk
  - stability risk
  - cost risk
  - data quality risk
  - local execution risk
- MVP recommendation:
  - Use in MVP
  - Use later
  - Research more
  - Avoid
- Short justification

At the end, create:
1. A comparison table.
2. A ranked MVP candidate list.
3. A list of sources that should be avoided initially.
4. A list of technical unknowns that require manual validation in browser DevTools or Postman.
5. A suggested next step for hands-on validation.
```

---

## 14. Deep Research Prompt - Official Competition Websites

Use this prompt to investigate the official competition sites listed in this document.

```text
Research the official competition websites listed below as potential official data sources for a local-first sports data monitoring system.

The system must run locally on the user's machine and should avoid cloud dependency in the MVP.

For each competition website, investigate whether it provides access to:
- fixtures
- live matches
- live score
- match status
- match events
- goal scorer
- yellow/red cards
- substitutions
- official match report
- lineups
- statistics

Also investigate the technical access method:
- official documented API
- public JSON endpoint
- WebSocket/SSE
- RSS/feed
- widget/embed
- HTML-only pages
- commercial data provider alternative
- not viable

Important boundaries:
Do not provide instructions to bypass captcha, anti-bot protections, fingerprinting, login restrictions, paywalls, or access controls. If the site does not offer a reliable public method, classify it as risky or not suitable for MVP.

For each competition, return:
- competition name
- website
- whether live structured data is available
- whether official data can be collected reliably
- access method classification
- local execution compatibility
- risks
- MVP recommendation
- short conclusion

Competitions to research:
[PASTE COMPETITION LIST HERE]
```

---

## 15. Deep Research Prompt - Specific Source

Use this when researching one source at a time.

```text
Research [SOURCE_NAME] as a possible data source for a local-first sports data divergence monitoring system.

The system must run on the user's local machine as a local server/application. Avoid assumptions that require cloud infrastructure unless absolutely necessary.

I need to know the most reliable and legitimate way to access:
- live football matches
- score
- match status
- match events
- goal scorer
- cards
- substitutions
- statistics
- odds
- live odds
- bookmaker markets

Please investigate:
- whether [SOURCE_NAME] has an official public API
- whether it provides widgets
- whether documentation exists
- whether third-party APIs or commercial providers expose this data
- whether access is stable enough for a production-like local monitoring system
- whether it requires login, API key, fixed IP, or partner account
- whether it can run reliably from a normal residential connection
- whether it should be used in the MVP or only later

Do not provide bypass instructions for anti-bot, captcha, login restrictions, fingerprinting, paywalls, or private access controls. If the only available approach is scraping or undocumented endpoints, explain the risk clearly and classify it as high-risk or experimental.

Return the result as a structured technical source profile with:
- basic information
- access methods
- available data
- local execution compatibility
- costs and limits
- risks
- MVP recommendation
- conclusion
```

---

## 16. Manual Technical Validation Checklist

For each source that looks promising, perform manual validation.

### 15.1 Browser DevTools Validation

Steps:

1. Open the source website.
2. Open a live or recent match page.
3. Open DevTools.
4. Go to the Network tab.
5. Filter by Fetch/XHR.
6. Filter by WebSocket if applicable.
7. Reload the page.
8. Inspect responses.
9. Check if data is JSON, HTML, or streamed.
10. Check whether the response includes events, players, score, cards, and odds.
11. Check whether requests require cookies, auth headers, tokens, or session IDs.
12. Check whether the data can be requested directly without browser state.
13. Check whether the endpoint remains stable after reload.
14. Check whether rate limiting or blocking appears.

Validation result:

| Item | Result | Notes |
|---|---|---|
| JSON response found | TBD | TBD |
| Match events found | TBD | TBD |
| Goal scorer found | TBD | TBD |
| Cards found | TBD | TBD |
| Odds found | TBD | TBD |
| Requires cookies | TBD | TBD |
| Requires token | TBD | TBD |
| Requires login | TBD | TBD |
| Endpoint stable | TBD | TBD |
| Suitable for MVP | TBD | TBD |

### 15.2 Postman/cURL Validation

For each candidate API or endpoint:

| Request | Result | Notes |
|---|---|---|
| Get live matches | TBD | TBD |
| Get match details | TBD | TBD |
| Get match events | TBD | TBD |
| Get odds | TBD | TBD |
| Get live odds | TBD | TBD |
| Get competition fixtures | TBD | TBD |

Required observations:

- Response status
- Response format
- Required headers
- Required authentication
- Latency
- Stability
- Rate limit behavior
- Local machine compatibility

---

## 17. Scoring Matrix

Each source should receive a score from 0 to 5.

| Criterion | Score | Notes |
|---|---:|---|
| Access simplicity | TBD | 0 = impossible, 5 = simple documented API |
| Data quality | TBD | 0 = poor, 5 = excellent |
| Live data support | TBD | 0 = none, 5 = strong live data |
| Event detail | TBD | 0 = none, 5 = detailed events |
| Odds support | TBD | 0 = none, 5 = strong odds support |
| Competition coverage | TBD | 0 = poor, 5 = broad coverage |
| Local execution compatibility | TBD | 0 = impossible locally, 5 = excellent locally |
| Cost efficiency | TBD | 0 = too expensive, 5 = free/cheap |
| Stability | TBD | 0 = unstable, 5 = stable |
| Legal/terms safety | TBD | 0 = unacceptable, 5 = safe/allowed |
| MVP fit | TBD | 0 = avoid, 5 = use in MVP |

Total score:

```text
TBD / 55
```

---

## 18. Source Comparison Matrix

| Source | Category | Best Access Method | Local Compatibility | Data Quality | Risk | Cost | MVP Recommendation | Notes |
|---|---|---|---|---|---|---|---|---|
| TBD | TBD | TBD | TBD | TBD | TBD | TBD | TBD | TBD |

---

## 19. MVP Candidate Sources

Sources recommended for MVP will be listed here after research.

| Rank | Source | Reason | Required Data | Local Fit | Status |
|---:|---|---|---|---|---|
| 1 | TBD | TBD | TBD | TBD | TBD |

---

## 20. Sources to Avoid Initially

Sources that are too risky, unstable, expensive, or incompatible with local execution.

| Source | Reason to Avoid | Revisit Later? |
|---|---|---|
| TBD | TBD | TBD |

---

## 21. Technical Unknowns

Items that require hands-on validation.

| Unknown | Source | Validation Method | Status |
|---|---|---|---|
| TBD | TBD | DevTools/Postman/API trial | Pending |

---

## 22. Architecture Implications

This section must only be finalized after source research is complete.

Potential architecture decisions depending on research findings:

| Research Finding | Architecture Implication |
|---|---|
| Most sources are REST APIs | Use .NET polling workers with rate limiting |
| Some sources use WebSocket | Add provider-specific persistent connection workers |
| APIs have strict limits | Add scheduler, cache, prioritization, and request budget |
| Sources have different IDs | Build Match Resolver early |
| Team/player names vary heavily | Build Normalization Engine early |
| Live odds are expensive | Separate odds polling from event polling |
| Local machine may be unstable | Add auto-restart, health checks, persistent logs |
| Some sources fail often | Add circuit breaker and provider health status |
| SQLite is enough | Keep local DB simple |
| Data volume grows fast | Consider PostgreSQL later, probably via Docker |

---

## 23. Future Architecture Direction - Not Final Yet

This is only a directional placeholder. Final architecture depends on Phase 01 results.

Potential local-first architecture:

```text
SportsMonitor.sln
│
├── SportsMonitor.Domain
│   ├── Match
│   ├── Team
│   ├── Player
│   ├── MatchEvent
│   ├── OddsSnapshot
│   ├── DataSource
│   ├── Divergence
│   └── Alert
│
├── SportsMonitor.Application
│   ├── MatchMonitoringService
│   ├── DivergenceDetectionService
│   ├── SourceNormalizationService
│   ├── MatchResolverService
│   └── AlertService
│
├── SportsMonitor.Infrastructure
│   ├── Providers
│   ├── Persistence
│   ├── Cache
│   ├── Logging
│   └── Notifications
│
├── SportsMonitor.Workers
│   ├── LiveMatchesWorker
│   ├── MatchEventsPollingWorker
│   ├── OddsPollingWorker
│   ├── DivergenceWorker
│   └── AlertWorker
│
├── SportsMonitor.Bff
│   └── Local ASP.NET Core API + SignalR
│
└── SportsMonitor.Web
    └── Angular local dashboard
```

Initial local stack candidate:

```text
.NET 8/9
ASP.NET Core
Worker Services
Angular
SignalR
SQLite
Serilog
Telegram/Discord notifications
```

This should not be treated as final before research is complete.

---

## 24. Phase 01 Completion Criteria

Phase 01 is complete when this document answers:

1. Which sources can be consumed reliably from a local machine?
2. Which sources offer official or commercial APIs?
3. Which sources provide live events?
4. Which sources provide odds/live odds?
5. Which sources provide bookmaker-specific markets?
6. Which sources cover the required competitions?
7. Which sources should be used in the MVP?
8. Which sources should be avoided initially?
9. What are the technical risks?
10. What are the legal/terms risks?
11. What are the cost risks?
12. What needs manual validation?
13. What does the architecture need to support based on real source behavior?

---

## 25. Next Steps

1. Add the bookmaker/source list provided by the stakeholder.
2. Research official/commercial sports APIs.
3. Research odds APIs.
4. Research official competition websites from the provided list.
5. Research live score apps/sites.
6. Research bookmakers and third-party coverage.
7. Fill one source profile per source.
8. Build the comparison matrix.
9. Select MVP candidates.
10. Only then start architecture planning.

---

## 26. Current Status

| Item | Status |
|---|---|
| Local-first requirement defined | Done |
| Initial competition list added | Done |
| Source research template created | Done |
| Deep research prompts created | Done |
| Bookmaker/source list from stakeholder | Pending |
| Actual source research | Pending |
| MVP source selection | Pending |
| Architecture definition | Blocked until research completion |

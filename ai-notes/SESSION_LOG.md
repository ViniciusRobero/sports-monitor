## 2026-06-07 - GCP Deploy, VM Cleanup, and 365Scores Disk Fix

### Goal

Deploy SportsMonitor to GCP, clean the VM disk, and keep the account focused on SportsMonitor.

### Work Done

- Used project `sportsmonitor-prod`, VM `sportsmonitor-vm`, zone `southamerica-east1-b`.
- Cleaned the VM disk after `/opt/sportsmonitor/data/2026-06-07/snapshots/365scores.jsonl` reached about 16GB and filled the 20GB disk.
- Removed old remote upload artifacts and old snapshot data.
- Deployed the BFF + Angular dashboard behind Nginx/systemd.
- Validated public HTTP access at `http://34.151.245.70/`.
- Found that 365Scores was persisting huge raw payloads repeatedly; changed `Scores365Provider` to stop storing `RawJson` for this source.
- Redeployed the corrected BFF and cleaned remote data again.
- After several polling cycles, `365scores.jsonl` stayed around hundreds of KB instead of GB.
- Listed other accessible GCP projects. No Compute/Cloud Run/SQL/GKE/Functions resources were found outside `sportsmonitor-prod`; only old buckets were listed in other projects. Cross-project bucket deletion was not executed because it is broad and irreversible.

### Validation

- `dotnet build src\SportsMonitor.slnx --no-restore`: passed.
- `dotnet test src\SportsMonitor.slnx --no-build`: 78 passed.
- Production dashboard: HTTP 200.
- Production `/api/matches/live`: returned live match groups.
- Remote services: `sportsmonitor` active, `nginx` active.
- Remote disk after cleanup/fix: about 20% used, about 16GB free.

### Access

- URL: `http://34.151.245.70/`

---
# Session Log

## 2026-06-07 - SofaScore Playwright Migration + GCP Deploy Completo

### Goal

1. Substituir HttpClient por Microsoft.Playwright no SofaScoreProvider para contornar HTTP 403 (TLS fingerprinting).
2. Confirmar que Playwright funciona no GCP.
3. Subir o projeto no GCP Compute Engine com IP pÃºblico acessÃ­vel.

### Work Done

**SofaScore â€” migraÃ§Ã£o para Playwright (headless Chrome):**
- Removido `HttpClient` do `SofaScoreProvider`.
- Adicionado `Microsoft.Playwright 1.60.0` ao `SportsMonitor.Infrastructure.csproj`.
- `SofaScoreProvider` reescrito: lazy-init do browser via `SemaphoreSlim(1,1)`, cria `BrowserContext` por poll, usa `page.EvaluateAsync<string>` para executar `fetch()` dentro do Chrome real â€” TLS fingerprint genuÃ­no (JA3/JA4).
- `SofaScoreMapper` extraÃ­do como `internal static class` em arquivo separado, com `[assembly: InternalsVisibleTo("SportsMonitor.Tests")]`.
- `Program.cs` atualizado: `AddSingleton<SofaScoreProvider>()` (sem HttpClient registration).
- `sportsmonitor.service` atualizado com `Environment=PLAYWRIGHT_BROWSERS_PATH=/opt/sportsmonitor/.playwright`.
- Testes de `SofaScoreProvider` reescritos para testar `SofaScoreMapper` diretamente (sem stub HTTP, sem Playwright).

**GCP Deploy â€” trÃªs bugs corrigidos no deploy-gcp.ps1:**
1. **BOM UTF-8 (PS5.1):** `Set-Content -Encoding UTF8` escreve BOM que quebra `#!/bin/bash`. Fix: `[System.IO.File]::WriteAllText(path, content, [System.Text.UTF8Encoding]::new($false))`.
2. **pscp nesting:** Se destino jÃ¡ existe, pscp aninha source dir dentro dele. Fix: `rm -rf && mkdir -p` antes do SCP, e `cp -a ".../publish-linux/."` no install.sh.
3. **playwright.sh inexistente:** Publish output sÃ³ tem `playwright.ps1`, nÃ£o `.sh`. Fix: usar Node.js embutido em `.playwright/node/linux-x64/node` + `.playwright/package/cli.js` diretamente para `install-deps` e `install chromium`.

**GCP Deploy â€” resultado:**
- Chromium 148.0.7778.96 instalado em `/opt/sportsmonitor/.playwright/chromium-1223`.
- nginx configurado e testado OK.
- systemd service `sportsmonitor` ativo e respondendo.
- 365Scores polling confirmado nos logs (HTTP 200).
- App acessÃ­vel em `http://34.151.245.70/`.

### GCP Details

| Campo | Valor |
|---|---|
| Project ID | `sportsmonitor-prod` |
| VM | `sportsmonitor-vm` |
| Zone | `southamerica-east1-b` |
| IP Externo | `34.151.245.70` |
| URL | `http://34.151.245.70/` |
| Billing Account | `01FE90-8618C8-3CEE8F` |
| Google User | `viniciusroberto17@gmail.com` (authuser=1 em links GCP) |

### Validation

- Deploy script executado com sucesso de ponta a ponta.
- `systemctl is-active sportsmonitor` â†’ `active`.
- Logs: `Scores365Provider` fazendo polling HTTP 200 a cada 10s.
- SofaScore worker inicializa browser Playwright no primeiro poll.

### Files Changed

- `src/SportsMonitor.Infrastructure/SportsMonitor.Infrastructure.csproj`
- `src/SportsMonitor.Infrastructure/Providers/SofaScoreProvider.cs`
- `src/SportsMonitor.Infrastructure/Providers/SofaScoreMapper.cs` (novo)
- `src/SportsMonitor.Bff/Program.cs`
- `src/SportsMonitor.Tests/Providers/SofaScoreProviderTests.cs`
- `sportsmonitor.service`
- `deploy-gcp.ps1`
- `setup-gcp-vm.ps1`

### Remaining

- Validar SofaScore em produÃ§Ã£o (buscar partidas ao vivo e confirmar respostas da API via Playwright).
- Google API key (opcional): `.\setup-google-search-key.ps1 -ProjectId sportsmonitor-prod`.
- Instalar Playwright localmente para dev: `& src\SportsMonitor.Bff\bin\Debug\net10.0\playwright.ps1 install chromium`.

---


## 2026-06-07 - Real Providers, Frontend UX, and Linux/GCP Deploy Prep

### Goal

Implement the next planned step: enable real providers without paid keys, improve dashboard UX, and prepare deployment to a GCP Compute Engine VM.

### Work Done

- Updated `src/SportsMonitor.Bff/appsettings.json`:
  - `Demo.Enabled=false`
  - SofaScore enabled with 30s polling
  - 365Scores enabled with 20s polling
  - Google enabled with 300s polling and credential placeholders
  - ApiFootball and BetsAPI remain disabled
- Changed `SofaScoreProvider` to fetch incidents sequentially with a 200ms delay between matches to reduce burst/rate-limit risk.
- Added dashboard filters:
  - text search by team or competition
  - HalfTime visibility toggle
- Added alert UX:
  - sound on/off toggle
  - active-alert panel grouped by severity
  - ignore-all action
  - active alert logic now excludes Confirmed, FalsePositive, and Ignored statuses
- Improved mobile layout:
  - responsive header
  - stacked alert action buttons on narrow screens
  - smaller score type on mobile cards
- Preserved and integrated existing Confirmed / FalsePositive buttons in divergence cards.
- Added Linux/GCP deployment files:
  - `publish-linux.sh`
  - `deploy.sh`
  - `publish-linux.ps1`
  - `setup-gcp-vm.ps1`
  - `deploy-gcp.ps1`
  - `sportsmonitor.service`
  - `nginx-sportsmonitor.conf`
- Recorded the operational requirement that all GCP configuration and deployment must be done via CLI (`gcloud`, SSH, SCP/rsync, and repository scripts), not through the Console web as the primary path.
- Updated `.gitignore` for `appsettings.Production.json` and `publish-linux/`.
- Rebuilt Angular production assets into `src/SportsMonitor.Bff/wwwroot`.
- Updated README, PROJECT_CONTEXT, NEXT_STEPS, DECISIONS, and API_REFERENCE.

### Validation

- `npm run build -- --configuration production` passed without warnings.
- `dotnet test src\SportsMonitor.slnx` passed: 78 tests.
- `dotnet publish src\SportsMonitor.Bff\SportsMonitor.Bff.csproj --configuration Release --runtime linux-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true --output publish-linux` passed.
- `bash -n publish-linux.sh deploy.sh` could not run because `/bin/bash` is unavailable in this Windows/WSL environment.

### Remaining

- Provide real Google Custom Search API key and Search Engine ID.
- Provide VM external IP and SSH user.
- Configure production secrets outside Git.
- Install/enable systemd and Nginx files on the VM.
- Run live-match validation during active games.
- Keep GCP setup and deploy command-line first.

### Files Changed

- `.gitignore`
- `README.md`
- `PROJECT_CONTEXT.md`
- `ai-notes/NEXT_STEPS.md`
- `ai-notes/SESSION_LOG.md`
- `ai-notes/DECISIONS.md`
- `ai-notes/API_REFERENCE.md`
- `src/SportsMonitor.Bff/appsettings.json`
- `src/SportsMonitor.Infrastructure/Providers/SofaScoreProvider.cs`
- `src/SportsMonitor.Web/src/app/alert.service.ts`
- `src/SportsMonitor.Web/src/app/app.ts`
- `src/SportsMonitor.Web/src/app/divergence-card.ts`
- `src/SportsMonitor.Web/src/app/source-match-card.ts`
- `src/SportsMonitor.Web/src/app/source-panel.ts`
- `src/SportsMonitor.Web/src/styles.css`
- `src/SportsMonitor.Bff/wwwroot/*`
- `publish-linux.sh`
- `deploy.sh`
- `publish-linux.ps1`
- `setup-gcp-vm.ps1`
- `deploy-gcp.ps1`
- `sportsmonitor.service`
- `nginx-sportsmonitor.conf`

### Next Step

Configure production credentials and deploy to the VM.

---

## 2026-06-07 - SofaScore Official External API Check

### Goal

Check the user-provided SofaScore external API docs URL:

`https://api.sofascore.com/api/docs/external#tag/Betting-Odds/operation/get_sofascore_app_external_api_v1_bettingodds_list`

### Work Done

- Tried to open the official external docs through browser/web tooling.
- Tested docs and likely related endpoints through CLI with browser-like headers.
- Updated `ai-notes/CREDENTIALS_GUIDE.md`, `ai-notes/SOURCE_RESEARCH_STATUS.md`, and `ai-notes/DECISIONS.md`.

### Findings

- The URL fragment references a `Betting Odds` operation, not directly live score/incidents.
- `https://api.sofascore.com/api/docs/external` returned HTTP 403 locally.
- `https://api.sofascore.com/api/docs/external/openapi.json` returned HTTP 403 locally.
- `https://api.sofascore.com/api/docs/external/swagger.json` returned HTTP 403 locally.
- Probable no-auth endpoint `https://api.sofascore.com/api/v1/betting-odds/list` returned HTTP 403 locally.
- Interpretation: the external API/docs appear access-controlled, partner-gated, or blocked from this environment. Do not infer token/header format until the Swagger security scheme or a generated cURL command is available.

### Next Step

If the docs open in the user's browser, capture the Swagger `Authorize` security scheme or generated cURL command, masking secrets. Otherwise, keep SofaScore optional/blocked and rely on 365Scores + Google or a licensed provider for production reliability.

---

## 2026-06-07 - Handoff Persistence Reminder

### Goal

Record the user's reminder that important project information must be persisted because the work may continue across different AI models.

### Work Done

- Added a continuity rule to `PROJECT_CONTEXT.md`.
- Added the same handoff expectation to `ai-notes/NEXT_STEPS.md`.
- Recorded a decision in `ai-notes/DECISIONS.md`.

### Decision

Important user-shared information must be stored in repository handoff files before ending work. Do not rely on conversation history alone.

### Next Step

Continue keeping `PROJECT_CONTEXT.md` and `/ai-notes/` synchronized whenever credentials, GCP choices, deployment results, validation findings, or blockers are shared.

---

## 2026-06-07 - Google Credential Recovery Check

### Goal

Determine whether previously shared Google Custom Search credentials were already available locally, so the user would not need to recreate anything manually.

### Work Done

- Searched the repository and local Claude history for Google Custom Search credentials.
- Found a previously stored `SearchEngineId`: `25c69f98aa10d4ba0`.
- Found an old Google API key in local Claude history, but did not print or commit it.
- Generated a temporary `appsettings.Production.json` locally to test the old key.
- Tested one Custom Search request; Google returned `403 PERMISSION_DENIED` with message: "This project does not have the access to Custom Search JSON API."
- Removed the temporary `appsettings.Production.json` to avoid deploying an invalid key.
- Added `setup-google-search-key.ps1` to create a new restricted API key via `gcloud`, enable required APIs, and write a gitignored `appsettings.Production.json`.

### Current Credential Status

- Search Engine ID can be reused: `25c69f98aa10d4ba0`.
- Old API key should not be reused; create a new key in the selected GCP project via CLI.
- User still needs to choose/confirm the GCP project because that controls billing/resources.

### Next Step

After user confirms the `ProjectId`, run:

```powershell
.\setup-google-search-key.ps1 -ProjectId SEU_PROJECT_ID
.\setup-gcp-vm.ps1 -ProjectId SEU_PROJECT_ID
.\deploy-gcp.ps1 -ProjectId SEU_PROJECT_ID
```

---

## 2026-06-07 - Provider Token and Endpoint Check

### Goal

Answer whether SofaScore, 365Scores, and Google tokens/configuration are correct.

### Findings

- SofaScore:
  - No token is configured or required by the current provider.
  - Uses `BaseUrl=https://api.sofascore.com` and browser `User-Agent`.
  - Local HTTP test to `/api/v1/sport/football/events/live` returned `403 Forbidden`, even with browser-like headers.
  - Interpretation: this is likely endpoint/IP/anti-bot blocking, not missing token.
- 365Scores:
  - No token is configured or required by the current provider.
  - Local `curl.exe` test to `/web/games/?appTypeId=5&langId=31&timezoneName=America%2FSao_Paulo&userCountryId=-1&onlyLive=true` returned HTTP 200 with a large JSON payload.
- Google:
  - Requires API key + Search Engine ID.
  - Search Engine ID found: `25c69f98aa10d4ba0`.
  - Old API key found in local Claude history failed with `403 PERMISSION_DENIED`: project does not have access to Custom Search JSON API.
  - Use `setup-google-search-key.ps1` to create a fresh restricted API key in the selected GCP project.

### Next Step

Before production validation, either solve SofaScore 403 with a compliant source/access strategy or rely primarily on 365Scores + Google until SofaScore access is stable.

---

## 2026-06-07 - Release Workflow Requirement

### Goal

Record the user's requirement for a structured release process as GCP deployment matures.

### Work Done

- Created `ai-notes/RELEASE_WORKFLOW.md`.
- Documented the required sequence: correction -> tests -> code review -> commit -> GCP publication -> production validation -> handoff update.
- Added references in `PROJECT_CONTEXT.md`, `ai-notes/NEXT_STEPS.md`, and `ai-notes/DECISIONS.md`.

### Decision

As deployment becomes reliable, every production change should use and improve the release workflow document. Any new deploy lesson, command, blocker, or validation result must be saved there or in the relevant handoff docs.

### Next Step

Use `ai-notes/RELEASE_WORKFLOW.md` as the checklist before the first real GCP publication.

---

## 2026-06-07 - Credentials Guide

### Goal

Document how to obtain tokens/keys for SofaScore, 365Scores, and Google if possible.

### Work Done

- Created `ai-notes/CREDENTIALS_GUIDE.md`.
- Documented that SofaScore and 365Scores do not use tokens in the current implementation.
- Documented that Google requires API key + Search Engine ID.
- Recorded that `SearchEngineId=25c69f98aa10d4ba0` is known and the old API key failed with 403.
- Documented safe boundaries: no cookies, no CAPTCHA bypass, no fingerprint spoofing, no private mobile-token extraction.

### Next Step

After the user chooses a GCP project, create the Google key with `setup-google-search-key.ps1`.

---

## 2026-06-07 - Fastest Possible Data Update Requirement

### Goal

Record and implement the user's requirement that live data should update as fast as realistically possible.

### Work Done

- Updated default provider polling profile:
  - SofaScore: 10s, if endpoint access works
  - 365Scores: 10s
  - Google: remains 300s because it is quota/cost constrained and used for verification snippets
- Recorded the latency requirement in `PROJECT_CONTEXT.md`, `ai-notes/NEXT_STEPS.md`, `ai-notes/DECISIONS.md`, and `ai-notes/CREDENTIALS_GUIDE.md`.

### Notes

- Faster free-provider polling increases blocking/rate-limit risk.
- SofaScore currently returns HTTP 403 locally, so faster polling only matters after access is stable.
- Reliable sub-10s event data likely requires paid/licensed provider validation.

### Next Step

During live-match validation, measure actual end-to-end latency per source and update this requirement with observed values.

---

## 2026-05-26 - Phase 01 Planning and Handoff Standard

### Goal

Define the Phase 01 research plan for a local-first sports data divergence monitoring system and add a continuity standard for AI/model handoff.

### Work Done

- Defined Phase 01 as research-only.
- Confirmed that architecture and implementation must wait until source feasibility is clear.
- Added the local-first requirement.
- Added the initial list of 63 competitions and official websites.
- Added the requirement to keep `PROJECT_CONTEXT.md` updated after meaningful sessions.
- Added supporting AI notes files.

### Findings

- The project should avoid mandatory cloud infrastructure for the MVP.
- Every source must be evaluated for local execution compatibility.
- Research must classify each source by access method, cost, latency, limits, risk, and MVP suitability.

### Decisions

- Use `PROJECT_CONTEXT.md` as the main continuity/handoff file.
- Use `/ai-notes/SESSION_LOG.md` for chronological notes.
- Use `/ai-notes/NEXT_STEPS.md` for immediate tasks.
- Use `/ai-notes/DECISIONS.md` for decisions already made.
- Use `/ai-notes/SOURCE_RESEARCH_STATUS.md` for source-level tracking.

### Files Changed

- `PHASE_01_DATA_SOURCE_RESEARCH.md`
- `PROJECT_CONTEXT.md`
- `/ai-notes/SESSION_LOG.md`
- `/ai-notes/NEXT_STEPS.md`
- `/ai-notes/DECISIONS.md`
- `/ai-notes/SOURCE_RESEARCH_STATUS.md`

### Next Step

Research the first batch of data sources and fill the source viability matrix.

---

## 2026-05-26 - Phase 01 Source Research (Sports APIs, Odds APIs, Live Score Apps, Bookmakers)

### Goal

Research all source categories except official competition websites and produce detailed source profiles for the Phase 01 viability report.

### Work Done

- Researched Sports Data APIs: API-Football, Sportmonks, football-data.org, Sportradar, SportsDataIO
- Researched Odds APIs: The Odds API, OpticOdds, BetsAPI/b365api
- Researched Live Score Apps: SofaScore, Flashscore, FotMob
- Researched Bookmakers: Betfair Exchange, Pinnacle, Bet365, Betano, Sportingbet
- Researched data providers: Sportradar, Genius Sports, Stats Perform/Opta, Kambi
- Created `PHASE_01_RESEARCH_RESULTS.md` with detailed profiles for all researched sources
- Updated `SOURCE_RESEARCH_STATUS.md` with full status table
- Updated `PROJECT_CONTEXT.md` with findings, decisions, and next steps

### Findings

- **API-Football** ($19-39/mo): Best MVP candidate for sports data. Live events every 15s, 1200+ competitions, BrasileirÃ£o/Libertadores/Copa do Brasil covered. No live odds.
- **Sportmonks** (â‚¬129/mo for Brazil): Alternative with slightly better Brazil coverage. Worldwide plan needed. Free 14-day trial. No WebSocket.
- **football-data.org** (â‚¬0-29/mo): Low cost secondary source. BrasileirÃ£o free. No Copa Libertadores. No odds.
- **Sportradar**: Enterprise only ($10k+/mo). Not viable for MVP.
- **The Odds API** ($29-99/mo): Best documented odds API. Pro $29/mo for pre-match, Business $99/mo for live + Pinnacle + 50+ books. BrasileirÃ£o Serie A covered.
- **BetsAPI**: Only source with confirmed Bet365 live odds + market suspension status (3-5s update). Pricing requires login. Medium legal/ToS risk (commercial redistributor).
- **SofaScore**: No official API. FAQ confirms unavailability. Excluded from MVP.
- **Flashscore**: No official API. Only Apify scrapers. Excluded from MVP.
- **FotMob**: No official API. Only unofficial wrappers. Excluded from MVP.
- **Betfair Exchange**: Free official API with live streaming (WebSocket) and market suspension status. Exchange (peer-to-peer), not traditional bookmaker. Brazil account creation needs validation.
- **Pinnacle**: Public API closed July 2025. Brazil geographically blocked. Access via The Odds API Business plan.
- **Bet365/Betano/Sportingbet**: No public APIs. Access via BetsAPI or The Odds API.
- **Sportradar, Genius Sports, Stats Perform, Kambi**: Enterprise or B2B only. Not viable for small local systems.

### Decisions Made

- Live score apps (SofaScore, Flashscore, FotMob) excluded from MVP â€” no official APIs
- Sportradar and OpticOdds excluded from MVP â€” enterprise pricing
- Direct bookmaker integration (Bet365, Betano, etc.) excluded â€” no public APIs, ToS violation
- BetsAPI identified as primary path for Bet365 live odds + suspension status
- Betfair Exchange API identified as only free bookmaker API with live streaming

### Open Questions

- Is Betfair account creation feasible from Brazil?
- BetsAPI exact pricing and coverage for Brazilian competitions?
- Does The Odds API Business plan include market suspension status field?
- Which official competition websites expose structured data?

### Files Changed

- `PHASE_01_RESEARCH_RESULTS.md` (created)
- `ai-notes/SOURCE_RESEARCH_STATUS.md` (updated)
- `PROJECT_CONTEXT.md` (updated)
- `ai-notes/SESSION_LOG.md` (updated)

### Next Step

1. Research official competition websites (63 listed) â€” done in next session
2. Manual validation of BetsAPI pricing and The Odds API suspension status
3. Confirm Betfair Brazil feasibility
4. Produce final Phase 01 viability report
5. Start architecture only after Phase 01 is complete

---

## 2026-05-26 - Phase 01 Official Competition Sites Research (63 sites)

### Goal

Research all 63 official competition websites to determine if any expose a public developer API for live match data, or if all data must come from commercial aggregators.

### Work Done

- Grouped 63 sites into ~20 research batches by parent organization
- Researched all groups: UEFA, CBF, CONMEBOL, Bundesliga, Premier League, EFL, LaLiga, SPFL, MLS, Argentina, Ligue 1, Serie A, Eredivisie, Primeira Liga, J-League, Nordic leagues, smaller European leagues, South American leagues, Asian/Oceanic leagues, special cups
- Created PHASE_01_OFFICIAL_SITES_RESEARCH.md with full classification table and group profiles
- Updated SOURCE_RESEARCH_STATUS.md with Phase 01 COMPLETE status
- Updated PROJECT_CONTEXT.md marking Phase 01 as complete

### Findings

- **Central finding:** NO official competition site has a public developer API for live match data
- **Exception: OpenLigaDB** â€” free, no-auth community API covering Bundesliga, 2. Bundesliga, DFB-Pokal. api.openligadb.de. Rate: 1000 req/h. No authentication.
- **Lega Serie A:** Genius Sports holds exclusive official data rights through 2029 (no direct access)
- **UEFA:** Azure API management portal exists but is partner/media-only
- **CBF:** campeonatos.cbf.com.br is an internal endpoint â€” unofficial and fragile
- **All other sites:** depend on API-Football, Sportmonks, or football-data.org
- **Bonus discovery: API Futebol** (api-futebol.com.br) â€” Brazilian-specific commercial API for BrasileirÃ£o, Copa do Brasil, Libertadores, Estaduais, Copinha
- **Bonus discovery: TheSportsDB** â€” free crowdsourced database, good for metadata/logos, not live events
- **API-Football Pro ($19/mo) covers all 63 competitions**

### Decisions Made

- Official competition sites excluded from MVP data sources (all classified G or D with limitations)
- OpenLigaDB added as free supplementary source for Bundesliga/DFB-Pokal
- API Futebol flagged for evaluation as optional Brazilian supplement
- Phase 01 declared COMPLETE

### Files Changed

- `PHASE_01_OFFICIAL_SITES_RESEARCH.md` (created)
- `ai-notes/SOURCE_RESEARCH_STATUS.md` (updated)
- `PROJECT_CONTEXT.md` (updated)
- `ai-notes/SESSION_LOG.md` (this entry)
- `ai-notes/NEXT_STEPS.md` (updated)

### Next Step

Phase 01 complete. Begin Phase 02: Architecture planning.
Remaining manual validations: BetsAPI pricing, The Odds API suspension status, Betfair Brazil feasibility.

---

## 2026-05-26 - Phase 02+ Planning Update Integration

### Goal

Integrate `PHASE_02_PLUS_PLANNING_UPDATE.md` (operational requirements from Josias) into the project context and answer which file to send to the next chat for Phase 02 planning.

### Work Done

- Extracted full content of PHASE_02_PLUS_PLANNING_UPDATE.md from session transcript
- Saved PHASE_02_PLUS_PLANNING_UPDATE.md to project folder
- Added desktop-first requirement (WPF/WinForms + WebView2 shell) and .NET stack confirmation to the document
- Updated PROJECT_CONTEXT.md with: operational workflow, source priority, desktop-first requirement, World Cup 2026 (#64), phase roadmap 02â€“08, tech stack, new decisions, open questions
- Updated NEXT_STEPS.md with Phase 02 deliverable and source tension documentation

### Findings

- Josias confirmed operational flow: system alerts â†’ analyst manually verifies â†’ analyst manually acts
- Primary comparison sources: 365Scores, SofaScore, Google, official competition website
- Alert is audible ("apito") + dashboard card â€” mandatory for MVP
- Desktop-first: Windows app (WPF/WinForms + WebView2), trivially migratable to web (same ASP.NET Core core)
- FIFA World Cup 2026 added as competition #64 (Very High priority, June 11â€“July 19 2026, 48 teams, 104 matches)
- **Critical tension**: SofaScore, 365Scores, Google have no official APIs but are the desired primary sources

### Decisions Made

- Desktop-first: WPF or WinForms + WebView2 as thin shell around ASP.NET Core + Angular
- Technology stack locked: ASP.NET Core (.NET 8+), Angular, SignalR, SQLite, WPF/WinForms, WebView2
- Betting action confirmed as fully manual â€” system only alerts
- Manual verification workflow is a required MVP feature
- Official competition website = preferred truth/reference source

### Open Questions

- How to access 365Scores data? (never researched)
- How to access FIFA.com live data for World Cup?
- Source tension: SofaScore/Google/365Scores access without ToS violation?

### Files Changed

- `PHASE_02_PLUS_PLANNING_UPDATE.md` (created)
- `PROJECT_CONTEXT.md` (updated)
- `ai-notes/NEXT_STEPS.md` (updated)
- `ai-notes/SESSION_LOG.md` (this entry)

### Answer to User's Question

**"Which .md file to send to the next chat for Phase 02 planning?"**

Send **`PROJECT_CONTEXT.md`** â€” the master handoff file, now updated with all new requirements.

Optionally send **`PHASE_02_PLUS_PLANNING_UPDATE.md`** alongside it for full operational detail (workflow, verification table, divergence types, dashboard requirements, prompt template for next phase).

### Next Step

Create `PHASE_02_FUNCTIONAL_REQUIREMENTS_AND_OPERATIONAL_WORKFLOW.md`.

---

## 2026-05-26 - Comparison Sources Research (SofaScore, 365Scores, Google)

### Goal

Research unofficial internal API endpoints for SofaScore, 365Scores, and Google live scores. User confirmed HTML scraping and unofficial endpoints are acceptable as long as no active anti-bot bypass is required.

### Work Done

- Researched SofaScore: confirmed api.sofascore.com/api/v1 endpoints
- Researched 365Scores: confirmed webws.365scores.com/web/ endpoints
- Researched Google live scores: determined not viable as automated source
- Created PHASE_01_COMPARISON_SOURCES_RESEARCH.md with full endpoint docs, .NET integration strategy, and SQLite history schema
- Registered user requirement: historical data must be persisted per source per poll
- Updated SOURCE_RESEARCH_STATUS.md (SofaScore/365Scores confirmed viable)
- Updated PROJECT_CONTEXT.md (source priority table resolved, historical data requirement added)

### Findings

**SofaScore:**
- Base URL: `https://api.sofascore.com/api/v1`
- Live events: `GET /sport/football/events/live`
- Incidents (goals/cards/subs): `GET /event/{eventId}/incidents`
- CloudFlare bÃ¡sico â€” User-Agent browser + 25-30s interval; sem auth
- ViÃ¡vel: sim

**365Scores:**
- Base URL: `https://webws.365scores.com/web/`
- Game details: `GET /game/?appTypeId=5&langId=31&timezoneName=America/Sao_Paulo&userCountryId=-1&gameId={id}`
- Results by competition: `GET /games/results/?...&competitions={id}`
- Sem auth; proteÃ§Ã£o bÃ¡sica
- ViÃ¡vel: sim

**Google:**
- Sem endpoint JSON pÃºblico acessÃ­vel sem serviÃ§o pago (SerpApi $50+/mo)
- SoluÃ§Ã£o: dashboard gera link de busca Google para verificaÃ§Ã£o manual
- Automatizado: nÃ£o viÃ¡vel

**HistÃ³rico:**
- UsuÃ¡rio confirmou que quer histÃ³rico salvo por fonte (SQLite ou arquivo)
- Schema proposto: tabelas `source_readings`, `source_events`, `divergences`, `matches`
- Alternativa simples: JSONL por partida em /data/matches/

### Decisions Made

- SofaScore: integrar via api.sofascore.com/api/v1
- 365Scores: integrar via webws.365scores.com/web/
- Google: link manual no dashboard â€” nÃ£o automatizado
- HistÃ³rico: persistir todos os payloads coletados no SQLite (uma linha por poll por fonte)
- NormalizaÃ§Ã£o de IDs Ã© problema crÃ­tico: Match Resolver necessÃ¡rio

### Files Changed

- `PHASE_01_COMPARISON_SOURCES_RESEARCH.md` (created)
- `ai-notes/SOURCE_RESEARCH_STATUS.md` (updated)
- `PROJECT_CONTEXT.md` (updated)
- `ai-notes/DECISIONS.md` (updated)
- `ai-notes/SESSION_LOG.md` (this entry)

### Next Step

Phase 01 research fully complete (including comparison sources).
Begin Phase 02: create `PHASE_02_FUNCTIONAL_REQUIREMENTS_AND_OPERATIONAL_WORKFLOW.md`.

---

## 2026-05-26 - Phase 03 Technical Architecture

### Goal

Definir a arquitetura tÃ©cnica do sistema com design patterns simples, extensÃ­veis e orientados a tempo real.

### Work Done

- Discutiu opÃ§Ãµes de workers (orquestrador vs independentes vs channel)
- Decidiu por workers independentes + detecÃ§Ã£o reativa via SnapshotStore event
- Adicionou IOptionsMonitor para intervalos de polling configurÃ¡veis e hot-reloadable
- Criou PHASE_03_TECHNICAL_ARCHITECTURE.md com arquitetura completa

### Decisions Made

- Workers independentes por fonte (SofaScoreWorker, Api365ScoresWorker, ApiFootballWorker, BetfairStreamWorker, AlertWorker)
- DivergenceEngine Ã© reativo â€” dispara quando qualquer snapshot atualiza (nÃ£o tem loop prÃ³prio)
- IOptionsMonitor<T> para polling intervals â€” hot-reload via appsettings.json, futuro: UI settings
- PollingWorker<TOptions> base class elimina repetiÃ§Ã£o nos workers
- Channel<Divergence> desacopla detecÃ§Ã£o do envio de alerta
- JSONL como storage MVP (JsonlMatchHistoryRepository) â€” trocar por SQLite = sÃ³ trocar registro no DI
- IMatchDataProvider, IDivergenceRule, IAlertChannel, IMatchHistoryRepository como extensibility points
- FuzzyMatchResolver para correlaÃ§Ã£o de IDs entre fontes (nome + horÃ¡rio Â±5min + competiÃ§Ã£o)

### Files Changed

- `PHASE_03_TECHNICAL_ARCHITECTURE.md` (created)
- `PROJECT_CONTEXT.md` (updated â€” Phase 03 complete, next = Phase 04)
- `ai-notes/NEXT_STEPS.md` (updated)
- `ai-notes/SESSION_LOG.md` (this entry)

### Next Step

Phase 04: criar PHASE_04_MVP_IMPLEMENTATION_PLAN.md com tasks implementÃ¡veis em ordem.

---

## 2026-05-26 - Phase 04 MVP Implementation Start

### Goal

Retomar a partir do plano de implementaÃ§Ã£o e estabilizar os primeiros componentes do MVP com testes.

### Work Done

- Implementou `JsonlMatchHistoryRepository`
- Corrigiu `MatchBuilder.WithCollectedAt`
- Adicionou `DivergenceBuilder` para testes
- Implementou `DivergenceEngine`
- Adicionou modelos de configuraÃ§Ã£o dos providers
- Implementou `ApiFootballProvider` com mapeamento de JSON mockado
- Adicionou testes para JSONL, engine reativo e provider
- Atualizou handoff em `PROJECT_CONTEXT.md` e `ai-notes/NEXT_STEPS.md`

### Current Test Status

- `dotnet test SportsMonitor.slnx`
- Resultado: 40 passed, 0 failed

### Next Step

Criar projeto Workers e implementar `PollingWorker` base + `ApiFootballWorker`, depois iniciar o BFF/SignalR.

---

## 2026-05-26 - Phase 04 Workers and BFF Start

### Goal

Continuar a implementaÃ§Ã£o do MVP e salvar contexto antes do limite da sessÃ£o.

### Work Done

- Criou projeto `SportsMonitor.Workers`
- Implementou `PollingWorker` base com `CollectOnceAsync` testÃ¡vel
- Implementou `ApiFootballWorker`
- Implementou `AlertWorker`
- Criou projeto `SportsMonitor.Bff`
- Implementou endpoints:
  - `GET /api/matches/live`
  - `GET /api/divergences`
  - `POST /api/divergences/{id}/verify`
- Implementou `AlertHub` em `/hubs/alerts`
- Implementou `SignalRAlertChannel`
- Fez wiring inicial de DI no `Program.cs`
- Adicionou `appsettings.json` com `ApiFootball` desabilitado por padrÃ£o

### Current Test Status

- `dotnet test SportsMonitor.slnx`
- Resultado: 40 passed, 0 failed

### Next Step

Retomar validando `dotnet run --project SportsMonitor.Bff`, depois criar o dashboard Angular mÃ­nimo com SignalR e som de alerta.

---

## 2026-05-29 - Demo Expansion, Dashboard Layout Fix, and Packaging

### Goal

Continue from the packaged MVP and make the demo more useful for user testing, especially around Bet365 and Google as the main sources.

### Work Done

- Reworked `DemoWorker` to inject richer demo data without API keys.
- Added Bet365 and Google as first-class mock sources in the demo.
- Added five demo match groups:
  - Flamengo x Palmeiras
  - Botafogo x Corinthians
  - Manchester City x Liverpool
  - Brasil x Argentina
  - Real Madrid x Barcelona
- Added a quick Botafogo x Corinthians score mismatch that appears about 5 seconds after startup.
- Implemented a phased Flamengo x Palmeiras simulation that updates every 10 seconds.
- Added mocked Google snippets and links for each demo match.
- Fixed dashboard source ordering so Google appears immediately after Bet365.
- Fixed the dashboard grid to support five source columns and scrolling.
- Rebuilt Angular production assets into `src/SportsMonitor.Bff/wwwroot`.
- Ran `publish.ps1` and generated the local distributable package under `publish\`.
- Verified the published BFF returns 5 match groups and 5 Google snapshots after the quick scenario runs.
- Updated README, PROJECT_CONTEXT, NEXT_STEPS, and SOURCE_RESEARCH_STATUS.

### Test Status

- `dotnet test src\SportsMonitor.slnx`
- Result: 68 passed, 0 failed

### Packaging Status

- Deliver the full `publish\` folder, not only `SportsMonitor.Desktop.exe`.
- Start the app with `publish\SportsMonitor.Desktop.exe`.
- Demo mode is enabled by default and requires no API keys.

### Next Step

Smoke test the published desktop app on a clean Windows machine, then validate real BetsAPI and Google credentials.


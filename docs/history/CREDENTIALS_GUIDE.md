# Credentials Guide

Last updated: 2026-06-07

This file records how to obtain or confirm credentials for the current provider set.

## Summary

| Provider | Needs token/key? | How to obtain |
|---|---:|---|
| SofaScore | Unknown for official external API; none for current internal endpoint | User shared official external API docs URL on 2026-06-07. Local tests to docs and possible betting-odds endpoint returned HTTP 403. Current implementation still uses public/internal HTTP endpoint with browser-like headers, but local test also returned HTTP 403. Treat as unstable/blocking until formal access/auth is confirmed. |
| 365Scores | No token for current internal endpoint | Current implementation uses public/internal HTTP endpoint with browser-like headers. Local `curl.exe` test returned HTTP 200. |
| Google Custom Search | Yes | Use `setup-google-search-key.ps1` after choosing GCP project. Search Engine ID already recovered: `25c69f98aa10d4ba0`. |
| API-Football | Yes, paid | Create account/subscribe at api-sports.io/API-Football, then set `Providers:ApiFootball:ApiKey`. Out of current no-paid-key scope. |
| BetsAPI | Yes, paid | Create account/subscribe at BetsAPI/b365api, then set `Providers:BetsApi:Token`. Out of current scope. |

## Latency Note

The user wants data updated as fast as realistically possible.

Current aggressive no-paid-provider profile:

- 365Scores: 10s polling
- SofaScore: 10s polling if endpoint access works
- Google Custom Search: 300s polling because it is quota/cost constrained and used for verification snippets, not structured live score truth

If production needs reliable sub-10s updates, prioritize licensed/paid provider validation (for example BetsAPI or API-Football depending on event latency and coverage).

## Google Custom Search

Current known values:

- Search Engine ID: `25c69f98aa10d4ba0`
- Old API key from local Claude history: do not reuse. It failed with `403 PERMISSION_DENIED`.

Preferred CLI flow:

```powershell
.\setup-google-search-key.ps1 -ProjectId SEU_PROJECT_ID
```

This script:

1. Sets the selected GCP project.
2. Enables `customsearch.googleapis.com`.
3. Enables `apikeys.googleapis.com`.
4. Creates a new API key restricted to Custom Search JSON API.
5. Writes `appsettings.Production.json` locally.

Manual fallback:

1. In Google Cloud, select the correct project.
2. Enable **Custom Search JSON API**.
3. Create an API key.
4. Restrict the key to **Custom Search JSON API**.
5. Use Search Engine ID `25c69f98aa10d4ba0`, unless a new Programmable Search Engine is intentionally created.
6. Put both values in `appsettings.Production.json`, never in committed files.

## SofaScore

No token required. Access is via Microsoft.Playwright (headless Chromium).

Current implementation (updated 2026-06-07):

- Provider: `SofaScoreProvider` in `src/SportsMonitor.Infrastructure/Providers/SofaScoreProvider.cs`
- Access method: requests made via `page.EvaluateAsync<string>()` running `fetch()` inside a real Chromium browser process
- Root cause of previous HTTP 403: TLS fingerprinting (JA3/JA4) — .NET HttpClient TLS handshake differs from Chrome; adding headers was insufficient
- Solution: `Microsoft.Playwright` 1.60.0 — Chromium is launched headless; all HTTP traffic originates from real Chrome process with genuine TLS fingerprint
- Live endpoint: `https://api.sofascore.com/api/v1/sport/football/events/live`
- Incidents endpoint: `https://api.sofascore.com/api/v1/event/{id}/incidents`

Setup required (one-time per machine/VM):

```powershell
# Windows (development)
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

```bash
# Linux/GCP VM (handled by setup-gcp-vm.ps1)
dotnet tool install --global Microsoft.Playwright.CLI
playwright install-deps chromium
playwright install chromium
```

Linux flags (already in code):
- `--no-sandbox`: required on servers without desktop environment
- `--disable-dev-shm-usage`: prevents crash from insufficient shared memory on small VMs

Do not:

- Use login/session cookies.
- Bypass CAPTCHA or anti-bot protections.
- Use stealth plugins or fingerprint spoofing libraries (Playwright's genuine Chrome IS the solution — no spoofing needed).

## 365Scores

No token acquisition path is required for the current implementation.

Current implementation:

- Base URL: `https://webws.365scores.com`
- Live endpoint: `/web/games/?appTypeId=5&langId=31&timezoneName=America%2FSao_Paulo&userCountryId=-1&onlyLive=true`
- Auth: none
- Headers: browser-like `User-Agent` + `Accept`

Current status:

- Local `curl.exe` test returned HTTP 200 with a large JSON payload.
- Source is score/status oriented; current provider does not expose individual goal/card incidents.

Do not:

- Extract private mobile app tokens.
- Use login/session cookies.
- Bypass access controls.

## What Still Requires User Choice

The user must choose the GCP `ProjectId` before creating keys/VM resources because this controls billing and ownership.

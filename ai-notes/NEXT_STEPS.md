# Next Steps

## Current Status: MVP Packaged

Last updated: 2026-05-29

The MVP is implemented and locally packaged. The demo mode is enabled by default and does not require API keys.

Completed:
- Domain, Application, Infrastructure, Workers, BFF, Angular dashboard, and WPF/WebView2 shell
- Providers for SofaScore, 365Scores, API-Football, BetsAPI, and Google Custom Search
- Divergence rules for score, goal scorer, missing goal, card, and status mismatches
- DemoWorker with Bet365 and Google as main demo sources
- Quick Botafogo x Corinthians score mismatch appearing about 5 seconds after startup
- Phased Flamengo x Palmeiras live simulation updating every 10 seconds
- Mock Google snippets per demo match
- Dashboard source layout fixed: Bet365 first, Google second, remaining sources after; grid scroll enabled
- Production Angular build copied to `src/SportsMonitor.Bff/wwwroot`
- `publish.ps1` generated `publish\SportsMonitor.Desktop.exe`
- Tests: `dotnet test src\SportsMonitor.slnx` => 68 passing

## Immediate Tasks

1. Smoke test `publish\SportsMonitor.Desktop.exe` on a clean Windows machine.
2. Zip and deliver the entire `publish\` folder, not only `SportsMonitor.Desktop.exe`.
3. Confirm WebView2 Runtime is available on the tester machine.
4. Validate real BetsAPI payloads and adjust `BetsApiProvider` event parsing if needed.
5. Configure real Google Custom Search credentials and verify quota behavior.
6. Research FIFA.com live data options for World Cup 2026.

## Packaging Notes

Run:

```powershell
.\publish.ps1
```

Deliver:

```text
publish\
```

The user starts:

```text
publish\SportsMonitor.Desktop.exe
```

The desktop app starts the BFF automatically, opens the dashboard, and writes BFF logs under `publish\logs\`.

## Real Provider Switch

When real credentials are available:

1. Edit `publish\appsettings.json`.
2. Set `"Demo": { "Enabled": false }`.
3. Enable the desired providers under `"Providers"`.
4. Fill `BetsApi.Token`, `Google.ApiKey`, `Google.SearchEngineId`, and other provider keys.

## Still Out Of Scope

- Automated betting
- Login automation
- CAPTCHA solving
- Fingerprint spoofing
- Scraping protected pages or bypassing access controls

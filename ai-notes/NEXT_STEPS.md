# Next Steps

## Current Status: Real Providers + Deploy Prep Implemented

Last updated: 2026-06-07

The MVP is implemented, locally packaged, and now prepared for real-provider operation and Linux/GCP deployment.

Continuity rule: this project may move between AI models. Store important user-shared information in `PROJECT_CONTEXT.md` and `/ai-notes/` before ending work, especially credentials status, chosen GCP project, deployment decisions, validation results, and blockers.

Release process rule: production work must follow `ai-notes/RELEASE_WORKFLOW.md`: correction -> tests -> code review -> commit -> GCP publication -> production validation -> handoff update.

Provider token/status check:
- SofaScore: **FIXED** — migrated to Microsoft.Playwright 1.60.0 (headless Chromium). HTTP 403 was TLS fingerprinting of .NET HttpClient; requests now run inside real Chrome process via page.EvaluateAsync fetch(). Requires `playwright install chromium` on first run.
- 365Scores: no token required; current local `curl.exe` test returned HTTP 200.
- Google: requires API key + Search Engine ID. Search Engine ID is `25c69f98aa10d4ba0`; old API key failed with 403, so create a fresh key with `setup-google-search-key.ps1`.

Latency requirement:
- User wants live data updated as fast as realistically possible.
- Current aggressive free-provider profile: 365Scores 10s, SofaScore 10s if access works, Google 300s due quota/cost.
- Google should not be treated as fast live score truth; it is verification snippets.
- For reliable sub-10s data, evaluate paid/licensed providers such as BetsAPI/API-Football or another official feed.

Credential instructions are tracked in `ai-notes/CREDENTIALS_GUIDE.md`.

Completed:
- SofaScore migrated to Microsoft.Playwright headless Chromium (HTTP 403 TLS fingerprint issue resolved)
- SofaScore and 365Scores enabled in `src/SportsMonitor.Bff/appsettings.json`
- Demo disabled by default for real operation
- Google Custom Search enabled with 300s polling and credential placeholders
- SofaScore incident calls slowed with a sequential 200ms delay
- Dashboard filters: text search and HalfTime toggle
- Alert UX: sound toggle, active-alert panel, ignore-all action, active status filtering
- Mobile layout improvements for header, scores, and alert action buttons
- Linux/GCP artifacts: `publish-linux.sh`, `deploy.sh`, `sportsmonitor.service`, `nginx-sportsmonitor.conf`
- Windows-friendly GCP scripts: `publish-linux.ps1`, `setup-gcp-vm.ps1`, `deploy-gcp.ps1`
- Google key automation: `setup-google-search-key.ps1` creates a restricted API key and writes gitignored `appsettings.Production.json`
- Secrets protected: `appsettings.Production.json` and `publish-linux/` added to `.gitignore`
- Validation: Angular production build passed, Linux publish passed, .NET tests passed
- Tests: `dotnet test src\SportsMonitor.slnx` => 78 passing

## Immediate Tasks

0. Install Playwright Chromium locally (one-time, Windows dev machine):
   ```powershell
   dotnet tool install --global Microsoft.Playwright.CLI
   playwright install chromium
   ```
   Then test: `dotnet run --project src\SportsMonitor.Bff` and watch logs for SofaScore polling.

1. Confirm the GCP `ProjectId`. Search Engine ID is already known: `25c69f98aa10d4ba0`. The old API key found in local history failed with 403, so create a new key via `setup-google-search-key.ps1`.
2. Create/prepare the Compute Engine VM via CLI:
   - Ubuntu 22.04 LTS
   - HTTP traffic allowed
   - .NET 10 ASP.NET Core runtime or self-contained binary support
   - Nginx installed
3. Create production config on the VM as `appsettings.Production.json` or environment variables:
   - `Providers__Google__ApiKey`
   - `Providers__Google__SearchEngineId`
   - keep `Demo__Enabled=false`
4. Install `sportsmonitor.service` on the VM and replace `VM_USER` with the real Linux user.
5. Install `nginx-sportsmonitor.conf` in Nginx and enable it.
6. Run deploy from local machine:

```bash
VM_USER=seu_usuario VM_IP=IP_DA_VM ./deploy.sh
```

7. Smoke test `http://IP_DA_VM/`.
8. During live matches, validate:
   - SofaScore and 365Scores columns receive real matches
   - `FuzzyMatchResolver` groups the same match across sources
   - Google snippets appear in the Google column
   - divergence alert sound/toggle/panel/actions work
   - mobile layout is usable

## Still Pending / Manual

- Real Google API key and Search Engine ID
- VM external IP and SSH user
- Production secrets outside Git
- Real live-match validation during game windows
- BetsAPI token and payload validation, if BetsAPI returns to scope
- API-Football key, if the paid official source is enabled
- Clean Windows smoke test of `publish\SportsMonitor.Desktop.exe`

## Build Commands

GCP setup must be command-line first:

```bash
gcloud auth login
gcloud config set project SEU_PROJECT_ID

gcloud compute instances create sportsmonitor-vm \
  --zone=southamerica-east1-b \
  --machine-type=e2-small \
  --image-family=ubuntu-2204-lts \
  --image-project=ubuntu-os-cloud \
  --boot-disk-size=20GB \
  --tags=http-server,https-server

gcloud compute firewall-rules create allow-sportsmonitor-http \
  --allow=tcp:80 \
  --target-tags=http-server

gcloud compute ssh sportsmonitor-vm --zone=southamerica-east1-b
```

Windows desktop package:

```powershell
.\publish.ps1
```

Linux/GCP package:

```bash
./publish-linux.sh
```

Deploy to VM:

```powershell
.\setup-google-search-key.ps1 -ProjectId SEU_PROJECT_ID
.\setup-gcp-vm.ps1 -ProjectId SEU_PROJECT_ID
.\deploy-gcp.ps1 -ProjectId SEU_PROJECT_ID
```

Linux shell alternative:

```bash
VM_USER=seu_usuario VM_IP=IP_DA_VM ./deploy.sh
```

## Boundaries

- No automated betting
- No login automation
- No CAPTCHA solving
- No fingerprint spoofing
- No scraping protected pages or bypassing access controls
- No relying on chat memory for important project state; update handoff docs before stopping

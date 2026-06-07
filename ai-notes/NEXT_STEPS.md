## Latest Production Validation - 2026-06-07

- URL: `http://34.151.245.70/`
- Dashboard returned HTTP 200.
- `/api/matches/live` returned live match groups.
- Remote services: `sportsmonitor` active, `nginx` active.
- VM disk was cleaned from full disk to about 20% used.
- 365Scores history bloat was fixed by no longer persisting `RawJson` for that provider.
- After several polling cycles, `/opt/sportsmonitor/data/.../365scores.jsonl` stayed around hundreds of KB instead of GB.
- Keep watching `/opt/sportsmonitor/data` after future provider changes.
# Next Steps

## Current Status: GCP DEPLOYED â€” acessÃ­vel em http://34.151.245.70/

Last updated: 2026-06-07

### GCP Details

| Campo | Valor |
|---|---|
| Project ID | `sportsmonitor-prod` |
| VM | `sportsmonitor-vm` |
| Zone | `southamerica-east1-b` |
| IP Externo | `34.151.245.70` |
| URL | **http://34.151.245.70/** |
| Billing Account | `01FE90-8618C8-3CEE8F` |

Re-deploy (apÃ³s qualquer mudanÃ§a):
```powershell
.\deploy-gcp.ps1 -ProjectId sportsmonitor-prod
```

Ver logs ao vivo:
```powershell
gcloud compute ssh sportsmonitor-vm --zone southamerica-east1-b --project sportsmonitor-prod --command "sudo journalctl -u sportsmonitor -f"
```

The MVP is implemented, locally packaged, and now prepared for real-provider operation and Linux/GCP deployment.

Continuity rule: this project may move between AI models. Store important user-shared information in `PROJECT_CONTEXT.md` and `/ai-notes/` before ending work, especially credentials status, chosen GCP project, deployment decisions, validation results, and blockers.

Release process rule: production work must follow `ai-notes/RELEASE_WORKFLOW.md`: correction -> tests -> code review -> commit -> GCP publication -> production validation -> handoff update.

Provider token/status check:
- SofaScore: **FIXED** â€” migrated to Microsoft.Playwright 1.60.0 (headless Chromium). HTTP 403 was TLS fingerprinting of .NET HttpClient; requests now run inside real Chrome process via page.EvaluateAsync fetch(). Requires `playwright install chromium` on first run.
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

1. **Validar SofaScore em produÃ§Ã£o**: Abrir `http://34.151.245.70/` durante uma janela de jogos ao vivo e confirmar que a coluna SofaScore aparece com partidas (Playwright via headless Chrome).
2. **Instalar Playwright localmente** (dev machine, one-time):
   ```powershell
   & src\SportsMonitor.Bff\bin\Debug\net10.0\playwright.ps1 install chromium
   ```
3. **Google API key** (opcional â€” Google fica desligado atÃ© ter a key):
   ```powershell
   .\setup-google-search-key.ps1 -ProjectId sportsmonitor-prod
   ```
4. Durante jogos ao vivo, validar:
   - SofaScore e 365Scores recebem partidas reais
   - `FuzzyMatchResolver` agrupa a mesma partida das duas fontes
   - Alertas de divergÃªncia disparam, som funciona, toggle/ignore agem corretamente
   - Layout mobile Ã© usÃ¡vel

## Still Pending / Manual

- ValidaÃ§Ã£o ao vivo com partidas reais (SofaScore + 365Scores)
- Google API key (opcional)
- BetsAPI token e validaÃ§Ã£o do campo LA, se retornar ao escopo
- API-Football key, se o provider pago for habilitado
- Smoke test do pacote Windows `publish\SportsMonitor.Desktop.exe` em mÃ¡quina limpa

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


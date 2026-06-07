# Release Workflow

Last updated: 2026-06-07

This project must use a repeatable command-line-first workflow for production changes and GCP publication.

## Required Flow

Every production change should move through this sequence:

1. Correction
2. Tests
3. Code review
4. Commit
5. GCP publication
6. Production validation
7. Handoff update

## 1. Correction

- Implement the smallest safe change that solves the current issue.
- Preserve unrelated user changes in the working tree.
- Keep secrets out of Git.
- If a deploy or provider behavior is learned during the fix, update this file or the relevant handoff note.

## 2. Tests

Run at minimum:

```powershell
npm run build -- --configuration production
dotnet test src\SportsMonitor.slnx
```

When deployment files changed, also validate PowerShell syntax:

```powershell
$files = @(
  'publish-linux.ps1',
  'setup-google-search-key.ps1',
  'setup-gcp-vm.ps1',
  'deploy-gcp.ps1'
)

foreach ($file in $files) {
  $tokens = $null
  $errors = $null
  [System.Management.Automation.Language.Parser]::ParseFile(
    (Resolve-Path $file),
    [ref]$tokens,
    [ref]$errors
  ) | Out-Null

  if ($errors.Count -gt 0) {
    $errors | Format-List *
    throw "$file has syntax errors"
  }
}
```

When Linux packaging changed, validate publish:

```powershell
.\publish-linux.ps1
```

## 3. Code Review

Before committing:

```powershell
git status --short
git diff --stat
git diff
```

Review checklist:

- No real secrets committed.
- `appsettings.Production.json` remains untracked/ignored.
- Generated Angular assets in `src/SportsMonitor.Bff/wwwroot` match the latest build.
- Provider config matches the intended environment.
- Documentation and handoff notes reflect anything learned.
- Existing unrelated changes are not reverted.

## 4. Commit

Commit only after tests and review are clean.

Suggested format:

```powershell
git add <files>
git commit -m "Prepare real-provider deploy workflow"
```

If there are unrelated working-tree changes, commit only the files belonging to the current change.

## 5. GCP Publication

GCP deploy must be command-line first. Do not rely on the Console web as the primary path.

Current planned PowerShell flow:

```powershell
.\setup-google-search-key.ps1 -ProjectId SEU_PROJECT_ID
.\setup-gcp-vm.ps1 -ProjectId SEU_PROJECT_ID
.\deploy-gcp.ps1 -ProjectId SEU_PROJECT_ID
```

Known current GCP/provider facts:

- `gcloud` is installed and authenticated locally as `viniciusroberto17@gmail.com`.
- GCP project is not chosen yet.
- Search Engine ID recovered from local history: `25c69f98aa10d4ba0`.
- Old Google API key failed with `403 PERMISSION_DENIED`; create a fresh key via `setup-google-search-key.ps1`.
- 365Scores does not need a token and returned HTTP 200 in local `curl.exe` test.
- SofaScore does not need a token but returned HTTP 403 in local test; treat as unstable/blocking until access is solved.

## 6. Production Validation

After deployment:

```powershell
gcloud compute instances describe sportsmonitor-vm `
  --zone=southamerica-east1-b `
  --project SEU_PROJECT_ID `
  --format "value(networkInterfaces[0].accessConfigs[0].natIP)"

gcloud compute ssh sportsmonitor-vm `
  --zone=southamerica-east1-b `
  --project SEU_PROJECT_ID `
  --command "sudo systemctl status sportsmonitor --no-pager"

gcloud compute ssh sportsmonitor-vm `
  --zone=southamerica-east1-b `
  --project SEU_PROJECT_ID `
  --command "sudo journalctl -u sportsmonitor -n 100 --no-pager"
```

Validate:

- `http://VM_IP/` loads.
- SignalR connects.
- 365Scores column receives real data when live matches exist.
- Google snippets appear after polling when credentials are valid.
- Alert actions work: Confirmar, Falso positivo, Ignorar, Ignorar todos.
- Mobile layout remains usable.
- SofaScore status is explicitly reported as working or blocked.

## 7. Handoff Update

Before ending a session, update:

- `PROJECT_CONTEXT.md`
- `ai-notes/NEXT_STEPS.md`
- `ai-notes/SESSION_LOG.md`
- This file, when deploy steps changed

Record:

- Commands run
- Test results
- GCP project/zone/VM name used
- VM external IP, if created
- Validation results
- Blockers
- Any manual action still required from the user

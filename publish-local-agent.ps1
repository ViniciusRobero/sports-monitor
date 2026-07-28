Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$outDir = Join-Path $root "publish-local-agent"
$wwwDownloads = Join-Path $root "src\SportsMonitor.Bff\wwwroot\downloads"
$zipPath = Join-Path $root "local-agent.zip"

Write-Host "==> Publishing LocalAgent for Windows x64..." -ForegroundColor Cyan
# PublishSingleFile is intentionally NOT used: Playwright's Node.js driver cannot
# locate its assets when embedded in a single-file exe (Assembly.Location is empty).
# Distributing as a folder/zip is the supported approach.
dotnet publish (Join-Path $root "src\SportsMonitor.LocalAgent") `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o $outDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Copy-Item (Join-Path $root "src\SportsMonitor.LocalAgent\appsettings.json") $outDir -Force
Copy-Item (Join-Path $root "start-local-agent.bat") $outDir -Force

# Criar ZIP para distribuicao
Write-Host "==> Creating local-agent.zip..." -ForegroundColor Cyan
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$outDir\*" -DestinationPath $zipPath
Write-Host "    ZIP criado: $zipPath"

# Copiar ZIP para wwwroot/downloads so the BFF can serve it at /downloads/local-agent.zip
New-Item -ItemType Directory -Force -Path $wwwDownloads | Out-Null
Copy-Item $zipPath (Join-Path $wwwDownloads "local-agent.zip") -Force

Write-Host ""
Write-Host "==> Pronto!" -ForegroundColor Green
Write-Host ""
Write-Host "  Pasta de distribuicao:  publish-local-agent\"
Write-Host "  ZIP para distribuicao:  local-agent.zip"
Write-Host "  Link de download: sera definido quando o novo servidor for escolhido."
Write-Host ""
Write-Host "  Proximo passo: validar localmente antes de escolher o novo servidor."

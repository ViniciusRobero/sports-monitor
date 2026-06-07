Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$outDir = Join-Path $root "publish-local-agent"

Write-Host "==> Publishing LocalAgent for Windows x64..." -ForegroundColor Cyan
dotnet publish (Join-Path $root "src\SportsMonitor.LocalAgent") `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o $outDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Copy-Item (Join-Path $root "src\SportsMonitor.LocalAgent\appsettings.json") $outDir -Force
Copy-Item (Join-Path $root "start-local-agent.bat") $outDir -Force

Write-Host ""
Write-Host "==> Pronto! Pasta de saida: publish-local-agent\" -ForegroundColor Green
Write-Host ""
Write-Host "  Copie a pasta publish-local-agent\ para o notebook."
Write-Host "  No notebook: abra start-local-agent.bat"
Write-Host ""
Write-Host "  Para mudar o servidor GCP: edite appsettings.json (campo BffUrl)"
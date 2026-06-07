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
Write-Host "  Arquivos para copiar para o notebook:"
Write-Host "    start-local-agent.bat         — ABRIR ISSO (inicia tudo automaticamente)"
Write-Host "    SportsMonitor.LocalAgent.exe  — o executavel (auto-suficiente, sem instalar .NET)"
Write-Host "    appsettings.json              — configuracao"
Write-Host ""
Write-Host "  Para mudar o servidor GCP:"
Write-Host "    1. Edite appsettings.json e mude o campo BffUrl"
Write-Host "    2. OU execute: SportsMonitor.LocalAgent.exe --bff-url http://NOVO-IP"
Write-Host ""
Write-Host "  Para autostart no boot do notebook (PowerShell como Admin):"
Write-Host "    .\Register-Startup.ps1"

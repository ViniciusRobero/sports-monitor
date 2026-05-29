# publish.ps1 — gera a pasta publish/ pronta para uso
# Uso: .\publish.ps1
# Resultado: publish\SportsMonitor.Desktop.exe (abre o app) + publish\SportsMonitor.Bff.exe (iniciado automaticamente)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root    = $PSScriptRoot
$out     = Join-Path $root "publish"
$web     = Join-Path $root "src\SportsMonitor.Web"
$bff     = Join-Path $root "src\SportsMonitor.Bff\SportsMonitor.Bff.csproj"
$desktop = Join-Path $root "src\SportsMonitor.Desktop\SportsMonitor.Desktop.csproj"

Write-Host "==> Limpando publish/ anterior..." -ForegroundColor Cyan
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory $out | Out-Null

# 1. Angular build → src/SportsMonitor.Bff/wwwroot
Write-Host "==> Buildando Angular..." -ForegroundColor Cyan
Push-Location $web
npm run build -- --configuration production
if ($LASTEXITCODE -ne 0) { throw "Angular build falhou" }
Pop-Location

# 2. BFF — self-contained win-x64 single-file
Write-Host "==> Publicando BFF..." -ForegroundColor Cyan
dotnet publish $bff `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    --output $out

if ($LASTEXITCODE -ne 0) { throw "BFF publish falhou" }

# 3. Desktop — self-contained win-x64 (WPF não suporta single-file com DLLs nativas)
Write-Host "==> Publicando Desktop..." -ForegroundColor Cyan
dotnet publish $desktop `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $out

if ($LASTEXITCODE -ne 0) { throw "Desktop publish falhou" }

# 4. Garantir pasta data/ para os arquivos JSONL
New-Item -ItemType Directory -Force (Join-Path $out "data") | Out-Null

Write-Host ""
Write-Host "==> Pronto! Arquivos em: $out" -ForegroundColor Green
Write-Host "    Execute: $out\SportsMonitor.Desktop.exe" -ForegroundColor Green
Write-Host ""
Write-Host "    Antes de rodar, edite publish\appsettings.json e habilite os provedores desejados." -ForegroundColor Yellow

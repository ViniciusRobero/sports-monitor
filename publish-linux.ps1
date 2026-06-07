Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$web = Join-Path $root "src\SportsMonitor.Web"
$bff = Join-Path $root "src\SportsMonitor.Bff\SportsMonitor.Bff.csproj"
$out = Join-Path $root "publish-linux"

Write-Host "==> Building Angular..." -ForegroundColor Cyan
Push-Location $web
npm ci
if ($LASTEXITCODE -ne 0) { throw "npm ci failed" }
npm run build -- --configuration production
if ($LASTEXITCODE -ne 0) { throw "Angular build failed" }
Pop-Location

Write-Host "==> Publishing BFF for linux-x64..." -ForegroundColor Cyan
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

dotnet publish $bff `
    --configuration Release `
    --runtime linux-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    --output $out

if ($LASTEXITCODE -ne 0) { throw "BFF publish failed" }

New-Item -ItemType Directory -Force (Join-Path $out "data") | Out-Null

Write-Host ""
Write-Host "==> Build complete: $out" -ForegroundColor Green
Write-Host "    Configure production secrets with appsettings.Production.json or environment variables." -ForegroundColor Yellow

param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectId,

    [string]$InstanceName = "sportsmonitor-vm",
    [string]$Zone = "southamerica-east1-b",
    [string]$AppDir = "/opt/sportsmonitor",
    [switch]$PromptGoogleSecrets
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$publishDir = Join-Path $root "publish-linux"
$serviceTemplate = Join-Path $root "sportsmonitor.service"
$nginxConf = Join-Path $root "nginx-sportsmonitor.conf"
$localProductionConfig = Join-Path $root "appsettings.Production.json"
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("sportsmonitor-deploy-" + [Guid]::NewGuid().ToString("N"))

function Invoke-Gcloud {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Args)
    & gcloud @Args
    if ($LASTEXITCODE -ne 0) {
        throw "gcloud $($Args -join ' ') failed"
    }
}

function ConvertFrom-SecureStringPlainText {
    param([securestring]$Secure)
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Secure)
    try {
        [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    }
}

if (-not (Get-Command gcloud -ErrorAction SilentlyContinue)) {
    throw "gcloud CLI was not found. Install Google Cloud CLI first."
}

try {
    New-Item -ItemType Directory -Force $tempDir | Out-Null

    Write-Host "==> Building local linux package..." -ForegroundColor Cyan
    & (Join-Path $root "publish-linux.ps1")
    if ($LASTEXITCODE -ne 0) { throw "publish-linux.ps1 failed" }

    Write-Host "==> Detecting remote user..." -ForegroundColor Cyan
    $remoteUser = (& gcloud compute ssh $InstanceName `
        --zone $Zone `
        --project $ProjectId `
        --command "whoami").Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($remoteUser)) {
        throw "Could not determine remote SSH user."
    }

    $serviceFile = Join-Path $tempDir "sportsmonitor.service"
    (Get-Content -LiteralPath $serviceTemplate -Raw).Replace("VM_USER", $remoteUser) |
        Set-Content -LiteralPath $serviceFile -Encoding UTF8

    $productionConfigToUpload = $null
    if ($PromptGoogleSecrets) {
        Write-Host "==> Prompting for Google production secrets..." -ForegroundColor Cyan
        $apiKey = ConvertFrom-SecureStringPlainText (Read-Host "Google API Key" -AsSecureString)
        $searchEngineId = ConvertFrom-SecureStringPlainText (Read-Host "Google Search Engine ID" -AsSecureString)
        $configPath = Join-Path $tempDir "appsettings.Production.json"
        $config = [ordered]@{
            Demo = [ordered]@{ Enabled = $false }
            Providers = [ordered]@{
                SofaScore = [ordered]@{ Enabled = $true; PollingIntervalSeconds = 30 }
                Scores365 = [ordered]@{ Enabled = $true; PollingIntervalSeconds = 20 }
                Google = [ordered]@{
                    Enabled = $true
                    PollingIntervalSeconds = 300
                    ApiKey = $apiKey
                    SearchEngineId = $searchEngineId
                    ResultsPerMatch = 3
                }
                ApiFootball = [ordered]@{ Enabled = $false }
                BetsApi = [ordered]@{ Enabled = $false }
            }
        }
        $config | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $configPath -Encoding UTF8
        $productionConfigToUpload = $configPath
    } elseif (Test-Path $localProductionConfig) {
        $productionConfigToUpload = $localProductionConfig
    }

    Write-Host "==> Uploading app package..." -ForegroundColor Cyan
    Invoke-Gcloud compute scp --recurse $publishDir "${InstanceName}:~/sportsmonitor-upload" `
        --zone $Zone `
        --project $ProjectId

    Write-Host "==> Uploading service and Nginx config..." -ForegroundColor Cyan
    Invoke-Gcloud compute scp $serviceFile "${InstanceName}:~/sportsmonitor.service" `
        --zone $Zone `
        --project $ProjectId
    Invoke-Gcloud compute scp $nginxConf "${InstanceName}:~/nginx-sportsmonitor.conf" `
        --zone $Zone `
        --project $ProjectId

    if ($productionConfigToUpload) {
        Write-Host "==> Uploading production appsettings..." -ForegroundColor Cyan
        Invoke-Gcloud compute scp $productionConfigToUpload "${InstanceName}:~/appsettings.Production.json" `
            --zone $Zone `
            --project $ProjectId
    } else {
        Write-Host "==> No production config uploaded. Existing remote config will be kept if present." -ForegroundColor Yellow
    }

    $remoteCommand = @"
set -e
sudo mkdir -p '$AppDir'
sudo find '$AppDir' -mindepth 1 -maxdepth 1 -exec rm -rf {} +
sudo cp -a "`$HOME/sportsmonitor-upload/." '$AppDir/'
if [ -f "`$HOME/appsettings.Production.json" ]; then sudo cp "`$HOME/appsettings.Production.json" '$AppDir/'; fi
sudo chmod +x '$AppDir/SportsMonitor.Bff'
sudo chmod +x '$AppDir/playwright.sh'
sudo mkdir -p '$AppDir/data'
sudo chown -R '${remoteUser}:${remoteUser}' '$AppDir'
echo "==> Installing Playwright Chromium system dependencies..."
sudo '$AppDir/playwright.sh' install-deps chromium
echo "==> Downloading Chromium browser binary..."
PLAYWRIGHT_BROWSERS_PATH='$AppDir/.playwright' '$AppDir/playwright.sh' install chromium
sudo mv "`$HOME/sportsmonitor.service" /etc/systemd/system/sportsmonitor.service
sudo mv "`$HOME/nginx-sportsmonitor.conf" /etc/nginx/sites-available/sportsmonitor
sudo ln -sfn /etc/nginx/sites-available/sportsmonitor /etc/nginx/sites-enabled/sportsmonitor
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl daemon-reload
sudo systemctl enable sportsmonitor
sudo systemctl restart sportsmonitor
sudo systemctl restart nginx
"@

    Write-Host "==> Installing and restarting app on VM..." -ForegroundColor Cyan
    Invoke-Gcloud compute ssh $InstanceName `
        --zone $Zone `
        --project $ProjectId `
        --command $remoteCommand

    $ip = (& gcloud compute instances describe $InstanceName `
        --zone $Zone `
        --project $ProjectId `
        --format "value(networkInterfaces[0].accessConfigs[0].natIP)").Trim()

    Write-Host ""
    Write-Host "==> Deploy complete" -ForegroundColor Green
    Write-Host "    URL: http://$ip/"
    Write-Host "    Logs: gcloud compute ssh $InstanceName --zone $Zone --project $ProjectId --command `"sudo journalctl -u sportsmonitor -f`""
} finally {
    if (Test-Path $tempDir) {
        Remove-Item $tempDir -Recurse -Force
    }
}

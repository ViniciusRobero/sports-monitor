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
    # PowerShell 5.1 turns any stderr from native executables into ErrorRecords.
    # gcloud forwards nginx stderr ("syntax is ok") which would stop the script.
    # Temporarily silence ErrorAction so stderr output doesn't abort execution;
    # we still check the actual exit code manually.
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    & gcloud @Args
    $code = $LASTEXITCODE
    $ErrorActionPreference = $prev
    if ($code -ne 0) {
        throw "gcloud $($Args -join ' ') failed with exit code $code"
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
                SofaScore = [ordered]@{ Enabled = $false; PollingIntervalSeconds = 30 }
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

    $homeDir = "/home/$remoteUser"

    Write-Host "==> Uploading app package..." -ForegroundColor Cyan
    # pscp always nests source dir inside an existing destination dir,
    # so we create the parent upload dir; the contents land in sportsmonitor-upload/publish-linux/
    Invoke-Gcloud compute ssh $InstanceName --zone $Zone --project $ProjectId --command "rm -rf $homeDir/sportsmonitor-upload && mkdir -p $homeDir/sportsmonitor-upload"
    Invoke-Gcloud compute scp --recurse $publishDir "${InstanceName}:${homeDir}/sportsmonitor-upload" `
        --zone $Zone `
        --project $ProjectId

    Write-Host "==> Uploading service and Nginx config..." -ForegroundColor Cyan
    Invoke-Gcloud compute scp $serviceFile "${InstanceName}:${homeDir}/sportsmonitor.service" `
        --zone $Zone `
        --project $ProjectId
    Invoke-Gcloud compute scp $nginxConf "${InstanceName}:${homeDir}/nginx-sportsmonitor.conf" `
        --zone $Zone `
        --project $ProjectId

    if ($productionConfigToUpload) {
        Write-Host "==> Uploading production appsettings..." -ForegroundColor Cyan
        Invoke-Gcloud compute scp $productionConfigToUpload "${InstanceName}:${homeDir}/appsettings.Production.json" `
            --zone $Zone `
            --project $ProjectId
    } else {
        Write-Host "==> No production config uploaded. Existing remote config will be kept if present." -ForegroundColor Yellow
    }

    $installScript = @"
#!/bin/bash
set -e
sudo mkdir -p '$AppDir'
sudo find '$AppDir' -mindepth 1 -maxdepth 1 -exec rm -rf {} +
sudo cp -a "$homeDir/sportsmonitor-upload/publish-linux/." '$AppDir/'
if [ -f "$homeDir/appsettings.Production.json" ]; then sudo cp "$homeDir/appsettings.Production.json" '$AppDir/'; fi
sudo chmod +x '$AppDir/SportsMonitor.Bff'
sudo mkdir -p '$AppDir/data'
sudo mkdir -p '$AppDir/downloads'
sudo chown -R '${remoteUser}:${remoteUser}' '$AppDir'
sudo mv "$homeDir/sportsmonitor.service" /etc/systemd/system/sportsmonitor.service
sudo mv "$homeDir/nginx-sportsmonitor.conf" /etc/nginx/sites-available/sportsmonitor
sudo ln -sfn /etc/nginx/sites-available/sportsmonitor /etc/nginx/sites-enabled/sportsmonitor
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl daemon-reload
sudo systemctl enable sportsmonitor
sudo systemctl restart sportsmonitor
sudo systemctl restart nginx
"@

    $installScriptPath = Join-Path $tempDir "install.sh"
    # UTF-8 without BOM — required for bash shebang on Linux
    [System.IO.File]::WriteAllText($installScriptPath, $installScript, [System.Text.UTF8Encoding]::new($false))

    Write-Host "==> Uploading install script..." -ForegroundColor Cyan
    Invoke-Gcloud compute scp $installScriptPath "${InstanceName}:${homeDir}/install.sh" `
        --zone $Zone `
        --project $ProjectId

    Write-Host "==> Installing and restarting app on VM..." -ForegroundColor Cyan
    Invoke-Gcloud compute ssh $InstanceName `
        --zone $Zone `
        --project $ProjectId `
        --command "bash $homeDir/install.sh"

    $ip = (& gcloud compute instances describe $InstanceName `
        --zone $Zone `
        --project $ProjectId `
        --format "value(networkInterfaces[0].accessConfigs[0].natIP)").Trim()

    $localAgentZip = Join-Path $root "local-agent.zip"
    if (Test-Path $localAgentZip) {
        Write-Host "==> Uploading LocalAgent zip to /downloads/..." -ForegroundColor Cyan
        Invoke-Gcloud compute scp $localAgentZip "${InstanceName}:${homeDir}/local-agent.zip" `
            --zone $Zone `
            --project $ProjectId
        Invoke-Gcloud compute ssh $InstanceName `
            --zone $Zone `
            --project $ProjectId `
            --command "sudo mv $homeDir/local-agent.zip '$AppDir/downloads/local-agent.zip' && sudo chmod 644 '$AppDir/downloads/local-agent.zip'"
        Write-Host "    Download: http://$ip/downloads/local-agent.zip" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "==> Deploy complete" -ForegroundColor Green
    Write-Host "    URL: http://$ip/"
    Write-Host "    Logs: gcloud compute ssh $InstanceName --zone $Zone --project $ProjectId --command `"sudo journalctl -u sportsmonitor -f`""
} finally {
    if (Test-Path $tempDir) {
        Remove-Item $tempDir -Recurse -Force
    }
}

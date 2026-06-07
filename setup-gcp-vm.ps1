param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectId,

    [string]$InstanceName = "sportsmonitor-vm",
    [string]$Zone = "southamerica-east1-b",
    [string]$MachineType = "e2-small",
    [string]$BootDiskSize = "20GB"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-Gcloud {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Args)
    & gcloud @Args
    if ($LASTEXITCODE -ne 0) {
        throw "gcloud $($Args -join ' ') failed"
    }
}

if (-not (Get-Command gcloud -ErrorAction SilentlyContinue)) {
    throw "gcloud CLI was not found. Install Google Cloud CLI first."
}

Write-Host "==> Setting gcloud project: $ProjectId" -ForegroundColor Cyan
Invoke-Gcloud config set project $ProjectId

Write-Host "==> Enabling required Compute API..." -ForegroundColor Cyan
Invoke-Gcloud services enable compute.googleapis.com

$instanceExists = $false
try {
    $null = gcloud compute instances describe $InstanceName --zone $Zone --project $ProjectId 2>&1
    if ($LASTEXITCODE -eq 0) { $instanceExists = $true }
} catch { $instanceExists = $false }

if (-not $instanceExists) {
    Write-Host "==> Creating VM: $InstanceName" -ForegroundColor Cyan
    Invoke-Gcloud compute instances create $InstanceName `
        --zone $Zone `
        --project $ProjectId `
        --machine-type $MachineType `
        --image-family ubuntu-2204-lts `
        --image-project ubuntu-os-cloud `
        --boot-disk-size $BootDiskSize `
        --tags "http-server,https-server"
} else {
    Write-Host "==> VM already exists: $InstanceName" -ForegroundColor Yellow
}

$firewallExists = $false
try {
    $null = gcloud compute firewall-rules describe allow-sportsmonitor-http --project $ProjectId 2>&1
    if ($LASTEXITCODE -eq 0) { $firewallExists = $true }
} catch { $firewallExists = $false }

if (-not $firewallExists) {
    Write-Host "==> Creating firewall rule for HTTP..." -ForegroundColor Cyan
    Invoke-Gcloud compute firewall-rules create allow-sportsmonitor-http `
        --project $ProjectId `
        --allow tcp:80 `
        --target-tags http-server
} else {
    Write-Host "==> Firewall rule already exists: allow-sportsmonitor-http" -ForegroundColor Yellow
}

Write-Host "==> Installing VM packages via SSH..." -ForegroundColor Cyan
Invoke-Gcloud compute ssh $InstanceName `
    --zone $Zone `
    --project $ProjectId `
    --command "sudo apt-get update && sudo apt-get install -y nginx ca-certificates"


$ip = (& gcloud compute instances describe $InstanceName `
    --zone $Zone `
    --project $ProjectId `
    --format "value(networkInterfaces[0].accessConfigs[0].natIP)").Trim()

Write-Host ""
Write-Host "==> VM ready" -ForegroundColor Green
Write-Host "    Instance: $InstanceName"
Write-Host "    Zone:     $Zone"
Write-Host "    IP:       $ip"
Write-Host "    URL:      http://$ip/"

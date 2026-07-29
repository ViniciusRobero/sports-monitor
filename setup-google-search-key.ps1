param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectId,

    [string]$SearchEngineId = "25c69f98aa10d4ba0",
    [string]$DisplayName = "sportsmonitor-google-search"
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

function Get-JsonProperty {
    param($Object, [string[]]$Paths)

    foreach ($path in $Paths) {
        $current = $Object
        $ok = $true
        foreach ($part in $path.Split('.')) {
            if ($null -eq $current -or -not ($current.PSObject.Properties.Name -contains $part)) {
                $ok = $false
                break
            }
            $current = $current.$part
        }
        if ($ok -and $null -ne $current -and -not [string]::IsNullOrWhiteSpace([string]$current)) {
            return [string]$current
        }
    }

    return $null
}

if (-not (Get-Command gcloud -ErrorAction SilentlyContinue)) {
    throw "gcloud CLI was not found. Install Google Cloud CLI first."
}

Write-Host "==> Setting gcloud project: $ProjectId" -ForegroundColor Cyan
Invoke-Gcloud config set project $ProjectId

Write-Host "==> Enabling required Google APIs..." -ForegroundColor Cyan
Invoke-Gcloud services enable customsearch.googleapis.com apikeys.googleapis.com --project $ProjectId

Write-Host "==> Creating API key restricted to Custom Search JSON API..." -ForegroundColor Cyan
$createdRaw = & gcloud services api-keys create `
    --project $ProjectId `
    --display-name $DisplayName `
    --api-target service=customsearch.googleapis.com `
    --format json

if ($LASTEXITCODE -ne 0) { throw "gcloud services api-keys create failed" }

$created = $createdRaw | ConvertFrom-Json
$keyName = Get-JsonProperty $created @("name", "response.name")
if (-not $keyName) {
    throw "Could not determine created API key resource name."
}

$keyRaw = & gcloud services api-keys get-key-string $keyName --project $ProjectId --format json
if ($LASTEXITCODE -ne 0) { throw "gcloud services api-keys get-key-string failed" }

$apiKey = $null
try {
    $keyJson = $keyRaw | ConvertFrom-Json
    $apiKey = Get-JsonProperty $keyJson @("keyString")
} catch {
    $apiKey = $null
}

if (-not $apiKey) {
    $joined = ($keyRaw -join "`n")
    $match = [regex]::Match($joined, 'AIza[0-9A-Za-z_-]{20,}')
    if ($match.Success) { $apiKey = $match.Value }
}

if (-not $apiKey) {
    throw "Could not determine API key string."
}

$config = [ordered]@{
    Demo = [ordered]@{ Enabled = $false }
    Providers = [ordered]@{
        SofaScore = [ordered]@{ Enabled = $false; PollingIntervalSeconds = 90 }
        Scores365 = [ordered]@{ Enabled = $true; PollingIntervalSeconds = 60 }
        Google = [ordered]@{
            Enabled = $true
            PollingIntervalSeconds = 300
            ApiKey = $apiKey
            SearchEngineId = $SearchEngineId
            ResultsPerMatch = 3
        }
        ApiFootball = [ordered]@{ Enabled = $false }
        BetsApi = [ordered]@{ Enabled = $false }
    }
}

$configPath = Join-Path $PSScriptRoot "appsettings.Production.json"
$config | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $configPath -Encoding UTF8

Write-Host ""
Write-Host "==> Google Custom Search key created and appsettings.Production.json generated." -ForegroundColor Green
Write-Host "    SearchEngineId: $SearchEngineId"
Write-Host "    API key:        AIza********MASKED********"

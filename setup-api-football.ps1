param(
    [ValidateRange(15, 86400)]
    [int]$PollingIntervalSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$configPath = Join-Path $PSScriptRoot "appsettings.Production.json"

Write-Host "==> Configuring API-Football (API-Sports)" -ForegroundColor Cyan
Write-Host "    The key will be stored only in appsettings.Production.json (ignored by Git)."
Write-Host "    Do not paste or send the key in chat."

$secureKey = Read-Host "API key" -AsSecureString
$credential = [System.Net.NetworkCredential]::new("", $secureKey)
$apiKey = $credential.Password.Trim()

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    throw "API key cannot be empty."
}

try {
    if (Test-Path -LiteralPath $configPath) {
        $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    } else {
        $config = [PSCustomObject]@{}
    }

    if ($null -eq $config.PSObject.Properties["Providers"]) {
        $config | Add-Member -NotePropertyName Providers -NotePropertyValue ([PSCustomObject]@{})
    }

    $apiFootball = [PSCustomObject]@{
        Enabled = $true
        PollingIntervalSeconds = $PollingIntervalSeconds
        ApiKey = $apiKey
        BaseUrl = "https://v3.football.api-sports.io"
    }

    if ($null -ne $config.Providers.PSObject.Properties["ApiFootball"]) {
        $config.Providers.ApiFootball = $apiFootball
    } else {
        $config.Providers | Add-Member -NotePropertyName ApiFootball -NotePropertyValue $apiFootball
    }

    $config | ConvertTo-Json -Depth 20 |
        Set-Content -LiteralPath $configPath -Encoding UTF8
} finally {
    $credential.Password = ""
    $apiKey = $null
    $secureKey.Dispose()
}

Write-Host ""
Write-Host "==> API-Football enabled successfully." -ForegroundColor Green
Write-Host "    Config: $configPath"
Write-Host "    Polling interval: $PollingIntervalSeconds seconds"
Write-Host "    Key: ********MASKED********"

if ($PollingIntervalSeconds -eq 60) {
    Write-Host "    Free-plan test window: up to 90 minutes (90 requests + 10 reserved)." -ForegroundColor Yellow
    Write-Warning "Stop the monitor after the test window so it does not consume the remaining daily quota."
} elseif ($PollingIntervalSeconds -lt 900) {
    Write-Warning "This interval can exceed the free plan's 100 requests/day if the monitor stays on continuously."
}

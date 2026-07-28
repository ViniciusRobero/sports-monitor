param(
    [ValidateRange(15, 86400)]
    [int]$PollingIntervalSeconds = 900
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

    if (-not ($config.PSObject.Properties.Name -contains "Providers")) {
        $config | Add-Member -NotePropertyName Providers -NotePropertyValue ([PSCustomObject]@{})
    }

    $apiFootball = [PSCustomObject]@{
        Enabled = $true
        PollingIntervalSeconds = $PollingIntervalSeconds
        ApiKey = $apiKey
        BaseUrl = "https://v3.football.api-sports.io"
    }

    if ($config.Providers.PSObject.Properties.Name -contains "ApiFootball") {
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

if ($PollingIntervalSeconds -lt 900) {
    Write-Warning "This interval can exceed the free plan's 100 requests/day if the monitor stays on for long periods."
}

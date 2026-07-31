Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$serverConfigPath = Join-Path $PSScriptRoot "appsettings.Production.json"
$agentConfigPath = Join-Path $PSScriptRoot "local-agent.appsettings.json"
$publishedAgentConfigPath = Join-Path $PSScriptRoot "publish-local-agent\appsettings.json"

function Read-JsonConfig([string]$Path) {
    if (Test-Path -LiteralPath $Path) {
        return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    }

    return [PSCustomObject]@{}
}

function Set-Property(
    [PSCustomObject]$Object,
    [string]$Name,
    $Value
) {
    if ($null -ne $Object.PSObject.Properties[$Name]) {
        $Object.$Name = $Value
    } else {
        $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
    }
}

Write-Host "==> Configuring the private LocalAgent relay key" -ForegroundColor Cyan
Write-Host "    The key is generated automatically and will not be displayed."

$randomBytes = New-Object byte[] 32
$randomNumberGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()

try {
    $randomNumberGenerator.GetBytes($randomBytes)
    $agentKey = [Convert]::ToBase64String($randomBytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")

    $serverConfig = Read-JsonConfig $serverConfigPath
    $relayOptions = [PSCustomObject]@{ AgentKey = $agentKey }
    Set-Property $serverConfig "RelayOptions" $relayOptions
    $serverConfig | ConvertTo-Json -Depth 20 |
        Set-Content -LiteralPath $serverConfigPath -Encoding UTF8

    $agentConfig = [PSCustomObject]@{
        BffUrl = "http://localhost:5000"
        IntervalSeconds = 90
        AgentKey = $agentKey
        SofaScore = [PSCustomObject]@{
            BaseUrl = "https://api.sofascore.com"
        }
    }
    $agentConfig | ConvertTo-Json -Depth 20 |
        Set-Content -LiteralPath $agentConfigPath -Encoding UTF8

    $publishedAgentDirectory = Split-Path -Parent $publishedAgentConfigPath
    if (Test-Path -LiteralPath $publishedAgentDirectory) {
        Copy-Item -LiteralPath $agentConfigPath -Destination $publishedAgentConfigPath -Force
    }
} finally {
    $randomNumberGenerator.Dispose()
    [Array]::Clear($randomBytes, 0, $randomBytes.Length)
    $agentKey = $null
}

Write-Host ""
Write-Host "==> LocalAgent relay configured successfully." -ForegroundColor Green
Write-Host "    Server config: appsettings.Production.json"
Write-Host "    LocalAgent config: local-agent.appsettings.json"
Write-Host "    Key: ********MASKED********"

#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Registra o SportsMonitor.LocalAgent para iniciar automaticamente no boot do Windows.

.DESCRIPTION
    Cria uma tarefa agendada que roda o LocalAgent como SYSTEM (sem precisar de login),
    com reinicio automatico em caso de falha.

    Execute este script UMA VEZ no notebook portatil, na pasta onde o LocalAgent.exe esta.

.PARAMETER ExePath
    Caminho para SportsMonitor.LocalAgent.exe. Padrao: .\SportsMonitor.LocalAgent.exe

.EXAMPLE
    .\Register-Startup.ps1
    .\Register-Startup.ps1 -ExePath "C:\SportsMonitor\SportsMonitor.LocalAgent.exe"

.NOTES
    Para parar:   Stop-ScheduledTask  "SportsMonitor LocalAgent"
    Para iniciar: Start-ScheduledTask "SportsMonitor LocalAgent"
    Para remover: Unregister-ScheduledTask "SportsMonitor LocalAgent" -Confirm:$false
#>
param(
    [string]$ExePath = ".\SportsMonitor.LocalAgent.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$resolved = Resolve-Path $ExePath -ErrorAction Stop | Select-Object -ExpandProperty Path
$workDir  = Split-Path $resolved

$taskName = "SportsMonitor LocalAgent"

$action    = New-ScheduledTaskAction -Execute $resolved -WorkingDirectory $workDir
$trigger   = New-ScheduledTaskTrigger -AtStartup
$settings  = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -RestartCount 5 `
    -RestartInterval (New-TimeSpan -Minutes 2) `
    -ExecutionTimeLimit ([TimeSpan]::Zero)   # sem timeout
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -RunLevel Highest

Register-ScheduledTask `
    -TaskName  $taskName `
    -Action    $action `
    -Trigger   $trigger `
    -Settings  $settings `
    -Principal $principal `
    -Force | Out-Null

Write-Host ""
Write-Host "==> LocalAgent registrado no Task Scheduler." -ForegroundColor Green
Write-Host "    Executavel: $resolved"
Write-Host "    Pasta de trabalho: $workDir"
Write-Host ""
Write-Host "  Iniciando agora..."
Start-ScheduledTask $taskName

Start-Sleep -Seconds 2
$state = (Get-ScheduledTask $taskName).State
Write-Host "  Status: $state" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Comandos uteis:"
Write-Host "    Parar:   Stop-ScheduledTask  '$taskName'"
Write-Host "    Iniciar: Start-ScheduledTask '$taskName'"
Write-Host "    Remover: Unregister-ScheduledTask '$taskName' -Confirm:`$false"

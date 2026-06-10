@echo off
setlocal EnableDelayedExpansion
title SportsMonitor LocalAgent
color 0A

:: === Configuracoes ===
set "REPO_URL=https://github.com/ViniciusRobero/sports-monitor.git"
set "EXE=%~dp0SportsMonitor.LocalAgent.exe"
set "LOCAL_PROJECT=%~dp0src\SportsMonitor.LocalAgent"
set "SETTINGS=%~dp0appsettings.json"
set "LOG_DIR=%~dp0logs"
set "RESTART_DELAY=10"

if not exist "%LOG_DIR%\" mkdir "%LOG_DIR%"

echo.
echo  ============================================================
echo   SportsMonitor LocalAgent
echo  ============================================================
echo.
echo   O QUE ESSE PROGRAMA FAZ:
echo   Roda em segundo plano no seu PC e busca dados de futebol
echo   ao vivo no SofaScore, enviando para o servidor na nuvem.
echo   Necessario porque o servidor (GCP) tem IP bloqueado pelo
echo   SofaScore — o seu PC nao tem essa restricao.
echo.
echo   COMO USAR:
echo   - Deixe esta janela aberta enquanto quiser monitorar jogos
echo   - "relayed X matches" = funcionando, X partidas ao vivo
echo   - "relayed 0 matches" = normal, nao ha jogos ao vivo agora
echo   - Para parar: feche esta janela ou pressione Ctrl+C
echo   - Se cair por qualquer motivo, reinicia sozinho em 10s
echo.
echo   PRIMEIRA VEZ: vai baixar o Chrome (~150MB). Aguarde.
echo  ============================================================
echo.

:: ============================================================
:: PASSO 1: encontrar ou compilar o executavel
:: ============================================================
if exist "%EXE%" goto :run

echo  [SETUP] Executavel nao encontrado. Iniciando configuracao...
echo.

if exist "%LOCAL_PROJECT%\" (
    echo  [SETUP] Projeto local detectado. Compilando...
    call :compile "%LOCAL_PROJECT%"
    if errorlevel 1 goto :erro_compilacao
    goto :run
)

if exist "C:\SportsMonitor\src\SportsMonitor.LocalAgent\" (
    echo  [SETUP] Projeto encontrado em C:\SportsMonitor. Compilando...
    call :compile "C:\SportsMonitor\src\SportsMonitor.LocalAgent"
    if errorlevel 1 goto :erro_compilacao
    goto :run
)

:: Nada encontrado — mostrar tutorial
echo  ============================================================
echo   PRIMEIRO USO — Siga os passos abaixo:
echo  ============================================================
echo.
echo   1. Abra outro terminal e rode:
echo.
echo      git clone %REPO_URL% C:\SportsMonitor
echo.
echo   2. Volte aqui e pressione ENTER para continuar.
echo.
echo   (Se nao tiver git: instale em https://git-scm.com/download/win)
echo.
pause

if not exist "C:\SportsMonitor\src\SportsMonitor.LocalAgent\" (
    echo.
    echo  [ERRO] Repositorio nao encontrado em C:\SportsMonitor
    echo         Verifique se o clone foi concluido corretamente.
    pause
    exit /b 1
)

echo  [SETUP] Verificando .NET SDK...
where dotnet >nul 2>&1
if errorlevel 1 (
    echo.
    echo  [ERRO] .NET 10 SDK nao encontrado.
    echo         Instale em: https://dot.net/download
    echo         Apos instalar, feche e reabra este arquivo.
    pause
    exit /b 1
)

echo  [SETUP] Compilando (aguarde ~1-2 minutos)...
call :compile "C:\SportsMonitor\src\SportsMonitor.LocalAgent"
if errorlevel 1 goto :erro_compilacao

:: ============================================================
:: EXECUCAO EM LOOP (reinicia automaticamente se cair)
:: ============================================================
:run
echo.
echo  [OK] Tudo pronto! Iniciando monitoramento...
echo.

:loop
    :: Nome do log pelo dia atual (PowerShell garante formato fixo independente do locale)
    for /f %%d in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd"') do set "LOGDATE=%%d"
    set "SM_LOG=%LOG_DIR%\agent-!LOGDATE!.log"

    echo [%time%] Iniciando LocalAgent...
    echo [%date% %time%] === INICIO === >> "!SM_LOG!"

    :: Executa o agent — output aparece no console, eventos gravados no log
    "%EXE%"
    set "EXIT_CODE=!ERRORLEVEL!"

    echo.
    echo [%time%] Processo encerrado (codigo: !EXIT_CODE!).
    echo [%date% %time%] === FIM (codigo !EXIT_CODE!) === >> "!SM_LOG!"
    echo [%time%] Reiniciando em %RESTART_DELAY%s... (feche a janela para parar)
    echo.
    timeout /t %RESTART_DELAY% /nobreak > nul
goto loop

:: ============================================================
:: Sub-rotinas
:: ============================================================
:compile
    dotnet publish "%~1" ^
        -c Release -r win-x64 --self-contained true ^
        -p:PublishSingleFile=true ^
        -o "%~dp0" --nologo -v q
    if errorlevel 1 exit /b 1
    if not exist "%SETTINGS%" (
        copy /y "%~1\appsettings.json" "%SETTINGS%" >nul 2>&1
        echo  [SETUP] appsettings.json criado.
    )
    echo  [SETUP] Compilado com sucesso!
exit /b 0

:erro_compilacao
    echo.
    echo  [ERRO] Falha na compilacao.
    echo         Verifique se o .NET 10 SDK esta instalado: https://dot.net/download
    pause
    exit /b 1

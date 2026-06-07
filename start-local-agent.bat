@echo off
setlocal EnableDelayedExpansion
title SportsMonitor LocalAgent
color 0A

:: === Configuracoes ===
set "REPO_URL=https://github.com/ViniciusRobero/sports-monitor.git"
set "INSTALL_DIR=C:\SportsMonitor"
set "EXE=%~dp0SportsMonitor.LocalAgent.exe"
set "LOCAL_PROJECT=%~dp0src\SportsMonitor.LocalAgent"
set "SETTINGS=%~dp0appsettings.json"
set "LOG_DIR=%~dp0logs"
set "RESTART_DELAY=10"

if not exist "%LOG_DIR%\" mkdir "%LOG_DIR%"

echo.
echo  ============================================
echo   SportsMonitor LocalAgent
echo  ============================================
echo.

:: ============================================================
:: PASSO 1: encontrar ou compilar o executavel
:: ============================================================
if exist "%EXE%" goto :run

echo  [SETUP] Executavel nao encontrado. Iniciando configuracao...
echo.

:: Caso A: rodando de dentro do repositorio (ex: C:\projetoBets\)
if exist "%LOCAL_PROJECT%\" (
    echo  [SETUP] Repositorio local detectado. Compilando...
    call :compile "%LOCAL_PROJECT%"
    if errorlevel 1 goto :erro_compilacao
    goto :run
)

:: Caso B: repositorio em C:\SportsMonitor
if exist "%INSTALL_DIR%\src\SportsMonitor.LocalAgent\" (
    echo  [SETUP] Repositorio encontrado em %INSTALL_DIR%. Compilando...
    call :compile "%INSTALL_DIR%\src\SportsMonitor.LocalAgent"
    if errorlevel 1 goto :erro_compilacao
    goto :run
)

:: Caso C: nada encontrado — mostrar tutorial e baixar
echo  ============================================================
echo   PRIMEIRO USO — Siga os passos abaixo:
echo  ============================================================
echo.
echo   1. Abra outro terminal (PowerShell ou cmd) e rode:
echo.
echo      git clone %REPO_URL% %INSTALL_DIR%
echo.
echo   2. Volte aqui e pressione ENTER para continuar.
echo.
echo   (Se nao tiver git: instale em https://git-scm.com/download/win)
echo.
pause

if not exist "%INSTALL_DIR%\src\SportsMonitor.LocalAgent\" (
    echo.
    echo  [ERRO] Repositorio nao encontrado em %INSTALL_DIR%
    echo         Verifique se o clone foi concluido corretamente.
    pause
    exit /b 1
)

echo.
echo  [SETUP] Repositorio encontrado. Verificando .NET SDK...
where dotnet >nul 2>&1
if errorlevel 1 (
    echo.
    echo  [ERRO] .NET 10 SDK nao encontrado.
    echo         Instale em: https://dot.net/download  ^(escolha .NET 10 SDK^)
    echo         Apos instalar, feche e reabra este arquivo.
    pause
    exit /b 1
)

echo  [SETUP] Compilando LocalAgent (aguarde ~1-2 minutos)...
call :compile "%INSTALL_DIR%\src\SportsMonitor.LocalAgent"
if errorlevel 1 goto :erro_compilacao

:: ============================================================
:: EXECUCAO EM LOOP (reinicia automaticamente se cair)
:: ============================================================
:run
echo.
echo  [OK] LocalAgent pronto. Iniciando em loop...
echo       Logs em: %LOG_DIR%
echo       Para parar: feche esta janela.
echo.

:loop
    for /f "tokens=2 delims==" %%d in ('wmic os get LocalDateTime /value 2^>nul') do set "WMIDT=%%d"
    if defined WMIDT (
        set "LOGDATE=!WMIDT:~0,4!-!WMIDT:~4,2!-!WMIDT:~6,2!"
    ) else (
        set "LOGDATE=%date:~6,4%-%date:~3,2%-%date:~0,2%"
    )
    set "SM_LOG=%LOG_DIR%\agent-!LOGDATE!.log"

    echo [%time%] Iniciando...
    echo [%date% %time%] === INICIO === >> "!SM_LOG!"

    set "SM_EXE=%EXE%"
    powershell -NoProfile -Command ^
        "& { & $env:SM_EXE *>&1 | Tee-Object -FilePath $env:SM_LOG -Append }"

    echo [%time%] Encerrado. Reiniciando em %RESTART_DELAY%s...
    echo [%date% %time%] Encerrado (reiniciando). >> "!SM_LOG!"
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
        echo  [SETUP] appsettings.json criado. Edite BffUrl se necessario.
    )
    echo  [SETUP] Pronto!
exit /b 0

:erro_compilacao
    echo.
    echo  [ERRO] Falha na compilacao.
    echo         Verifique se o .NET 10 SDK esta instalado: https://dot.net/download
    pause
    exit /b 1

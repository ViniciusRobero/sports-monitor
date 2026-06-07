@echo off
setlocal EnableDelayedExpansion
title SportsMonitor LocalAgent
color 0A

:: === Configuracoes ===
set "REPO_URL=https://github.com/ViniciusRobero/sports-monitor.git"
set "EXE=%~dp0SportsMonitor.LocalAgent.exe"
set "SETTINGS=%~dp0appsettings.json"
set "LOG_DIR=%~dp0logs"
set "RESTART_DELAY=10"

:: === Criar pasta de logs ===
if not exist "%LOG_DIR%\" mkdir "%LOG_DIR%"

:: === Banner ===
echo ============================================
echo  SportsMonitor LocalAgent
echo ============================================
echo  Exe:  %EXE%
echo  Logs: %LOG_DIR%
echo  Para parar: feche esta janela ou Ctrl+C
echo ============================================
echo.

:: === Instalar se necessario ===
if not exist "%EXE%" (
    echo [SETUP] SportsMonitor.LocalAgent.exe nao encontrado nesta pasta.
    call :try_build
    if errorlevel 1 (
        echo.
        echo [ERRO] Nao foi possivel instalar o LocalAgent automaticamente.
        echo        Opcao manual: copie SportsMonitor.LocalAgent.exe para esta pasta.
        echo        GitHub: %REPO_URL%
        pause
        exit /b 1
    )
    echo.
)

:: === Loop de execucao com reinicio automatico ===
:loop
    :: Determina nome do log pelo dia (WMIC com fallback)
    for /f "tokens=2 delims==" %%d in ('wmic os get LocalDateTime /value 2^>nul') do set "WMIDT=%%d"
    if defined WMIDT (
        set "LOGDATE=!WMIDT:~0,4!-!WMIDT:~4,2!-!WMIDT:~6,2!"
    ) else (
        set "LOGDATE=%date:~6,4%-%date:~3,2%-%date:~0,2%"
    )
    set "SM_LOG=%LOG_DIR%\agent-!LOGDATE!.log"

    echo [%time%] Iniciando LocalAgent...
    echo [%date% %time%] =========== INICIO =========== >> "!SM_LOG!"

    :: Usa variaveis de ambiente para evitar problemas com espacos nos caminhos
    set "SM_EXE=%EXE%"
    powershell -NoProfile -Command ^
        "& { & $env:SM_EXE *>&1 | Tee-Object -FilePath $env:SM_LOG -Append }"

    echo [%time%] Processo encerrado. Reiniciando em %RESTART_DELAY%s...
    echo [%date% %time%] Processo encerrado (reiniciando em %RESTART_DELAY%s). >> "!SM_LOG!"
    timeout /t %RESTART_DELAY% /nobreak > nul
goto loop

:: ============================================================
:: Sub-rotina: clonar repositorio e compilar o LocalAgent
:: ============================================================
:try_build
    echo [SETUP] Verificando prerequisitos (git + .NET 10 SDK)...

    where git >nul 2>&1
    if errorlevel 1 (
        echo [ERRO] git nao encontrado.
        echo        Instale em: https://git-scm.com/download/win
        exit /b 1
    )

    where dotnet >nul 2>&1
    if errorlevel 1 (
        echo [ERRO] .NET SDK nao encontrado.
        echo        Instale em: https://dot.net/download  (escolha .NET 10 SDK)
        exit /b 1
    )

    set "CLONE_DIR=%TEMP%\sm-agent-clone"
    echo [SETUP] Clonando repositorio (apenas ultima versao, pode demorar ~30s)...
    if exist "!CLONE_DIR!" rmdir /s /q "!CLONE_DIR!"
    git clone "%REPO_URL%" "!CLONE_DIR!" --depth 1 -q
    if errorlevel 1 (
        echo [ERRO] Clone falhou. Verifique conexao com a internet e tente novamente.
        exit /b 1
    )

    echo [SETUP] Compilando LocalAgent (aguarde ~1-2 minutos)...
    set "PUB_PROJECT=!CLONE_DIR!\src\SportsMonitor.LocalAgent"
    dotnet publish "!PUB_PROJECT!" ^
        -c Release -r win-x64 --self-contained true ^
        -p:PublishSingleFile=true ^
        -o "%~dp0" --nologo -v q
    if errorlevel 1 (
        echo [ERRO] Compilacao falhou. Verifique se o .NET 10 SDK esta instalado corretamente.
        exit /b 1
    )

    :: Copia config padrao apenas se ainda nao existir
    if not exist "%SETTINGS%" (
        copy /y "!CLONE_DIR!\src\SportsMonitor.LocalAgent\appsettings.json" "%SETTINGS%" >nul
        echo [SETUP] appsettings.json criado. Edite o campo BffUrl se necessario.
    )

    rmdir /s /q "!CLONE_DIR!" 2>nul
    echo [SETUP] LocalAgent instalado com sucesso!
exit /b 0

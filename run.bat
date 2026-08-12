@echo off
rem ---------------------------------------------------------------------------
rem  Technical Inspection PoC - one-command startup.
rem
rem  1. makes sure the Podman machine is up
rem  2. starts (or creates) the SQL Server 2022 container
rem  3. waits until SQL Server actually accepts connections
rem  4. applies EF Core migrations and seeds master data
rem  5. runs the web application
rem
rem  Usage:  run.bat            full sequence
rem          run.bat web        skip the database steps, just run the app
rem ---------------------------------------------------------------------------

setlocal

cd /d "%~dp0"

set SQL_CONTAINER=ti-poc-sql
set SQL_VOLUME=ti-poc-sqldata
set SQL_IMAGE=mcr.microsoft.com/mssql/server:2022-latest
set SQL_PORT=14330
set SQL_PASSWORD=Str0ng!Passw0rd
set APP_URL=https://localhost:44350

if /i "%~1"=="web" goto :runweb

echo.
echo === 1/4  Checking Podman =================================================
where podman >nul 2>&1
if errorlevel 1 (
    echo [ERROR] podman was not found on PATH.
    echo         Install Podman, or start SQL Server yourself and run: run.bat web
    exit /b 1
)

podman info >nul 2>&1
if errorlevel 1 (
    echo Podman machine is not running - starting it ^(this can take a minute^)...
    podman machine start
    if errorlevel 1 (
        echo [ERROR] Could not start the Podman machine.
        exit /b 1
    )
)

echo.
echo === 2/4  SQL Server container ============================================
podman container exists %SQL_CONTAINER% >nul 2>&1
if errorlevel 1 (
    echo Creating container %SQL_CONTAINER% on host port %SQL_PORT% ...
    podman run -d --name %SQL_CONTAINER% -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=%SQL_PASSWORD%" -p %SQL_PORT%:1433 -v %SQL_VOLUME%:/var/opt/mssql %SQL_IMAGE%
    if errorlevel 1 (
        echo [ERROR] Could not create the SQL Server container.
        exit /b 1
    )
) else (
    echo Starting existing container %SQL_CONTAINER% ...
    podman start %SQL_CONTAINER% >nul
    if errorlevel 1 (
        echo [ERROR] Could not start the SQL Server container.
        exit /b 1
    )
)

echo Waiting for SQL Server to accept connections ...
set /a ATTEMPT=0

:waitsql
set /a ATTEMPT+=1
podman exec %SQL_CONTAINER% /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "%SQL_PASSWORD%" -C -Q "SELECT 1" >nul 2>&1
if not errorlevel 1 goto :sqlready
if %ATTEMPT% GEQ 40 (
    echo [ERROR] SQL Server did not become ready in time.
    echo         Check the logs with:  podman logs %SQL_CONTAINER%
    exit /b 1
)
rem ping is used rather than `timeout`, which aborts with
rem "Input redirection is not supported" when stdout/stdin are redirected.
ping -n 3 127.0.0.1 >nul
goto :waitsql

:sqlready
echo SQL Server is ready.

echo.
echo === 3/4  Database migrations and seeding =================================
dotnet run --project src\TechnicalInspection.PoC.DbMigrator --environment Development
if errorlevel 1 (
    echo [ERROR] The database migrator failed.
    exit /b 1
)

:runweb
echo.
echo === 4/4  Starting the web application ====================================
echo.
echo   URL       %APP_URL%
echo   Login     admin / 1q2w3E*
echo   Demo      %APP_URL%/Requests
echo.
echo   Press Ctrl+C to stop. The SQL container keeps running; stop it with:
echo       podman stop %SQL_CONTAINER%
echo.

dotnet run --project src\TechnicalInspection.PoC.Web --environment Development

endlocal

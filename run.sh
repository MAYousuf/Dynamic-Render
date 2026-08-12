#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  Technical Inspection PoC - one-command startup.
#
#  1. makes sure the Podman machine is up
#  2. starts (or creates) the SQL Server 2022 container
#  3. waits until SQL Server actually accepts connections
#  4. applies EF Core migrations and seeds master data
#  5. runs the web application
#
#  Usage:  ./run.sh            full sequence
#          ./run.sh web        skip the database steps, just run the app
#
#  The bash counterpart of run.bat; both must be kept in step.
# ---------------------------------------------------------------------------

set -euo pipefail

cd "$(dirname "$0")"

SQL_CONTAINER=ti-poc-sql
SQL_VOLUME=ti-poc-sqldata
SQL_IMAGE=mcr.microsoft.com/mssql/server:2022-latest
SQL_PORT=14330
SQL_PASSWORD='Str0ng!Passw0rd'
APP_URL=https://localhost:44350

run_web() {
    echo
    echo "=== 4/4  Starting the web application ===================================="
    echo
    echo "  URL       $APP_URL"
    echo "  Login     admin / 1q2w3E*"
    echo "  Demo      $APP_URL/Requests"
    echo
    echo "  Press Ctrl+C to stop. The SQL container keeps running; stop it with:"
    echo "      podman stop $SQL_CONTAINER"
    echo

    exec dotnet run --project src/TechnicalInspection.PoC.Web
}

if [ "${1:-}" = "web" ]; then
    run_web
fi

echo
echo "=== 1/4  Checking Podman ================================================="

if ! command -v podman >/dev/null 2>&1; then
    echo "[ERROR] podman was not found on PATH."
    echo "        Install Podman, or start SQL Server yourself and run: ./run.sh web"
    exit 1
fi

if ! podman info >/dev/null 2>&1; then
    echo "Podman machine is not running - starting it (this can take a minute)..."
    if ! podman machine start; then
        echo "[ERROR] Could not start the Podman machine."
        exit 1
    fi
fi

echo
echo "=== 2/4  SQL Server container ============================================"

if podman container exists "$SQL_CONTAINER" >/dev/null 2>&1; then
    echo "Starting existing container $SQL_CONTAINER ..."
    if ! podman start "$SQL_CONTAINER" >/dev/null; then
        echo "[ERROR] Could not start the SQL Server container."
        exit 1
    fi
else
    echo "Creating container $SQL_CONTAINER on host port $SQL_PORT ..."
    if ! podman run -d --name "$SQL_CONTAINER" \
        -e "ACCEPT_EULA=Y" \
        -e "MSSQL_SA_PASSWORD=$SQL_PASSWORD" \
        -p "$SQL_PORT:1433" \
        -v "$SQL_VOLUME:/var/opt/mssql" \
        "$SQL_IMAGE"; then
        echo "[ERROR] Could not create the SQL Server container."
        exit 1
    fi
fi

echo "Waiting for SQL Server to accept connections ..."

attempt=0
until podman exec "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$SQL_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; do
    attempt=$((attempt + 1))
    if [ "$attempt" -ge 40 ]; then
        echo "[ERROR] SQL Server did not become ready in time."
        echo "        Check the logs with:  podman logs $SQL_CONTAINER"
        exit 1
    fi
    sleep 3
done

echo "SQL Server is ready."

echo
echo "=== 3/4  Database migrations and seeding ================================="

if ! dotnet run --project src/TechnicalInspection.PoC.DbMigrator; then
    echo "[ERROR] The database migrator failed."
    exit 1
fi

run_web

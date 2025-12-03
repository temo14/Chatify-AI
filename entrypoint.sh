#!/bin/bash
set -e

echo "Waiting for SQL Server to be ready..."
until /opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -P "YourStrong@Password123" -Q "SELECT 1" -C > /dev/null 2>&1; do
  echo "SQL Server is unavailable - sleeping"
  sleep 2
done

echo "SQL Server is up - starting application (migrations will run automatically)"
exec dotnet ChatAI.Api.dll

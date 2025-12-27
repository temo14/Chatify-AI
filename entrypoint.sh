#!/bin/bash
set -e

echo "Starting Chatify AI application..."
echo "Environment: ${ASPNETCORE_ENVIRONMENT:-Production}"
echo "Port: ${ASPNETCORE_URLS:-http://+:8080}"

# Start the application
exec dotnet ChatAI.Api.dll

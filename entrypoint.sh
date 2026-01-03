#!/bin/bash
set -e

# Wait for dependencies to be ready (if needed)
echo "Starting Chatify AI application..."

# Run database migrations on startup
echo "Running database migrations..."
dotnet ChatAI.Api.dll --migrate || echo "Migration failed or not needed"

# Start the application
echo "Starting application..."
exec dotnet ChatAI.Api.dll

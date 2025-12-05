# Multi-stage build for optimized image size
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files (layer caching optimization)
COPY ["Chatify AI.sln", "./"]
COPY ["ChatAI.Api/ChatAI.Api.csproj", "ChatAI.Api/"]
COPY ["ChatAI.Application/ChatAI.Application.csproj", "ChatAI.Application/"]
COPY ["ChatAI.Domain/ChatAI.Domain.csproj", "ChatAI.Domain/"]
COPY ["ChatAI.Infrastructure/ChatAI.Infrastructure.csproj", "ChatAI.Infrastructure/"]

# Restore dependencies (cached if csproj files unchanged)
RUN dotnet restore "Chatify AI.sln"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/ChatAI.Api"
RUN dotnet build "ChatAI.Api.csproj" -c Release -o /app/build --no-restore

# Publish the application (optimized for production)
FROM build AS publish
RUN dotnet publish "ChatAI.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --no-build \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false \
    /p:PublishReadyToRun=true

# Final stage - runtime image (minimal)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN groupadd -r chatai && useradd -r -g chatai chatai

# Install dependencies
USER root
RUN apt-get update && apt-get install -y \
    curl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=publish /app/publish .

# Copy entrypoint script
COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh && \
    chown -R chatai:chatai /app

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Switch to non-root user
USER chatai

ENTRYPOINT ["/app/entrypoint.sh"]

# Multi-stage build for optimized image size
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY ["Chatify AI.sln", "./"]
COPY ["ChatAI.Api/ChatAI.Api.csproj", "ChatAI.Api/"]
COPY ["ChatAI.Application/ChatAI.Application.csproj", "ChatAI.Application/"]
COPY ["ChatAI.Domain/ChatAI.Domain.csproj", "ChatAI.Domain/"]
COPY ["ChatAI.Infrastructure/ChatAI.Infrastructure.csproj", "ChatAI.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Chatify AI.sln"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/ChatAI.Api"
RUN dotnet build "ChatAI.Api.csproj" -c Release -o /app/build

# Install dotnet-ef tool in build stage
RUN dotnet tool install --global dotnet-ef --version 10.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"

# Publish the application
FROM build AS publish
RUN dotnet publish "ChatAI.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install SQL Server tools for healthcheck
USER root
RUN apt-get update && apt-get install -y curl apt-transport-https gnupg2 && \
    curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
    curl https://packages.microsoft.com/config/debian/11/prod.list > /etc/apt/sources.list.d/mssql-release.list && \
    apt-get update && \
    ACCEPT_EULA=Y apt-get install -y msodbcsql18 mssql-tools18 && \
    echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=publish /app/publish .

# Copy dotnet-ef tool from build stage
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy entrypoint script
COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["/app/entrypoint.sh"]

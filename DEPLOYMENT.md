# Azure Deployment Guide - ChatifyAI

## Quick Deploy (One Command)
```powershell
.\deploy-to-azure.ps1
```

## Manual Deployment Steps

### 1. Build and Push Docker Image
```bash
# Navigate to project
cd "C:\Users\tbaindurashvili\source\repos\Chatify AI"

# Login to ACR
az acr login --name chatifyregistry

# Build image
docker build -t chatifyregistry.azurecr.io/chatify-api:latest -f Dockerfile .

# Push image
docker push chatifyregistry.azurecr.io/chatify-api:latest
```

### 2. Update Container App
```bash
az containerapp update \
    --name chatify-api \
    --resource-group chatify-prod-rg \
    --image chatifyregistry.azurecr.io/chatify-api:latest
```

## View Logs
```bash
# Follow logs in real-time
az containerapp logs show -n chatify-api -g chatify-prod-rg --follow

# View recent logs
az containerapp logs show -n chatify-api -g chatify-prod-rg --tail 100
```

## Run Database Migration
```bash
# Connect to container
az containerapp exec -n chatify-api -g chatify-prod-rg --command /bin/bash

# Inside container, run:
dotnet ef database update --project /app/ChatAI.Infrastructure.dll --startup-project /app/ChatAI.Api.dll
```

## Environment Check
```bash
# Check app status
az containerapp show -n chatify-api -g chatify-prod-rg --query "properties.runningStatus"

# Check health endpoint
curl https://chatify-api.nicesky-e1e1b24e.eastus.azurecontainerapps.io/health
```

## Azure Resources
- **Resource Group:** chatify-prod-rg
- **Container App:** chatify-api
- **Registry:** chatifyregistry.azurecr.io
- **Key Vault:** chatify-kv-4021
- **App URL:** https://chatify-api.nicesky-e1e1b24e.eastus.azurecontainerapps.io

## Troubleshooting

### Check if image is in registry
```bash
az acr repository show-tags --name chatifyregistry --repository chatify-api
```

### Restart container app
```bash
az containerapp revision restart -n chatify-api -g chatify-prod-rg
```

### View environment variables
```bash
az containerapp show -n chatify-api -g chatify-prod-rg --query "properties.template.containers[0].env"
```

---
**Last Updated:** January 4, 2026

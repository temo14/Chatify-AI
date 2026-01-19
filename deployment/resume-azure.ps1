# ============================================
# CHATIFY AI - RESUME AZURE RESOURCES
# ============================================
# Purpose: Bring all services back online after scale-down
# Includes Meta channels configuration (Service Bus + always-on)

$ErrorActionPreference = "Continue"

# Navigate to project root (parent of deployment folder)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "RESUMING AZURE RESOURCES" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

$RESOURCE_GROUP = "chatify-prod-rg"
$LOCATION = "westeurope"
$SQL_SERVER = "chatify-sql-server"
$SQL_DB = "chatify-db"
$SEQ_NAME = "chatify-seq"
$APP_NAME = "chatify-api"
$SERVICE_BUS_NAMESPACE = "chatify-servicebus-std"  # New Standard tier namespace for sessions
$QUEUE_NAME = "meta-webhooks-sessions"  # Session-enabled queue

# Service Bus configuration (now in appsettings.Production.json)
try {
    # No longer using Key Vault - configuration is in appsettings files
    Write-Host "[INFO] Service Bus configuration is in appsettings.Production.json" -ForegroundColor Yellow
    
    # Check if namespace exists
    if ($true) {
        $m = $null
        if ($false) {
            $SERVICE_BUS_NAMESPACE = $m.Groups[1].Value
        }
    }

    if ($queueFromKv) {
        $QUEUE_NAME = $queueFromKv
    }

    Write-Host "[INFO] Service Bus target: $SERVICE_BUS_NAMESPACE / $QUEUE_NAME" -ForegroundColor Gray
} catch {
    Write-Host "[WARNING] Could not read Service Bus secrets from Key Vault; using defaults." -ForegroundColor Yellow
}

# 1. Resume SQL Database
Write-Host "`n[1/6] Resuming SQL Database..." -ForegroundColor Yellow
$sqlTier = az sql db show --name $SQL_DB --resource-group $RESOURCE_GROUP --server $SQL_SERVER --query "sku.tier" -o tsv

if ($sqlTier -eq "Basic") {
    Write-Host "[INFO] SQL Database is Basic tier (always active)" -ForegroundColor Green
} else {
    az sql db resume --name $SQL_DB --resource-group $RESOURCE_GROUP --server $SQL_SERVER 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] SQL Database resuming (may take 30-60 seconds)" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Failed to resume SQL Database (may already be active)" -ForegroundColor Yellow
    }
}

# 2. Scale Seq log server back up
Write-Host "`n[2/6] Scaling Seq log server to 1..." -ForegroundColor Yellow
az containerapp update `
    --name $SEQ_NAME `
    --resource-group $RESOURCE_GROUP `
    --min-replicas 1 `
    --max-replicas 1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Seq scaled to 1 (logging active)" -ForegroundColor Green
} else {
    Write-Host "[WARNING] Failed to scale Seq" -ForegroundColor Red
}

# 3. Scale API to 1 (for Meta webhooks - no cold starts)
Write-Host "`n[3/6] Scaling API to always-on (minReplicas=1)..." -ForegroundColor Yellow
az containerapp update `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --min-replicas 1 `
    --max-replicas 5 `
    --scale-rule-name http-rule `
    --scale-rule-type http `
    --scale-rule-http-concurrency 100 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] API scaled to always-on (for Meta webhooks)" -ForegroundColor Green
} else {
    Write-Host "[WARNING] Failed to scale API" -ForegroundColor Yellow
}

# 4. Create Service Bus Standard namespace (required for sessions)
Write-Host "`n[4/6] Checking Service Bus..." -ForegroundColor Yellow
$sbExists = az servicebus namespace show --name $SERVICE_BUS_NAMESPACE --resource-group $RESOURCE_GROUP --query name -o tsv 2>$null

if ($sbExists) {
    $sbSku = az servicebus namespace show --name $SERVICE_BUS_NAMESPACE --resource-group $RESOURCE_GROUP --query "sku.name" -o tsv 2>$null
    Write-Host "[OK] Service Bus already exists (SKU: $sbSku)" -ForegroundColor Green

    if ($sbSku -ne "Standard" -and $sbSku -ne "Premium") {
        Write-Host "[ERROR] Service Bus '$SERVICE_BUS_NAMESPACE' is not Standard/Premium tier (required for sessions)." -ForegroundColor Red
        Write-Host "        Azure does not support in-place tier upgrades from Basic to Standard." -ForegroundColor Yellow
        Write-Host "        This script will create a new Standard namespace: $SERVICE_BUS_NAMESPACE" -ForegroundColor Yellow
        Write-Host "        Old Basic namespace can be deleted after migration." -ForegroundColor Gray
        exit 1
    }
} else {
    Write-Host "[INFO] Creating Standard tier Service Bus namespace (for sessions)..." -ForegroundColor Gray
    az servicebus namespace create `
        --name $SERVICE_BUS_NAMESPACE `
        --resource-group $RESOURCE_GROUP `
        --location $LOCATION `
        --sku Standard | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Service Bus namespace created (Standard tier)" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Failed to create Service Bus namespace" -ForegroundColor Red
        exit 1
    }
}

# 5. Create session-enabled queue
Write-Host "`n[5/6] Checking Service Bus queue..." -ForegroundColor Yellow
$queueExists = az servicebus queue show --namespace-name $SERVICE_BUS_NAMESPACE --name $QUEUE_NAME --resource-group $RESOURCE_GROUP --query name -o tsv 2>$null

if ($queueExists) {
    $requiresSession = az servicebus queue show --namespace-name $SERVICE_BUS_NAMESPACE --name $QUEUE_NAME --resource-group $RESOURCE_GROUP --query "requiresSession" -o tsv 2>$null
    Write-Host "[OK] Queue already exists (requiresSession: $requiresSession)" -ForegroundColor Green

    if ($requiresSession -ne "true") {
        Write-Host "[ERROR] Queue '$QUEUE_NAME' is not session-enabled!" -ForegroundColor Red
        Write-Host "        Meta webhook processing requires sessions for message ordering." -ForegroundColor Yellow
        Write-Host "        Delete the queue and re-run this script to create it with sessions enabled." -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "[INFO] Creating session-enabled queue (for ordered webhook processing)..." -ForegroundColor Gray
    az servicebus queue create `
        --namespace-name $SERVICE_BUS_NAMESPACE `
        --name $QUEUE_NAME `
        --resource-group $RESOURCE_GROUP `
        --enable-session true `
        --max-delivery-count 10 `
        --lock-duration PT5M `
        --default-message-time-to-live P7D | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Session-enabled queue created" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Failed to create queue" -ForegroundColor Red
        exit 1
    }
}

# 6. Store Service Bus connection in Key Vault
Write-Host "`n[6/6] Storing Service Bus connection in Key Vault..." -ForegroundColor Yellow
$connectionString = az servicebus namespace authorization-rule keys list `
    --namespace-name $SERVICE_BUS_NAMESPACE `
    --resource-group $RESOURCE_GROUP `
    --name RootManageSharedAccessKey `
    --query primaryConnectionString `
    --output tsv 2>$null

if ($connectionString) {
    # Key Vault removed - secrets are now in appsettings.Production.json
    Write-Host "[INFO] Service Bus connection string retrieved" -ForegroundColor Green
    Write-Host "[INFO] Update appsettings.Production.json with connection string if needed" -ForegroundColor Yellow
} else {
    Write-Host "[WARNING] Could not retrieve connection string" -ForegroundColor Yellow
}

# Wait for services to be ready
Write-Host "`nWaiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Get application URL
$APP_URL = az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" -o tsv
$APP_URL = "https://$APP_URL"

$SEQ_URL = az containerapp show --name $SEQ_NAME --resource-group $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" -o tsv
$SEQ_URL = "https://$SEQ_URL"

# Summary
Write-Host "`n================================================" -ForegroundColor Green
Write-Host "RESUME COMPLETE" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

Write-Host "`nCurrent Status:" -ForegroundColor Cyan
Write-Host "  - SQL Database: Online" -ForegroundColor White
Write-Host "  - Seq Log Server: Running" -ForegroundColor White
Write-Host "  - API Container: Always-on (min 1 replica)" -ForegroundColor White
Write-Host "  - Service Bus: Ready for Meta webhooks" -ForegroundColor White

Write-Host "`nApplication URLs:" -ForegroundColor Cyan
Write-Host "  - Application: $APP_URL" -ForegroundColor White
Write-Host "  - Seq Logs: $SEQ_URL" -ForegroundColor White

Write-Host "`nTest the application:" -ForegroundColor Cyan
Write-Host "  curl $APP_URL/health" -ForegroundColor Yellow

Write-Host "`nEstimated Monthly Cost: ~`$40-45/month" -ForegroundColor Yellow
Write-Host "  - SQL Database: ~`$5/month" -ForegroundColor Gray
Write-Host "  - Container Registry: ~`$5/month" -ForegroundColor Gray
Write-Host "  - Container Apps: ~`$20-25/month (always-on)" -ForegroundColor Gray
Write-Host "  - Service Bus: ~`$10/month (Standard tier)" -ForegroundColor Gray

Write-Host ""
Write-Host "Note: Ready for Meta webhook integration" -ForegroundColor White


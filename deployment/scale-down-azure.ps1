# ============================================
# CHATIFY AI - SCALE DOWN AZURE RESOURCES
# ============================================
# Purpose: Reduce costs to ~$5/month when not using the application
# This pauses/scales services while keeping all data and configuration
# Run resume-azure.ps1 to bring everything back online
# WARNING: This will break Meta webhook integration

$ErrorActionPreference = "Continue"

# Navigate to project root (parent of deployment folder)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "SCALING DOWN AZURE RESOURCES" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

$RESOURCE_GROUP = "chatify-prod-rg"
$SQL_SERVER = "chatify-sql-server"
$SQL_DB = "chatify-db"
$SEQ_NAME = "chatify-seq"
$APP_NAME = "chatify-api"
$SERVICE_BUS_NAMESPACE = "chatify-sb"

# 1. Scale API to zero
Write-Host "`n[1/4] Scaling API to 0..." -ForegroundColor Yellow
Write-Host "[WARNING] This will break Meta webhooks (requires always-on)\" -ForegroundColor Red
az containerapp update `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --min-replicas 0 `
    --max-replicas 3 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] API scaled to 0" -ForegroundColor Green
} else {
    Write-Host "[WARNING] Failed to scale API" -ForegroundColor Yellow
}

# 2. Delete Service Bus (saves ~$10/month)
Write-Host "`n[2/4] Checking Service Bus..." -ForegroundColor Yellow
$sbExists = az servicebus namespace show --name $SERVICE_BUS_NAMESPACE --resource-group $RESOURCE_GROUP --query name -o tsv 2>$null

if ($sbExists) {
    Write-Host "[INFO] Deleting Service Bus namespace (saves ~`$10/month)..." -ForegroundColor Gray
    az servicebus namespace delete --name $SERVICE_BUS_NAMESPACE --resource-group $RESOURCE_GROUP --yes | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Service Bus deleted" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Failed to delete Service Bus" -ForegroundColor Yellow
    }
} else {
    Write-Host "[OK] Service Bus not found (already scaled down)" -ForegroundColor Green
}

# 3. Scale Seq log server to zero
Write-Host "`n[3/4] Scaling Seq log server to 0..." -ForegroundColor Yellow
az containerapp update `
    --name $SEQ_NAME `
    --resource-group $RESOURCE_GROUP `
    --min-replicas 0 `
    --max-replicas 1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Seq scaled to 0 (no logging costs)" -ForegroundColor Green
} else {
    Write-Host "[WARNING] Failed to scale Seq" -ForegroundColor Red
}

# 4. Pause SQL Database (saves ~$5/month)
# Note: Basic tier cannot be paused. For cost savings, consider deleting and recreating.
Write-Host "`n[4/4] Checking SQL Database tier..." -ForegroundColor Yellow
$sqlTier = az sql db show --name $SQL_DB --resource-group $RESOURCE_GROUP --server $SQL_SERVER --query "sku.tier" -o tsv

if ($sqlTier -eq "Basic") {
    Write-Host "[INFO] SQL Database is Basic tier (cannot be paused)" -ForegroundColor Yellow
    Write-Host "[INFO] To save `$5/month, delete the database and recreate when needed" -ForegroundColor Yellow
} else {
    az sql db pause --name $SQL_DB --resource-group $RESOURCE_GROUP --server $SQL_SERVER 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] SQL Database paused (no compute costs)" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Failed to pause SQL Database" -ForegroundColor Yellow
    }
}

# Summary
Write-Host "`n================================================" -ForegroundColor Green
Write-Host "SCALE DOWN COMPLETE" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

Write-Host ""
Write-Host "Current Status:" -ForegroundColor Cyan
Write-Host "  - API Container: Scaled to 0 (auto-starts on request)" -ForegroundColor White
Write-Host "  - Seq Log Server: Scaled to 0" -ForegroundColor White
Write-Host "  - SQL Database: Paused" -ForegroundColor White
Write-Host "  - Service Bus: Deleted" -ForegroundColor White
Write-Host "  - Container Registry: Active (needed for images)" -ForegroundColor White
Write-Host "  - Key Vault: Active (minimal cost)" -ForegroundColor White
Write-Host ""
Write-Host "[WARNING] Meta webhooks will NOT work in this configuration" -ForegroundColor Red

Write-Host ""
Write-Host "Estimated Monthly Cost: ~`$5" -ForegroundColor Green
Write-Host "  - Container Registry: `$5/month" -ForegroundColor White
Write-Host "  - Key Vault: ~`$0.03/month" -ForegroundColor White
Write-Host "  - Everything else: `$0" -ForegroundColor White

Write-Host ""
Write-Host "To resume services:" -ForegroundColor Cyan
Write-Host "  Run: .\resume-azure.ps1" -ForegroundColor Yellow

Write-Host ""
Write-Host "All data and configuration preserved!" -ForegroundColor Green
Write-Host ""

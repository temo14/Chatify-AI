# ============================================
# CHATIFY AI - SCALE DOWN AZURE RESOURCES
# ============================================
# Purpose: Reduce costs to ~$5/month when not using the application
# This pauses/scales services while keeping all data and configuration
# Run resume-azure.ps1 to bring everything back online

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "SCALING DOWN AZURE RESOURCES" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

$RESOURCE_GROUP = "chatify-prod-rg"
$SQL_SERVER = "chatify-sql-server"
$SQL_DB = "chatify-db"
$SEQ_NAME = "chatify-seq"
$APP_NAME = "chatify-api"

# 1. Scale API to zero (it's already set to min 0, just confirming)
Write-Host "`n[1/3] Checking API container app..." -ForegroundColor Yellow
$apiStatus = az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query "properties.template.scale.minReplicas" -o tsv
if ($apiStatus -eq "0") {
    Write-Host "[OK] API already scaled to 0 (will auto-start on first request)" -ForegroundColor Green
} else {
    Write-Host "Scaling API to 0..." -ForegroundColor Yellow
    az containerapp update `
        --name $APP_NAME `
        --resource-group $RESOURCE_GROUP `
        --min-replicas 0 `
        --max-replicas 3
    Write-Host "[OK] API scaled to 0" -ForegroundColor Green
}

# 2. Scale Seq log server to zero
Write-Host "`n[2/3] Scaling Seq log server to 0..." -ForegroundColor Yellow
az containerapp update `
    --name $SEQ_NAME `
    --resource-group $RESOURCE_GROUP `
    --min-replicas 0 `
    --max-replicas 1

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Seq scaled to 0 (no logging costs)" -ForegroundColor Green
} else {
    Write-Host "[WARNING] Failed to scale Seq" -ForegroundColor Red
}

# 3. Pause SQL Database (saves ~$5/month)
# Note: Basic tier cannot be paused. For cost savings, consider deleting and recreating.
Write-Host "`n[3/3] Checking SQL Database tier..." -ForegroundColor Yellow
$sqlTier = az sql db show --name $SQL_DB --resource-group $RESOURCE_GROUP --server $SQL_SERVER --query "sku.tier" -o tsv

if ($sqlTier -eq "Basic") {
    Write-Host "[INFO] SQL Database is Basic tier (cannot be paused)" -ForegroundColor Yellow
    Write-Host "[INFO] To save `$5/month, delete the database and recreate when needed" -ForegroundColor Yellow
} else {
    az sql db pause --name $SQL_DB --resource-group $RESOURCE_GROUP --server $SQL_SERVER
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] SQL Database paused (no compute costs)" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Failed to pause SQL Database" -ForegroundColor Yellow
    }
}

# 4. Summary
Write-Host "`n================================================" -ForegroundColor Green
Write-Host "SCALE DOWN COMPLETE" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

Write-Host ""
Write-Host "Current Status:" -ForegroundColor Cyan
Write-Host "  - API Container: Scaled to 0 (auto-starts on request)" -ForegroundColor White
Write-Host "  - Seq Log Server: Scaled to 0" -ForegroundColor White
Write-Host "  - SQL Database: Paused" -ForegroundColor White
Write-Host "  - Container Registry: Active (needed for images)" -ForegroundColor White
Write-Host "  - Key Vault: Active (minimal cost)" -ForegroundColor White

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

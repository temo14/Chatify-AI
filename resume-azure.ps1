# ============================================
# CHATIFY AI - RESUME AZURE RESOURCES
# ============================================
# Purpose: Bring all services back online after scale-down
# This resumes SQL database and scales Seq log server back up
# API will auto-start on first request (already configured)

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "RESUMING AZURE RESOURCES" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

$RESOURCE_GROUP = "chatify-prod-rg"
$SQL_SERVER = "chatify-sql-server"
$SQL_DB = "chatify-db"
$SEQ_NAME = "chatify-seq"
$APP_NAME = "chatify-api"

# 1. Resume SQL Database
Write-Host "`n[1/3] Resuming SQL Database..." -ForegroundColor Yellow
$sqlTier = az sql db show --name $SQL_DB --resource-group $RESOURCE_GROUP --server $SQL_SERVER --query "sku.tier" -o tsv

if ($sqlTier -eq "Basic") {
    Write-Host "[INFO] SQL Database is Basic tier (always active)" -ForegroundColor Green
} else {
    az sql db resume --name $SQL_DB --resource-group $RESOURCE_GROUP --server $SQL_SERVER
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] SQL Database resuming (may take 30-60 seconds)" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Failed to resume SQL Database (may already be active)" -ForegroundColor Yellow
    }
}

# 2. Scale Seq log server back up
Write-Host "`n[2/3] Scaling Seq log server to 1..." -ForegroundColor Yellow
az containerapp update `
    --name $SEQ_NAME `
    --resource-group $RESOURCE_GROUP `
    --min-replicas 1 `
    --max-replicas 1

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Seq scaled to 1 (logging active)" -ForegroundColor Green
} else {
    Write-Host "[WARNING] Failed to scale Seq" -ForegroundColor Red
}

# 3. Check API status (should already be min 0, will auto-start)
Write-Host "`n[3/3] Checking API container app..." -ForegroundColor Yellow
$apiStatus = az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query "properties.template.scale.minReplicas" -o tsv
Write-Host "[OK] API configured to auto-start on first request (min replicas: $apiStatus)" -ForegroundColor Green

# 4. Wait for services to be ready
Write-Host "`nWaiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# 5. Get application URL
$APP_URL = az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" -o tsv
$APP_URL = "https://$APP_URL"

$SEQ_URL = az containerapp show --name $SEQ_NAME --resource-group $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" -o tsv
$SEQ_URL = "https://$SEQ_URL"

# 6. Summary
Write-Host "`n================================================" -ForegroundColor Green
Write-Host "RESUME COMPLETE" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

Write-Host "`nCurrent Status:" -ForegroundColor Cyan
Write-Host "  - SQL Database: Online" -ForegroundColor White
Write-Host "  - Seq Log Server: Running" -ForegroundColor White
Write-Host "  - API Container: Will auto-start on first request" -ForegroundColor White

Write-Host "`nApplication URLs:" -ForegroundColor Cyan
Write-Host "  - Application: $APP_URL" -ForegroundColor White
Write-Host "  - Seq Logs: $SEQ_URL" -ForegroundColor White

Write-Host "`nTest the application:" -ForegroundColor Cyan
Write-Host "  curl $APP_URL/health" -ForegroundColor Yellow

Write-Host "`nEstimated Monthly Cost: ~`$27-60 (usage-based)" -ForegroundColor Yellow

Write-Host "`nNote: First request may take 10-15 seconds as API container starts" -ForegroundColor White
Write-Host ""

# ============================================
# CHATIFY AI - DELETE EVERYTHING
# ============================================
# Purpose: Delete all Azure resources for Chatify AI
# WARNING: This will permanently delete:
#   - All databases and data
#   - All secrets in Key Vault
#   - All Docker images
#   - All application state
# THIS CANNOT BE UNDONE!

$ErrorActionPreference = "Continue"

# Navigate to project root (parent of deployment folder)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "`n================================================" -ForegroundColor Red
Write-Host "CHATIFY AI - DELETE EVERYTHING" -ForegroundColor Red
Write-Host "================================================" -ForegroundColor Red
Write-Host ""

# Configuration
$SUBSCRIPTION_ID = "6017cf60-a38f-4e64-9654-e6a36caf40d5"
$RESOURCE_GROUP = "chatify-prod-rg"

Write-Host "This will DELETE the following:" -ForegroundColor Yellow
Write-Host "  ❌ Resource Group:  $RESOURCE_GROUP" -ForegroundColor White
Write-Host "  ❌ SQL Database:    All data will be lost" -ForegroundColor White
Write-Host "  ❌ Key Vault:       All secrets will be lost" -ForegroundColor White
Write-Host "  ❌ Container Apps:  API and Seq" -ForegroundColor White
Write-Host "  ❌ Service Bus:     All queued webhooks" -ForegroundColor White
Write-Host "  ❌ Registry:        All Docker images" -ForegroundColor White
Write-Host ""
Write-Host "[WARNING] THIS CANNOT BE UNDONE!" -ForegroundColor Red
Write-Host ""

# First confirmation
$confirmation1 = Read-Host "Are you absolutely sure you want to delete everything? (type 'DELETE' to continue)"
if ($confirmation1 -ne "DELETE") {
    Write-Host "`nDeletion cancelled. No changes made." -ForegroundColor Green
    exit 0
}

# Second confirmation
Write-Host ""
Write-Host "This is your last chance to cancel!" -ForegroundColor Yellow
$confirmation2 = Read-Host "Type the resource group name '$RESOURCE_GROUP' to confirm"
if ($confirmation2 -ne $RESOURCE_GROUP) {
    Write-Host "`nDeletion cancelled. No changes made." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "Proceeding with deletion..." -ForegroundColor Red
Write-Host ""

# Set Azure subscription
Write-Host "[1/2] Setting Azure subscription..." -ForegroundColor Cyan
az account set --subscription $SUBSCRIPTION_ID
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Subscription set" -ForegroundColor Green
} else {
    Write-Host "  ❌ Failed to set subscription" -ForegroundColor Red
    exit 1
}

# Delete resource group (this deletes everything inside it)
Write-Host "`n[2/2] Deleting resource group and all resources..." -ForegroundColor Cyan
Write-Host "  This may take 5-10 minutes..." -ForegroundColor Gray

az group delete `
    --name $RESOURCE_GROUP `
    --yes `
    --no-wait

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Deletion initiated" -ForegroundColor Green
} else {
    Write-Host "  ❌ Failed to delete resource group" -ForegroundColor Red
    exit 1
}

# Summary
Write-Host "`n================================================" -ForegroundColor Yellow
Write-Host "DELETION IN PROGRESS" -ForegroundColor Yellow
Write-Host "================================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Azure is deleting all resources in the background." -ForegroundColor White
Write-Host "This process may take 5-10 minutes to complete." -ForegroundColor White
Write-Host ""
Write-Host "Deleted resources:" -ForegroundColor Cyan
Write-Host "  ❌ Resource Group:     $RESOURCE_GROUP" -ForegroundColor White
Write-Host "  ❌ Container App:      chatify-api" -ForegroundColor White
Write-Host "  ❌ Seq Log Server:     chatify-seq" -ForegroundColor White
Write-Host "  ❌ SQL Server:         chatify-sql-server" -ForegroundColor White
Write-Host "  ❌ SQL Database:       chatify-db" -ForegroundColor White
Write-Host "  ❌ Service Bus:        chatify-sb" -ForegroundColor White
Write-Host "  ❌ Key Vault:          chatify-kv-4021" -ForegroundColor White
Write-Host "  ❌ Container Registry: chatifyregistry" -ForegroundColor White
Write-Host "  ❌ Container Env:      chatify-env" -ForegroundColor White
Write-Host ""
Write-Host "Check deletion status:" -ForegroundColor Cyan
Write-Host "  az group show -n $RESOURCE_GROUP" -ForegroundColor Gray
Write-Host ""
Write-Host "To redeploy from scratch:" -ForegroundColor Cyan
Write-Host "  .\deploy-from-scratch.ps1" -ForegroundColor Yellow
Write-Host ""

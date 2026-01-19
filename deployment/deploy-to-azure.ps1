# ============================================
# CHATIFY AI - DEPLOY TO AZURE
# ============================================
# Purpose: Build and deploy application updates to existing infrastructure
# Use this for: Code updates, config changes, or re-deployments

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "   ChatifyAI - Azure Deployment" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ResourceGroup = "chatify-prod-rg"
$ContainerApp = "chatify-api"
$ContainerRegistry = "chatifyregistry"
$ImageName = "chatify-api"
$ImageTag = "latest"
$FullImageName = "$ContainerRegistry.azurecr.io/$ImageName`:$ImageTag"

# Navigate to project root (parent of deployment folder)
Write-Host "Step 1/6: Navigating to project..." -ForegroundColor Yellow
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot
Write-Host "  Location: $projectRoot" -ForegroundColor Gray
Write-Host ""

# Login to Azure Container Registry
Write-Host "Step 2/6: Logging into Azure Container Registry..." -ForegroundColor Yellow
$loginResult = az acr login --name $ContainerRegistry 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: $loginResult" -ForegroundColor Red
    exit 1
}
Write-Host "  Login successful" -ForegroundColor Green
Write-Host ""

# Build Docker image
Write-Host "Step 3/6: Building Docker image..." -ForegroundColor Yellow
Write-Host "  Image: $FullImageName" -ForegroundColor Gray
Write-Host "  This may take 2-5 minutes..." -ForegroundColor Gray
$buildStart = Get-Date
& docker build -t $FullImageName -f Dockerfile .
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Docker build failed!" -ForegroundColor Red
    exit 1
}
$buildDuration = (Get-Date) - $buildStart
Write-Host "  Build completed in $([math]::Round($buildDuration.TotalSeconds, 1))s" -ForegroundColor Green
Write-Host ""

# Push to Azure Container Registry
Write-Host "Step 4/6: Pushing image to Azure Container Registry..." -ForegroundColor Yellow
Write-Host "  This may take 1-3 minutes..." -ForegroundColor Gray
$pushStart = Get-Date
& docker push $FullImageName
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Docker push failed!" -ForegroundColor Red
    exit 1
}
$pushDuration = (Get-Date) - $pushStart
Write-Host "  Push completed in $([math]::Round($pushDuration.TotalSeconds, 1))s" -ForegroundColor Green
Write-Host ""

# Get the actual FQDN from Azure
Write-Host "Step 5/6: Getting Container App URL..." -ForegroundColor Yellow
$fqdn = az containerapp show -n $ContainerApp -g $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv
$appUrl = "https://$fqdn"
Write-Host "  App URL: $appUrl" -ForegroundColor Gray
Write-Host ""

# Update Container App with new image
Write-Host "Step 6/6: Updating Azure Container App..." -ForegroundColor Yellow
Write-Host "  This may take 30-60 seconds..." -ForegroundColor Gray
& az containerapp update --name $ContainerApp --resource-group $ResourceGroup --image $FullImageName
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Container App update failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Update completed" -ForegroundColor Green
Write-Host ""

Write-Host "======================================" -ForegroundColor Green
Write-Host "   Deployment Successful!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "App URL:    $appUrl" -ForegroundColor Cyan
Write-Host "Health:     $appUrl/health" -ForegroundColor Cyan
Write-Host "Admin:      $appUrl/admin-login.html" -ForegroundColor Cyan
Write-Host ""
Write-Host "Monitoring:" -ForegroundColor Yellow
Write-Host "  View logs:       az containerapp logs show -n chatify-api -g chatify-prod-rg --follow" -ForegroundColor Gray
Write-Host "  Check replicas:  az containerapp replica list -n chatify-api -g chatify-prod-rg" -ForegroundColor Gray
Write-Host "  Restart app:     `$revision = az containerapp revision list --name chatify-api --resource-group chatify-prod-rg --query `"[0].name`" -o tsv; az containerapp revision restart --resource-group chatify-prod-rg --name chatify-api --revision `$revision" -ForegroundColor Gray
Write-Host ""

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy Chatify AI to Azure App Service (Container)

.DESCRIPTION
    This script automates the deployment of Chatify AI to Azure App Service using:
    - Azure Container Registry (ACR) for Docker image hosting
    - Azure App Service on Linux with container support
    - Managed Identity for secure ACR access (no passwords)
    - Azure SQL Database (optional - can use existing)
    
.PARAMETER ResourceGroup
    Name of the Azure resource group (will be created if it doesn't exist)
    
.PARAMETER Location
    Azure region (e.g., eastus, westeurope, westus2)
    
.PARAMETER AppName
    Globally unique name for the App Service (only lowercase letters, numbers, hyphens)
    
.PARAMETER AcrName
    Globally unique name for Azure Container Registry (only lowercase letters and numbers)
    
.PARAMETER Sku
    App Service Plan SKU (B1, B2, B3, P1V2, P2V2, P3V2, etc.)
    
.PARAMETER SqlServer
    Optional: existing Azure SQL Server name (if not provided, you'll need to configure ConnectionString manually)
    
.PARAMETER SqlDatabase
    Optional: existing Azure SQL Database name
    
.EXAMPLE
    .\deploy-azure.ps1 -ResourceGroup chatify-prod-rg -Location eastus -AppName chatify-api-prod -AcrName chatifyacr001
    
.NOTES
    Prerequisites:
    - Azure CLI installed and logged in (az login)
    - Docker installed (for local testing)
    - Contributor or Owner role on the subscription
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[a-z0-9-]{3,24}$')]
    [string]$AppName,
    
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[a-z0-9]{5,50}$')]
    [string]$AcrName,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet('B1', 'B2', 'B3', 'P1V2', 'P2V2', 'P3V2', 'S1', 'S2', 'S3')]
    [string]$Sku = "B1",
    
    [Parameter(Mandatory=$false)]
    [string]$SqlServer = "",
    
    [Parameter(Mandatory=$false)]
    [string]$SqlDatabase = ""
)

$ErrorActionPreference = "Stop"

# Configuration
$PlanName = "$AppName-plan"
$ImageName = "chatify-api"
$ImageTag = "1.0.0"
$FullImageName = "$ImageName`:$ImageTag"

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Chatify AI - Azure App Service Deployment" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Resource Group: $ResourceGroup"
Write-Host "  Location: $Location"
Write-Host "  App Name: $AppName"
Write-Host "  ACR Name: $AcrName"
Write-Host "  SKU: $Sku"
Write-Host "  Image: $FullImageName"
Write-Host ""

# Check if Azure CLI is installed
Write-Host "[1/10] Checking prerequisites..." -ForegroundColor Green
try {
    az version | Out-Null
} catch {
    Write-Error "Azure CLI is not installed. Install from: https://aka.ms/installazurecliwindows"
    exit 1
}

# Check if logged in
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged in to Azure. Run: az login"
    exit 1
}

Write-Host "  ✓ Logged in as: $($account.user.name)" -ForegroundColor Gray
Write-Host "  ✓ Subscription: $($account.name)" -ForegroundColor Gray
Write-Host ""

# Create resource group
Write-Host "[2/10] Creating resource group..." -ForegroundColor Green
az group create --name $ResourceGroup --location $Location --output none
Write-Host "  ✓ Resource group: $ResourceGroup" -ForegroundColor Gray
Write-Host ""

# Create Azure Container Registry
Write-Host "[3/10] Creating Azure Container Registry..." -ForegroundColor Green
$acrExists = az acr show --name $AcrName --resource-group $ResourceGroup 2>$null
if (-not $acrExists) {
    az acr create `
        --resource-group $ResourceGroup `
        --name $AcrName `
        --sku Basic `
        --admin-enabled false `
        --output none
    Write-Host "  ✓ ACR created: $AcrName" -ForegroundColor Gray
} else {
    Write-Host "  ✓ ACR already exists: $AcrName" -ForegroundColor Gray
}
Write-Host ""

# Build and push image to ACR
Write-Host "[4/10] Building and pushing Docker image to ACR..." -ForegroundColor Green
Write-Host "  (This may take 3-5 minutes...)" -ForegroundColor Gray

$buildOutput = az acr build `
    --registry $AcrName `
    --image $FullImageName `
    --file Dockerfile `
    . `
    2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker build failed. Check output above."
    exit 1
}

Write-Host "  ✓ Image pushed to ACR: $FullImageName" -ForegroundColor Gray
Write-Host ""

# Get ACR login server
$acrLoginServer = az acr show --name $AcrName --query loginServer -o tsv
$acrId = az acr show --name $AcrName --query id -o tsv

# Create App Service Plan
Write-Host "[5/10] Creating App Service Plan..." -ForegroundColor Green
$planExists = az appservice plan show --name $PlanName --resource-group $ResourceGroup 2>$null
if (-not $planExists) {
    az appservice plan create `
        --name $PlanName `
        --resource-group $ResourceGroup `
        --is-linux `
        --sku $Sku `
        --output none
    Write-Host "  ✓ App Service Plan created: $PlanName ($Sku)" -ForegroundColor Gray
} else {
    Write-Host "  ✓ App Service Plan already exists: $PlanName" -ForegroundColor Gray
}
Write-Host ""

# Create Web App
Write-Host "[6/10] Creating App Service (Web App)..." -ForegroundColor Green
$appExists = az webapp show --name $AppName --resource-group $ResourceGroup 2>$null
if (-not $appExists) {
    az webapp create `
        --resource-group $ResourceGroup `
        --plan $PlanName `
        --name $AppName `
        --deployment-container-image-name "$acrLoginServer/$FullImageName" `
        --output none
    Write-Host "  ✓ Web App created: $AppName" -ForegroundColor Gray
} else {
    Write-Host "  ✓ Web App already exists: $AppName" -ForegroundColor Gray
}
Write-Host ""

# Enable managed identity
Write-Host "[7/10] Configuring Managed Identity for ACR access..." -ForegroundColor Green
az webapp identity assign `
    --resource-group $ResourceGroup `
    --name $AppName `
    --output none

$principalId = az webapp identity show `
    --resource-group $ResourceGroup `
    --name $AppName `
    --query principalId `
    -o tsv

# Assign AcrPull role to managed identity
az role assignment create `
    --assignee $principalId `
    --role AcrPull `
    --scope $acrId `
    --output none 2>$null

Write-Host "  ✓ Managed Identity enabled and granted ACR access" -ForegroundColor Gray
Write-Host ""

# Configure container settings
Write-Host "[8/10] Configuring container registry..." -ForegroundColor Green
az webapp config container set `
    --resource-group $ResourceGroup `
    --name $AppName `
    --docker-custom-image-name "$acrLoginServer/$FullImageName" `
    --docker-registry-server-url "https://$acrLoginServer" `
    --output none

Write-Host "  ✓ Container configured" -ForegroundColor Gray
Write-Host ""

# Configure app settings
Write-Host "[9/10] Configuring application settings..." -ForegroundColor Green

# Base settings
$settings = @(
    "WEBSITES_PORT=8080"
    "ASPNETCORE_URLS=http://+:8080"
    "ASPNETCORE_ENVIRONMENT=Production"
    "WEBSITE_HTTPLOGGING_RETENTION_DAYS=7"
)

# Add SQL connection string if provided
if ($SqlServer -and $SqlDatabase) {
    Write-Host "  → Configuring Azure SQL connection..." -ForegroundColor Gray
    # Note: You'll need to set the password separately via Portal or as a parameter
    $sqlConnection = "Server=tcp:$SqlServer.database.windows.net,1433;Database=$SqlDatabase;User Id=CHANGE_ME;Password=CHANGE_ME;Encrypt=True;"
    $settings += "ConnectionStrings__DefaultConnection=$sqlConnection"
}

az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name $AppName `
    --settings $settings `
    --output none

Write-Host "  ✓ Base settings configured" -ForegroundColor Gray
Write-Host ""

# Get Web App URL
$appUrl = az webapp show `
    --resource-group $ResourceGroup `
    --name $AppName `
    --query defaultHostName `
    -o tsv

Write-Host "[10/10] Deployment complete!" -ForegroundColor Green
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  🎉 Deployment Successful!" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "App URL: " -NoNewline
Write-Host "https://$appUrl" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Configure required secrets in Azure Portal → App Service → Configuration:" -ForegroundColor White
Write-Host "     - ConnectionStrings__DefaultConnection (Azure SQL)" -ForegroundColor Gray
Write-Host "     - AzureOpenAI__Endpoint" -ForegroundColor Gray
Write-Host "     - AzureOpenAI__ApiKey" -ForegroundColor Gray
Write-Host "     - AzureOpenAI__ChatDeploymentName" -ForegroundColor Gray
Write-Host "     - AzureOpenAI__EmbeddingDeploymentName" -ForegroundColor Gray
Write-Host "     - Jwt__Secret" -ForegroundColor Gray
Write-Host "     - Qdrant__Endpoint (Qdrant Cloud or self-hosted)" -ForegroundColor Gray
Write-Host "     - Qdrant__CollectionName" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Run EF Core migrations against Azure SQL:" -ForegroundColor White
Write-Host "     dotnet ef database update -p ChatAI.Infrastructure -s ChatAI.Api" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Restart the app after configuration:" -ForegroundColor White
Write-Host "     az webapp restart -g $ResourceGroup -n $AppName" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. Monitor logs:" -ForegroundColor White
Write-Host "     az webapp log tail -g $ResourceGroup -n $AppName" -ForegroundColor Gray
Write-Host ""
Write-Host "  5. Access the app:" -ForegroundColor White
Write-Host "     https://$appUrl/swagger (dev only)" -ForegroundColor Gray
Write-Host "     https://$appUrl/health (health check)" -ForegroundColor Gray
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

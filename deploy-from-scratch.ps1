# ============================================
# CHATIFY AI - COMPLETE AZURE DEPLOYMENT
# ============================================
# Purpose: Deploy entire Chatify AI infrastructure from scratch
# This script creates all Azure resources and deploys the application
# Use this for: New deployments, disaster recovery, or environment cloning

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "CHATIFY AI - COMPLETE AZURE DEPLOYMENT" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# ============================================
# CONFIGURATION VARIABLES
# ============================================
Write-Host "`nLoading configuration..." -ForegroundColor Yellow

$SUBSCRIPTION_ID = "6017cf60-a38f-4e64-9654-e6a36caf40d5"
$RESOURCE_GROUP = "chatify-prod-rg"
$LOCATION = "westeurope"
$REGISTRY_NAME = "chatifyregistry"
$SQL_SERVER = "chatify-sql-server"
$SQL_DB = "chatify-db"
$SQL_ADMIN_USER = "sqladmin"
$SQL_ADMIN_PASSWORD = "Chatify@2026!Secure"
$KEYVAULT_NAME = "chatify-kv-4021"
$CONTAINER_ENV = "chatify-env"
$APP_NAME = "chatify-api"
$SEQ_NAME = "chatify-seq"
$IMAGE_VERSION = "latest"

# Azure OpenAI Configuration
$OPENAI_ENDPOINT = "https://ecommerceai-openai.openai.azure.com/"
$OPENAI_API_KEY = "4uRTO2lAtHpX4P3CC3otWUVsqCgHIIcD3WkOBqKDIAYouV49EnVbJQQJ99BJAC5RqLJXJ3w3AAABACOGO4Tv"

# Application Secrets
$JWT_SECRET = "Secret!!!1231321@edasfawadasdaddsadsasddddddddddddddddddddddddddd"
$ADMIN_USERNAME = "admin"
$ADMIN_PASSWORD = "Admin@ChatifyGeorgia2026"
$ADMIN_EMAIL = "admin@chatify.ge"
$EMAIL_USERNAME = "temo599922030@gmail.com"
$EMAIL_PASSWORD = "iucaetrhmcggvzeh"
$EMAIL_FROM = "temo599922030@gmail.com"
$EMAIL_ADMIN = "t.baindurashvili.gm@gmail.com"

# Seq Configuration
$SEQ_ADMIN_PASSWORD = "Admin@123"

Write-Host "[OK] Configuration loaded" -ForegroundColor Green

# ============================================
# STEP 1: Setup Azure Subscription
# ============================================
Write-Host "`nStep 1/13: Setting up Azure subscription..." -ForegroundColor Cyan
az account set --subscription $SUBSCRIPTION_ID
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Subscription set: $SUBSCRIPTION_ID" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Failed to set subscription" -ForegroundColor Red
    exit 1
}

# ============================================
# STEP 2: Create Resource Group
# ============================================
Write-Host "`nStep 2/13: Creating resource group..." -ForegroundColor Cyan
az group create --name $RESOURCE_GROUP --location $LOCATION
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Resource group created: $RESOURCE_GROUP" -ForegroundColor Green
} else {
    Write-Host "[INFO] Resource group may already exist" -ForegroundColor Yellow
}

# ============================================
# STEP 3: Create Container Registry
# ============================================
Write-Host "`nStep 3/13: Creating Azure Container Registry..." -ForegroundColor Cyan
az acr create `
    --resource-group $RESOURCE_GROUP `
    --name $REGISTRY_NAME `
    --sku Basic

az acr update --name $REGISTRY_NAME --admin-enabled true

$ACR_CREDS = az acr credential show --name $REGISTRY_NAME --resource-group $RESOURCE_GROUP | ConvertFrom-Json
$ACR_USERNAME = $ACR_CREDS.username
$ACR_PASSWORD = $ACR_CREDS.passwords[0].value

Write-Host "[OK] ACR created: $REGISTRY_NAME.azurecr.io" -ForegroundColor Green

# ============================================
# STEP 4: Create SQL Database
# ============================================
Write-Host "`nStep 4/13: Creating SQL Server and Database..." -ForegroundColor Cyan
az sql server create `
    --name $SQL_SERVER `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION `
    --admin-user $SQL_ADMIN_USER `
    --admin-password $SQL_ADMIN_PASSWORD

az sql db create `
    --resource-group $RESOURCE_GROUP `
    --server $SQL_SERVER `
    --name $SQL_DB `
    --service-objective Basic

az sql server firewall-rule create `
    --resource-group $RESOURCE_GROUP `
    --server $SQL_SERVER `
    --name AllowAzureServices `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0

Write-Host "[OK] SQL Server created: $SQL_SERVER.database.windows.net" -ForegroundColor Green

# ============================================
# STEP 5: Create Key Vault
# ============================================
Write-Host "`nStep 5/13: Creating Azure Key Vault..." -ForegroundColor Cyan
az keyvault create `
    --name $KEYVAULT_NAME `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION `
    --enable-rbac-authorization true

Write-Host "[OK] Key Vault created: $KEYVAULT_NAME.vault.azure.net" -ForegroundColor Green

# ============================================
# STEP 6: Grant Key Vault Permissions
# ============================================
Write-Host "`nStep 6/13: Granting Key Vault permissions..." -ForegroundColor Cyan
$USER_ID = az ad signed-in-user show --query id --output tsv

az role assignment create `
    --role "Key Vault Secrets Officer" `
    --assignee $USER_ID `
    --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME"

Write-Host "Waiting 30 seconds for RBAC propagation..." -ForegroundColor Yellow
Start-Sleep -Seconds 30
Write-Host "[OK] Permissions granted" -ForegroundColor Green

# ============================================
# STEP 7: Populate Key Vault
# ============================================
Write-Host "`nStep 7/13: Populating Key Vault with secrets..." -ForegroundColor Cyan

$CONNECTION_STRING = "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User Id=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;Encrypt=True;TrustServerCertificate=False;"

az keyvault secret set --vault-name $KEYVAULT_NAME --name "ConnectionStrings--DefaultConnection" --value $CONNECTION_STRING | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "AzureOpenAI--ApiKey" --value $OPENAI_API_KEY | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "AzureOpenAI--Endpoint" --value $OPENAI_ENDPOINT | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Jwt--Secret" --value $JWT_SECRET | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Admin--Username" --value $ADMIN_USERNAME | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Admin--Password" --value $ADMIN_PASSWORD | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Email--Username" --value $EMAIL_USERNAME | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Email--Password" --value $EMAIL_PASSWORD | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Email--FromEmail" --value $EMAIL_FROM | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Email--AdminEmail" --value $EMAIL_ADMIN | Out-Null

Write-Host "[OK] All 10 secrets stored in Key Vault" -ForegroundColor Green

# ============================================
# STEP 8: Create Container Apps Environment
# ============================================
Write-Host "`nStep 8/13: Creating Container Apps Environment..." -ForegroundColor Cyan
az containerapp env create `
    --name $CONTAINER_ENV `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION

Write-Host "[OK] Environment created: $CONTAINER_ENV" -ForegroundColor Green

# ============================================
# STEP 9: Build and Push Docker Image
# ============================================
Write-Host "`nStep 9/13: Building and pushing Docker image..." -ForegroundColor Cyan
az acr login --name $REGISTRY_NAME

docker build -t chatify-ai:$IMAGE_VERSION .
docker tag chatify-ai:$IMAGE_VERSION $REGISTRY_NAME.azurecr.io/chatify-ai:$IMAGE_VERSION
docker push $REGISTRY_NAME.azurecr.io/chatify-ai:$IMAGE_VERSION

Write-Host "[OK] Image pushed: $REGISTRY_NAME.azurecr.io/chatify-ai:$IMAGE_VERSION" -ForegroundColor Green

# ============================================
# STEP 10: Deploy Seq Log Server
# ============================================
Write-Host "`nStep 10/13: Deploying Seq log server..." -ForegroundColor Cyan
az containerapp create `
    --name $SEQ_NAME `
    --resource-group $RESOURCE_GROUP `
    --environment $CONTAINER_ENV `
    --image datalust/seq:latest `
    --target-port 80 `
    --ingress external `
    --cpu 0.5 `
    --memory 1Gi `
    --min-replicas 1 `
    --max-replicas 1 `
    --env-vars `
        "ACCEPT_EULA=Y" `
        "SEQ_STORAGE_INMEMORY=true" `
        "SEQ_FIRSTRUN_ADMINUSERNAME=admin" `
        "SEQ_FIRSTRUN_ADMINPASSWORD=$SEQ_ADMIN_PASSWORD"

$SEQ_URL = az containerapp show --name $SEQ_NAME --resource-group $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" -o tsv
$SEQ_URL = "https://$SEQ_URL"
Write-Host "[OK] Seq deployed: $SEQ_URL" -ForegroundColor Green

# ============================================
# STEP 11: Deploy Main Application
# ============================================
Write-Host "`nStep 11/13: Deploying Chatify AI application..." -ForegroundColor Cyan
az containerapp create `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --environment $CONTAINER_ENV `
    --image "$REGISTRY_NAME.azurecr.io/chatify-ai:$IMAGE_VERSION" `
    --target-port 8080 `
    --ingress external `
    --registry-server "$REGISTRY_NAME.azurecr.io" `
    --registry-username $ACR_USERNAME `
    --registry-password $ACR_PASSWORD `
    --cpu 1.0 `
    --memory 2Gi `
    --min-replicas 0 `
    --max-replicas 3 `
    --env-vars `
        "ASPNETCORE_ENVIRONMENT=Production" `
        "Seq__ServerUrl=$SEQ_URL"

$APP_URL = az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" -o tsv
$APP_URL = "https://$APP_URL"
Write-Host "[OK] Application deployed: $APP_URL" -ForegroundColor Green

# ============================================
# STEP 12: Configure Managed Identity
# ============================================
Write-Host "`nStep 12/13: Configuring managed identity..." -ForegroundColor Cyan
$IDENTITY = az containerapp identity assign `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --system-assigned | ConvertFrom-Json

$PRINCIPAL_ID = $IDENTITY.principalId

az role assignment create `
    --role "Key Vault Secrets User" `
    --assignee $PRINCIPAL_ID `
    --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME"

Write-Host "[OK] Managed Identity configured: $PRINCIPAL_ID" -ForegroundColor Green
Write-Host "Waiting 30 seconds for RBAC propagation..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# ============================================
# STEP 13: Restart Application
# ============================================
Write-Host "`nStep 13/13: Restarting application..." -ForegroundColor Cyan
$REVISION = az containerapp revision list --name $APP_NAME --resource-group $RESOURCE_GROUP --query "[0].name" -o tsv
az containerapp revision restart `
    --resource-group $RESOURCE_GROUP `
    --name $APP_NAME `
    --revision $REVISION

Write-Host "[OK] Application restarted" -ForegroundColor Green

# ============================================
# DEPLOYMENT COMPLETE
# ============================================
Write-Host "`n================================================" -ForegroundColor Green
Write-Host "DEPLOYMENT COMPLETE" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

Write-Host "`nApplication URLs:" -ForegroundColor Cyan
Write-Host "  - Application: $APP_URL" -ForegroundColor White
Write-Host "  - Seq Logs: $SEQ_URL (admin / $SEQ_ADMIN_PASSWORD)" -ForegroundColor White
Write-Host "  - Swagger: $APP_URL/swagger" -ForegroundColor White

Write-Host "`nCredentials:" -ForegroundColor Cyan
Write-Host "  - Username: $ADMIN_USERNAME" -ForegroundColor White
Write-Host "  - Password: $ADMIN_PASSWORD" -ForegroundColor White

Write-Host "`nWaiting 30 seconds for application startup..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host "`nTesting health endpoint..." -ForegroundColor Cyan
try {
    $healthResponse = Invoke-WebRequest -Uri "$APP_URL/health" -UseBasicParsing
    Write-Host "[OK] Health Check: $($healthResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "[INFO] Health check failed (app may still be starting)" -ForegroundColor Yellow
}

Write-Host "`nNext Steps:" -ForegroundColor Cyan
Write-Host "  1. Open: $APP_URL/swagger" -ForegroundColor White
Write-Host "  2. Login with: $ADMIN_USERNAME / $ADMIN_PASSWORD" -ForegroundColor White
Write-Host "  3. View logs: $SEQ_URL" -ForegroundColor White
Write-Host "  4. To scale down: .\scale-down-azure.ps1" -ForegroundColor White

Write-Host "`nEstimated Monthly Cost: ~`$27-60 (usage-based)" -ForegroundColor Yellow
Write-Host ""

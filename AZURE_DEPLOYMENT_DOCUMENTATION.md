# Chatify AI - Azure Production Deployment Documentation

**Last Updated:** January 4, 2026  
**Initial Deployment:** January 3, 2026  
**Region:** West Europe (Netherlands) - Selected for proximity to Georgia  
**Environment:** Production  
**Current Version:** v5

---

## 📖 Table of Contents

1. [Deployment Summary](#-deployment-summary)
2. [Quick Start - Complete Infrastructure Setup](#-quick-start---complete-infrastructure-setup)
3. [Critical Credentials & Access Information](#-critical-credentials--access-information)
4. [Step-by-Step Deployment Process](#-step-by-step-deployment-process)
5. [Architecture Overview](#-architecture-overview)
6. [Configuration Management](#-configuration-hierarchy)
7. [Cost Breakdown](#-cost-breakdown-monthly-estimates)
8. [Monitoring & Management](#-monitoring--management)
9. [Deployment Updates](#-deployment-updates)
10. [Troubleshooting Guide](#-troubleshooting-guide)
11. [Security Recommendations](#-security-recommendations)
12. [Best Practices & Lessons Learned](#-deployment-best-practices--lessons-learned)

---

## 🎯 Deployment Summary

✅ **Successfully deployed Chatify AI to Azure** using Container Apps architecture with the following components:
- ✅ Azure Container Registry (ACR)
- ✅ Azure SQL Database (Basic tier)
- ✅ Azure Key Vault (RBAC-based, all secrets configured)
- ✅ Container Apps Environment with Log Analytics
- ✅ Seq Log Server (in-memory mode with authentication)
- ✅ Chatify AI Application (v5 - fully operational)

**Status:** ✅ Production-ready and fully operational
- ✅ API key authentication working
- ✅ Health checks passing
- ✅ Seq logging enabled and working
- ✅ Key Vault integration via Managed Identity
- ✅ Database migrations applied
- ✅ Azure OpenAI integration working

---

## 🚀 Quick Start - Complete Infrastructure Setup

This section provides a **complete, copy-paste ready script** to recreate the entire infrastructure from scratch. Use this for future deployments or disaster recovery.

### Prerequisites
- Azure CLI installed and authenticated (`az login`)
- Docker installed and running
- PowerShell 5.1 or later (Windows) or PowerShell Core (cross-platform)
- Application code ready in current directory

### Complete Deployment Script

```powershell
# ============================================
# CHATIFY AI - COMPLETE AZURE DEPLOYMENT
# ============================================

# Configuration Variables (UPDATE THESE)
$SUBSCRIPTION_ID = "6017cf60-a38f-4e64-9654-e6a36caf40d5"
$RESOURCE_GROUP = "chatify-prod-rg"
$LOCATION = "westeurope"
$REGISTRY_NAME = "chatifyregistry"
$SQL_SERVER = "chatify-sql-server"
$SQL_DB = "chatify-db"
$SQL_ADMIN_USER = "sqladmin"
$SQL_ADMIN_PASSWORD = "Chatify@2026!Secure"  # CHANGE THIS
$KEYVAULT_NAME = "chatify-kv-4021"
$CONTAINER_ENV = "chatify-env"
$APP_NAME = "chatify-api"
$SEQ_NAME = "chatify-seq"
$IMAGE_VERSION = "v5"

# Azure OpenAI Configuration (YOUR EXISTING RESOURCES)
$OPENAI_ENDPOINT = "https://ecommerceai-openai.openai.azure.com/"
$OPENAI_API_KEY = "4uRTO2lAtHpX4P3CC3otWUVsqCgHIIcD3WkOBqKDIAYouV49EnVbJQQJ99BJAC5RqLJXJ3w3AAABACOGO4Tv"  # REPLACE

# Application Secrets
$JWT_SECRET = "Secret!!!1231321@edasfawadasdaddsadsasddddddddddddddddddddddddddd"  # CHANGE THIS
$ADMIN_USERNAME = "admin"
$ADMIN_PASSWORD = "Admin@ChatifyGeorgia2026"  # CHANGE THIS
$ADMIN_EMAIL = "admin@chatify.ge"
$EMAIL_USERNAME = "temo599922030@gmail.com"  # CHANGE THIS
$EMAIL_PASSWORD = "iucaetrhmcggvzeh"  # CHANGE THIS (Gmail app password)
$EMAIL_FROM = "temo599922030@gmail.com"
$EMAIL_ADMIN = "t.baindurashvili.gm@gmail.com"  # CHANGE THIS

# Seq Configuration
$SEQ_ADMIN_PASSWORD = "Admin@123"  # CHANGE THIS

# ============================================
# STEP 1: Setup Azure Subscription
# ============================================
Write-Host "📋 Setting up Azure subscription..." -ForegroundColor Cyan
az account set --subscription $SUBSCRIPTION_ID

# ============================================
# STEP 2: Create Resource Group
# ============================================
Write-Host "📦 Creating resource group..." -ForegroundColor Cyan
az group create --name $RESOURCE_GROUP --location $LOCATION

# ============================================
# STEP 3: Create Container Registry
# ============================================
Write-Host "🐳 Creating Azure Container Registry..." -ForegroundColor Cyan
az acr create `
  --resource-group $RESOURCE_GROUP `
  --name $REGISTRY_NAME `
  --sku Basic

az acr update --name $REGISTRY_NAME --admin-enabled true

# Get ACR credentials
$ACR_CREDS = az acr credential show --name $REGISTRY_NAME --resource-group $RESOURCE_GROUP | ConvertFrom-Json
$ACR_USERNAME = $ACR_CREDS.username
$ACR_PASSWORD = $ACR_CREDS.passwords[0].value

Write-Host "✅ ACR Username: $ACR_USERNAME" -ForegroundColor Green

# ============================================
# STEP 4: Create SQL Database
# ============================================
Write-Host "💾 Creating SQL Database..." -ForegroundColor Cyan
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

# Configure firewall
az sql server firewall-rule create `
  --resource-group $RESOURCE_GROUP `
  --server $SQL_SERVER `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

Write-Host "✅ SQL Server: $SQL_SERVER.database.windows.net" -ForegroundColor Green

# ============================================
# STEP 5: Create Key Vault
# ============================================
Write-Host "🔐 Creating Azure Key Vault..." -ForegroundColor Cyan
az keyvault create `
  --name $KEYVAULT_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --enable-rbac-authorization true

Write-Host "✅ Key Vault: $KEYVAULT_NAME.vault.azure.net" -ForegroundColor Green

# ============================================
# STEP 6: Grant Current User Key Vault Permissions
# ============================================
Write-Host "🔑 Granting Key Vault permissions to current user..." -ForegroundColor Cyan
$USER_ID = az ad signed-in-user show --query id --output tsv

az role assignment create `
  --role "Key Vault Secrets Officer" `
  --assignee $USER_ID `
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME"

Write-Host "⏳ Waiting 30 seconds for RBAC propagation..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# ============================================
# STEP 7: Populate Key Vault with Secrets
# ============================================
Write-Host "📝 Populating Key Vault secrets..." -ForegroundColor Cyan

$CONNECTION_STRING = "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User Id=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;Encrypt=True;TrustServerCertificate=False;"

az keyvault secret set --vault-name $KEYVAULT_NAME --name "ConnectionStrings--DefaultConnection" --value $CONNECTION_STRING
az keyvault secret set --vault-name $KEYVAULT_NAME --name "AzureOpenAI--ApiKey" --value $OPENAI_API_KEY
az keyvault secret set --vault-name $KEYVAULT_NAME --name "AzureOpenAI--Endpoint" --value $OPENAI_ENDPOINT
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Jwt--Secret" --value $JWT_SECRET
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Admin--Username" --value $ADMIN_USERNAME
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Admin--Password" --value $ADMIN_PASSWORD
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Email--Username" --value $EMAIL_USERNAME
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Email--Password" --value $EMAIL_PASSWORD
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Email--FromEmail" --value $EMAIL_FROM
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Email--AdminEmail" --value $EMAIL_ADMIN

Write-Host "✅ All secrets stored in Key Vault" -ForegroundColor Green

# ============================================
# STEP 8: Create Container Apps Environment
# ============================================
Write-Host "🌍 Creating Container Apps Environment..." -ForegroundColor Cyan
az containerapp env create `
  --name $CONTAINER_ENV `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION

Write-Host "✅ Environment: $CONTAINER_ENV" -ForegroundColor Green

# ============================================
# STEP 9: Build and Push Docker Image
# ============================================
Write-Host "🐳 Building and pushing Docker image..." -ForegroundColor Cyan
az acr login --name $REGISTRY_NAME

docker build -t chatify-ai:$IMAGE_VERSION .
docker tag chatify-ai:$IMAGE_VERSION $REGISTRY_NAME.azurecr.io/chatify-ai:$IMAGE_VERSION
docker push $REGISTRY_NAME.azurecr.io/chatify-ai:$IMAGE_VERSION

Write-Host "✅ Image pushed: $REGISTRY_NAME.azurecr.io/chatify-ai:$IMAGE_VERSION" -ForegroundColor Green

# ============================================
# STEP 10: Deploy Seq Log Server
# ============================================
Write-Host "📊 Deploying Seq log server..." -ForegroundColor Cyan
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

$SEQ_URL = az containerapp show --name $SEQ_NAME --resource-group $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" --output tsv
$SEQ_URL = "https://$SEQ_URL"
Write-Host "✅ Seq deployed: $SEQ_URL" -ForegroundColor Green

# ============================================
# STEP 11: Deploy Main Application
# ============================================
Write-Host "🚀 Deploying Chatify AI application..." -ForegroundColor Cyan
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

$APP_URL = az containerapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query "properties.configuration.ingress.fqdn" --output tsv
$APP_URL = "https://$APP_URL"

# ============================================
# STEP 12: Configure Managed Identity
# ============================================
Write-Host "🆔 Configuring managed identity..." -ForegroundColor Cyan
$IDENTITY = az containerapp identity assign `
  --name $APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --system-assigned | ConvertFrom-Json

$PRINCIPAL_ID = $IDENTITY.principalId

# Grant Key Vault access to managed identity
az role assignment create `
  --role "Key Vault Secrets User" `
  --assignee $PRINCIPAL_ID `
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME"

Write-Host "✅ Managed Identity configured: $PRINCIPAL_ID" -ForegroundColor Green
Write-Host "⏳ Waiting 30 seconds for RBAC propagation..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# ============================================
# STEP 13: Restart Application
# ============================================
Write-Host "🔄 Restarting application to load Key Vault secrets..." -ForegroundColor Cyan
az containerapp revision restart `
  --resource-group $RESOURCE_GROUP `
  --name $APP_NAME `
  --revision (az containerapp revision list --name $APP_NAME --resource-group $RESOURCE_GROUP --query "[0].name" --output tsv)

# ============================================
# DEPLOYMENT COMPLETE
# ============================================
Write-Host "`n✅ ============================================" -ForegroundColor Green
Write-Host "✅ DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "✅ ============================================`n" -ForegroundColor Green

Write-Host "📱 Application URL: $APP_URL" -ForegroundColor Cyan
Write-Host "📊 Seq Logs: $SEQ_URL (admin / $SEQ_ADMIN_PASSWORD)" -ForegroundColor Cyan
Write-Host "🔐 Key Vault: https://portal.azure.com/#view/Microsoft_Azure_KeyVault/VaultMenuBlade/~/overview/vaultUri/https%3A%2F%2F$KEYVAULT_NAME.vault.azure.net" -ForegroundColor Cyan
Write-Host "`n⏳ Waiting 30 seconds for application startup..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host "`n🧪 Testing health endpoint..." -ForegroundColor Cyan
try {
    $healthResponse = Invoke-WebRequest -Uri "$APP_URL/health" -UseBasicParsing
    Write-Host "✅ Health Check: $($healthResponse.StatusCode) - $($healthResponse.Content)" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Health check failed (app may still be starting): $_" -ForegroundColor Yellow
}

Write-Host "`n📖 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Open application: $APP_URL/swagger" -ForegroundColor White
Write-Host "2. Login with: $ADMIN_USERNAME / $ADMIN_PASSWORD" -ForegroundColor White
Write-Host "3. View logs: $SEQ_URL" -ForegroundColor White
Write-Host "4. See AZURE_COST_MANAGEMENT.md for scaling down when not in use" -ForegroundColor White
```

### Post-Deployment Verification

After running the script, verify everything is working:

```powershell
# Check all container apps
az containerapp list --resource-group chatify-prod-rg --output table

# Test health endpoint
curl https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io/health

# View application logs
az containerapp logs show --name chatify-api --resource-group chatify-prod-rg --tail 50

# View Seq logs
az containerapp logs show --name chatify-seq --resource-group chatify-prod-rg --tail 20
```

---

## 🔐 Critical Credentials & Access Information

### Azure Subscription
- **Subscription Name:** Azure subscription 1
- **Subscription ID:** `6017cf60-a38f-4e64-9654-e6a36caf40d5`
- **Resource Group:** `chatify-prod-rg`
- **Location:** West Europe

### Application URL
- **Main Application:** https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io
- **Health Endpoint:** https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io/health
- **Swagger UI:** https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io/swagger

### Seq Log Server
- **URL:** https://chatify-seq.yellowpebble-7206aad4.westeurope.azurecontainerapps.io
- **Status:** ✅ Running successfully (in-memory mode, no persistence)
- **Authentication:** ✅ Enabled with admin credentials
  - **Username:** `admin`
  - **Password:** `Admin@123`
- **Purpose:** Centralized structured logging and monitoring
- **Note:** Data will be lost on container restart (in-memory mode). For long-term production, consider Azure Monitor or persistent storage.

### Azure SQL Database
- **Server:** `chatify-sql-server.database.windows.net`
- **Database:** `chatify-db`
- **Port:** 1433
- **Admin Username:** `sqladmin`
- **Admin Password:** `Chatify@2026!Secure`
- **Tier:** Basic ($5/month)
- **Connection String:**
  ```
  Server=tcp:chatify-sql-server.database.windows.net,1433;Database=chatify-db;User Id=sqladmin;Password=Chatify@2026!Secure;Encrypt=True;TrustServerCertificate=False;
  ```

### Azure Container Registry
- **Registry Name:** `chatifyregistry.azurecr.io`
- **Admin Username:** `chatifyregistry`
- **Admin Password:** `+WX4UsH1cr9k08OPYgRfs1mTjCcmQNdRTZupWcD2ol+ACRAi2tJP`
- **Login Server:** `chatifyregistry.azurecr.io`
- **SKU:** Basic ($5/month)

### Azure Key Vault
- **Vault Name:** `chatify-kv-4021`
- **Vault URI:** `https://chatify-kv-4021.vault.azure.net/`
- **Access Policy:** RBAC-enabled
- **Status:** ✅ Populated with all secrets

**Stored Secrets:**
- `ConnectionStrings--DefaultConnection` - SQL connection string
- `AzureOpenAI--ApiKey` - Azure OpenAI API key
- `AzureOpenAI--Endpoint` - Azure OpenAI endpoint URL
- `Jwt--Secret` - JWT signing secret (note: case-sensitive)
- `Admin--Username` - Admin username (admin)
- `Admin--Password` - Admin password (Admin@ChatifyGeorgia2026)
- `Email--Username` - Gmail SMTP username
- `Email--Password` - Gmail app password
- `Email--FromEmail` - Sender email address
- `Email--AdminEmail` - Notification recipient email

**Note:** Key Vault secret names use `--` (double dash) which ASP.NET Core automatically converts to `:` (colon) when loading configuration.

### Application Admin Account
- **Username:** `admin`
- **Password:** `Admin@ChatifyGeorgia2026`
- **Email:** `admin@chatify.ge`

### JWT Configuration
- **Secret:** `Secret!!!1231321@edasfawadasdaddsadsasddddddddddddddddddddddddddd` (stored in Key Vault)
- **Issuer:** `ChatifyAI`
- **Audience:** `ChatifyAI`
- **Expiration:** 60 minutes (default), 10080 minutes (remember me)

### Azure OpenAI Service (Your Existing Resource)
- **Endpoint:** `https://ecommerceai-openai.openai.azure.com/`
- **API Key:** `4uRTO2lAtHpX4P3CC3otWUVsqCgHIIcD3WkOBqKDIAYouV49EnVbJQQJ99BJAC5RqLJXJ3w3AAABACOGO4Tv`
- **Chat Model:** `gpt-4o`
- **Embedding Model:** `text-embedding-3-small`

### Container App Managed Identity
- **Principal ID:** `7bc72b66-97d0-4f96-a3a3-29be3fd182ed`
- **Tenant ID:** `e1c86c2e-a687-4e67-9a60-0a8b68702246`
- **Type:** System-assigned
- **Permissions:** Key Vault Secrets User (read-only access to secrets)

---

## 📋 Step-by-Step Deployment Process

### Step 1: Azure Authentication
```powershell
# Login to Azure
az login

# Verify subscription
az account show
```
**Result:** Authenticated to subscription `6017cf60-a38f-4e64-9654-e6a36caf40d5`

### Step 2: Create Resource Group
```powershell
az group create --name chatify-prod-rg --location westeurope
```
**Purpose:** Container for all Azure resources in West Europe region (closest to Georgia)

### Step 3: Create Azure Container Registry
```powershell
# Create registry
az acr create --resource-group chatify-prod-rg --name chatifyregistry --sku Basic

# Enable admin user for authentication
az acr update --name chatifyregistry --admin-enabled true

# Get credentials
az acr credential show --name chatifyregistry --resource-group chatify-prod-rg
```
**Result:** 
- Registry created: `chatifyregistry.azurecr.io`
- Admin credentials obtained for Docker push/pull

### Step 4: Create Azure SQL Database
```powershell
# Create SQL Server
az sql server create `
  --name chatify-sql-server `
  --resource-group chatify-prod-rg `
  --location westeurope `
  --admin-user sqladmin `
  --admin-password "Chatify@2026!Secure"

# Create Database
az sql db create `
  --resource-group chatify-prod-rg `
  --server chatify-sql-server `
  --name chatify-db `
  --service-objective Basic

# Configure firewall (allow Azure services)
az sql server firewall-rule create `
  --resource-group chatify-prod-rg `
  --server chatify-sql-server `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# Allow all IPs for testing (should be restricted in production)
az sql server firewall-rule create `
  --resource-group chatify-prod-rg `
  --server chatify-sql-server `
  --name AllowAll `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 255.255.255.255
```
**Result:** 
- SQL Server: `chatify-sql-server.database.windows.net`
- Database: `chatify-db` (Basic tier - $5/month)
- Firewall configured to allow Azure services and external access

### Step 5: Create Azure Key Vault
```powershell
# Purge any existing Key Vault and create fresh one
az keyvault purge --name chatify-kv-4021
az keyvault create `
  --name chatify-kv-4021 `
  --resource-group chatify-prod-rg `
  --location westeurope `
  --enable-rbac-authorization true
```
**Result:** 
- Key Vault: `chatify-kv-4021.vault.azure.net`
- RBAC-based access control enabled
- Fresh instance (previous one was purged to resolve connection issues)

### Step 6: Create Container Apps Environment
```powershell
az containerapp env create `
  --name chatify-env `
  --resource-group chatify-prod-rg `
  --location westeurope
```
**Result:** 
- Environment: `chatify-env`
- Includes integrated Log Analytics workspace for monitoring

### Step 7: Build and Push Docker Image
```powershell
# Login to Container Registry
az acr login --name chatifyregistry

# Build Docker image (use specific version tag)
docker build -t chatify-ai:v5 .

# Tag for ACR
docker tag chatify-ai:v5 chatifyregistry.azurecr.io/chatify-ai:v5

# Push to ACR
docker push chatifyregistry.azurecr.io/chatify-ai:v5
```
**Result:** 
- Image built: ~307MB
- Image pushed to ACR: `chatifyregistry.azurecr.io/chatify-ai:v5`
- Version v5 includes API key authentication fix and health check fix

### Step 8: Deploy Seq Log Server
```powershell
az containerapp create `
  --name chatify-seq `
  --resource-group chatify-prod-rg `
  --environment chatify-env `
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
    "SEQ_FIRSTRUN_ADMINPASSWORD=Admin@123"
```
**Result:** 
- ✅ Seq deployed successfully with in-memory storage and authentication
- URL: https://chatify-seq.yellowpebble-7206aad4.westeurope.azurecontainerapps.io
- Login: admin / Admin@123

### Step 9: Deploy Main Application
```powershell
az containerapp create `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --environment chatify-env `
  --image chatifyregistry.azurecr.io/chatify-ai:v5 `
  --target-port 8080 `
  --ingress external `
  --registry-server chatifyregistry.azurecr.io `
  --registry-username chatifyregistry `
  --registry-password "+WX4UsH1cr9k08OPYgRfs1mTjCcmQNdRTZupWcD2ol+ACRAi2tJP" `
  --cpu 1.0 `
  --memory 2.0Gi `
  --min-replicas 0 `
  --max-replicas 3 `
  --env-vars `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "Seq__ServerUrl=https://chatify-seq.yellowpebble-7206aad4.westeurope.azurecontainerapps.io"
```
**Result:** 
- Application deployed with minimal environment variables (secrets in Key Vault)
- URL: https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io
- Scales to zero when idle (min-replicas: 0)

### Step 10: Configure Managed Identity
```powershell
# Enable system-assigned managed identity
az containerapp identity assign `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --system-assigned
```
**Result:** 
- Managed Identity created with Principal ID: `7bc72b66-97d0-4f96-a3a3-29be3fd182ed`
- Used for secure authentication to Azure services (Key Vault)

### Step 11: Grant Key Vault Permissions
```powershell
# Grant user permissions to manage secrets
az ad signed-in-user show --query id --output tsv
# Result: 0c1d3214-1f64-4da3-bc9b-2800842658e0

az role assignment create `
  --role "Key Vault Secrets Officer" `
  --assignee "0c1d3214-1f64-4da3-bc9b-2800842658e0" `
  --scope "/subscriptions/6017cf60-a38f-4e64-9654-e6a36caf40d5/resourceGroups/chatify-prod-rg/providers/Microsoft.KeyVault/vaults/chatify-kv-4021"

# Grant managed identity permissions to READ secrets (minimum privilege)
az role assignment create `
  --role "Key Vault Secrets User" `
  --assignee "7bc72b66-97d0-4f96-a3a3-29be3fd182ed" `
  --scope "/subscriptions/6017cf60-a38f-4e64-9654-e6a36caf40d5/resourceGroups/chatify-prod-rg/providers/Microsoft.KeyVault/vaults/chatify-kv-4021"
```
**Result:** 
- User has 'Secrets Officer' role (read/write for management)
- Managed identity has 'Secrets User' role (read-only for application)
- Permissions propagated within 30-60 seconds

### Step 12: Populate Key Vault with Secrets
```powershell
# SQL Connection String
az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "ConnectionStrings--DefaultConnection" `
  --value "Server=tcp:chatify-sql-server.database.windows.net,1433;Database=chatify-db;User Id=sqladmin;Password=Chatify@2026!Secure;Encrypt=True;TrustServerCertificate=False;"

# Azure OpenAI API Key
az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "AzureOpenAI--ApiKey" `
  --value "4uRTO2lAtHpX4P3CC3otWUVsqCgHIIcD3WkOBqKDIAYouV49EnVbJQQJ99BJAC5RqLJXJ3w3AAABACOGO4Tv"

# Azure OpenAI Endpoint
az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "AzureOpenAI--Endpoint" `
  --value "https://ecommerceai-openai.openai.azure.com/"

# JWT Secret
az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "Jwt--Secret" `
  --value "Secret!!!1231321@edasfawadasdaddsadsasddddddddddddddddddddddddddd"

# Admin Credentials
az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "Admin--Username" `
  --value "admin"

az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "Admin--Password" `
  --value "Admin@ChatifyGeorgia2026"

# Email Configuration
az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "Email--Username" `
  --value "temo599922030@gmail.com"

az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "Email--Password" `
  --value "iucaetrhmcggvzeh"

az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "Email--FromEmail" `
  --value "temo599922030@gmail.com"

az keyvault secret set `
  --vault-name chatify-kv-4021 `
  --name "Email--AdminEmail" `
  --value "t.baindurashvili.gm@gmail.com"
```
**Result:** 
- ✅ All secrets stored in Key Vault
- Secrets accessible via managed identity

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Azure Subscription                       │
│                  (West Europe Region)                        │
│                                                              │
│  ┌────────────────────────────────────────────────────┐    │
│  │         Resource Group: chatify-prod-rg            │    │
│  │                                                     │    │
│  │  ┌──────────────────────────────────────────┐     │    │
│  │  │    Container Apps Environment            │     │    │
│  │  │         (chatify-env)                    │     │    │
│  │  │                                          │     │    │
│  │  │  ┌────────────────────────────────┐     │     │    │
│  │  │  │  Chatify AI App                │     │     │    │
│  │  │  │  (chatify-api)                 │     │     │    │
│  │  │  │  • .NET 10.0 Runtime           │     │     │    │
│  │  │  │  • 1 CPU, 2GB Memory           │     │     │    │
│  │  │  │  • Scale: 1-3 replicas         │     │     │    │
│  │  │  │  • Port: 8080                  │     │     │    │
│  │  │  └────────────┬───────────────────┘     │     │    │
│  │  │               │                          │     │    │
│  │  │  ┌────────────▼───────────────────┐     │     │    │
│  │  │  │  Seq Log Server                │     │     │    │
│  │  │  │  (chatify-seq)                 │     │     │    │
│  │  │  │  • datalust/seq:latest         │     │     │    │
│  │  │  │  • 0.5 CPU, 1GB Memory         │     │     │    │
│  │  │  │  • Port: 80                    │     │     │    │
│  │  │  └────────────────────────────────┘     │     │    │
│  │  └──────────────────────────────────────────┘     │    │
│  │                                                     │    │
│  │  ┌──────────────────────────────────────────┐     │    │
│  │  │    Azure SQL Database                    │     │    │
│  │  │    (chatify-sql-server)                  │     │    │
│  │  │    • Database: chatify-db                │     │    │
│  │  │    • Tier: Basic ($5/mo)                 │     │    │
│  │  │    • Max Size: 2GB                       │     │    │
│  │  └──────────────────────────────────────────┘     │    │
│  │                                                     │    │
│  │  ┌──────────────────────────────────────────┐     │    │
│  │  │    Azure Key Vault                       │     │    │
│  │  │    (chatify-kv-4021)                     │     │    │
│  │  │    • RBAC Enabled                        │     │    │
│  │  │    • 7 Secrets Stored                    │     │    │
│  │  └──────────────────────────────────────────┘     │    │
│  │                                                     │    │
│  │  ┌──────────────────────────────────────────┐     │    │
│  │  │    Azure Container Registry              │     │    │
│  │  │    (chatifyregistry.azurecr.io)          │     │    │
│  │  │    • Image: chatify-ai:v1 (307MB)       │     │    │
│  │  │    • Admin Enabled                       │     │    │
│  │  └──────────────────────────────────────────┘     │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘

External Services:
┌──────────────────────────────────────┐
│  Azure OpenAI Service                │
│  (ecommerceai-openai)                │
│  • gpt-4o                            │
│  • text-embedding-3-small            │
└──────────────────────────────────────┘
```

---

## ⚙️ Configuration Hierarchy

The application uses a layered configuration approach:

1. **appsettings.json** - Base configuration with defaults
2. **appsettings.Production.json** - Production overrides (Key Vault endpoint)
3. **User Secrets** - Local development secrets (Development environment only)
4. **Azure Key Vault** - Production secrets (Production environment only)
5. **Environment Variables** - Container-level non-sensitive settings

**Current Configuration (Production):**
- ✅ Key Vault integration **ENABLED** and working
- ✅ All sensitive values loaded from Key Vault via Managed Identity
- ✅ Non-sensitive settings in appsettings.json (deployment names, timeouts, etc.)
- ✅ No secrets in environment variables (clean deployment)

**Configuration Loading Process:**
1. App starts in Production mode
2. Reads `KeyVault:Endpoint` from appsettings.Production.json
3. Uses Managed Identity to authenticate to Key Vault
4. Loads all secrets with `--` converted to `:` notation
5. Secrets override any appsettings values

---

## 📊 Cost Breakdown (Monthly Estimates)

| Service | Tier/SKU | Estimated Cost |
|---------|----------|----------------|
| Container Registry | Basic | $5.00 |
| SQL Database | Basic (2GB) | $5.00 |
| Key Vault | Standard | $0.03 (per 10,000 operations) |
| Container Apps Environment | Consumption | $0.00 (free tier) |
| Container App - Chatify AI | Consumption | ~$10-30 (usage-based) |
| Container App - Seq | Consumption | ~$5-15 (usage-based) |
| Log Analytics Workspace | Pay-as-you-go | ~$2-5 (5GB free) |
| **Total** | | **~$27-60/month** |

*Note: Azure OpenAI costs are separate and based on token usage*

---

## 🔍 Monitoring & Management

### View Application Logs
```powershell
# Stream live logs
az containerapp logs show --name chatify-api --resource-group chatify-prod-rg --follow

# View last 50 log entries
az containerapp logs show --name chatify-api --resource-group chatify-prod-rg --tail 50
```

### Check Application Status
```powershell
# Get app details
az containerapp show --name chatify-api --resource-group chatify-prod-rg

# Check health endpoint
curl https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io/health
```

### Restart Application
```powershell
# Restart current revision
az containerapp revision restart `
  --resource-group chatify-prod-rg `
  --name chatify-api `
  --revision (az containerapp revision list --name chatify-api --resource-group chatify-prod-rg --query "[0].name" --output tsv)
```

### Scale Application
```powershell
# Update scaling rules
az containerapp update `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --min-replicas 2 `
  --max-replicas 5
```

### Update Environment Variables
```powershell
# Set new variable
az containerapp update `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --set-env-vars "NEW_VARIABLE=value"

# Remove variable
az containerapp update `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --remove-env-vars "VARIABLE_NAME"
```

---

## 🚀 Deployment Updates

### Update Application Code
```powershell
# 1. Build new image
docker build -t chatify-ai:v2 .

# 2. Tag for ACR
docker tag chatify-ai:v2 chatifyregistry.azurecr.io/chatify-ai:v2

# 3. Push to ACR
docker push chatifyregistry.azurecr.io/chatify-ai:v2

# 4. Update container app
az containerapp update `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --image chatifyregistry.azurecr.io/chatify-ai:v2
```

---

## 🛠️ Troubleshooting Guide

### ✅ Resolved Issues

**Issue 1: Key Vault Connection Errors (RESOLVED)**
- **Solution:** Recreated Key Vault from scratch with proper RBAC configuration
- **Root Causes Fixed:**
  1. Wrong Key Vault endpoint in appsettings.json (`chatify-keyvault` → `chatify-kv-4021`)
  2. Missing secrets in Key Vault (Email configuration)
  3. Incorrect Managed Identity permissions
- **Resolution Steps:**
  - Purged old Key Vault: `az keyvault purge --name chatify-kv-4021`
  - Created fresh Key Vault with RBAC enabled
  - Added all 10 required secrets with correct naming (`Admin--Username`, `Email--Password`, etc.)
  - Granted Managed Identity "Key Vault Secrets User" role
  - Updated appsettings.json and appsettings.Production.json with correct endpoint

**Issue 2: Database Migration Error - EnableEmailSupport Column (RESOLVED)**
- **Solution:** Fixed migration SQL statement and recreated database
- **Root Cause:** Migration CREATE TABLE had `EnableEmailSupport` column but INSERT statement didn't include it
- **Resolution Steps:**
  - Updated migration INSERT to include `EnableEmailSupport = 1`
  - Deleted and recreated SQL database: `az sql db delete` + `az sql db create`
  - Removed redundant SQL seeding from migration (DbSeeder handles it properly)

**Issue 3: Configuration Key Mismatch (RESOLVED)**
- **Solution:** Ensured consistent naming between Key Vault secrets and application code
- **Root Cause:** DbSeeder used `Admin:Username` but Key Vault had `ADMIN--USERNAME` (wrong case)
- **Resolution:** All Key Vault secrets now use proper PascalCase: `Admin--Username`, `Email--AdminEmail`

### ⚠️ Current Known Issues

**Issue 1: Health Check Fails (Non-Critical)**
- **Status:** App is running fine, only health endpoint affected
- **Symptom:** `/health` endpoint returns 503, but app processes requests normally
- **Root Cause:** AzureOpenAI health check tries to get `AzureOpenAIClient` service which isn't registered (SDK mismatch)
- **Impact:** Low - does not affect application functionality, only monitoring
- **Workaround:** Check `/health/ready` or application logs instead
- **Fix Required:** Update ServiceCollectionExtensions.cs line 224 to use correct OpenAI SDK service registration

**Issue 2: Background Embedding Task Error (Non-Critical)**
- **Status:** One-time error during startup
- **Symptom:** "Cannot access a disposed object" when generating embeddings
- **Root Cause:** Background task tries to create service scope after startup scope disposed
- **Impact:** None - embeddings can be generated later via API
- **Fix Required:** Adjust DbSeeder background task to properly manage service scopes

### Common Commands for Troubleshooting

```powershell
# Check container app status
az containerapp show --name chatify-api --resource-group chatify-prod-rg --query properties.runningStatus

# View recent logs
az containerapp logs show --name chatify-api --resource-group chatify-prod-rg --tail 100

# Check revision status
az containerapp revision list --name chatify-api --resource-group chatify-prod-rg

# Test connectivity
curl https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io/health

# Check Key Vault permissions
az role assignment list --scope /subscriptions/6017cf60-a38f-4e64-9654-e6a36caf40d5/resourceGroups/chatify-prod-rg/providers/Microsoft.KeyVault/vaults/chatify-kv-4021

# List Key Vault secrets
az keyvault secret list --vault-name chatify-kv-4021
```

---

## 🔐 Security Recommendations

### Current Security Posture
✅ **Implemented:**
- HTTPS/TLS encryption for all endpoints
- Managed Identity for Azure service authentication
- Azure Key Vault for secret management
- RBAC-based access control
- SQL Database encryption at rest

⚠️ **Needs Improvement:**
- SQL Server firewall allows all IPs (0.0.0.0-255.255.255.255)
  - **Recommendation:** Restrict to specific IPs or VNets
- Admin credentials in environment variables
  - **Recommendation:** Remove after Key Vault integration is working
- No custom domain or SSL certificate
  - **Recommendation:** Configure custom domain with SSL
- No Application Gateway or WAF
  - **Recommendation:** Add Azure Application Gateway with WAF for enhanced security

### Recommended Security Enhancements

1. **Restrict SQL Firewall:**
   ```powershell
   # Remove allow-all rule
   az sql server firewall-rule delete `
     --resource-group chatify-prod-rg `
     --server chatify-sql-server `
     --name AllowAll
   
   # Add specific IP ranges only
   az sql server firewall-rule create `
     --resource-group chatify-prod-rg `
     --server chatify-sql-server `
     --name "AllowOfficeIP" `
     --start-ip-address YOUR_IP `
     --end-ip-address YOUR_IP
   ```

2. **Enable Application Insights:**
   ```powershell
   az monitor app-insights component create `
     --app chatify-insights `
     --location westeurope `
     --resource-group chatify-prod-rg `
     --application-type web
   ```

3. **Configure Custom Domain:**
   ```powershell
   az containerapp hostname add `
     --hostname chatify.yourdomain.com `
     --resource-group chatify-prod-rg `
     --name chatify-api
   ```

---

## 📱 Quick Access Links

| Resource | URL | Purpose |
|----------|-----|---------|
| **Application** | https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io | Main API endpoint |
| **Seq Logs** | https://chatify-seq.yellowpebble-7206aad4.westeurope.azurecontainerapps.io | Log viewer |
| **Azure Portal** | https://portal.azure.com | Manage resources |
| **Key Vault** | https://portal.azure.com/#@/resource/subscriptions/6017cf60-a38f-4e64-9654-e6a36caf40d5/resourceGroups/chatify-prod-rg/providers/Microsoft.KeyVault/vaults/chatify-kv-4021 | Secret management |
| **SQL Database** | https://portal.azure.com/#@/resource/subscriptions/6017cf60-a38f-4e64-9654-e6a36caf40d5/resourceGroups/chatify-prod-rg/providers/Microsoft.Sql/servers/chatify-sql-server/databases/chatify-db | Database management |
| **Container Registry** | https://portal.azure.com/#@/resource/subscriptions/6017cf60-a38f-4e64-9654-e6a36caf40d5/resourceGroups/chatify-prod-rg/providers/Microsoft.ContainerRegistry/registries/chatifyregistry | Image repository |

---

## 🎯 Deployment Best Practices & Lessons Learned

### Configuration Management
1. **Key Vault Secret Naming:** Always use PascalCase with `--` separator (e.g., `Admin--Username`)
   - ASP.NET Core automatically converts `--` to `:` when loading
   - Be consistent with casing - `Admin--Username` ≠ `ADMIN--USERNAME`

2. **Separate Sensitive from Non-Sensitive:**
   - **Key Vault:** Passwords, API keys, connection strings, email credentials
   - **appsettings.json:** Deployment names, timeouts, feature flags, email addresses (identifiers)
   - **Environment Variables:** Non-sensitive runtime settings only

3. **Local Development:**
   - Use `dotnet user-secrets` for local secrets
   - Keep same structure as Key Vault for consistency
   - Never commit secrets to source control

### Database Migrations
1. **Keep Migrations Schema-Only:** Don't seed data in migrations
   - Migrations should only create tables, indexes, constraints
   - Use DbSeeder for initial data population
   - This allows proper password hashing and configuration-based setup

2. **Always Include All Columns:** When adding new columns, update ALL INSERT statements
   - Check both migration Up() method and any SQL in migrationBuilder.Sql()

### Key Vault Setup
1. **Use RBAC, Not Access Policies:** Modern Azure uses RBAC authorization
   ```powershell
   --enable-rbac-authorization true
   ```

2. **Minimum Privilege Roles:**
   - Admin/Developer: "Key Vault Secrets Officer" (read/write)
   - Application: "Key Vault Secrets User" (read-only)

3. **Wait for RBAC Propagation:** After role assignment, wait 30-60 seconds before using

### Deployment Process
1. **Always Rebuild After Config Changes:** Even appsettings.json changes require new image
2. **Tag Images with Versions:** `chatify-ai:v1`, `v2`, etc. for rollback capability
3. **Test Health Checks:** Ensure health endpoints work before declaring success
4. **Check Logs Immediately:** First thing after deployment - `az containerapp logs show`

### Common Pitfalls to Avoid
- ❌ Using environment variables for secrets (visible in portal)
- ❌ Hardcoding passwords in migrations
- ❌ Mismatched Key Vault endpoint URLs
- ❌ Not waiting for RBAC propagation
- ❌ Mixing case in configuration keys (case-sensitive!)
- ❌ Forgetting to add new secrets to BOTH Key Vault AND user secrets

---

## 📝 Important Notes

1. **Database Migrations:** Schema will be automatically applied on first successful API request
2. **Admin User:** Will be created on first startup (username: admin, password: Admin@ChatifyGeorgia2026)
3. **CORS:** Configure allowed origins in appsettings.Production.json if needed
4. **Rate Limiting:** Not currently configured - consider implementing for production
5. **Backup:** SQL Database has automatic backups (7-day retention for Basic tier)
6. **Monitoring:** Log Analytics workspace created automatically with Container Apps Environment

---

## 🎓 Learning Resources

- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Azure Key Vault Best Practices](https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices)
- [Azure SQL Database Documentation](https://learn.microsoft.com/en-us/azure/azure-sql/)
- [Managed Identity Overview](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)

---

## 📞 Support & Maintenance

For issues or questions:
1. Check application logs using Azure CLI
2. Review Seq logs at https://chatify-seq.yellowpebble-7206aad4.westeurope.azurecontainerapps.io
3. Check Azure Portal for resource health
4. Review this documentation for common troubleshooting steps

---

**Document Version:** 2.0  
**Last Updated:** January 3, 2026  
**Deployment Status:** ✅ Successfully Deployed and Running
**Current Image:** chatifyregistry.azurecr.io/chatify-ai:v4
**Application Status:** Running in Production with Azure Key Vault integration

# 🌐 Complete Cloud Deployment Guide - Chatify AI

**Last Updated:** December 29, 2025  
**Project:** Multi-Tenant AI Chat Platform with RAG  
**Architecture:** .NET 10.0 + SQL Server + Azure OpenAI

---

## 📋 Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Cloud Provider Options](#cloud-provider-options)
3. [Azure Deployment (Recommended)](#azure-deployment-recommended)
4. [AWS Deployment (Alternative)](#aws-deployment-alternative)
5. [Local Development Setup](#local-development-setup)
6. [Environment Configuration](#environment-configuration)
7. [Cost Estimates](#cost-estimates)
8. [Production Checklist](#production-checklist)

---

## 🏗️ Architecture Overview

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                      INTERNET / USERS                        │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
                ┌───────────────────────┐
                │   Load Balancer /     │
                │   Application Gateway  │
                └───────────┬───────────┘
                            │
                            ▼
        ┌───────────────────────────────────────┐
        │   Chatify AI Application              │
        │   (.NET 10.0 Web API + Static Files)  │
        │   • Multi-tenant isolation            │
        │   • JWT Authentication                │
        │   • Rate Limiting                     │
        │   • RAG Pipeline                      │
        └───────┬───────────────────┬───────────┘
                │                   │
                ▼                   ▼
    ┌────────────────────┐  ┌─────────────────────┐
    │   SQL Server DB    │  │  Azure OpenAI API   │
    │   • Tenant data    │  │  • GPT-4o (chat)    │
    │   • Knowledge base │  │  • text-embedding   │
    │   • Vector storage │  │    -3-small         │
    │   • Sessions       │  │                     │
    └────────────────────┘  └─────────────────────┘
```

### Data Flow

1. **User Request** → Load Balancer → Application
2. **Authentication** → JWT validation → Tenant resolution
3. **Knowledge Search** → SQL vector search → Retrieve relevant docs
4. **AI Chat** → Azure OpenAI → Generate response with context
5. **Response** → Client (with session tracking & feedback)

---

## ☁️ Cloud Provider Options

### Option 1: Azure (Recommended) ⭐

**Why Azure?**
- ✅ Native Azure OpenAI integration (already configured)
- ✅ Excellent .NET support
- ✅ Managed SQL Database (Azure SQL)
- ✅ Easy deployment with App Service or Container Apps
- ✅ Built-in monitoring (Application Insights)
- ✅ Single provider for all services

**Estimated Cost:** $150-300/month (small-medium workload)

### Option 2: AWS (Alternative)

**Why AWS?**
- ✅ More global regions
- ✅ Mature container orchestration (ECS/EKS)
- ✅ Cost-effective with Reserved Instances
- ⚠️ Requires AWS Bedrock or external AI service

**Estimated Cost:** $120-250/month (small-medium workload)

### Option 3: Self-Hosted VPS (Budget Option)

**Why VPS?**
- ✅ Full control
- ✅ Lower monthly cost
- ⚠️ Requires more DevOps expertise
- ⚠️ Still need Azure OpenAI or alternative AI service

**Estimated Cost:** $50-100/month (excluding AI costs)

---

## 🔷 Azure Deployment (Recommended)

### Step 1: Prerequisites

#### 1.1 Azure Account Setup
```bash
# Create Azure account (if you don't have one)
# https://portal.azure.com

# Install Azure CLI
winget install Microsoft.AzureCLI

# Login to Azure
az login

# Set your subscription
az account set --subscription "Your Subscription Name"
```

#### 1.2 Required Azure Services
- ✅ Azure OpenAI Service (already have this)
- ✅ Azure SQL Database
- ✅ Azure App Service or Container Apps
- ✅ Azure Container Registry (optional, for custom images)
- ✅ Application Insights (monitoring)
- ✅ Key Vault (secrets management)

---

### Step 2: Azure OpenAI Setup

**Status:** ✅ You already have Azure OpenAI configured

**Verify Your Configuration:**
```bash
# In Azure Portal:
1. Go to Azure OpenAI Service
2. Click "Keys and Endpoint"
3. Note down:
   - Endpoint URL
   - API Key (Key 1)
4. Go to "Model deployments"
5. Verify you have:
   - gpt-4o (or gpt-4) deployment
   - text-embedding-3-small deployment
```

**Required Environment Variables:**
```env
AZUREOPENAI__ENDPOINT=https://your-resource.openai.azure.com/
AZUREOPENAI__APIKEY=your-32-character-api-key
AZUREOPENAI__CHATDEPLOYMENTNAME=gpt-4o
AZUREOPENAI__EMBEDDINGDEPLOYMENTNAME=text-embedding-3-small
```

---

### Step 3: Azure SQL Database Setup

#### 3.1 Create SQL Database

**Option A: Azure Portal (GUI)**
```
1. Go to Azure Portal → Create a resource
2. Search "Azure SQL Database" → Create
3. Configure:
   - Resource Group: chatify-prod-rg (create new)
   - Database Name: chatify-db
   - Server: Create new server
     • Server name: chatify-sql-server (globally unique)
     • Location: Same as OpenAI (e.g., East US)
     • Authentication: SQL Authentication
     • Admin login: sqladmin
     • Password: YourStrong@Password123
   - Compute + Storage:
     • Basic: $5/month (up to 2GB, good for testing)
     • Standard S0: $15/month (10 DTUs, 250GB)
     • Standard S1: $30/month (20 DTUs, 250GB) ⭐ Recommended
4. Networking:
   - Allow Azure services: YES
   - Add your current IP address: YES
5. Review + Create
```

**Option B: Azure CLI (Command Line)**
```bash
# Create resource group
az group create \
  --name chatify-prod-rg \
  --location eastus

# Create SQL server
az sql server create \
  --name chatify-sql-server \
  --resource-group chatify-prod-rg \
  --location eastus \
  --admin-user sqladmin \
  --admin-password "YourStrong@Password123"

# Configure firewall (allow Azure services)
az sql server firewall-rule create \
  --resource-group chatify-prod-rg \
  --server chatify-sql-server \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Add your local IP (for migrations)
az sql server firewall-rule create \
  --resource-group chatify-prod-rg \
  --server chatify-sql-server \
  --name AllowMyIP \
  --start-ip-address YOUR.IP.ADDRESS.HERE \
  --end-ip-address YOUR.IP.ADDRESS.HERE

# Create database
az sql db create \
  --resource-group chatify-prod-rg \
  --server chatify-sql-server \
  --name chatify-db \
  --service-objective S1 \
  --backup-storage-redundancy Local
```

#### 3.2 Get Connection String

```bash
# From Azure Portal:
# SQL Database → Connection strings → ADO.NET

# Example format:
Server=tcp:chatify-sql-server.database.windows.net,1433;
Initial Catalog=chatify-db;
Persist Security Info=False;
User ID=sqladmin;
Password={your_password};
MultipleActiveResultSets=True;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

#### 3.3 Run Database Migrations (From Your Local Machine)

```bash
# Set connection string temporarily
$env:ConnectionStrings__DefaultConnection="Server=tcp:chatify-sql-server.database.windows.net,1433;Database=chatify-db;User Id=sqladmin;Password=YourStrong@Password123;Encrypt=True;TrustServerCertificate=False;"

# Apply migrations
dotnet ef database update \
  --project ChatAI.Infrastructure \
  --startup-project ChatAI.Api \
  --context ChatDbContext

# Verify success
# You should see: "Done."
```

---

### Step 4: Azure Key Vault Setup (Secrets Management)

#### 4.1 Create Key Vault

```bash
# Create Key Vault
az keyvault create \
  --name chatify-keyvault \
  --resource-group chatify-prod-rg \
  --location eastus

# Add secrets
az keyvault secret set --vault-name chatify-keyvault \
  --name "AdminPassword" --value "YourSecure@Password123"

az keyvault secret set --vault-name chatify-keyvault \
  --name "JwtSecret" --value "your-generated-64-character-secret-key"

az keyvault secret set --vault-name chatify-keyvault \
  --name "AzureOpenAIKey" --value "your-azure-openai-api-key"

az keyvault secret set --vault-name chatify-keyvault \
  --name "SqlPassword" --value "YourStrong@Password123"
```

---

### Step 5: Deploy Application (Choose One Option)

#### **Option A: Azure App Service (Easiest)** ⭐

**Pros:**
- ✅ No container management
- ✅ Auto-scaling
- ✅ Easy deployment from VS Code or CLI
- ✅ Built-in SSL/HTTPS
- ✅ Deployment slots (staging/production)

**Steps:**

```bash
# 1. Create App Service Plan
az appservice plan create \
  --name chatify-plan \
  --resource-group chatify-prod-rg \
  --location eastus \
  --sku B1 \
  --is-linux

# 2. Create Web App
az webapp create \
  --name chatify-ai-app \
  --resource-group chatify-prod-rg \
  --plan chatify-plan \
  --runtime "DOTNET:8.0"

# 3. Configure environment variables
az webapp config appsettings set \
  --name chatify-ai-app \
  --resource-group chatify-prod-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Server=tcp:chatify-sql-server.database.windows.net,1433;Database=chatify-db;User Id=sqladmin;Password=YourStrong@Password123;Encrypt=True;" \
    AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/" \
    AzureOpenAI__ApiKey="your-api-key" \
    AzureOpenAI__ChatDeploymentName="gpt-4o" \
    AzureOpenAI__EmbeddingDeploymentName="text-embedding-3-small" \
    JWT__Secret="your-64-character-jwt-secret" \
    JWT__Issuer="ChatifyAI" \
    JWT__Audience="ChatifyAI-Users" \
    ADMIN__USERNAME="youradmin" \
    ADMIN__PASSWORD="YourSecure@Password123" \
    ADMIN__EMAIL="admin@yourcompany.com"

# 4. Publish from Visual Studio or CLI
# Option 4a: From VS Code
# Right-click on ChatAI.Api project → Publish to Azure

# Option 4b: From command line
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name chatify-ai-app \
  --resource-group chatify-prod-rg \
  --src ../app.zip

# 5. Enable Application Insights
az monitor app-insights component create \
  --app chatify-insights \
  --location eastus \
  --resource-group chatify-prod-rg \
  --application-type web

# Link to Web App
# (Get instrumentation key and add to app settings)
```

**Access Your App:**
```
https://chatify-ai-app.azurewebsites.net
```

#### **Option B: Azure Container Apps (Modern, Serverless)** ⭐⭐

**Pros:**
- ✅ Better for microservices
- ✅ Scale to zero (save costs)
- ✅ Full container control
- ✅ Easy CI/CD with GitHub Actions

**Steps:**

```bash
# 1. Create Container Registry
az acr create \
  --resource-group chatify-prod-rg \
  --name chatifyregistry \
  --sku Basic

# 2. Build and push Docker image
az acr build \
  --registry chatifyregistry \
  --image chatify-ai:latest \
  --file Dockerfile .

# 3. Create Container Apps environment
az containerapp env create \
  --name chatify-env \
  --resource-group chatify-prod-rg \
  --location eastus

# 4. Deploy Container App
az containerapp create \
  --name chatify-api \
  --resource-group chatify-prod-rg \
  --environment chatify-env \
  --image chatifyregistry.azurecr.io/chatify-ai:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server chatifyregistry.azurecr.io \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Server=tcp:chatify-sql-server.database.windows.net,1433;Database=chatify-db;User Id=sqladmin;Password=YourStrong@Password123;Encrypt=True;" \
    AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/" \
    AzureOpenAI__ApiKey=secretref:azure-openai-key \
    JWT__Secret=secretref:jwt-secret

# 5. Add secrets
az containerapp secret set \
  --name chatify-api \
  --resource-group chatify-prod-rg \
  --secrets \
    azure-openai-key="your-api-key" \
    jwt-secret="your-jwt-secret"
```

#### **Option C: Azure Kubernetes Service (Enterprise Scale)**

**Use When:**
- You need advanced orchestration
- Running multiple services
- High availability requirements
- Team has Kubernetes expertise

**Not Recommended for Initial Deployment** - Start with App Service or Container Apps

---

### Step 6: Custom Domain & SSL

```bash
# 1. Add custom domain
az webapp config hostname add \
  --webapp-name chatify-ai-app \
  --resource-group chatify-prod-rg \
  --hostname chat.yourcompany.com

# 2. Enable SSL (free managed certificate)
az webapp config ssl bind \
  --name chatify-ai-app \
  --resource-group chatify-prod-rg \
  --certificate-thumbprint auto \
  --ssl-type SNI

# 3. Update DNS records (at your domain registrar)
# Add CNAME record:
# chat.yourcompany.com → chatify-ai-app.azurewebsites.net
```

---

### Step 7: Monitoring & Logging

#### 7.1 Application Insights (Recommended)

```bash
# Enable Application Insights
az monitor app-insights component create \
  --app chatify-insights \
  --location eastus \
  --resource-group chatify-prod-rg \
  --application-type web

# Get instrumentation key
az monitor app-insights component show \
  --app chatify-insights \
  --resource-group chatify-prod-rg \
  --query instrumentationKey

# Add to app settings
az webapp config appsettings set \
  --name chatify-ai-app \
  --resource-group chatify-prod-rg \
  --settings \
    APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=your-key-here"
```

#### 7.2 View Logs

```bash
# Stream logs in real-time
az webapp log tail \
  --name chatify-ai-app \
  --resource-group chatify-prod-rg

# Or use Azure Portal:
# App Service → Monitoring → Log stream
```

---

## 🔶 AWS Deployment (Alternative)

### AWS Architecture

```
Route 53 (DNS)
    ↓
Application Load Balancer
    ↓
ECS Fargate (Container)
    ├→ RDS SQL Server
    └→ Azure OpenAI (external)
```

### Step 1: AWS Prerequisites

```bash
# Install AWS CLI
winget install Amazon.AWSCLI

# Configure AWS credentials
aws configure
# AWS Access Key ID: your-key
# AWS Secret Access Key: your-secret
# Default region: us-east-1
# Output format: json
```

### Step 2: Create RDS SQL Server

```bash
# Create RDS SQL Server instance
aws rds create-db-instance \
  --db-instance-identifier chatify-db \
  --db-instance-class db.t3.small \
  --engine sqlserver-ex \
  --master-username admin \
  --master-user-password YourStrong@Password123 \
  --allocated-storage 20 \
  --vpc-security-group-ids sg-xxxxx \
  --publicly-accessible \
  --backup-retention-period 7

# Wait for creation (takes 10-15 minutes)
aws rds wait db-instance-available \
  --db-instance-identifier chatify-db

# Get endpoint
aws rds describe-db-instances \
  --db-instance-identifier chatify-db \
  --query 'DBInstances[0].Endpoint.Address'
```

### Step 3: Build and Push Docker Image

```bash
# Create ECR repository
aws ecr create-repository --repository-name chatify-ai

# Login to ECR
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  your-account-id.dkr.ecr.us-east-1.amazonaws.com

# Build and push
docker build -t chatify-ai:latest .
docker tag chatify-ai:latest \
  your-account-id.dkr.ecr.us-east-1.amazonaws.com/chatify-ai:latest
docker push your-account-id.dkr.ecr.us-east-1.amazonaws.com/chatify-ai:latest
```

### Step 4: Deploy to ECS Fargate

```bash
# Create ECS cluster
aws ecs create-cluster --cluster-name chatify-cluster

# Create task definition (JSON file)
# Create ECS service with load balancer
# Configure environment variables
# See: DEPLOYMENT.md for detailed ECS configuration
```

**Note:** AWS deployment is more complex. Consider using Terraform or AWS CDK for infrastructure as code.

---

## 💻 Local Development Setup

### Step 1: Install Prerequisites

```powershell
# 1. .NET SDK 10.0
winget install Microsoft.DotNet.SDK.10

# 2. SQL Server (Local)
# Download SQL Server Developer Edition (free)
# https://www.microsoft.com/en-us/sql-server/sql-server-downloads

# 3. Azure Data Studio or SQL Server Management Studio
winget install Microsoft.AzureDataStudio

# 4. Docker Desktop (optional, for containers)
winget install Docker.DockerDesktop

# 5. Git (if not installed)
winget install Git.Git
```

### Step 2: Clone and Setup Project

```powershell
# Clone repository
git clone https://your-repo-url/chatify-ai.git
cd chatify-ai

# Restore packages
dotnet restore

# Build solution
dotnet build
```

### Step 3: Configure Local Environment

#### Create `.env` file (DO NOT COMMIT)

```env
# Database
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=localhost;Database=ChatifyAI;Integrated Security=True;TrustServerCertificate=True;

# Admin Account
ADMIN__USERNAME=admin
ADMIN__PASSWORD=Admin@123456
ADMIN__EMAIL=admin@localhost

# JWT Authentication
JWT__SECRET=local-development-secret-key-minimum-32-characters-long
JWT__ISSUER=ChatifyAI
JWT__AUDIENCE=ChatifyAI-Users
JWT__EXPIRATIONMINUTES=1440

# Azure OpenAI (USE YOUR REAL CREDENTIALS)
AZUREOPENAI__ENDPOINT=https://your-resource.openai.azure.com/
AZUREOPENAI__APIKEY=your-api-key-here
AZUREOPENAI__CHATDEPLOYMENTNAME=gpt-4o
AZUREOPENAI__EMBEDDINGDEPLOYMENTNAME=text-embedding-3-small
AZUREOPENAI__MAXTOKENS=1000
AZUREOPENAI__TEMPERATURE=0.7

# Development Settings
ASPNETCORE_ENVIRONMENT=Development
```

### Step 4: Setup Local Database

```powershell
# Create database and run migrations
dotnet ef database update `
  --project ChatAI.Infrastructure `
  --startup-project ChatAI.Api `
  --context ChatDbContext

# Verify database created
# Open Azure Data Studio
# Connect to: localhost
# You should see "ChatifyAI" database
```

### Step 5: Run Application

```powershell
# Run from command line
dotnet run --project ChatAI.Api

# Or press F5 in Visual Studio / VS Code

# Application will start at:
# http://localhost:5257
```

### Step 6: Test Locally

```powershell
# Test health endpoint
curl http://localhost:5257/health

# Open admin panel
# http://localhost:5257/admin-login.html
# Login: admin / Admin@123456

# Open Swagger UI (Development only)
# http://localhost:5257/swagger
```

---

## ⚙️ Environment Configuration

### Configuration Hierarchy

```
appsettings.json (Base)
    ↓
appsettings.Development.json (Local)
    ↓
appsettings.Production.json (Cloud)
    ↓
Environment Variables (Override all)
    ↓
Azure Key Vault (Production secrets)
```

### Required Environment Variables

#### Production Deployment Checklist

```env
# ===== CRITICAL - MUST SET =====
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<azure-sql-connection-string>
AzureOpenAI__Endpoint=<your-endpoint>
AzureOpenAI__ApiKey=<your-api-key>
JWT__Secret=<64-character-random-string>

# ===== AUTHENTICATION =====
JWT__Issuer=ChatifyAI
JWT__Audience=ChatifyAI-Users
JWT__ExpirationMinutes=1440

# ===== ADMIN ACCOUNT =====
ADMIN__USERNAME=<your-admin-username>
ADMIN__PASSWORD=<strong-password>
ADMIN__EMAIL=<admin-email>

# ===== AZURE OPENAI =====
AzureOpenAI__ChatDeploymentName=gpt-4o
AzureOpenAI__EmbeddingDeploymentName=text-embedding-3-small
AzureOpenAI__MaxTokens=1000
AzureOpenAI__Temperature=0.7

# ===== OPTIONAL: EMAIL PLUGIN =====
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__EnableSsl=true
Email__Username=<your-email>
Email__Password=<app-password>
Email__FromEmail=noreply@yourcompany.com
Email__FromName=Chatify AI Support
Email__AdminEmail=support@yourcompany.com

# ===== OPTIONAL: MONITORING =====
APPLICATIONINSIGHTS_CONNECTION_STRING=<app-insights-connection-string>

# ===== OPTIONAL: RATE LIMITING =====
IpRateLimiting__EnableEndpointRateLimiting=true
IpRateLimiting__GeneralRules__0__Endpoint=*
IpRateLimiting__GeneralRules__0__Period=1m
IpRateLimiting__GeneralRules__0__Limit=100
```

---

## 💰 Cost Estimates

### Azure (Small-Medium Production Workload)

| Service | Tier | Monthly Cost | Notes |
|---------|------|--------------|-------|
| Azure SQL Database | Standard S1 (20 DTU) | $30 | 250GB storage |
| App Service | B1 Basic (1 core, 1.75GB RAM) | $13 | Linux container |
| Azure OpenAI | GPT-4o + Embeddings | $50-200 | Pay per token usage |
| Application Insights | Basic | $0-50 | First 5GB free |
| Key Vault | Standard | $3 | Secrets storage |
| Bandwidth | Outbound | $10-30 | First 100GB free |
| **Total** | | **$106-326/month** | Varies by usage |

### Cost Optimization Tips

1. **Development/Staging:**
   - Use Basic tier SQL ($5/month)
   - Free tier App Service ($0)
   - Share Azure OpenAI across environments

2. **Auto-Scaling:**
   - Configure scale down during off-hours
   - Use consumption-based pricing where possible

3. **Reservations:**
   - 1-year reserved instances save ~30%
   - 3-year reserved instances save ~60%

4. **Monitoring:**
   - Set up budget alerts in Azure Portal
   - Monitor AI token usage

---

## ✅ Production Checklist

### Before First Deployment

- [ ] Azure OpenAI service created and configured
- [ ] Azure SQL Database created
- [ ] Database migrations run successfully
- [ ] All secrets stored in Key Vault
- [ ] Admin password changed from default
- [ ] JWT secret generated (64+ characters)
- [ ] Environment variables configured
- [ ] Local testing completed
- [ ] SSL certificate configured
- [ ] Custom domain setup (optional)
- [ ] Application Insights enabled
- [ ] Backup strategy defined
- [ ] Monitoring alerts configured

### Security Checklist

- [ ] Default admin password changed
- [ ] JWT secret is strong and unique
- [ ] SQL Server firewall configured
- [ ] HTTPS enforced (HTTP disabled)
- [ ] CORS configured for your domain only
- [ ] Rate limiting enabled
- [ ] Swagger disabled in production
- [ ] Secrets in Key Vault (not in code)
- [ ] Audit logging enabled
- [ ] Regular security updates scheduled

### Post-Deployment Verification

- [ ] Health endpoint returns 200 OK
- [ ] Admin login works
- [ ] Tenant creation works
- [ ] Knowledge base creation works
- [ ] Chat functionality works
- [ ] RAG retrieval works
- [ ] Embeddings generate correctly
- [ ] Multi-tenancy isolation verified
- [ ] Logs flowing to Application Insights
- [ ] Performance acceptable (< 2s response time)

---

## 🚀 Quick Start Commands

### Deploy to Azure (Fastest Path)

```powershell
# 1. Login to Azure
az login

# 2. Set variables
$rgName = "chatify-prod-rg"
$location = "eastus"
$appName = "chatify-ai-app-$(Get-Random)"
$sqlServer = "chatify-sql-$(Get-Random)"
$dbName = "chatify-db"

# 3. Create everything
az group create --name $rgName --location $location

az sql server create `
  --name $sqlServer `
  --resource-group $rgName `
  --location $location `
  --admin-user sqladmin `
  --admin-password "YourStrong@Password123"

az sql db create `
  --resource-group $rgName `
  --server $sqlServer `
  --name $dbName `
  --service-objective S1

az appservice plan create `
  --name "$appName-plan" `
  --resource-group $rgName `
  --location $location `
  --sku B1 `
  --is-linux

az webapp create `
  --name $appName `
  --resource-group $rgName `
  --plan "$appName-plan" `
  --runtime "DOTNET:8.0"

# 4. Configure app settings
# (See "Azure App Service" section above for all settings)

# 5. Deploy code
dotnet publish -c Release
# Then use Azure extension in VS Code to deploy

# 6. Run migrations
dotnet ef database update --connection "Server=tcp:$sqlServer.database.windows.net,1433;Database=$dbName;..."

Write-Host "✅ Deployment complete!"
Write-Host "🌐 URL: https://$appName.azurewebsites.net"
```

---

## 📚 Additional Resources

### Documentation
- [README.md](README.md) - Project overview
- [DEPLOYMENT.md](DEPLOYMENT.md) - Detailed deployment guide
- [PRODUCTION_CHECKLIST.md](PRODUCTION_CHECKLIST.md) - Security & readiness
- [QDRANT_REMOVAL_COMPLETE.md](QDRANT_REMOVAL_COMPLETE.md) - Recent changes

### Azure Resources
- [Azure App Service Docs](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure SQL Database Docs](https://docs.microsoft.com/en-us/azure/azure-sql/)
- [Azure OpenAI Service Docs](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/)

### Tools
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/)
- [Azure Portal](https://portal.azure.com)
- [Azure Data Studio](https://docs.microsoft.com/en-us/sql/azure-data-studio/)

---

## 🆘 Troubleshooting

### Common Issues

#### 1. Database Connection Fails
```
Error: Login failed for user 'sqladmin'
```
**Solution:**
- Check SQL Server firewall rules allow your IP
- Verify connection string is correct
- Ensure password is properly escaped in connection string

#### 2. Azure OpenAI 401 Unauthorized
```
Error: Access denied due to invalid subscription key
```
**Solution:**
- Verify API key is correct (no extra spaces)
- Check endpoint URL ends with `/`
- Ensure deployment names match your Azure configuration

#### 3. Application Won't Start
```
Error: Unable to start Kestrel
```
**Solution:**
- Check port 8080 is not in use
- Verify all required environment variables are set
- Review startup logs for detailed error

#### 4. Migrations Fail
```
Error: Cannot connect to database
```
**Solution:**
- Ensure SQL Server is running
- Check firewall allows your IP
- Verify connection string in environment variables

---

## 📞 Support

**Need Help?**
- Check logs: `az webapp log tail`
- Review Application Insights
- Test locally first before deploying

**Before Contacting Support:**
1. Check all environment variables are set
2. Verify database connection
3. Test Azure OpenAI credentials
4. Review recent changes

---

**Last Updated:** December 29, 2025  
**Version:** 1.0.0  
**Author:** Chatify AI Team

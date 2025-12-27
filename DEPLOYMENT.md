# Deploying Chatify AI to Azure App Service

This guide walks you through deploying Chatify AI to Azure App Service using Docker containers.

## 📋 Prerequisites

Before you begin, ensure you have:

- **Azure Account** with an active subscription ([Create free account](https://azure.microsoft.com/free/))
- **Azure CLI** installed ([Installation guide](https://learn.microsoft.com/cli/azure/install-azure-cli))
- **Docker** installed (optional, for local testing)
- **Contributor or Owner** role on your Azure subscription
- **.NET 10 SDK** installed (for running migrations)

## 🏗️ Architecture Overview

The deployment consists of:

- **Azure Container Registry (ACR)**: Stores your Docker image
- **Azure App Service (Linux)**: Hosts the API container
- **Azure SQL Database**: Stores application data (users, sessions, feedback, knowledge documents)
- **Qdrant**: Vector database for RAG (Qdrant Cloud or self-hosted)
- **Azure OpenAI**: Powers the chat and embeddings

```
┌─────────────────────────────────────────────────────────────┐
│                     Azure Subscription                       │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              Azure Container Registry                 │  │
│  │         (chatifyacr.azurecr.io)                      │  │
│  │            stores: chatify-api:1.0.0                 │  │
│  └──────────────────────────────────────────────────────┘  │
│                              │                               │
│                              │ pulls image via               │
│                              │ Managed Identity              │
│                              ▼                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           Azure App Service (Linux)                   │  │
│  │        chatify-api.azurewebsites.net                 │  │
│  │                                                       │  │
│  │  ┌─────────────────────────────────────────────┐    │  │
│  │  │    Chatify AI API Container                  │    │  │
│  │  │    - Port 8080                               │    │  │
│  │  │    - Managed Identity                        │    │  │
│  │  │    - Auto-scaling                            │    │  │
│  │  └─────────────────────────────────────────────┘    │  │
│  └──────────────────────────────────────────────────────┘  │
│                              │                               │
│                              │ connects to                   │
│                              ▼                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           Azure SQL Database                          │  │
│  │         (chatify-sql.database.windows.net)           │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
         │                              │
         │ connects to                  │ connects to
         ▼                              ▼
┌──────────────────┐          ┌──────────────────┐
│  Azure OpenAI    │          │  Qdrant Cloud    │
│  (gpt-4o)        │          │  (vector DB)     │
└──────────────────┘          └──────────────────┘
```

## 🚀 Quick Start: Automated Deployment

### Step 1: Clone and Navigate to Project

```powershell
cd "c:\Users\tbaindurashvili\source\repos\Chatify AI"
```

### Step 2: Login to Azure

```powershell
az login
```

### Step 3: Run Deployment Script

```powershell
.\deploy-azure.ps1 `
  -ResourceGroup "chatify-prod-rg" `
  -Location "eastus" `
  -AppName "chatify-api-prod" `
  -AcrName "chatifyacr001" `
  -Sku "B1"
```

**Parameter Details:**

| Parameter | Description | Example |
|-----------|-------------|---------|
| `ResourceGroup` | Azure resource group name | `chatify-prod-rg` |
| `Location` | Azure region | `eastus`, `westeurope`, `westus2` |
| `AppName` | App Service name (globally unique) | `chatify-api-prod` |
| `AcrName` | Container Registry name (globally unique, lowercase, no hyphens) | `chatifyacr001` |
| `Sku` | App Service pricing tier | `B1`, `B2`, `B3`, `P1V2`, `P2V2` |

**Recommended SKUs:**
- **B1** ($13/month): Development/testing, 1 CPU, 1.75 GB RAM
- **B2** ($26/month): Small production, 2 CPU, 3.5 GB RAM
- **P1V2** ($75/month): Production, 1 CPU, 3.5 GB RAM, auto-scale
- **P2V2** ($149/month): High-traffic, 2 CPU, 7 GB RAM, auto-scale

### Step 4: Configure Secrets in Azure Portal

After deployment, configure required secrets:

1. Open [Azure Portal](https://portal.azure.com)
2. Navigate to: **Resource Groups** → `chatify-prod-rg` → **App Service** → `chatify-api-prod`
3. Go to: **Settings** → **Configuration** → **Application settings**
4. Click **+ New application setting** and add:

#### Required Settings:

| Name | Value | Where to Get |
|------|-------|--------------|
| `ConnectionStrings__DefaultConnection` | `Server=tcp:<server>.database.windows.net,1433;Database=ChatifyAI;User Id=<user>;Password=<pass>;Encrypt=True;` | Azure SQL connection string |
| `AzureOpenAI__Endpoint` | `https://<resource>.openai.azure.com/` | Azure OpenAI resource |
| `AzureOpenAI__ApiKey` | `<your-api-key>` | Azure OpenAI → Keys and Endpoint |
| `AzureOpenAI__ChatDeploymentName` | `gpt-4o` | Your deployment name |
| `AzureOpenAI__EmbeddingDeploymentName` | `text-embedding-3-small` | Your deployment name |
| `Jwt__Secret` | `<64-char-random-string>` | Generate securely (see below) |
| `Qdrant__Endpoint` | `https://<cluster>.cloud.qdrant.io` | Qdrant Cloud dashboard |
| `Qdrant__CollectionName` | `knowledge-base-prod` | Your collection name |

#### Generate JWT Secret (PowerShell):

```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | % {[char]$_})
```

#### Optional Settings:

| Name | Value | Purpose |
|------|-------|---------|
| `ADMIN__USERNAME` | `admin` | Override default admin username |
| `ADMIN__PASSWORD` | `YourSecurePassword123!` | Override default admin password |
| `Qdrant__ApiKey` | `<qdrant-api-key>` | If using Qdrant Cloud authentication |

5. Click **Save** → **Continue**

### Step 5: Restart the App Service

```powershell
az webapp restart -g chatify-prod-rg -n chatify-api-prod
```

### Step 6: Run Database Migrations

Migrations are NOT run automatically in production. Run them manually:

```powershell
# Update connection string in appsettings.json or use environment variable
$env:ConnectionStrings__DefaultConnection = "Server=tcp:chatify-sql.database.windows.net,1433;Database=ChatifyAI;User Id=admin;Password=YourPassword123!;Encrypt=True;"

# Run migrations
dotnet ef database update -p ChatAI.Infrastructure -s ChatAI.Api
```

**Alternative: Azure Data Studio or SSMS:**
1. Connect to your Azure SQL Database
2. Run migration SQL scripts from `ChatAI.Infrastructure/Migrations/`

### Step 7: Verify Deployment

```powershell
# Check health endpoint
curl https://chatify-api-prod.azurewebsites.net/health

# Monitor logs
az webapp log tail -g chatify-prod-rg -n chatify-api-prod
```

Expected health check response:
```json
{
  "status": "Healthy"
}
```

---

## 🗄️ Setting Up Azure SQL Database

### Option 1: Create via Azure Portal

1. Go to [Azure Portal](https://portal.azure.com) → **Create a resource** → **SQL Database**
2. Fill in:
   - **Resource group**: `chatify-prod-rg`
   - **Database name**: `ChatifyAI`
   - **Server**: Create new server
     - **Server name**: `chatify-sql` (globally unique)
     - **Location**: Same as App Service
     - **Authentication**: SQL authentication
     - **Admin login**: `chatifyadmin`
     - **Password**: Strong password
   - **Compute + storage**: Basic (5 DTUs, $5/month) or Standard S0 ($15/month)
3. **Networking** → **Public access** → Allow Azure services
4. Click **Review + create** → **Create**

### Option 2: Create via Azure CLI

```powershell
# Create SQL Server
az sql server create `
  --resource-group chatify-prod-rg `
  --name chatify-sql `
  --location eastus `
  --admin-user chatifyadmin `
  --admin-password "YourSecurePassword123!"

# Allow Azure services to access server
az sql server firewall-rule create `
  --resource-group chatify-prod-rg `
  --server chatify-sql `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# Create database
az sql db create `
  --resource-group chatify-prod-rg `
  --server chatify-sql `
  --name ChatifyAI `
  --service-objective Basic
```

### Get Connection String

```powershell
az sql db show-connection-string `
  --client ado.net `
  --server chatify-sql `
  --name ChatifyAI
```

Replace `<username>` and `<password>` with your actual credentials.

---

## 🔍 Setting Up Qdrant Vector Database

### Option 1: Qdrant Cloud (Recommended)

1. Go to [Qdrant Cloud](https://cloud.qdrant.io/)
2. Create account and free cluster
3. Get cluster URL: `https://<cluster-id>.aws.cloud.qdrant.io`
4. Get API key from dashboard
5. Configure in App Service:
   - `Qdrant__Endpoint`: cluster URL
   - `Qdrant__ApiKey`: API key
   - `Qdrant__CollectionName`: `knowledge-base-prod`

### Option 2: Self-Hosted on Azure Container Apps

```powershell
# Create Container App environment
az containerapp env create `
  --name chatify-env `
  --resource-group chatify-prod-rg `
  --location eastus

# Deploy Qdrant
az containerapp create `
  --name chatify-qdrant `
  --resource-group chatify-prod-rg `
  --environment chatify-env `
  --image qdrant/qdrant:latest `
  --target-port 6333 `
  --ingress external `
  --cpu 1 `
  --memory 2Gi

# Get Qdrant URL
az containerapp show `
  --name chatify-qdrant `
  --resource-group chatify-prod-rg `
  --query properties.configuration.ingress.fqdn `
  -o tsv
```

Configure in App Service:
- `Qdrant__Endpoint`: `https://<fqdn>`

---

## 🔧 Post-Deployment Configuration

### Enable Application Insights (Recommended)

Monitor performance and errors:

```powershell
# Create Application Insights
az monitor app-insights component create `
  --app chatify-insights `
  --location eastus `
  --resource-group chatify-prod-rg `
  --application-type web

# Get instrumentation key
$insightsKey = az monitor app-insights component show `
  --app chatify-insights `
  --resource-group chatify-prod-rg `
  --query instrumentationKey `
  -o tsv

# Configure App Service
az webapp config appsettings set `
  --resource-group chatify-prod-rg `
  --name chatify-api-prod `
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$insightsKey"
```

### Enable Logging

```powershell
# Enable application logging
az webapp log config `
  --resource-group chatify-prod-rg `
  --name chatify-api-prod `
  --application-logging filesystem `
  --level information

# Enable HTTP logging
az webapp log config `
  --resource-group chatify-prod-rg `
  --name chatify-api-prod `
  --web-server-logging filesystem
```

### Configure Custom Domain (Optional)

1. Azure Portal → App Service → **Custom domains**
2. Add your domain (e.g., `api.yourdomain.com`)
3. Add DNS records as instructed
4. Enable **HTTPS** → **Managed certificate**

### Enable Auto-Scale (P1V2+ tiers)

```powershell
az monitor autoscale create `
  --resource-group chatify-prod-rg `
  --resource chatify-api-prod `
  --resource-type Microsoft.Web/serverfarms `
  --name chatify-autoscale `
  --min-count 1 `
  --max-count 5 `
  --count 1

az monitor autoscale rule create `
  --resource-group chatify-prod-rg `
  --autoscale-name chatify-autoscale `
  --condition "Percentage CPU > 75 avg 5m" `
  --scale out 1
```

---

## 🐛 Troubleshooting

### Container Won't Start

**Check logs:**
```powershell
az webapp log tail -g chatify-prod-rg -n chatify-api-prod
```

**Common issues:**
- Missing required environment variables (check Configuration)
- Database connection string incorrect
- Azure OpenAI credentials invalid
- Qdrant not accessible

### Database Connection Fails

**Verify firewall rules:**
```powershell
az sql server firewall-rule list `
  --resource-group chatify-prod-rg `
  --server chatify-sql
```

**Add your IP for testing:**
```powershell
az sql server firewall-rule create `
  --resource-group chatify-prod-rg `
  --server chatify-sql `
  --name MyIP `
  --start-ip-address 1.2.3.4 `
  --end-ip-address 1.2.3.4
```

### Container Image Not Found

**Check ACR permissions:**
```powershell
az role assignment list `
  --assignee <managed-identity-principal-id> `
  --scope /subscriptions/<sub-id>/resourceGroups/chatify-prod-rg/providers/Microsoft.ContainerRegistry/registries/chatifyacr001
```

### High Response Times

- Upgrade to higher SKU (P-series)
- Enable auto-scale
- Check Azure OpenAI throttling limits
- Review Application Insights performance data

---

## 🔄 Updating Your Application

### Update Docker Image

```powershell
# Rebuild and push new image
az acr build `
  --registry chatifyacr001 `
  --image chatify-api:1.0.1 `
  --file Dockerfile `
  .

# Update App Service to use new image
az webapp config container set `
  --resource-group chatify-prod-rg `
  --name chatify-api-prod `
  --docker-custom-image-name chatifyacr001.azurecr.io/chatify-api:1.0.1

# Restart app
az webapp restart -g chatify-prod-rg -n chatify-api-prod
```

### Apply Database Migrations

```powershell
# Run new migrations
dotnet ef database update -p ChatAI.Infrastructure -s ChatAI.Api
```

---

## 💰 Cost Estimation

**Monthly costs (approximate):**

| Service | SKU | Cost |
|---------|-----|------|
| App Service | B1 | $13 |
| Azure SQL | Basic | $5 |
| Container Registry | Basic | $5 |
| Azure OpenAI | Pay-per-use | $10-50+ |
| Qdrant Cloud | Free tier | $0 |
| Application Insights | First 5GB free | $0-10 |
| **Total** | | **$33-83+** |

**For production workloads:**

| Service | SKU | Cost |
|---------|-----|------|
| App Service | P1V2 | $75 |
| Azure SQL | S0 Standard | $15 |
| Container Registry | Basic | $5 |
| Azure OpenAI | Pay-per-use | $50-200+ |
| Qdrant Cloud | Paid tier | $25+ |
| Application Insights | ~10GB/month | $20 |
| **Total** | | **$190-340+** |

---

## 🔒 Security Best Practices

1. **Never commit secrets** to Git (use Azure Key Vault or App Service Configuration)
2. **Enable Managed Identity** for all Azure service connections
3. **Use Azure Key Vault** for storing sensitive configuration
4. **Enable HTTPS only** (already configured)
5. **Configure CORS** properly in production
6. **Enable DDoS protection** for critical workloads
7. **Use Azure Front Door** or Application Gateway for WAF
8. **Regularly update dependencies** and Docker base images
9. **Enable Azure Defender** for App Service
10. **Set up Azure Monitor alerts** for failures and anomalies

---

## 📚 Additional Resources

- [Azure App Service Documentation](https://learn.microsoft.com/azure/app-service/)
- [Azure SQL Database Documentation](https://learn.microsoft.com/azure/azure-sql/)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Qdrant Documentation](https://qdrant.tech/documentation/)
- [Azure Container Registry Documentation](https://learn.microsoft.com/azure/container-registry/)

---

## 🆘 Support

- **Azure Support**: [Azure Portal](https://portal.azure.com) → Support → New support request
- **GitHub Issues**: [Chatify AI Repository](https://github.com/temo14/Chatify-AI/issues)
- **Documentation**: See [README.md](README.md) and [FEATURE_ROADMAP.md](FEATURE_ROADMAP.md)

---

**🎉 Congratulations! Your Chatify AI application is now running on Azure App Service!**

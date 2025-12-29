# 🚀 Quick Deployment Reference

**Use this as a quick reference - Full details in [CLOUD_DEPLOYMENT_GUIDE.md](CLOUD_DEPLOYMENT_GUIDE.md)**

---

## 🎯 Choose Your Path

### Path 1: Azure App Service (Easiest) ⭐ RECOMMENDED
**Time:** 30-45 minutes  
**Complexity:** ⭐⭐☆☆☆  
**Cost:** ~$150-300/month

### Path 2: Azure Container Apps (Modern)
**Time:** 45-60 minutes  
**Complexity:** ⭐⭐⭐☆☆  
**Cost:** ~$100-250/month (scale to zero)

### Path 3: Docker Compose on VPS (Budget)
**Time:** 60-90 minutes  
**Complexity:** ⭐⭐⭐⭐☆  
**Cost:** ~$50-100/month + AI costs

---

## ⚡ Fast Track: Azure App Service (30 Minutes)

### Step 1: Create Azure Resources (15 min)

```powershell
# Login
az login

# Create resource group
az group create --name chatify-rg --location eastus

# Create SQL Server & Database
az sql server create `
  --name chatify-sql-$(Get-Random) `
  --resource-group chatify-rg `
  --location eastus `
  --admin-user sqladmin `
  --admin-password "YourStrong@Password123"

az sql db create `
  --resource-group chatify-rg `
  --server chatify-sql-* `
  --name chatify-db `
  --service-objective S1

# Create App Service
az webapp create `
  --name chatify-app-$(Get-Random) `
  --resource-group chatify-rg `
  --plan chatify-plan `
  --runtime "DOTNET:8.0"
```

### Step 2: Configure Environment (5 min)

```powershell
# Set all environment variables
az webapp config appsettings set `
  --name chatify-app-* `
  --resource-group chatify-rg `
  --settings `
    ASPNETCORE_ENVIRONMENT=Production `
    ConnectionStrings__DefaultConnection="Server=tcp:chatify-sql-*.database.windows.net,1433;Database=chatify-db;User Id=sqladmin;Password=YourStrong@Password123;Encrypt=True;" `
    AzureOpenAI__Endpoint="YOUR_ENDPOINT" `
    AzureOpenAI__ApiKey="YOUR_KEY" `
    JWT__Secret="$(openssl rand -base64 48)"
```

### Step 3: Deploy Code (5 min)

**Option A: Visual Studio Code**
1. Install "Azure App Service" extension
2. Right-click `ChatAI.Api` → Deploy to Web App
3. Select your app

**Option B: Command Line**
```powershell
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../app.zip
az webapp deployment source config-zip `
  --name chatify-app-* `
  --resource-group chatify-rg `
  --src ../app.zip
```

### Step 4: Run Migrations (5 min)

```powershell
$env:ConnectionStrings__DefaultConnection="<your-azure-sql-connection>"
dotnet ef database update --project ChatAI.Infrastructure --startup-project ChatAI.Api
```

### ✅ Done!

Visit: `https://chatify-app-*.azurewebsites.net`

---

## 📋 Configuration Checklist

### Required Before Deployment

#### 1. Azure OpenAI (You Already Have This)
```env
AZUREOPENAI__ENDPOINT=https://your-resource.openai.azure.com/
AZUREOPENAI__APIKEY=your-api-key
AZUREOPENAI__CHATDEPLOYMENTNAME=gpt-4o
AZUREOPENAI__EMBEDDINGDEPLOYMENTNAME=text-embedding-3-small
```

**Where to find:**
- Azure Portal → Azure OpenAI → Keys and Endpoint

#### 2. Database Connection
```env
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=tcp:yourserver.database.windows.net,1433;Database=chatify-db;User Id=sqladmin;Password=YourPassword;Encrypt=True;
```

**Where to find:**
- Azure Portal → SQL Database → Connection strings

#### 3. JWT Secret (Generate New)
```powershell
# PowerShell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | % {[char]$_})

# Or use OpenSSL
openssl rand -base64 48
```

```env
JWT__SECRET=your-generated-64-character-secret
JWT__ISSUER=ChatifyAI
JWT__AUDIENCE=ChatifyAI-Users
```

#### 4. Admin Account (Change Default!)
```env
ADMIN__USERNAME=youradmin
ADMIN__PASSWORD=YourSecure@Password123
ADMIN__EMAIL=admin@yourcompany.com
```

---

## 🔐 Security Quick Fixes

### Critical (Do Before Going Live)

```bash
# 1. Generate JWT Secret
openssl rand -base64 48

# 2. Change Admin Password
# Set as environment variable, NOT in code!

# 3. Enable HTTPS Only
az webapp update --https-only true --name <app-name> --resource-group <rg>

# 4. Disable Swagger in Production
# Edit Program.cs - wrap swagger in if(app.Environment.IsDevelopment())
```

---

## 💰 Monthly Cost Breakdown (Azure)

### Small Workload (< 1000 users)
| Service | Tier | Cost |
|---------|------|------|
| Azure SQL | S1 (20 DTU) | $30 |
| App Service | B1 Basic | $13 |
| Azure OpenAI | Pay-per-use | $50-150 |
| **Total** | | **$93-193** |

### Medium Workload (1000-5000 users)
| Service | Tier | Cost |
|---------|------|------|
| Azure SQL | S2 (50 DTU) | $75 |
| App Service | S1 Standard | $70 |
| Azure OpenAI | Pay-per-use | $150-300 |
| **Total** | | **$295-445** |

### Cost Saving Tips
- Use Basic tier for development ($5/month SQL)
- Auto-scale down during off-hours
- Monitor AI token usage closely
- Use 1-year reserved instances (-30%)

---

## 🧪 Testing Checklist

### After Deployment

```bash
# 1. Health Check
curl https://your-app.azurewebsites.net/health

# 2. Admin Login
# Navigate to: https://your-app.azurewebsites.net/admin-login.html

# 3. Create Test Tenant
# Login as admin → Tenant Management → Create

# 4. Test Knowledge Base
# Add a document → Verify embedding generation

# 5. Test Chat
# Send a message → Verify RAG works

# 6. Check Logs
az webapp log tail --name your-app --resource-group your-rg
```

---

## 🆘 Common Issues & Fixes

### Issue: Database Won't Connect
```
Error: Login failed for user 'sqladmin'
```
**Fix:**
```bash
# Add your IP to SQL firewall
az sql server firewall-rule create \
  --server yourserver \
  --name AllowMyIP \
  --start-ip-address YOUR.IP \
  --end-ip-address YOUR.IP

# Allow Azure services
az sql server firewall-rule create \
  --server yourserver \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### Issue: Azure OpenAI 401 Error
```
Error: Access denied due to invalid subscription key
```
**Fix:**
- Verify API key has no extra spaces
- Check endpoint URL ends with `/`
- Ensure deployment names match exactly

### Issue: App Won't Start
```
Error: Application startup failed
```
**Fix:**
```bash
# Check logs
az webapp log tail --name your-app --resource-group your-rg

# Verify environment variables
az webapp config appsettings list --name your-app --resource-group your-rg

# Restart app
az webapp restart --name your-app --resource-group your-rg
```

---

## 📊 Monitoring Setup

### Enable Application Insights (5 min)

```bash
# Create Application Insights
az monitor app-insights component create \
  --app chatify-insights \
  --location eastus \
  --resource-group chatify-rg

# Get connection string
$connStr = az monitor app-insights component show \
  --app chatify-insights \
  --resource-group chatify-rg \
  --query connectionString -o tsv

# Add to app settings
az webapp config appsettings set \
  --name chatify-app \
  --resource-group chatify-rg \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="$connStr"
```

### View Metrics
- Azure Portal → Application Insights → Live Metrics
- Monitor: Response times, request rates, failures
- Set up alerts for errors

---

## 🔄 CI/CD Setup (Optional)

### GitHub Actions (Recommended)

```yaml
# .github/workflows/deploy.yml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '10.0.x'
      
      - name: Build
        run: dotnet build --configuration Release
      
      - name: Publish
        run: dotnet publish -c Release -o ./publish
      
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: chatify-app
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

---

## 📞 Next Steps

1. **Read Full Guide:** [CLOUD_DEPLOYMENT_GUIDE.md](CLOUD_DEPLOYMENT_GUIDE.md)
2. **Security Checklist:** [PRODUCTION_CHECKLIST.md](PRODUCTION_CHECKLIST.md)
3. **Test Locally First:** See "Local Development Setup" section
4. **Deploy to Staging First:** Test before production
5. **Monitor After Deployment:** Set up alerts

---

## 🎓 Learning Resources

### Azure
- [Azure App Service Tutorial](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure SQL Database Guide](https://docs.microsoft.com/en-us/azure/azure-sql/)
- [Azure OpenAI Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/)

### .NET Deployment
- [ASP.NET Core Deployment](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/)
- [Entity Framework Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

**Questions?** Review the full [CLOUD_DEPLOYMENT_GUIDE.md](CLOUD_DEPLOYMENT_GUIDE.md) for detailed explanations.

**Ready to Deploy?** Start with the Azure App Service fast track above! 🚀

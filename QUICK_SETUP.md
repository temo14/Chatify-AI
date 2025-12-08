# Quick Setup Guide

## Initial Setup (5 minutes)

### 1. Clone Repository
```bash
git clone https://github.com/temo14/Chatify-AI.git
cd Chatify-AI
```

### 2. Create .env File
```bash
# Copy the template
cp .env.example .env

# Edit with your actual credentials
# Windows: notepad .env
# Mac/Linux: nano .env
```

### 3. Fill in Required Secrets in .env

**Minimum required for development**:
```bash
# Database (local SQL Server)
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=localhost;Database=ChatifyAI_Dev;Integrated Security=true;TrustServerCertificate=True;

# Azure OpenAI (get from Azure Portal)
AZUREOPENAI__ENDPOINT=https://your-resource.openai.azure.com/
AZUREOPENAI__APIKEY=your-api-key-here

# Email (optional for development, required for EmailPlugin)
EMAIL__USERNAME=your-email@gmail.com
EMAIL__PASSWORD=your-app-password
EMAIL__FROMEMAIL=your-email@gmail.com
```

**Gmail App Password Setup** (for EMAIL__ variables):
1. Go to https://myaccount.google.com/apppasswords
2. Enable 2-Factor Authentication if not already enabled
3. Generate App Password (select "Mail" and "Other")
4. Copy the 16-character password (no spaces)

### 4. Start Qdrant (Vector Database)
```bash
# Using Docker
docker run -p 6333:6333 qdrant/qdrant

# OR download from https://qdrant.tech/documentation/quick-start/
```

### 5. Run Application
```bash
dotnet restore
dotnet build
dotnet run --project ChatAI.Api
```

### 6. Initialize Default Configuration
1. Open http://localhost:5257/admin.html
2. Go to **Configuration** tab
3. Click **"Initialize Defaults"** button at bottom
4. Verify 32 configurations were created

### 7. Configure Support Email
1. In admin panel → Configuration tab
2. Find **Branding.SupportEmail**
3. Click Edit, set to your email
4. Click Save

### 8. Test Application
- **Demo Client**: http://localhost:5257/demo-client.html
- **Admin Panel**: http://localhost:5257/admin.html
- **API Docs**: http://localhost:5257/swagger

---

## Production Deployment

### Docker Compose
```bash
# 1. Create .env with production secrets
cp .env.example .env

# 2. Edit .env with production values
# - Use strong SQL Server password
# - Use production Azure OpenAI endpoint
# - Configure production SMTP

# 3. Start services
docker-compose up -d

# 4. Initialize configuration
curl -X POST http://localhost:5257/api/configuration/initialize-defaults
```

### Azure App Service
1. **Deploy code** (GitHub Actions / Azure DevOps)
2. **Set Application Settings** (replaces .env):
   - CONNECTIONSTRINGS__DEFAULTCONNECTION
   - AZUREOPENAI__ENDPOINT
   - AZUREOPENAI__APIKEY
   - EMAIL__USERNAME
   - EMAIL__PASSWORD
   - EMAIL__FROMEMAIL
3. **Restart app**
4. **Initialize config** via admin panel

### Kubernetes
```yaml
# secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: chatify-secrets
type: Opaque
stringData:
  CONNECTIONSTRINGS__DEFAULTCONNECTION: "Server=..."
  AZUREOPENAI__APIKEY: "..."
  EMAIL__PASSWORD: "..."
```

```bash
kubectl apply -f secrets.yaml
kubectl apply -f deployment.yaml
```

---

## Troubleshooting

### "Cannot connect to database"
✅ Check `CONNECTIONSTRINGS__DEFAULTCONNECTION` in .env  
✅ Verify SQL Server is running  
✅ Test connection: `sqlcmd -S localhost -d ChatifyAI_Dev -E`

### "Azure OpenAI error"
✅ Check `AZUREOPENAI__ENDPOINT` and `AZUREOPENAI__APIKEY` in .env  
✅ Verify deployment names match Azure: `gpt-4o`, `text-embedding-3-small`  
✅ Check Azure OpenAI quota and billing

### "Email sending failed"
✅ Check `EMAIL__` variables in .env  
✅ Gmail: Use App Password, not regular password  
✅ Enable in appsettings: `"Email": { "Enabled": true }`  
✅ Configure `Branding.SupportEmail` in admin panel

### "Qdrant connection error"
✅ Start Qdrant: `docker run -p 6333:6333 qdrant/qdrant`  
✅ Check `QDRANT__ENDPOINT` in .env (default: http://localhost:6333)

### "Rate limit exceeded"
✅ Development: Rate limiting is disabled (60 req/min global in prod)  
✅ Adjust in appsettings.json → IpRateLimiting → GeneralRules

---

## Configuration Cheat Sheet

### Where to Find Settings

| Setting Type | Location | Example |
|-------------|----------|---------|
| **Secrets** | `.env` file | API keys, passwords, connection strings |
| **Infrastructure** | `.env` or Cloud Config | Endpoints, service URLs |
| **Framework** | `appsettings.json` | Logging, caching, rate limits |
| **Business Logic** | Database (Admin Panel) | AI behavior, branding, features |

### Common Overrides

**Change AI Temperature** (without restarting):
- Admin Panel → Configuration → AI.Temperature → Edit → Save

**Enable File Upload**:
- Admin Panel → Configuration → Features.EnableFileUpload → Edit → true → Save

**Change Support Email**:
- Admin Panel → Configuration → Branding.SupportEmail → Edit → your-email@company.com → Save

**Increase Rate Limit** (requires restart):
- Edit `appsettings.json` → IpRateLimiting → GeneralRules → Limit: 100

---

## Next Steps

1. ✅ **Read** [CONFIGURATION_STRATEGY.md](CONFIGURATION_STRATEGY.md) for detailed architecture
2. ✅ **Review** [CONFIGURATION_CLEANUP_SUMMARY.md](CONFIGURATION_CLEANUP_SUMMARY.md) for recent changes
3. ✅ **Customize** AI behavior via Admin Panel → Configuration
4. ✅ **Add** knowledge documents via Admin Panel → Knowledge Base
5. ✅ **Test** chat functionality at `/demo-client.html`

---

## Support

**Issues**: https://github.com/temo14/Chatify-AI/issues  
**Documentation**: See `/CONFIGURATION_STRATEGY.md`  
**Email**: t.baindurashvili.gm@gmail.com

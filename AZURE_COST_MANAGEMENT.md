# Azure Cost Management Guide
**Application:** Chatify AI  
**Resource Group:** chatify-prod-rg  
**Last Updated:** January 4, 2026

---

## 💰 Daily Cost Breakdown

| Service | Always-On Cost/Day | Notes |
|---------|-------------------|-------|
| **SQL Database (Basic)** | **$0.17** | Cannot be paused/stopped |
| **Container Registry** | **$0.17** | Storage cost, always charged |
| **API Container (running)** | **$0.30-0.50** | Can scale to zero |
| **Seq Container (running)** | **$0.15-0.25** | Can scale to zero |
| **Key Vault + Logs** | **$0.11** | Minimal, usage-based |
| **Total (all running)** | **$0.90-1.20/day** | ~$27-36/month |
| **Total (apps scaled to 0)** | **$0.45/day** | ~$13.50/month |

---

## 🔽 How to Scale Down (Save Money When Not Using)

### Option 1: Scale Container Apps to Zero (Recommended)

**When to use:** Daily/overnight when not actively developing

```powershell
# Scale API to zero
az containerapp update `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --min-replicas 0 `
  --max-replicas 3

# Scale Seq to zero
az containerapp update `
  --name chatify-seq `
  --resource-group chatify-prod-rg `
  --min-replicas 0 `
  --max-replicas 1
```

**Cost Savings:** ~$0.45-0.75 per day  
**Downtime:** First request takes 10-30 seconds to cold start  
**Data Impact:** None - all data preserved

### Option 2: Stop Container Apps Completely

**When to use:** Long breaks (1+ weeks)

```powershell
# Stop both apps
az containerapp stop --name chatify-api --resource-group chatify-prod-rg
az containerapp stop --name chatify-seq --resource-group chatify-prod-rg
```

**Cost Savings:** ~$0.45-0.75 per day  
**Downtime:** Need to manually start when needed  
**Data Impact:** Seq data lost (in-memory), API data preserved in SQL

### Option 3: Delete Container Apps (Keep Images)

**When to use:** Extended breaks (1+ months), major cost savings

```powershell
# Delete apps but keep images in registry
az containerapp delete --name chatify-api --resource-group chatify-prod-rg --yes
az containerapp delete --name chatify-seq --resource-group chatify-prod-rg --yes
```

**Cost Savings:** ~$0.45-0.75 per day  
**Downtime:** Need to recreate apps (5-10 minutes)  
**Data Impact:** Seq data lost, API data preserved in SQL

**To recreate later, see "Scale Up" section below**

---

## 🔼 How to Scale Up (Bring Back When Needed)

### Option 1: Scale Container Apps Back to 1

**If you scaled to zero:**

```powershell
# Scale API back to 1 replica
az containerapp update `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --min-replicas 1 `
  --max-replicas 3

# Scale Seq back to 1 replica
az containerapp update `
  --name chatify-seq `
  --resource-group chatify-prod-rg `
  --min-replicas 1 `
  --max-replicas 1
```

**Time to ready:** 10-30 seconds  
**Verification:** Visit https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io/health

### Option 2: Start Container Apps

**If you stopped them:**

```powershell
# Start both apps
az containerapp start --name chatify-api --resource-group chatify-prod-rg
az containerapp start --name chatify-seq --resource-group chatify-prod-rg
```

**Time to ready:** 10-30 seconds  
**Verification:** Check status with `az containerapp show`

### Option 3: Recreate Container Apps

**If you deleted them:**

```powershell
# Recreate API
az containerapp create `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --environment chatify-env `
  --image chatifyregistry.azurecr.io/chatify-ai:v5 `
  --target-port 8080 `
  --ingress external `
  --registry-server chatifyregistry.azurecr.io `
  --cpu 1 `
  --memory 2Gi `
  --min-replicas 0 `
  --max-replicas 3 `
  --env-vars "ASPNETCORE_ENVIRONMENT=Production" "ASPNETCORE_URLS=http://+:8080"

# Assign managed identity
az containerapp identity assign `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --system-assigned

# Get the principal ID (output will show principalId)
az containerapp identity show `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --query principalId -o tsv

# Grant Key Vault access (replace PRINCIPAL_ID with output from above)
az role assignment create `
  --role "Key Vault Secrets User" `
  --assignee <PRINCIPAL_ID> `
  --scope "/subscriptions/6017cf60-a38f-4e64-9654-e6a36caf40d5/resourceGroups/chatify-prod-rg/providers/Microsoft.KeyVault/vaults/chatify-kv-4021"

# Recreate Seq
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
  --env-vars "ACCEPT_EULA=Y" "SEQ_STORAGE_INMEMORY=true" "SEQ_FIRSTRUN_ADMINUSERNAME=admin" "SEQ_FIRSTRUN_ADMINPASSWORD=Admin@123"
```

**Time to ready:** 5-10 minutes  
**Verification:** Check both health endpoints

---

## 📊 Quick Status Check Commands

```powershell
# Check all container apps status
az containerapp list `
  --resource-group chatify-prod-rg `
  --query "[].{name:name,status:properties.runningStatus,replicas:properties.template.scale.minReplicas}" `
  --output table

# Check current replica count
az containerapp replica list `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --output table

# Check recent logs
az containerapp logs show `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --tail 50
```

---

## 🎯 Recommended Daily Workflow

### **End of Day (Save Money):**
```powershell
# Quick scale down
az containerapp update --name chatify-api --resource-group chatify-prod-rg --min-replicas 0
az containerapp update --name chatify-seq --resource-group chatify-prod-rg --min-replicas 0
```

### **Start of Day (Resume Work):**
```powershell
# Quick scale up
az containerapp update --name chatify-api --resource-group chatify-prod-rg --min-replicas 1
az containerapp update --name chatify-seq --resource-group chatify-prod-rg --min-replicas 1

# Wait 30 seconds, then verify
Start-Sleep -Seconds 30
Invoke-WebRequest -Uri "https://chatify-api.yellowpebble-7206aad4.westeurope.azurecontainerapps.io/health" -UseBasicParsing
```

### **Weekend/Vacation (Maximum Savings):**
```powershell
# Delete container apps (keep everything else)
az containerapp delete --name chatify-api --resource-group chatify-prod-rg --yes
az containerapp delete --name chatify-seq --resource-group chatify-prod-rg --yes
```

**When returning:** Use "Option 3: Recreate Container Apps" above

---

## ⚠️ Important Notes

### What Cannot Be Scaled/Paused:
- ❌ **SQL Database (Basic tier)** - Always charges $0.17/day
- ❌ **Container Registry** - Always charges $0.17/day for storage
- ❌ **Key Vault** - Minimal cost, not worth optimizing

### What Scales Automatically:
- ✅ **Container Apps** - Can scale to zero, auto-start on demand
- ✅ **Log Analytics** - Only charges for data ingested

### Data Persistence:
- ✅ **SQL Database** - Always preserved (even if apps are deleted)
- ✅ **Key Vault Secrets** - Always preserved
- ✅ **Container Images** - Preserved in Container Registry
- ⚠️ **Seq Logs** - Lost when container restarts (in-memory mode)
- ✅ **API Configuration** - Preserved in Key Vault and Database

---

## 💡 Cost Optimization Tips

1. **Scale to zero by default** - Set `min-replicas 0` for both apps
2. **Apps auto-start on first request** - No manual intervention needed
3. **Use Azure CLI scripts** - Automate scaling up/down
4. **Monitor costs** - Use Azure Cost Management dashboard
5. **Consider SQL Database tiers:**
   - Basic ($5/month) - Current setup, always-on
   - Serverless (per-use) - Can pause automatically, more expensive when active
   - DTU-based (flexible) - Can pause/resume manually

---

## 🔧 Troubleshooting

### Container won't start after scaling up:
```powershell
# Check logs for errors
az containerapp logs show --name chatify-api --resource-group chatify-prod-rg --tail 100

# Restart the container
az containerapp restart --name chatify-api --resource-group chatify-prod-rg
```

### Managed Identity lost after recreation:
- Follow the "Recreate Container Apps" steps above
- Make sure to assign system identity and grant Key Vault access

### Health check fails:
```powershell
# Force a new revision deployment
az containerapp update `
  --name chatify-api `
  --resource-group chatify-prod-rg `
  --force
```

---

## 📈 Cost Comparison

| Scenario | Daily Cost | Monthly Cost | When to Use |
|----------|-----------|--------------|-------------|
| **Always Running** | $0.90-1.20 | $27-36 | Active development, production use |
| **Scaled to Zero (12h/day)** | $0.68-0.90 | $20-27 | Regular development with breaks |
| **Apps Deleted (weekends)** | $0.61-0.85 | $18-26 | Weekend warrior development |
| **Minimal (apps always off)** | $0.45 | $13.50 | Long breaks, occasional use |

**Recommendation:** Use "Scale to Zero" approach for daily development - best balance of convenience and cost savings.

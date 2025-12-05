# Deployment Guide - Chatify AI

This guide covers deploying Chatify AI to various environments.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development](#local-development)
3. [Docker Deployment](#docker-deployment)
4. [GitHub Actions CI/CD](#github-actions-cicd)
5. [Azure Deployment](#azure-deployment)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

- **.NET 10.0 SDK** (preview) - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/downloads)

### Required Services

- **Azure OpenAI** - GPT-4o and text-embedding-3-small deployments
- **SQL Server** (optional if using Docker)
- **Qdrant** vector database (optional if using Docker)

### Get Azure OpenAI Credentials

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Azure OpenAI resource
3. Copy **Endpoint URL** and **API Key**
4. Note your deployment names (usually `gpt-4o` and `text-embedding-3-small`)

---

## Local Development

### Step 1: Clone Repository

```bash
git clone <repository-url>
cd "Chatify AI"
```

### Step 2: Configure Environment

Update `ChatAI.Api/appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "ChatDeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-3-small"
  }
}
```

### Step 3: Start Dependencies (Docker)

```bash
# Start SQL Server and Qdrant
docker-compose up -d sqlserver qdrant

# Wait for services to be healthy
docker-compose ps
```

### Step 4: Apply Database Migrations

```bash
cd ChatAI.Api
dotnet ef database update --project ../ChatAI.Infrastructure
```

### Step 5: Run Application

```bash
dotnet run --project ChatAI.Api
```

Access at: `http://localhost:5000`

### Step 6: Test API

```bash
# Health check
curl http://localhost:5000/health

# Chat request
curl -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-test-key-12345" \
  -d '{
    "userId": "user123",
    "message": "Hello!",
    "sessionId": null
  }'
```

---

## Docker Deployment

### Step 1: Create Environment File

```bash
cp .env.example .env
```

Edit `.env` with your values:

```env
# Database
SQL_SA_PASSWORD=YourStrong@Password123

# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key-here
AZURE_OPENAI_CHAT_DEPLOYMENT=gpt-4o
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-3-small

# API Keys
API_KEY_USER1=user1-key-12345
API_KEY_ADMIN=admin-key-67890
```

### Step 2: Build and Run

```bash
# Build all services
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f chatify-api
```

### Step 3: Verify Deployment

```bash
# Check service health
docker-compose ps

# API health check
curl http://localhost:8080/health

# Expected response:
# {
#   "status": "Healthy",
#   "database": "Healthy",
#   "qdrant": "Healthy"
# }
```

### Step 4: Apply Migrations (First Time)

```bash
docker-compose exec chatify-api \
  dotnet ef database update --project ChatAI.Infrastructure
```

### Step 5: Monitor

```bash
# View all logs
docker-compose logs -f

# View API logs only
docker-compose logs -f chatify-api

# View SQL Server logs
docker-compose logs -f sqlserver
```

### Docker Commands Reference

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (DELETE DATA)
docker-compose down -v

# Restart specific service
docker-compose restart chatify-api

# View resource usage
docker stats

# Access container shell
docker-compose exec chatify-api /bin/bash
```

---

## GitHub Actions CI/CD

### Step 1: Configure Repository Secrets

Go to **Settings → Secrets and variables → Actions**

Add the following secrets:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint | `https://xxx.openai.azure.com/` |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key | `abc123...` |
| `STAGING_URL` | Staging environment URL | `http://staging.example.com` |
| `PRODUCTION_URL` | Production environment URL | `http://prod.example.com` |

### Step 2: Enable GitHub Container Registry

1. Go to **Settings → Actions → General**
2. Under **Workflow permissions**, select **Read and write permissions**
3. Check **Allow GitHub Actions to create and approve pull requests**

### Step 3: Push to Trigger CI

```bash
git add .
git commit -m "Initial deployment"
git push origin master
```

This triggers:
- ✅ Build and test
- ✅ Code quality checks
- ✅ Docker image build
- ✅ Push to `ghcr.io`

### Step 4: View Workflow

1. Go to **Actions** tab in GitHub
2. Click on the running workflow
3. View logs for each job

### Step 5: Deploy to Production

Create a version tag to trigger production deployment:

```bash
git tag v1.0.0
git push origin v1.0.0
```

This triggers:
- ✅ Build and push Docker image
- ✅ Deploy to staging (auto)
- ⏸️ Wait for manual approval
- ✅ Deploy to production
- ✅ Health check verification

### Workflow Files

- **`.github/workflows/ci.yml`** - Continuous Integration
- **`.github/workflows/cd.yml`** - Continuous Deployment

---

## Azure Deployment

### Option 1: Azure Container Apps (Recommended)

#### Prerequisites

```bash
# Install Azure CLI
az login

# Install Container Apps extension
az extension add --name containerapp
```

#### Create Resources

```bash
# Variables
RESOURCE_GROUP="chatify-ai-rg"
LOCATION="eastus"
ENVIRONMENT="chatify-env"
APP_NAME="chatify-api"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create container app environment
az containerapp env create \
  --name $ENVIRONMENT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Create container app
az containerapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT \
  --image ghcr.io/<your-username>/chatify-ai:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 5 \
  --cpu 1.0 \
  --memory 2.0Gi \
  --env-vars \
    AZURE_OPENAI_ENDPOINT=<your-endpoint> \
    AZURE_OPENAI_API_KEY=secretref:openai-key

# Add secrets
az containerapp secret set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --secrets openai-key=<your-api-key>
```

#### Update CD Workflow

Edit `.github/workflows/cd.yml`:

```yaml
deploy-production:
  steps:
    - name: Deploy to Azure Container Apps
      run: |
        az containerapp update \
          --name chatify-api \
          --resource-group chatify-ai-rg \
          --image ghcr.io/${{ github.repository }}:${{ github.sha }}
```

### Option 2: Azure App Service

#### Create App Service

```bash
# Create App Service Plan
az appservice plan create \
  --name chatify-plan \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name chatify-api \
  --resource-group $RESOURCE_GROUP \
  --plan chatify-plan \
  --deployment-container-image-name ghcr.io/<username>/chatify-ai:latest

# Configure environment
az webapp config appsettings set \
  --name chatify-api \
  --resource-group $RESOURCE_GROUP \
  --settings \
    AZURE_OPENAI_ENDPOINT=<endpoint> \
    AZURE_OPENAI_API_KEY=<key>
```

### Option 3: Azure Kubernetes Service (AKS)

#### Create AKS Cluster

```bash
az aks create \
  --name chatify-aks \
  --resource-group $RESOURCE_GROUP \
  --node-count 2 \
  --node-vm-size Standard_B2s \
  --enable-managed-identity

# Get credentials
az aks get-credentials \
  --name chatify-aks \
  --resource-group $RESOURCE_GROUP
```

#### Deploy with Kubernetes

Create `k8s/deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: chatify-api
spec:
  replicas: 2
  selector:
    matchLabels:
      app: chatify-api
  template:
    metadata:
      labels:
        app: chatify-api
    spec:
      containers:
      - name: api
        image: ghcr.io/<username>/chatify-ai:latest
        ports:
        - containerPort: 8080
        env:
        - name: AZURE_OPENAI_ENDPOINT
          valueFrom:
            secretKeyRef:
              name: chatify-secrets
              key: endpoint
---
apiVersion: v1
kind: Service
metadata:
  name: chatify-api
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 8080
  selector:
    app: chatify-api
```

Deploy:

```bash
# Create secrets
kubectl create secret generic chatify-secrets \
  --from-literal=endpoint=<your-endpoint> \
  --from-literal=apikey=<your-key>

# Deploy
kubectl apply -f k8s/deployment.yaml

# Get external IP
kubectl get service chatify-api
```

---

## Troubleshooting

### Docker Build Issues

**Problem**: Build fails with "permission denied"

**Solution**:
```bash
# On Linux/Mac, ensure Docker is running
sudo systemctl start docker

# On Windows, restart Docker Desktop
# Settings → Restart Docker Desktop
```

### Database Connection Issues

**Problem**: API can't connect to SQL Server

**Solution**:
```bash
# Check SQL Server is running
docker-compose ps sqlserver

# View SQL Server logs
docker-compose logs sqlserver

# Verify connection string
docker-compose exec chatify-api env | grep CONNECTION
```

### Qdrant Connection Issues

**Problem**: "Failed to connect to Qdrant"

**Solution**:
```bash
# Check Qdrant is healthy
curl http://localhost:6333/health

# Restart Qdrant
docker-compose restart qdrant

# Check logs
docker-compose logs qdrant
```

### Azure OpenAI Errors

**Problem**: "401 Unauthorized" or "404 Not Found"

**Solution**:
1. Verify endpoint URL (should include `https://` and trailing `/`)
2. Check API key is correct
3. Verify deployment names match Azure portal
4. Ensure API key has proper permissions

### Rate Limiting Issues

**Problem**: Getting HTTP 429 errors

**Solution**:

Edit `appsettings.json`:
```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      { "Endpoint": "*", "Period": "1m", "Limit": 200 }
    ]
  }
}
```

Or disable for development:
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": false
  }
}
```

### GitHub Actions Failures

**Problem**: CI/CD workflow fails

**Solution**:
1. Check repository secrets are set correctly
2. Verify Docker build locally first
3. View detailed logs in Actions tab
4. Ensure GitHub Container Registry is enabled

### Memory Issues

**Problem**: Container runs out of memory

**Solution**:

Increase limits in `docker-compose.yml`:
```yaml
deploy:
  resources:
    limits:
      memory: 4G
```

Or run with more memory:
```bash
docker-compose up -d --scale chatify-api=1 \
  --memory=4g
```

### Migration Issues

**Problem**: "Cannot find migration"

**Solution**:
```bash
# List migrations
dotnet ef migrations list --project ChatAI.Infrastructure

# Remove last migration
dotnet ef migrations remove --project ChatAI.Infrastructure

# Re-create migration
dotnet ef migrations add <Name> --project ChatAI.Infrastructure

# Apply migrations
dotnet ef database update --project ChatAI.Infrastructure
```

### Health Check Failures

**Problem**: `/health` endpoint returns "Unhealthy"

**Solution**:
```bash
# Check individual dependencies
curl http://localhost:8080/health

# View detailed logs
docker-compose logs -f chatify-api | grep -i health

# Restart unhealthy services
docker-compose restart sqlserver qdrant
```

---

## Performance Tuning

### Production Recommendations

1. **Enable ReadyToRun compilation** (already in Dockerfile):
   ```dockerfile
   ENV PublishReadyToRun=true
   ```

2. **Increase cache limits**:
   ```json
   {
     "Cache": {
       "MaxCachedItems": 50000
     }
   }
   ```

3. **Adjust resource limits**:
   ```yaml
   deploy:
     resources:
       limits: { cpus: '4.0', memory: 4G }
   ```

4. **Enable response compression**:
   ```csharp
   builder.Services.AddResponseCompression();
   ```

5. **Monitor cache hit rate**:
   - Check logs for cache statistics
   - Aim for >70% hit rate
   - Adjust TTL values as needed

---

## Security Checklist

- [ ] Change default SQL Server password
- [ ] Rotate API keys regularly
- [ ] Use Azure Key Vault for secrets (production)
- [ ] Enable HTTPS (configured in production)
- [ ] Configure CORS for allowed origins
- [ ] Review rate limiting rules
- [ ] Enable Azure AD authentication (optional)
- [ ] Regular security updates (`dotnet outdated`)
- [ ] Monitor failed authentication attempts
- [ ] Configure firewall rules (Azure)

---

## Support

For issues or questions:
- Create GitHub Issue
- Check existing documentation
- Review application logs
- Contact support team

**Happy Deploying! 🚀**

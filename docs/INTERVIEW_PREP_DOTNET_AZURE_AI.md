# Interview Prep: .NET + Azure + AI Integration (based on this repo)

This document turns the codebase into interview-ready talking points (EPAM-style roles like **.NET Engineer with Azure** and **AI-Integration .NET Engineer**).

Use it as:
- a **study guide** (what the system does)
- a **story guide** (how you explain your decisions)
- a **code guide** (where to point reviewers)

---

## 1) 60-second self-intro (template)

> I built a cloud-native .NET backend that integrates Azure OpenAI for chat + embeddings, supports RAG with vector search, and runs as a containerized API. The system follows Clean Architecture and uses production practices like health checks, structured logging, rate limiting, and environment-based configuration. I also set up automated deployment to Azure Container Apps by building and pushing a Docker image to Azure Container Registry, then rolling out and validating with health checks.

Customize the bold parts depending on the vacancy:
- **AI role:** emphasize RAG, embeddings, tool calling, orchestration.
- **Azure/backend role:** emphasize cloud-native, CI/CD, reliability, security, observability.

---

## 2) What this project demonstrates (mapped to vacancy requirements)

### Strong .NET backend engineering
- Minimal hosting + DI + middleware pipeline
- Health endpoints
- Authentication & authorization
- Multi-tenancy middleware (tenant resolution in request pipeline)
- Rate limiting
- Clean Architecture separation

**Where to look:**
- `ChatAI.Api/Program.cs`

### Cloud-native + containerization
- Multi-stage Docker build
- Non-root runtime user
- Healthcheck in container
- Compose-based local stack (SQL Server + API)

**Where to look:**
- `Dockerfile`
- `docker-compose.yml`

### Azure + DevOps
- Pipeline builds image and pushes to **ACR**
- Deploys to **Azure Container Apps**
- Post-deploy health check
- Uses Azure service principal login

**Where to look:**
- `.github/workflows/cd.yml`

### AI integration (Azure OpenAI + Semantic Kernel)
- Azure OpenAI connector for chat completion
- Semantic Kernel factory (orchestration)
- Embeddings + vector storage interface

**Where to look:**
- `ChatAI.Infrastructure/AI/SemanticKernelFactory.cs`

### RAG / vector search (two storage strategies)
- Qdrant vector store implementation (tenant-filtered)
- SQL fallback implementation (cosine similarity)

**Where to look:**
- `ChatAI.Infrastructure/AI/QdrantVectorStorage.cs`
- `ChatAI.Infrastructure/AI/SqlVectorStorage.cs`

---

## 3) Code examples you should be able to explain

### 3.1 API startup and production middleware

**Why interviewers care:** shows real-world API composition, observability, security controls.

From `ChatAI.Api/Program.cs` (simplified excerpt):

```csharp
builder.Services.AddMemoryCache();
builder.Services.AddControllers();

builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);
builder.Services.AddAzureOpenAIServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddHealthCheckServices(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders(...);
await app.UseDatabaseMigrationsAsync(app.Environment);

app.UseGlobalExceptionHandler();
app.UseIpRateLimiting();

app.UseAuthentication();
app.UseTenantResolution();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
```

**What to say (quick bullets):**
- “I wire services via DI extensions to keep Program clean.”
- “Forwarded headers are important behind Azure proxies/ingress.”
- “Migrations run at startup in controlled environments.”
- “Auth first, then tenant resolution, then authorization.”
- “Health endpoints enable container/app platform probes.”

### 3.2 Qdrant-based vector search with tenant isolation

**Why interviewers care:** RAG + multi-tenant filtering is a very realistic AI backend requirement.

From `ChatAI.Infrastructure/AI/QdrantVectorStorage.cs` (key excerpt):

```csharp
var searchResult = await _client.SearchAsync(
    collectionName: _collectionName,
    vector: queryEmbedding,
    filter: new Filter
    {
        Must =
        {
            new Condition
            {
                Field = new FieldCondition
                {
                    Key = "tenant_id",
                    Match = new Match { Keyword = _tenantId.ToString() }
                }
            }
        }
    },
    limit: (ulong)limit,
    scoreThreshold: (float)scoreThreshold,
    cancellationToken: ct);
```

**What to say:**
- “Every vector operation is tenant-scoped to prevent data leakage.”
- “Cosine distance is used for semantic similarity.”
- “I treat vector DB as infra service behind an `IVectorStorage` interface.”

### 3.3 SQL-based vector search fallback (cosine similarity)

**Why interviewers care:** shows pragmatic engineering and tradeoffs.

From `ChatAI.Infrastructure/AI/SqlVectorStorage.cs` (key excerpt):

```csharp
var similarity = CosineSimilarity(queryEmbedding, embeddingData.Vector);
if (similarity >= scoreThreshold)
{
    similarities.Add((doc.Id, similarity));
}
```

**What to say:**
- “For small per-tenant document counts, in-memory cosine similarity is simple and cost-effective.”
- “For scale, we switch to Qdrant.”

### 3.4 Semantic Kernel wiring (Azure OpenAI connector)

From `ChatAI.Infrastructure/AI/SemanticKernelFactory.cs`:

```csharp
kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: options.ChatDeploymentName,
    endpoint: options.Endpoint,
    apiKey: options.ApiKey);
```

**What to say:**
- “The infra layer owns AI client wiring; app layer stays provider-agnostic.”
- “Options-based config supports different deployments/environments.”

---

## 4) CI/CD story you should tell (Azure Container Apps)

**Where to look:** `.github/workflows/cd.yml`

**High-level flow:**
1. Azure login (service principal)
2. Resolve ACR login server
3. Build Docker image via Buildx
4. Push to ACR with a commit/tag-based image tag
5. Deploy to Container Apps using `az containerapp update`
6. Health check `/health`

**What to say in interviews:**
- “I treat container image as the deployment artifact.”
- “Staging deploys on master; production deploys on version tags.”
- “Health check gates rollout to prevent broken releases.”

---

## 5) Reliability & security talking points (use in answers)

### Configuration & secrets
- Local dev uses environment files / appsettings.
- Production uses environment variables and secret stores (in Azure, you’d use App Settings + Key Vault).

See `README.md` for the configuration hierarchy.

### Observability
- Serilog is configured early (bootstrap logger) so startup failures are captured.

See `ChatAI.Api/Program.cs`.

### Rate limiting
- IP rate limiting is wired into middleware.

See `ChatAI.Api/Program.cs`.

### Multi-tenancy
- Tenant resolution happens between authentication and authorization.

See `ChatAI.Api/Program.cs`.

---

## 6) Likely interview questions (and how to answer using this project)

### Q1: “How do you integrate Azure OpenAI into a .NET service safely?”
Answer outline:
- Use options + DI to isolate provider config.
- Handle timeouts/retries (Polly or resilience layer if present).
- Don’t log secrets; store in env vars/Key Vault.
- Add request validation and usage limits.

Proof in code:
- `ChatAI.Infrastructure/AI/SemanticKernelFactory.cs`
- `README.md` configuration section

### Q2: “How would you implement RAG?”
Answer outline:
- Chunk documents, embed chunks, store vectors.
- On query: embed query, vector search, fetch top contexts, prompt with citations.
- Add tenant filter and score threshold.

Proof in code:
- `ChatAI.Infrastructure/AI/QdrantVectorStorage.cs`
- `ChatAI.Infrastructure/AI/SqlVectorStorage.cs`

### Q3: “How do you deploy to Azure?”
Answer outline:
- Containerize app, push to ACR, deploy to Container Apps.
- Add health checks + staged environments.

Proof in code:
- `.github/workflows/cd.yml`
- `Dockerfile`

### Q4: “How do you keep a backend maintainable as it grows?”
Answer outline:
- Clean Architecture layers.
- CQRS (MediatR) + validation.
- Keep Program.cs thin via extension methods.

Proof in code:
- `README.md` architecture section
- `ChatAI.Api/Program.cs`

### Q5: “How do you handle multi-tenancy?”
Answer outline:
- Resolve tenant early in pipeline.
- Apply tenant filters at DB and vector store layers.

Proof in code:
- `ChatAI.Infrastructure/AI/QdrantVectorStorage.cs` tenant filter

---

## 7) Quick study checklist (2 hours)

1. Read `README.md` (features + configuration + run steps)
2. Skim `ChatAI.Api/Program.cs` and explain the middleware order
3. Read `ChatAI.Infrastructure/AI/QdrantVectorStorage.cs` and explain tenant filtering
4. Read `.github/workflows/cd.yml` and be able to describe the deployment pipeline
5. Run the API locally and hit `/health` and one chat endpoint

---

## 8) Notes about framework version (.NET 10)

This repo references **.NET 10 preview** in the Docker image.

In interviews for .NET 6/8 roles, position it like this:
- “I built it with the latest runtime for learning and performance experiments.”
- “For enterprise production I’m comfortable targeting .NET 8 LTS and aligning dependencies accordingly.”

---

## 9) Your next step (recommended)

If you want, I can generate a **one-page CV bullet list** and a **LinkedIn project description** strictly based on this repo and the EPAM vacancy text.

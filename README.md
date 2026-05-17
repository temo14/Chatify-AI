# Chatify AI

A multi-tenant AI chat platform on .NET 10 that connects Facebook, Instagram, and WhatsApp pages to GPT-4o agents — each business gets an isolated tenant, knowledge base, and conversation history.

## What it does

Any business can sign up as a tenant, connect their social media pages through Meta OAuth, and have an AI agent auto-responding to inbound messages on those channels. Messages arrive via a shared Meta webhook endpoint, get routed to the correct tenant by page/account ID, pass through a RAG pipeline that retrieves relevant knowledge, and generate a streaming GPT-4o response — all within a single deployment serving unlimited tenants.

## Architecture

```
ChatAI.Api              — REST endpoints, SSE streaming, Meta webhook receiver
ChatAI.Application      — CQRS (MediatR), FluentValidation, business logic
ChatAI.Domain           — Entities, interfaces (no framework dependencies)
ChatAI.Infrastructure   — Azure OpenAI, Qdrant, SQL Server, Meta Graph API client
```

## Key Technical Features

### Multi-Tenancy

One deployment serves multiple isolated businesses. Tenant resolution runs in priority order on every request:

1. **JWT claim** (`tenant_id`) — authenticated admin/user requests
2. **Custom domain** — `acmecorp.com` maps to a tenant record
3. **Subdomain** — `acmecorp.yourplatform.com`
4. **`X-Tenant-Slug` header** — for headless API clients

Each tenant has a plan tier (Free / Basic / Pro / Enterprise), monthly message and document quotas tracked in real time, and data isolation enforced at the repository layer. The platform-admin role can create, update, and soft-delete tenants; tenant-admin users can configure their own chat settings and channel connections.

### Meta Webhook Integration

A single endpoint (`/api/webhooks/meta`) receives events for all tenants from Messenger, Instagram DMs, and WhatsApp Cloud API. Each request is:

1. **Signature-verified** — HMAC-SHA256 over the raw body against `X-Hub-Signature-256` before any other work
2. **Channel-routed** — the `object` field in the payload (`page` / `instagram` / `whatsapp_business_account`) determines channel type; `page_id`, `instagram_account_id`, or `phone_number_id` maps to a specific tenant connection
3. **Queued** — enqueued for async processing so Meta's delivery gets a fast 200 OK

**OAuth flow:** tenant admins initiate from the dashboard → Meta redirects back with an authorization code → the frontend calls an authenticated `/oauth/complete` endpoint to exchange the code. The anonymous callback endpoint intentionally only redirects to the UI and never touches the token, preventing CSRF and token-hijacking attacks. Access tokens are stored AES-encrypted with versioned keys.

### AI Pipeline

- **RAG** — user messages trigger semantic search in Qdrant (cosine similarity, 1536-d embeddings via `text-embedding-3-small`); top matches are injected as context before the GPT-4o call
- **Tool calling** — the agent can invoke registered tools autonomously (weather, search, custom functions) before generating a response
- **SSE streaming** — responses stream token-by-token via Server-Sent Events, with nginx buffering disabled via `X-Accel-Buffering: no`

## Tech Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 10 |
| AI | Azure OpenAI — GPT-4o + text-embedding-3-small |
| Vector DB | Qdrant |
| Relational DB | SQL Server 2022 |
| Channels | Meta Graph API (Messenger, Instagram, WhatsApp) |
| Deployment | Azure Container Apps |
| CI/CD | GitHub Actions → Azure Container Registry → Container Apps |

## Use Cases

**Customer support automation** — A retailer connects their Facebook page. When a customer sends "Where's my order?", the webhook fires, the AI retrieves the shipping policy from the tenant's knowledge base, and replies in seconds without human intervention.

**Multi-location businesses** — Each franchise location is a separate tenant with its own knowledge base and social pages. The platform owner manages all tenants from a single admin dashboard.

**SaaS resellers** — A digital agency onboards client businesses as tenants, connects their Instagram accounts via OAuth, and delivers a white-labeled AI agent — all from one deployment with per-tenant billing quotas.

## Setup

### Prerequisites

- .NET 10 SDK
- Docker (SQL Server + Qdrant)
- Azure OpenAI resource with `gpt-4o` and `text-embedding-3-small` deployments
- Meta App with Messenger/Instagram/WhatsApp products and a configured webhook

### Quick Start (Docker)

```bash
git clone https://github.com/temo14/Chatify-AI.git
cd Chatify-AI
cp .env.example .env           # fill in credentials
docker-compose up -d
docker exec chatify-api dotnet ef database update --project /app
# Swagger: http://localhost:5000/swagger
# Qdrant:  http://localhost:6333/dashboard
```

### Required Environment Variables

```bash
# Azure OpenAI
AZUREOPENAI__ENDPOINT=https://your-resource.openai.azure.com/
AZUREOPENAI__APIKEY=...

# Meta
Meta__AppSecret=...
Meta__OAuth__ClientId=...
Meta__OAuth__ClientSecret=...
Meta__OAuth__RedirectUri=https://your-domain.com/api/tenant/meta/oauth/callback
Meta__Webhook__VerifyToken=...

# Database & JWT
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=...;Database=ChatifyAI;...
JWT__SECRET=...
```

## CI/CD

Every push runs three parallel GitHub Actions jobs: **build** (restore → compile → test → publish TRX results), **code-quality** (`dotnet format` verification), and **docker** (image build with GHA layer cache).

On merge to `master`, the CD pipeline logs into Azure, pushes the image to ACR, updates the Container App, then polls `/health` until the new revision passes. Production deploys are gated on `v*` tags or a manual workflow dispatch with environment selection.

## Project Structure

```
ChatAI.Api/
  Controllers/     — REST endpoints + Meta webhook receiver
  Middleware/      — Tenant resolution (JWT → domain → subdomain → header)
ChatAI.Application/
  Features/        — CQRS commands/queries, one folder per feature slice
ChatAI.Domain/
  Entities/        — Tenant, MetaChannelConnection, KnowledgeDocument, ChatSession
ChatAI.Infrastructure/
  Services/Meta/   — Webhook signature validator, OAuth exchange, message sender
  Services/        — Qdrant vector service, Azure OpenAI client
  Data/            — EF Core DbContext + migrations
```

## License

MIT

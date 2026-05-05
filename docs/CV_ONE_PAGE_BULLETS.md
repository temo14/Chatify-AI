# One-page CV bullet list (tailored to .NET + Azure + AI roles)

Replace placeholders like **YOUR NAME** and keep this to 1 page.

---

## Header
**YOUR NAME** — .NET Engineer (Azure • AI Integration)  
City, Country • Email • Phone • LinkedIn • GitHub

---

## Summary (2–3 lines)
.NET backend engineer with hands-on experience building cloud-native APIs and integrating Azure OpenAI for chat + embeddings, including RAG/vector search. Strong focus on Clean Architecture, reliability (health checks, rate limiting, logging), and containerized deployments to Azure.

---

## Core skills
- **Backend:** C#, ASP.NET Core, REST APIs, DI, middleware, authn/authz
- **Architecture:** Clean Architecture, DDD layering, CQRS (MediatR), FluentValidation
- **AI:** Azure OpenAI (chat + embeddings), RAG, vector search, Microsoft Semantic Kernel
- **Data:** SQL Server, EF Core (migrations), multi-tenant data isolation
- **Cloud/DevOps:** Docker, GitHub Actions, Azure Container Registry (ACR), Azure Container Apps, health checks
- **Observability/Resilience:** Serilog (structured logging), rate limiting, production configuration via environment variables

---

## Project (featured): Chatify AI — Cloud-native AI backend (Azure OpenAI + RAG)
Tech: .NET (repo uses .NET 10 preview), ASP.NET Core, Azure OpenAI, Semantic Kernel, SQL Server, Qdrant, Docker, GitHub Actions, Azure Container Apps

Choose ONE of the two bullet sets below depending on the vacancy.

### Variant A — AI Integration focus (recommended for “AI Integration .NET Engineer”)
- Built an AI-enabled .NET backend integrating **Azure OpenAI** for chat completion and **embeddings**, enabling production-style AI features (prompting + retrieval).
- Implemented **RAG** with vector search, storing embeddings and retrieving relevant context for responses; supported scalable storage via **Qdrant**.
- Designed a tenant-aware vector search layer with per-tenant filtering to prevent cross-tenant data exposure.
- Added an alternative **SQL-based vector search** strategy using cosine similarity for smaller datasets to balance cost/complexity vs scale.
- Integrated **Microsoft Semantic Kernel** as an orchestration layer (provider wiring encapsulated in infrastructure) to keep application logic clean.
- Exposed API endpoints suitable for real-time experiences (including streaming patterns in the architecture) and production operations (health endpoints).

### Variant B — Azure backend focus (recommended for “Key .NET Engineer with Azure”)
- Engineered a containerized ASP.NET Core API with production middleware ordering (authn → tenant resolution → authz), rate limiting, and health endpoints.
- Implemented structured logging and startup diagnostics using Serilog (bootstrap + full configuration) to support observability in production.
- Built Docker images using multi-stage builds and a non-root runtime user; added container health checks for platform readiness/liveness.
- Automated deployments to **Azure Container Apps** using a CI/CD pipeline that builds and pushes images to **ACR** and performs post-deploy health checks.
- Implemented environment-driven configuration (12-factor style), supporting local dev and production secrets via environment variables.

---

## Selected technical highlights (optional, 3–5 bullets)
- Multi-tenancy support with tenant-aware data access and tenant-scoped vector retrieval.
- API hardening: rate limiting, global exception handling, health/readiness endpoints.
- Clean separation of concerns across Api/Application/Domain/Infrastructure layers.

---

## Testing
- Unit and integration tests for critical flows (including vector/RAG paths where applicable).

---

## Notes you can say in interview (not necessarily on CV)
- Repo targets **.NET 10 preview** for learning/experimentation; comfortable delivering client work on **.NET 8 LTS**.
- CI/CD shown via GitHub Actions; same concepts map directly to Azure DevOps pipelines (build artifact = container image, staged deploys, health gates).

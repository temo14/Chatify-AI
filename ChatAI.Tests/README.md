# ChatAI.Tests

Test suite for Chatify AI, organized into unit and integration layers.

## Structure

```
ChatAI.Tests/
├── Unit/
│   ├── Handlers/       — CQRS query handler tests
│   ├── Services/       — Service-layer unit tests
│   └── Validators/     — FluentValidation rule tests
└── Integration/        — Tests against real infrastructure
```

## What's Covered

**Unit tests** (no external services, run in milliseconds):

| File | What it tests |
|------|---------------|
| `GetSessionQueryHandlerTests` | Chat session retrieval, not-found handling |
| `ExportSessionQueryHandlerTests` | Session export formatting and edge cases |
| `ChatStreamServiceTests` | SSE streaming logic with mocked OpenAI |
| `MetaWebhookSignatureValidatorTests` | HMAC-SHA256 signature verification (13 cases) |
| `AzureServiceBusMetaWebhookQueueTests` | Webhook queue enqueue/dequeue behaviour (8 cases) |
| `SendChatCommandValidatorTests` | Input validation rules for chat commands |
| `AddKnowledgeDocumentCommandValidatorTests` | Validation for knowledge document ingestion |

**Integration tests** (require running infrastructure):

| File | What it tests | Requires |
|------|---------------|---------|
| `RagIntegrationTests` | RAG pipeline end-to-end: embed → store → retrieve | Qdrant |
| `ComprehensiveIntegrationTests` | Cross-layer flows | SQL Server + Qdrant |
| `MetaOAuthIntegrationTests` | OAuth flow against real API | CustomWebApplicationFactory (not yet implemented) |

## Running Tests

```bash
# All unit tests (fast, no dependencies)
dotnet test --filter "Category!=Integration"

# Integration tests (start Docker services first)
docker-compose up -d
dotnet test --filter "Category=Integration"

# Full suite with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Technologies

- **xUnit** — test framework
- **FluentAssertions** — assertion syntax
- **Moq** — dependency mocking
- **EF Core InMemory** — fast repository tests without a real database

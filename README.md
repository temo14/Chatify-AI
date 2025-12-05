# Chatify AI

An intelligent AI agent built with .NET 10, Azure OpenAI, and Clean Architecture principles.

## Features

- 🤖 **AI Agent with Tool Calling** - Autonomous decision-making with function execution
- 📚 **RAG (Retrieval-Augmented Generation)** - Semantic vector search with Qdrant
- 💾 **Persistent Chat Sessions** - Database-backed conversation history
- 🔧 **Extensible Tool System** - Easy to add custom tools/functions
- 🏗️ **Clean Architecture** - Domain-driven design with clear separation of concerns
- 🐳 **Docker Ready** - Full containerization with SQL Server and Qdrant
- 🔍 **Production Vector Search** - Semantic similarity with cosine distance

## Architecture

**Clean Architecture with CQRS, Validation, and AI Orchestration**

```
ChatAI.Api              - REST API endpoints & SSE streaming
ChatAI.Application      - CQRS commands/queries, validation, business logic
ChatAI.Domain           - Core entities & domain logic
ChatAI.Infrastructure   - Azure OpenAI, Qdrant, SQL Server, tools
```

**📖 Comprehensive Documentation:**
- [ARCHITECTURE.md](ARCHITECTURE.md) - Detailed architecture and design patterns
- [docs/IMPLEMENTATION_COMPLETE.md](docs/IMPLEMENTATION_COMPLETE.md) - Feature implementation guide
- [DEPLOYMENT.md](DEPLOYMENT.md) - Deployment instructions

**Design Patterns:**
- ✅ CQRS (MediatR) - Command/Query separation
- ✅ FluentValidation - Input/output validation with security checks
- ✅ Streaming (SSE) - Real-time token-by-token responses
- ✅ Semantic Kernel - AI orchestration with plugins (optional)
- ✅ Repository Pattern - Data access abstraction
- ✅ Dependency Injection - Inversion of control

## Tech Stack

- **.NET 10**: Latest framework with native AOT support
- **Azure OpenAI**: GPT-4o for chat, text-embedding-3-small for embeddings
- **Qdrant**: Production vector database for semantic search
- **SQL Server 2022**: Relational storage for metadata and sessions
- **Docker**: Containerized deployment with multi-service orchestration

## Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone https://github.com/temo14/Chatify-AI.git
   cd Chatify-AI
   ```

2. **Configure Azure OpenAI**
   ```bash
   cp .env.example .env
   # Edit .env with your Azure OpenAI credentials
   ```

3. **Run with Docker Compose**
   ```bash
   docker-compose up -d
   ```

4. **Apply database migrations**
   ```bash
   docker exec chatify-api dotnet ef database update --project /app
   ```

5. **Access the API**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - Qdrant Dashboard: http://localhost:6333/dashboard

## Manual Setup (without Docker)

### Prerequisites
- .NET 10 SDK
- SQL Server (or SQL Server Express)
- Azure OpenAI API access
- Qdrant (or use Docker for just Qdrant: `docker run -p 6333:6333 qdrant/qdrant`)

### Steps

1. **Restore packages**
   ```bash
   dotnet restore
   ```

2. **Configure secrets** (recommended for local dev)
   ```bash
   cd ChatAI.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ChatifyAI;Trusted_Connection=True;"
   dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
   dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key-here"
   dotnet user-secrets set "Qdrant:Endpoint" "http://localhost:6333"
   ```

   Or edit `ChatAI.Api/appsettings.json` directly (not recommended for production)

3. **Start Qdrant** (if not using Docker)
   ```bash
   docker run -p 6333:6333 -p 6334:6334 -v $(pwd)/qdrant-data:/qdrant/storage qdrant/qdrant:latest
   ```

4. **Apply migrations**
   ```bash
   cd ChatAI.Api
   dotnet ef database update --project ../ChatAI.Infrastructure/ChatAI.Infrastructure.csproj
   ```

5. **Run the application**
   ```bash
   dotnet run --project ChatAI.Api
   ```

The application will automatically:
- Initialize the Qdrant collection (`chatai_knowledge`)
- Seed the database with test users and knowledge documents
- Generate embeddings for knowledge documents on first add/update

## Authentication

All API requests require an `X-API-Key` header for authentication.

### Test Users

The database is seeded with test users and API keys:

| User ID | API Key | Description |
|---------|---------|-------------|
| demo-user | demo-key-12345 | Default demo user |
| test-user | test-key-67890 | Testing account |
| admin-user | admin-key-abcdef | Admin account |

## Seeded Data

The application automatically seeds the database with:

### Knowledge Base (5 Documents)
1. **Return Policy** - 30-day return policy details
2. **Warranty Information** - 1-year limited warranty coverage
3. **Shipping Policy** - Domestic and international shipping info
4. **Customer Support FAQ** - Common support questions
5. **Account Management** - User account information

### Demo Conversation
- Pre-populated conversation for `demo-user`
- 4 messages demonstrating chat history persistence
- Tests RAG retrieval and session management

## API Usage

### Send a Chat Message

```bash
# PowerShell
curl -X POST http://localhost:5000/api/chat/send `
  -H "X-API-Key: demo-key-12345" `
  -H "Content-Type: application/json" `
  -d '{\"userId\":\"demo-user\",\"message\":\"What is your return policy?\",\"useTools\":true}'

# Bash
curl -X POST http://localhost:5000/api/chat/send \
  -H "X-API-Key: demo-key-12345" \
  -H "Content-Type: application/json" \
  -d '{"userId":"demo-user","message":"What is your return policy?","useTools":true}'
```

### Quick Test

```powershell
# Run the provided test script
.\test-api.ps1
```

### Response Example

```json
{
  "reply": "According to our return policy, we offer a 30-day return window...",
  "sessionId": "abc-123-def",
  "toolCalled": false,
  "timestamp": "2025-12-04T10:30:00Z"
}
```

### Tool Calling Example

```json
{
  "reply": "The current weather in London is 15°C and cloudy.",
  "sessionId": "abc-123-def",
  "toolCalled": true,
  "toolCall": {
    "name": "get_weather",
    "arguments": "{\"city\":\"London\",\"unit\":\"celsius\"}",
    "result": "15°C, Cloudy"
  },
  "timestamp": "2025-12-04T10:30:00Z"
}
```

### Documentation

- **API Examples**: See `API_EXAMPLES.http` for comprehensive request examples
- **Testing Guide**: See `TESTING_GUIDE.md` for testing scenarios and validation

## Adding Knowledge to RAG

### Automatic Embedding Generation

When you add a document, embeddings are automatically generated and stored in Qdrant:

```sql
INSERT INTO KnowledgeDocuments (Id, Title, Content, Category, IsActive, CreatedAt)
VALUES (
  NEWID(),
  'Company Refund Policy',
  'Refunds are allowed within 30 days of purchase...',
  'policy',
  1,
  GETUTCDATE()
);
```

The application will:
1. Generate 1536-dimensional embedding via Azure OpenAI
2. Store embedding in Qdrant with metadata
3. Enable semantic search for this document

### Semantic Search

The RAG system uses **semantic similarity** instead of keyword matching:

**Example Query**: "How do I get my money back?"  
**Matches**: "Return Policy", "Refund Process", "Reimbursement Guidelines"  
**How**: Cosine similarity between query embedding and document embeddings (70% threshold)

**Fallback**: If no semantic matches found, falls back to traditional SQL text search.

## Project Structure

```
├── ChatAI.Api/
│   ├── Controllers/        # API endpoints
│   ├── DTOs/              # Data transfer objects
│   └── Program.cs         # DI configuration
├── ChatAI.Application/
│   ├── Configuration/     # Options classes
│   ├── Interfaces/        # Service contracts
│   ├── Models/           # Request/Response models
│   └── Services/         # Business logic
├── ChatAI.Domain/
│   ├── Entities/         # Domain models
│   └── Enums/           # Enumerations
├── ChatAI.Infrastructure/
│   ├── Data/             # DbContext & migrations
│   ├── Repositories/     # Data access
│   ├── Services/         # QdrantVectorService
│   ├── Tools/           # Tool executor
│   └── AzureService.cs  # Azure OpenAI client
└── docker-compose.yml
```

## Environment Variables

### Core Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint URL | Required |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key | Required |
| `AZURE_OPENAI_CHAT_DEPLOYMENT` | Chat model deployment name | gpt-4o |
| `AZURE_OPENAI_EMBEDDING_DEPLOYMENT` | Embedding model name | text-embedding-3-small |
| `ConnectionStrings__DefaultConnection` | Database connection string | See docker-compose.yml |

### Vector Search Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `QDRANT__ENDPOINT` | Qdrant connection URL | http://localhost:6333 |
| `QDRANT__COLLECTIONNAME` | Vector collection name | chatai_knowledge |
| `QDRANT__VECTORSIZE` | Embedding dimensions | 1536 |

### Security Configuration

See `SETUP_SECRETS.md` for detailed configuration instructions.

## Troubleshooting

### Qdrant Connection Issues

**Error**: "Unable to connect to Qdrant"  
**Solution**: Verify Qdrant is running:
```bash
curl http://localhost:6333/
# Should return: {"title":"qdrant - vector search engine","version":"..."}
```

### Embedding Generation Fails

**Error**: "Failed to generate embeddings"  
**Solution**: Check Azure OpenAI credentials and deployment name:
```bash
dotnet user-secrets list --project ChatAI.Api
```

### No Semantic Search Results

**Cause**: Query too specific or threshold too high  
**Solution**: Adjust similarity threshold in `KnowledgeRepository.cs` (default: 0.7)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

MIT License - see LICENSE file for details

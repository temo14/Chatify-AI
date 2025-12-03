# Chatify AI

An intelligent AI agent built with .NET 10, Azure OpenAI, and Clean Architecture principles.

## Features

- 🤖 **AI Agent with Tool Calling** - Autonomous decision-making with function execution
- 📚 **RAG (Retrieval-Augmented Generation)** - Knowledge base for contextual responses
- 💾 **Persistent Chat Sessions** - Database-backed conversation history
- 🔧 **Extensible Tool System** - Easy to add custom tools/functions
- 🏗️ **Clean Architecture** - Domain-driven design with clear separation of concerns
- 🐳 **Docker Ready** - Full containerization with SQL Server

## Architecture

```
ChatAI.Api          - REST API endpoints
ChatAI.Application  - Business logic & services
ChatAI.Domain       - Core entities & enums
ChatAI.Infrastructure - External integrations (DB, Azure OpenAI)
```

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

## Manual Setup (without Docker)

### Prerequisites
- .NET 10 SDK
- SQL Server (or SQL Server Express)
- Azure OpenAI API access

### Steps

1. **Restore packages**
   ```bash
   dotnet restore
   ```

2. **Update connection string**
   Edit `ChatAI.Api/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=ChatifyAI;Trusted_Connection=True;"
   }
   ```

3. **Update Azure OpenAI settings**
   Edit `ChatAI.Api/appsettings.json` with your credentials

4. **Apply migrations**
   ```bash
   cd ChatAI.Api
   dotnet ef database update --project ../ChatAI.Infrastructure/ChatAI.Infrastructure.csproj
   ```

5. **Run the application**
   ```bash
   dotnet run --project ChatAI.Api
   ```

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

You can add documents to the knowledge base via the database:

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

The AI will automatically search and use this knowledge when answering related questions.

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
│   ├── AI/              # Azure OpenAI client
│   ├── Data/            # DbContext
│   ├── Repositories/    # Data access
│   └── Tools/          # Tool executor
└── docker-compose.yml
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint URL | Required |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key | Required |
| `AZURE_OPENAI_CHAT_DEPLOYMENT` | Chat model deployment name | gpt-4 |
| `AZURE_OPENAI_EMBEDDING_DEPLOYMENT` | Embedding model name | text-embedding-ada-002 |
| `ConnectionStrings__DefaultConnection` | Database connection string | See docker-compose.yml |

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

MIT License - see LICENSE file for details

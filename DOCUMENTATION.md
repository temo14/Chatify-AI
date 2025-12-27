# Chatify AI - Complete System Documentation

**Version:** 1.0  
**Last Updated:** December 27, 2025  
**Architecture:** Clean Architecture + CQRS + Multi-Tenancy

---

## TABLE OF CONTENTS

1. [System Architecture Overview](#1-system-architecture-overview)
2. [Domain Layer Documentation](#2-domain-layer-documentation)
3. [Application Layer Documentation](#3-application-layer-documentation)
4. [Infrastructure Layer Documentation](#4-infrastructure-layer-documentation)
5. [API Layer Documentation](#5-api-layer-documentation)
6. [Authentication & Authorization Flows](#6-authentication--authorization-flows)
7. [Multi-Tenancy Implementation](#7-multi-tenancy-implementation)
8. [Database Schema & Migrations](#8-database-schema--migrations)
9. [API Endpoints Reference](#9-api-endpoints-reference)
10. [Security Implementation](#10-security-implementation)

---

## 1. SYSTEM ARCHITECTURE OVERVIEW

### 1.1 Project Structure

```
Chatify AI/
├── ChatAI.Domain/              # Core business entities and interfaces
├── ChatAI.Application/         # Business logic, CQRS handlers, validators
├── ChatAI.Infrastructure/      # External services, database, AI integration
├── ChatAI.Api/                 # REST API, controllers, middleware
└── ChatAI.Tests/              # Unit and integration tests
```

### 1.2 Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    ChatAI.Api                           │
│  • Controllers (HTTP endpoints)                         │
│  • Middleware (Global exception, tenant resolution)     │
│  • DTOs (Data Transfer Objects)                         │
│  • Attributes (Authorization)                           │
└────────────────────┬────────────────────────────────────┘
                     │ depends on ↓
┌────────────────────▼────────────────────────────────────┐
│              ChatAI.Application                         │
│  • Features (CQRS Commands/Queries)                     │
│  • Handlers (MediatR)                                   │
│  • Validators (FluentValidation)                        │
│  • Behaviors (Pipeline cross-cutting)                   │
│  • Services (Application-specific logic)                │
└────────────────────┬────────────────────────────────────┘
                     │ depends on ↓
┌────────────────────▼────────────────────────────────────┐
│            ChatAI.Infrastructure                        │
│  • DbContext (Entity Framework Core)                    │
│  • Repositories (Data access)                           │
│  • AI Services (Azure OpenAI integration)               │
│  • External APIs (Email, vector storage)                │
│  • Migrations (Database schema changes)                 │
└────────────────────┬────────────────────────────────────┘
                     │ depends on ↓
┌────────────────────▼────────────────────────────────────┐
│               ChatAI.Domain                             │
│  • Entities (Database models)                           │
│  • Interfaces (Repository contracts)                    │
│  • Enums (Domain constants)                             │
│  • Models (Domain value objects)                        │
│  • NO DEPENDENCIES (Pure domain logic)                  │
└─────────────────────────────────────────────────────────┘
```

### 1.3 Key Design Principles

1. **Dependency Rule**: Dependencies point inward (Domain has no dependencies)
2. **CQRS**: Commands (write) and Queries (read) are separated
3. **Mediator Pattern**: MediatR decouples request/response
4. **Repository Pattern**: Abstracts data access
5. **Clean Architecture**: Business logic independent of frameworks

### 1.4 Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Framework** | .NET 10.0 | Modern C# features, performance |
| **API** | ASP.NET Core | REST endpoints, middleware |
| **ORM** | Entity Framework Core | Database access |
| **Database** | SQL Server | Relational data storage |
| **AI** | Azure OpenAI | Chat completions, embeddings |
| **Vector DB** | Qdrant | Semantic search for RAG |
| **Validation** | FluentValidation | Input validation |
| **Logging** | Serilog | Structured logging |
| **Cache** | MemoryCache | Performance optimization |
| **Rate Limiting** | AspNetCoreRateLimit | API protection |

---

## 2. DOMAIN LAYER DOCUMENTATION

**Location:** `ChatAI.Domain/`  
**Purpose:** Core business entities, value objects, and domain interfaces  
**Dependencies:** None (innermost layer)

### 2.1 Entities

#### 2.1.1 Tenant Entity

**File:** `ChatAI.Domain/Entities/Tenant.cs`

**Purpose:** Represents a customer organization in the multi-tenant system. Each tenant is an isolated business unit (e.g., music studio, clinic, shop).

**Properties:**

| Property | Type | Description | Constraints |
|----------|------|-------------|-------------|
| `Id` | `Guid` | Primary key, unique identifier | Required, auto-generated |
| `Slug` | `string` | URL-friendly identifier | Required, unique, lowercase, alphanumeric |
| `Name` | `string` | Display name of the organization | Required, max 200 chars |
| `Email` | `string` | Primary contact email | Required, valid email format |
| `PlanTier` | `string` | Subscription plan level | "Free", "Basic", "Pro", "Enterprise" |
| `IsActive` | `bool` | Account status (soft delete) | Required, default true |
| `CustomDomain` | `string?` | Custom domain (e.g., chat.studio.com) | Optional, unique if provided |
| `LogoUrl` | `string?` | Branding logo URL | Optional, valid URL |
| `PrimaryColor` | `string?` | Brand color hex code | Optional, default #667eea |
| `MaxDocuments` | `int` | Knowledge document limit | Default 10, plan-based |
| `MaxMonthlyMessages` | `int` | Monthly chat message limit | Default 1000, plan-based |
| `CurrentDocumentCount` | `int` | Current document usage | Auto-calculated |
| `CurrentMonthMessages` | `int` | Messages this billing period | Auto-calculated |
| `BillingPeriodStart` | `DateTime` | Current billing cycle start | Required |
| `CreatedAt` | `DateTime` | Account creation timestamp | Required, UTC |
| `LastActivityAt` | `DateTime?` | Last interaction timestamp | Optional, auto-updated |
| `SubscriptionExpiresAt` | `DateTime?` | Subscription end date | Optional |

**Navigation Properties:**
```csharp
public virtual TenantSettings? Settings { get; set; }
public virtual ICollection<AdminUser> AdminUsers { get; set; }
public virtual ICollection<KnowledgeDocument> KnowledgeDocuments { get; set; }
```

**Business Rules:**
- Slug must be unique across all tenants
- CustomDomain must be unique if provided
- Cannot delete tenant with active users (must deactivate)
- MaxDocuments and MaxMonthlyMessages enforced by plan tier
- BillingPeriodStart resets CurrentMonthMessages monthly

**Database Indexes:**
- Primary key on `Id`
- Unique index on `Slug`
- Unique index on `CustomDomain`
- Index on `IsActive` (for filtering)

---

#### 2.1.2 TenantSettings Entity

**File:** `ChatAI.Domain/Entities/TenantSettings.cs`

**Purpose:** Configuration settings specific to each tenant. Controls AI behavior, features, and UI customization.

**Properties:**

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `Id` | `Guid` | Primary key | Auto-generated |
| `TenantId` | `Guid` | Foreign key to Tenant | Required |
| `WelcomeMessage` | `string?` | Greeting shown to end-users | "Hi! How can I help?" |
| `SystemPrompt` | `string?` | AI instructions override | null (uses global) |
| `EnableKnowledge` | `bool` | Enable RAG knowledge retrieval | true |
| `EnableToolCalling` | `bool` | Allow AI to use tools/functions | true |
| `EnableEmailSupport` | `bool` | Enable email sending tool | false |
| `EnableConversationHistory` | `bool` | Include past messages in context | true |
| `MaxHistoryMessages` | `int` | Number of past messages to include | 10 |
| `Temperature` | `float` | AI creativity (0.0-2.0) | 0.7 |
| `MaxTokens` | `int` | Maximum response length | 800 |
| `VectorStorageMode` | `string` | "InMemory" or "Qdrant" | "InMemory" |
| `QdrantCollectionName` | `string?` | Vector DB collection name | null |
| `EnableDocumentChunking` | `bool` | Split large documents | true |
| `ChunkSize` | `int` | Characters per chunk | 1000 |
| `ChunkOverlap` | `int` | Overlap between chunks | 200 |
| `EnableOverview` | `bool` | Show session summaries | true |

**Navigation Properties:**
```csharp
public virtual Tenant Tenant { get; set; }
```

**Business Rules:**
- One-to-one relationship with Tenant
- Temperature must be between 0.0 and 2.0
- MaxTokens must be between 100 and 4000
- ChunkOverlap must be less than ChunkSize
- If EnableKnowledge is false, RAG is disabled

**Usage Example:**
```csharp
// Override AI behavior for this tenant
tenantSettings.SystemPrompt = "You are a medical assistant. Be professional and accurate.";
tenantSettings.Temperature = 0.3; // Lower = more deterministic
tenantSettings.MaxHistoryMessages = 5; // Shorter context window
```

---

#### 2.1.3 AdminUser Entity

**File:** `ChatAI.Domain/Entities/AdminUser.cs`

**Purpose:** Administrative users who manage tenants, configurations, and knowledge base. Two roles: TenantAdmin (scoped to their tenant) and PlatformAdmin (access all tenants).

**Properties:**

| Property | Type | Description | Constraints |
|----------|------|-------------|-------------|
| `Id` | `Guid` | Primary key | Required |
| `TenantId` | `Guid` | Foreign key to Tenant | Required |
| `Username` | `string` | Login username | Required, unique per tenant |
| `PasswordHash` | `string` | BCrypt hashed password | Required, never plain text |
| `FullName` | `string` | Display name | Required |
| `Email` | `string` | Email address | Required, valid format |
| `Role` | `AdminRole` | TenantAdmin or PlatformAdmin | Required enum |
| `IsActive` | `bool` | Account status | Default true |
| `CreatedAt` | `DateTime` | Account creation | Required UTC |
| `LastLoginAt` | `DateTime?` | Last successful login | Optional |
| `CreatedBy` | `Guid?` | AdminUser who created this user | Optional |

**Navigation Properties:**
```csharp
public virtual Tenant Tenant { get; set; }
```

**AdminRole Enum:**
```csharp
public enum AdminRole
{
    TenantAdmin = 0,      // Manages their own tenant only
    PlatformAdmin = 1     // Manages all tenants (Dott staff)
}
```

**Business Rules:**
- Username unique within tenant (not globally unique)
- Password must be BCrypt hashed (WorkFactor >= 12)
- Email must be valid and unique per tenant
- PlatformAdmin users can access any tenant
- TenantAdmin users restricted to their TenantId
- Cannot delete last admin user of a tenant
- LastLoginAt updated on successful authentication

**Security:**
- Passwords stored using BCrypt hashing
- Login endpoint rate-limited to prevent brute force
- JWT tokens issued upon successful authentication
- Token contains: UserId, Username, Role, TenantId

**Database Indexes:**
- Primary key on `Id`
- Composite unique index on `(TenantId, Username)`
- Index on `Email`
- Index on `IsActive`

---

#### 2.1.4 ApiKey Entity

**File:** `ChatAI.Domain/Entities/ApiKey.cs`

**Purpose:** API key authentication for external applications. Allows third-party systems to integrate with the chat API. Keys are tenant-scoped for security isolation.

**Properties:**

| Property | Type | Description | Notes |
|----------|------|-------------|-------|
| `Id` | `Guid` | Primary key | Auto-generated |
| `KeyHash` | `string` | SHA256 hash of API key | Irreversible, unique |
| `TenantId` | `string` | Tenant identifier | Stored as string GUID |
| `ClientName` | `string` | Human-readable identifier | "Mobile App", "Website" |
| `Description` | `string?` | Optional notes | Purpose, owner, etc. |
| `IsActive` | `bool` | Soft delete flag | Default true |
| `RateLimitPerMinute` | `int` | Requests per minute limit | Default 20 |
| `RateLimitPerDay` | `int` | Requests per day limit | Default 1000 |
| `CreatedAt` | `DateTime` | Creation timestamp | UTC |
| `CreatedBy` | `Guid` | AdminUser who created it | Audit trail |
| `ExpiresAt` | `DateTime?` | Optional expiration | null = never expires |
| `LastUsedAt` | `DateTime?` | Last authentication | Auto-updated |
| `UsageCount` | `long` | Total requests made | Incremented on each use |

**Security Model:**
- **Plain Key Generation:** 32-character alphanumeric string
- **Storage:** Only SHA256 hash stored in database
- **Visibility:** Plain key shown **only once** upon creation
- **Validation:** Hash incoming key and compare with stored hash
- **Tenant Association:** TenantId = Admin's TenantId (set during creation)

**Authentication Flow:**
```
1. Client sends: X-API-Key header
2. Hash the provided key (SHA256)
3. Query database: GetByKeyHashAsync(hash)
4. Validate: IsActive = true, not expired
5. Create claims: tenant_id = ApiKey.TenantId
6. TenantResolutionMiddleware reads tenant_id claim
7. Request scoped to correct tenant
```

**Business Rules:**
- KeyHash must be unique
- TenantId must match creating admin's tenant
- Expired keys (ExpiresAt < Now) automatically rejected
- Inactive keys (IsActive = false) rejected
- Rate limits enforced per-key
- LastUsedAt and UsageCount updated on each request

**Database Indexes:**
- Primary key on `Id`
- Unique index on `KeyHash` (for fast lookups)
- Index on `TenantId` (for tenant filtering)
- Index on `IsActive`
- Index on `CreatedAt`

**API Key Format:**
```
Example: ch_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
Prefix: ch_ (identifies as ChatAI key)
Length: 32 characters (128-bit entropy)
Character set: a-z, A-Z, 0-9 (alphanumeric)
```

---

#### 2.1.5 ChatSession Entity

**File:** `ChatAI.Domain/Entities/ChatSession.cs`

**Purpose:** Groups related chat messages into conversations. Tracks session metadata and enables conversation history retrieval.

**Properties:**

| Property | Type | Description | Purpose |
|----------|------|-------------|---------|
| `Id` | `Guid` | Primary key, also SessionId | Message grouping |
| `TenantId` | `Guid` | Tenant isolation | Security |
| `UserId` | `string` | End-user identifier | User tracking |
| `Title` | `string?` | Session description | Auto-generated or user-set |
| `IsActive` | `bool` | Session status | Default true |
| `StartedAt` | `DateTime` | First message timestamp | UTC |
| `LastMessageAt` | `DateTime?` | Most recent message | Auto-updated |
| `MessageCount` | `int` | Total messages in session | Auto-calculated |
| `TotalTokensUsed` | `int` | Cumulative token usage | Cost tracking |
| `Metadata` | `string?` | JSON custom data | Extensibility |

**Navigation Properties:**
```csharp
public virtual ICollection<ChatMessage> Messages { get; set; }
```

**Lifecycle:**
```
1. First message → Create new ChatSession
2. Subsequent messages → Update LastMessageAt, MessageCount
3. User closes chat → Set IsActive = false (soft delete)
4. Reopen chat → Set IsActive = true (resume)
```

**Business Rules:**
- SessionId is used in ChatMessage.SessionId for grouping
- UserId can be anonymous identifier (not authenticated user)
- Title auto-generated from first message if not provided
- MessageCount and TotalTokensUsed incremented automatically
- Cannot delete session with messages (use IsActive = false)

**Usage Example:**
```csharp
// Create new session
var session = new ChatSession
{
    Id = Guid.NewGuid(),
    TenantId = tenantId,
    UserId = "user-123",
    Title = "Customer Support Inquiry",
    StartedAt = DateTime.UtcNow,
    IsActive = true
};

// Retrieve conversation history
var history = await dbContext.ChatSessions
    .Include(s => s.Messages)
    .Where(s => s.UserId == userId && s.IsActive)
    .OrderByDescending(s => s.LastMessageAt)
    .ToListAsync();
```

---

#### 2.1.6 ChatMessage Entity

**File:** `ChatAI.Domain/Entities/ChatMessage.cs`

**Purpose:** Individual messages within a chat session. Stores user questions and AI responses with metadata for analytics and conversation reconstruction.

**Properties:**

| Property | Type | Description | Values |
|----------|------|-------------|--------|
| `Id` | `Guid` | Primary key | Auto-generated |
| `TenantId` | `Guid` | Tenant isolation | Required |
| `SessionId` | `string` | Links to ChatSession | Required |
| `UserId` | `string` | End-user identifier | Required |
| `Role` | `string` | Message sender | "user" or "assistant" |
| `Content` | `string` | Message text | Max 4000 chars |
| `Timestamp` | `DateTime` | When sent/received | UTC |
| `InputTokens` | `int?` | Tokens in prompt | OpenAI usage |
| `OutputTokens` | `int?` | Tokens in response | Cost calculation |
| `Model` | `string?` | AI model used | "gpt-4", "gpt-3.5-turbo" |
| `IsToolCall` | `bool` | Whether AI used tools | Default false |
| `ToolCallsJson` | `string?` | Serialized tool invocations | JSON format |
| `EmbeddingReference` | `string?` | Vector DB reference | For RAG |

**Role Values:**
- `"user"`: Message from end-user
- `"assistant"`: Response from AI
- `"system"`: System prompts (not stored in DB)

**Navigation Properties:**
```csharp
public virtual ChatSession? Session { get; set; }
```

**Message Flow:**
```
1. User sends message → Create ChatMessage (Role="user")
2. Retrieve conversation history (last N messages)
3. Build OpenAI prompt with history
4. AI generates response → Create ChatMessage (Role="assistant")
5. Update ChatSession.LastMessageAt and MessageCount
```

**Tool Calling Example:**
```json
// ToolCallsJson format when IsToolCall = true
{
  "toolCalls": [
    {
      "id": "call_abc123",
      "type": "function",
      "function": {
        "name": "send_email",
        "arguments": "{\"to\":\"user@example.com\",\"subject\":\"Test\"}"
      }
    }
  ]
}
```

**Business Rules:**
- Role must be "user" or "assistant"
- Content cannot be empty
- SessionId must reference existing ChatSession
- InputTokens and OutputTokens track OpenAI usage for billing
- IsToolCall true when AI invoked functions (email, knowledge search)
- Messages immutable after creation (no updates)

**Database Indexes:**
- Primary key on `Id`
- Index on `(TenantId, SessionId)` for fast history retrieval
- Index on `(UserId, Timestamp)` for user analytics
- Index on `Timestamp` for chronological ordering

**Query Examples:**
```csharp
// Get conversation history
var history = await dbContext.ChatMessages
    .Where(m => m.SessionId == sessionId)
    .OrderBy(m => m.Timestamp)
    .Select(m => new { m.Role, m.Content })
    .ToListAsync();

// Calculate token usage for billing
var usage = await dbContext.ChatMessages
    .Where(m => m.TenantId == tenantId && m.Timestamp >= billingStart)
    .SumAsync(m => (m.InputTokens ?? 0) + (m.OutputTokens ?? 0));
```

---

#### 2.1.7 KnowledgeDocument Entity

**File:** `ChatAI.Domain/Entities/KnowledgeDocument.cs`

**Purpose:** Stores documents for Retrieval-Augmented Generation (RAG). Enables the AI to answer questions based on tenant-specific knowledge base.

**Properties:**

| Property | Type | Description | Purpose |
|----------|------|-------------|---------|
| `Id` | `Guid` | Primary key | Unique identifier |
| `TenantId` | `Guid` | Tenant isolation | Security |
| `Title` | `string` | Document name | Display, search |
| `Content` | `string` | Full text content | RAG source material |
| `Category` | `string?` | Classification | Filtering |
| `Tags` | `string?` | Comma-separated keywords | Search, filtering |
| `Source` | `string?` | Origin URL or file path | Audit trail |
| `Embedding` | `float[]?` | Vector representation | Semantic search |
| `IsActive` | `bool` | Visibility status | Soft delete |
| `CreatedAt` | `DateTime` | Upload timestamp | Audit |
| `UpdatedAt` | `DateTime?` | Last modification | Audit |
| `CreatedBy` | `Guid` | AdminUser who uploaded | Audit |

**Navigation Properties:**
```csharp
public virtual Tenant Tenant { get; set; }
```

**RAG Workflow:**
```
1. Admin uploads document → Create KnowledgeDocument
2. Extract text → Store in Content field
3. Generate embedding → Azure OpenAI embeddings API
4. Store vector → Embedding field (or Qdrant collection)
5. User asks question → Generate query embedding
6. Vector search → Find similar documents
7. Inject into prompt → AI generates answer with context
```

**Embedding Details:**
- **Model:** text-embedding-ada-002 (OpenAI)
- **Dimensions:** 1536 floats
- **Storage:** Database column (small docs) or Qdrant (large collections)
- **Similarity Metric:** Cosine similarity

**Business Rules:**
- Content max 100,000 characters (chunked if larger)
- Embedding regenerated on Content update
- IsActive = false excludes from RAG search
- Tenant-scoped (admins only see their documents)
- Supports markdown formatting in Content

**Chunking Strategy:**
```csharp
// For documents > 1000 characters
ChunkSize: 1000 characters
ChunkOverlap: 200 characters
Result: Multiple embeddings per document
Example: 5000-char doc → 5 chunks → 5 embeddings
```

**Database Indexes:**
- Primary key on `Id`
- Index on `(TenantId, IsActive)` for filtering
- Index on `CreatedAt` for sorting
- Full-text index on `Content` for keyword search

**Usage Example:**
```csharp
// Upload and embed document
var doc = new KnowledgeDocument
{
    TenantId = tenantId,
    Title = "Product FAQ",
    Content = "Q: What is the return policy? A: 30 days...",
    Category = "Support",
    Tags = "returns,policy,support",
    CreatedBy = adminUserId
};

// Generate embedding
var embedding = await embeddingClient.GenerateEmbeddingAsync(doc.Content);
doc.Embedding = embedding.ToArray();

await repository.CreateAsync(doc);
```

---

#### 2.1.8 Feedback Entity

**File:** `ChatAI.Domain/Entities/Feedback.cs`

**Purpose:** Collects user feedback on AI responses for quality monitoring and improvement. Tracks thumbs up/down with optional comments.

**Properties:**

| Property | Type | Description | Values |
|----------|------|-------------|--------|
| `Id` | `Guid` | Primary key | Auto-generated |
| `TenantId` | `Guid` | Tenant isolation | Required |
| `SessionId` | `string` | Chat session reference | Links to conversation |
| `MessageId` | `Guid` | Specific message rated | ChatMessage.Id |
| `UserId` | `string` | End-user identifier | Required |
| `Rating` | `int` | Satisfaction score | 1 (bad) to 5 (excellent) |
| `IsPositive` | `bool` | Binary feedback | true = helpful, false = not helpful |
| `Comment` | `string?` | Optional text feedback | Max 1000 chars |
| `Timestamp` | `DateTime` | When submitted | UTC |
| `Category` | `string?` | Issue classification | "Inaccurate", "Helpful", "Off-topic" |

**Navigation Properties:**
```csharp
public virtual ChatMessage? Message { get; set; }
```

**Feedback Flow:**
```
1. User receives AI response
2. UI shows thumbs up/down buttons
3. User clicks → POST /api/feedback/submit
4. Create Feedback entity
5. Analytics dashboard shows trends
```

**Rating Scale:**
- **1**: Very unhelpful
- **2**: Not helpful
- **3**: Neutral
- **4**: Helpful
- **5**: Very helpful

**Business Rules:**
- One feedback per message per user (unique constraint)
- MessageId must reference existing ChatMessage
- Rating must be between 1 and 5
- IsPositive derived from Rating (>= 4 = positive)
- Comment optional but encouraged for negative feedback

**Analytics Queries:**
```csharp
// Calculate positive feedback percentage
var stats = await dbContext.Feedbacks
    .Where(f => f.TenantId == tenantId && f.Timestamp >= startDate)
    .GroupBy(f => f.IsPositive)
    .Select(g => new { IsPositive = g.Key, Count = g.Count() })
    .ToListAsync();

// Find problematic message patterns
var negativeComments = await dbContext.Feedbacks
    .Where(f => f.TenantId == tenantId && !f.IsPositive && f.Comment != null)
    .OrderByDescending(f => f.Timestamp)
    .Take(50)
    .ToListAsync();
```

**Database Indexes:**
- Primary key on `Id`
- Unique index on `(MessageId, UserId)` (one feedback per message per user)
- Index on `(TenantId, Timestamp)` for analytics
- Index on `IsPositive` for filtering

---

#### 2.1.9 AdminConfiguration Entity

**File:** `ChatAI.Domain/Entities/AdminConfiguration.cs`

**Purpose:** Platform-wide configuration settings. Global parameters that apply to all tenants (unlike TenantSettings which are per-tenant).

**Properties:**

| Property | Type | Description | Examples |
|----------|------|-------------|----------|
| `Id` | `Guid` | Primary key | Auto-generated |
| `Key` | `string` | Configuration key | "SystemPrompt", "DefaultModel" |
| `Value` | `string` | Configuration value | Stored as string |
| `Description` | `string?` | Human-readable explanation | "AI instructions for all chats" |
| `Category` | `string?` | Grouping | "AI", "Security", "Features" |
| `DataType` | `string` | Value type hint | "string", "int", "bool", "json" |
| `IsActive` | `bool` | Enable/disable | Default true |
| `CreatedAt` | `DateTime` | Creation timestamp | UTC |
| `UpdatedAt` | `DateTime?` | Last modification | UTC |
| `UpdatedBy` | `Guid?` | AdminUser who last changed | Audit trail |

**Common Configuration Keys:**

| Key | Default Value | Purpose |
|-----|---------------|---------|
| `SystemPrompt` | "You are a helpful assistant..." | Default AI instructions |
| `DefaultModel` | "gpt-4" | OpenAI model to use |
| `MaxHistoryMessages` | "10" | Global conversation history limit |
| `DefaultTemperature` | "0.7" | Default AI creativity |
| `EnableGlobalKnowledge` | "false" | Share knowledge across tenants |
| `MaintenanceMode` | "false" | Disable API during updates |
| `ApiVersion` | "1.0.0" | Current API version |

**Business Rules:**
- Key must be unique (enforced by unique index)
- Value stored as string (parse based on DataType)
- IsActive = false effectively disables the setting
- TenantSettings override AdminConfiguration for tenant-specific values
- PlatformAdmin only can modify

**Type Parsing:**
```csharp
// Helper methods for type-safe access
public static class ConfigurationHelper
{
    public static string GetString(string key) => 
        dbContext.AdminConfigurations.FirstOrDefault(c => c.Key == key)?.Value ?? "";
    
    public static int GetInt(string key, int defaultValue = 0) => 
        int.TryParse(GetString(key), out var val) ? val : defaultValue;
    
    public static bool GetBool(string key, bool defaultValue = false) => 
        bool.TryParse(GetString(key), out var val) ? val : defaultValue;
}
```

**Usage Example:**
```csharp
// Get system prompt
var systemPrompt = await configService.GetValueAsync("SystemPrompt");

// Update configuration
var config = await repository.GetByKeyAsync("DefaultModel");
config.Value = "gpt-4-turbo";
config.UpdatedAt = DateTime.UtcNow;
config.UpdatedBy = adminUserId;
await repository.UpdateAsync(config);
```

**Database Indexes:**
- Primary key on `Id`
- Unique index on `Key`
- Index on `(Category, IsActive)` for filtering
- Index on `UpdatedAt` for audit trail

---

### 2.2 Domain Enums

#### 2.2.1 AdminRole Enum

**File:** `ChatAI.Domain/Enums/AdminRole.cs`

```csharp
public enum AdminRole
{
    TenantAdmin = 0,      // Manages their own tenant only
    PlatformAdmin = 1     // Full platform access (Dott staff)
}
```

**Usage:**
- **TenantAdmin**: Customer administrators who manage their own tenant's settings, knowledge, and API keys
- **PlatformAdmin**: Platform operators who can create tenants, view all data, and perform maintenance

**Authorization Rules:**
```csharp
// Controller authorization
[TenantAdmin]   // TenantAdmin OR PlatformAdmin can access
[PlatformAdmin] // Only PlatformAdmin can access
```

---

#### 2.2.2 PlanTier Enum

**File:** `ChatAI.Domain/Enums/PlanTier.cs`

```csharp
public enum PlanTier
{
    Free = 0,         // Limited features, max 10 docs, 1000 msgs/month
    Basic = 1,        // Standard features, 50 docs, 10000 msgs/month
    Pro = 2,          // Advanced features, 200 docs, 50000 msgs/month
    Enterprise = 3    // Custom limits, dedicated support
}
```

**Feature Matrix:**

| Feature | Free | Basic | Pro | Enterprise |
|---------|------|-------|-----|------------|
| Max Documents | 10 | 50 | 200 | Unlimited |
| Monthly Messages | 1,000 | 10,000 | 50,000 | Unlimited |
| Custom Domain | ❌ | ✅ | ✅ | ✅ |
| API Keys | 1 | 3 | 10 | Unlimited |
| Email Support | ❌ | ❌ | ✅ | ✅ |
| Advanced Analytics | ❌ | ❌ | ✅ | ✅ |
| Priority Support | ❌ | ❌ | ❌ | ✅ |

---

### 2.3 Domain Interfaces

#### 2.3.1 Repository Interfaces

**Purpose:** Define contracts for data access. Infrastructure layer implements these.

**IChatSessionRepository**
```csharp
public interface IChatSessionRepository
{
    Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ChatSession>> GetByUserIdAsync(string userId, bool onlyActive, CancellationToken ct = default);
    Task<ChatSession> CreateAsync(ChatSession session, CancellationToken ct = default);
    Task UpdateAsync(ChatSession session, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
```

**IApiKeyRepository**
```csharp
public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken ct = default);
    Task<List<ApiKey>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task<ApiKey> CreateAsync(ApiKey apiKey, CancellationToken ct = default);
    Task UpdateAsync(ApiKey apiKey, CancellationToken ct = default);
}
```

**IKnowledgeRepository**
```csharp
public interface IKnowledgeRepository
{
    Task<KnowledgeDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<KnowledgeDocument>> GetAllByTenantAsync(Guid tenantId, bool onlyActive, CancellationToken ct = default);
    Task<KnowledgeDocument> CreateAsync(KnowledgeDocument doc, CancellationToken ct = default);
    Task UpdateAsync(KnowledgeDocument doc, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<List<KnowledgeDocument>> SearchByVectorAsync(float[] queryEmbedding, int topK, CancellationToken ct = default);
}
```

**ITenantRepository**
```csharp
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Tenant?> GetByCustomDomainAsync(string domain, CancellationToken ct = default);
    Task<List<Tenant>> GetAllAsync(bool onlyActive, CancellationToken ct = default);
    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken ct = default);
    Task UpdateAsync(Tenant tenant, CancellationToken ct = default);
}
```

---

#### 2.3.2 Service Interfaces

**IChatService**
```csharp
public interface IChatService
{
    // Synchronous chat (complete response)
    Task<ChatResponseModel> SendMessageAsync(
        string userId, 
        string message, 
        string? sessionId = null, 
        bool useTools = true, 
        CancellationToken ct = default);
}
```

**IChatStreamService**
```csharp
public interface IChatStreamService
{
    // Asynchronous streaming (token-by-token)
    IAsyncEnumerable<string> StreamMessageAsync(
        string userId, 
        string message, 
        string? sessionId = null, 
        bool useTools = true, 
        CancellationToken ct = default);
}
```

**IApiKeyService**
```csharp
public interface IApiKeyService
{
    // Generate new API key and hash
    (string PlainKey, string KeyHash) GenerateApiKey();
    
    // Hash an API key for comparison
    string HashApiKey(string plainKey);
}
```

**IEmailService**
```csharp
public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
}
```

**ITenantContext**
```csharp
public interface ITenantContext
{
    Guid? TenantId { get; }
    Guid RequiredTenantId { get; }
    string? TenantSlug { get; }
    bool HasTenant { get; }
    void SetTenant(Guid tenantId, string tenantSlug);
    void Clear();
}
```

---

## 3. APPLICATION LAYER DOCUMENTATION

**Location:** `ChatAI.Application/`  
**Purpose:** Business logic, CQRS operations, validation, and application services  
**Dependencies:** ChatAI.Domain (no Infrastructure or API dependencies)

### 3.1 CQRS Pattern Overview

**Command Query Responsibility Segregation (CQRS):**
- **Commands**: Write operations that change state (Create, Update, Delete)
- **Queries**: Read operations that return data (Get, List, Search)
- **Handlers**: Process commands/queries via MediatR
- **Validators**: FluentValidation ensures data integrity

**Structure:**
```
ChatAI.Application/
└── Features/
    ├── Auth/
    │   ├── CreateApiKey/
    │   │   ├── CreateApiKeyCommand.cs          (Request)
    │   │   ├── CreateApiKeyCommandHandler.cs   (Logic)
    │   │   ├── CreateApiKeyValidator.cs        (Validation)
    │   │   └── ApiKeyResult.cs                 (Response)
    │   └── ValidateApiKey/
    │       ├── ValidateApiKeyQuery.cs
    │       └── ValidateApiKeyQueryHandler.cs
    └── Chat/
        ├── SendMessage/
        │   ├── SendChatCommand.cs
        │   └── SendChatCommandHandler.cs
        └── GetConversationHistory/
            ├── GetConversationHistoryQuery.cs
            └── GetConversationHistoryQueryHandler.cs
```

---

### 3.2 Authentication Features

#### 3.2.1 CreateApiKey Command

**File:** `ChatAI.Application/Features/Auth/CreateApiKey/CreateApiKeyCommand.cs`

**Purpose:** Create a new API key for external application authentication.

**Command Properties:**
```csharp
public class CreateApiKeyCommand : IRequest<ApiKeyResult>
{
    public string ClientName { get; set; } = string.Empty;      // Display name
    public string? Description { get; set; }                    // Optional notes
    public int RateLimitPerMinute { get; set; } = 20;          // Request throttling
    public int RateLimitPerDay { get; set; } = 1000;           // Daily quota
    public DateTime? ExpiresAt { get; set; }                   // Optional expiration
    public Guid CreatedBy { get; set; }                        // Admin who creates it
    public Guid TenantId { get; set; }                         // Tenant association
}
```

**Handler Logic:**
```csharp
public class CreateApiKeyCommandHandler : IRequestHandler<CreateApiKeyCommand, ApiKeyResult>
{
    public async Task<ApiKeyResult> Handle(CreateApiKeyCommand request, CancellationToken ct)
    {
        // 1. Generate API key (32-char alphanumeric)
        var (plainKey, keyHash) = _apiKeyService.GenerateApiKey();
        
        // 2. Create entity
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = keyHash,                            // Store hash only
            TenantId = request.TenantId.ToString(),       // Tenant scoping
            ClientName = request.ClientName,
            Description = request.Description,
            RateLimitPerMinute = request.RateLimitPerMinute,
            RateLimitPerDay = request.RateLimitPerDay,
            ExpiresAt = request.ExpiresAt,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        // 3. Save to database
        await _apiKeyRepository.CreateAsync(apiKey, ct);
        
        // 4. Return result with plain key (ONLY TIME it's visible)
        return new ApiKeyResult
        {
            Id = apiKey.Id,
            ClientName = apiKey.ClientName,
            TenantId = apiKey.TenantId,
            ApiKey = plainKey,  // ⚠️ Plain key returned once
            // ... other properties
        };
    }
}
```

**Validation Rules:**
```csharp
public class CreateApiKeyValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyValidator()
    {
        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("Client name is required")
            .MaximumLength(200);
        
        RuleFor(x => x.Description)
            .MaximumLength(500);
        
        RuleFor(x => x.RateLimitPerMinute)
            .GreaterThan(0).WithMessage("Rate limit must be positive")
            .LessThanOrEqualTo(100).WithMessage("Max 100 requests per minute");
        
        RuleFor(x => x.RateLimitPerDay)
            .GreaterThan(0)
            .LessThanOrEqualTo(100000).WithMessage("Max 100k requests per day");
        
        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow).When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration must be in the future");
        
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required");
    }
}
```

**API Usage:**
```http
POST /api/auth/api-keys
Authorization: Bearer <admin-jwt>
Content-Type: application/json

{
  "clientName": "Mobile App",
  "description": "iOS application integration",
  "rateLimitPerMinute": 20,
  "rateLimitPerDay": 5000,
  "expiresAt": "2026-12-31T23:59:59Z"
}

Response:
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "clientName": "Mobile App",
  "tenantId": "123e4567-e89b-12d3-a456-426614174000",
  "apiKey": "ch_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",  // ⚠️ Save this!
  "createdAt": "2025-12-27T18:30:00Z"
}
```

---

#### 3.2.2 ValidateApiKey Query

**File:** `ChatAI.Application/Features/Auth/ValidateApiKey/ValidateApiKeyQuery.cs`

**Purpose:** Validate an API key during authentication. Called by ApiKeyAuthenticationHandler.

**Query:**
```csharp
public class ValidateApiKeyQuery : IRequest<ApiKey?>
{
    public string ApiKey { get; set; } = string.Empty;  // Plain key from X-API-Key header
}
```

**Handler Logic:**
```csharp
public class ValidateApiKeyQueryHandler : IRequestHandler<ValidateApiKeyQuery, ApiKey?>
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ValidateApiKeyQueryHandler> _logger;

    public async Task<ApiKey?> Handle(ValidateApiKeyQuery request, CancellationToken ct)
    {
        // 1. Hash the provided key
        var keyHash = _apiKeyService.HashApiKey(request.ApiKey);
        
        // 2. Look up by hash
        var apiKey = await _apiKeyRepository.GetByKeyHashAsync(keyHash, ct);
        
        if (apiKey == null)
        {
            _logger.LogWarning("API key validation failed: Key not found");
            return null;
        }
        
        // 3. Check if active
        if (!apiKey.IsActive)
        {
            _logger.LogWarning("API key validation failed: Key inactive - Tenant: {TenantId}", apiKey.TenantId);
            return null;
        }
        
        // 4. Check expiration
        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("API key validation failed: Key expired - Tenant: {TenantId}", apiKey.TenantId);
            return null;
        }
        
        // 5. Update usage statistics
        apiKey.LastUsedAt = DateTime.UtcNow;
        apiKey.UsageCount++;
        await _apiKeyRepository.UpdateAsync(apiKey, ct);
        
        _logger.LogInformation("API key validated successfully for Tenant: {TenantId}", apiKey.TenantId);
        
        return apiKey;  // Success - returns full entity
    }
}
```

**Authentication Flow:**
```
Request → ApiKeyAuthenticationHandler
    ↓
    Extract X-API-Key header
    ↓
    Send ValidateApiKeyQuery
    ↓
    ValidateApiKeyQueryHandler
    ↓
    Hash key → Lookup → Validate
    ↓
    Return ApiKey entity or null
    ↓
    Create claims (tenant_id, role)
    ↓
    TenantResolutionMiddleware reads tenant_id
    ↓
    Request proceeds with tenant context
```

---

### 3.3 Chat Features

#### 3.3.1 SendMessage Command

**File:** `ChatAI.Application/Features/Chat/SendMessage/SendChatCommand.cs`

**Purpose:** Process user message and generate AI response. Handles conversation history, RAG, and tool calling.

**Command Properties:**
```csharp
public class SendChatCommand : IRequest<ChatResponseModel>
{
    public string? UserId { get; set; }            // End-user identifier (nullable for anonymous)
    public string Message { get; set; } = string.Empty;   // User's question/message
    public string? SessionId { get; set; }         // Existing session or null for new
    public bool UseTools { get; set; } = true;     // Enable tool calling (email, knowledge)
}
```

**Handler Logic (Simplified):**
```csharp
public class SendChatCommandHandler : IRequestHandler<SendChatCommand, ChatResponseModel>
{
    private readonly IChatService _chatService;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly ITenantContext _tenantContext;

    public async Task<ChatResponseModel> Handle(SendChatCommand request, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequiredTenantId;
        var userId = request.UserId ?? "anonymous";
        
        // 1. Get or create session
        var sessionId = request.SessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            var session = new ChatSession
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _sessionRepository.CreateAsync(session, ct);
            sessionId = session.Id.ToString();
        }
        
        // 2. Store user message
        var userMessage = new ChatMessage
        {
            TenantId = tenantId,
            SessionId = sessionId,
            UserId = userId,
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        };
        await _messageRepository.CreateAsync(userMessage, ct);
        
        // 3. Get conversation history
        var history = await _messageRepository.GetBySessionIdAsync(sessionId, limit: 10, ct);
        
        // 4. Build AI prompt with history
        var messages = history.Select(m => new ChatMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();
        
        // 5. Retrieve relevant knowledge (RAG)
        if (request.UseTools && _tenantSettings.EnableKnowledge)
        {
            var knowledgeContext = await _knowledgeService.SearchAsync(request.Message, ct);
            if (knowledgeContext.Any())
            {
                messages.Insert(0, new ChatMessage
                {
                    Role = "system",
                    Content = $"Relevant knowledge:\n{string.Join("\n", knowledgeContext)}"
                });
            }
        }
        
        // 6. Call Azure OpenAI
        var response = await _chatService.SendMessageAsync(
            userId, 
            request.Message, 
            sessionId, 
            request.UseTools, 
            ct);
        
        // 7. Store AI response
        var assistantMessage = new ChatMessage
        {
            TenantId = tenantId,
            SessionId = sessionId,
            UserId = userId,
            Role = "assistant",
            Content = response.Message,
            Timestamp = DateTime.UtcNow,
            InputTokens = response.TokensUsed?.InputTokens,
            OutputTokens = response.TokensUsed?.OutputTokens,
            Model = response.Model
        };
        await _messageRepository.CreateAsync(assistantMessage, ct);
        
        // 8. Update session
        await _sessionRepository.UpdateLastMessageAsync(sessionId, ct);
        
        return response;
    }
}
```

**Response Model:**
```csharp
public class ChatResponseModel
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Model { get; set; }
    public TokenUsage? TokensUsed { get; set; }
    public List<ToolCallInfo>? ToolCalls { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TokenUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
}

public class ToolCallInfo
{
    public string ToolName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}
```

**API Usage:**
```http
POST /api/chat/send
X-API-Key: ch_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
Content-Type: application/json

{
  "userId": "user-123",
  "message": "What is your return policy?",
  "sessionId": null,  // Creates new session
  "useTools": true
}

Response:
{
  "sessionId": "abc123-session-uuid",
  "message": "Our return policy allows returns within 30 days...",
  "model": "gpt-4",
  "tokensUsed": {
    "inputTokens": 150,
    "outputTokens": 75,
    "totalTokens": 225
  },
  "timestamp": "2025-12-27T18:45:00Z"
}
```

**Validation:**
```csharp
public class SendChatCommandValidator : AbstractValidator<SendChatCommand>
{
    public SendChatCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message cannot be empty")
            .MaximumLength(4000).WithMessage("Message too long");
        
        RuleFor(x => x.SessionId)
            .Must(BeValidGuid).When(x => !string.IsNullOrEmpty(x.SessionId))
            .WithMessage("Invalid session ID format");
    }
    
    private bool BeValidGuid(string? sessionId)
    {
        return Guid.TryParse(sessionId, out _);
    }
}
```

---

#### 3.3.2 GetConversationHistory Query

**File:** `ChatAI.Application/Features/Chat/GetConversationHistory/GetConversationHistoryQuery.cs`

**Purpose:** Retrieve message history for a chat session.

**Query:**
```csharp
public class GetConversationHistoryQuery : IRequest<List<ChatMessageDto>>
{
    public string SessionId { get; set; } = string.Empty;
    public int MaxMessages { get; set; } = 20;  // Limit results
}
```

**Handler:**
```csharp
public class GetConversationHistoryQueryHandler : IRequestHandler<GetConversationHistoryQuery, List<ChatMessageDto>>
{
    private readonly IChatMessageRepository _messageRepository;
    private readonly ITenantContext _tenantContext;

    public async Task<List<ChatMessageDto>> Handle(GetConversationHistoryQuery request, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequiredTenantId;
        
        // Retrieve messages for session
        var messages = await _messageRepository.GetBySessionIdAsync(
            request.SessionId, 
            request.MaxMessages, 
            ct);
        
        // Map to DTOs
        return messages.Select(m => new ChatMessageDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            Timestamp = m.Timestamp,
            IsToolCall = m.IsToolCall
        }).ToList();
    }
}
```

**API Usage:**
```http
GET /api/chat/sessions/abc123-session-uuid/messages?maxMessages=20
X-API-Key: ch_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6

Response:
[
  {
    "id": "msg-001",
    "role": "user",
    "content": "What is your return policy?",
    "timestamp": "2025-12-27T18:30:00Z",
    "isToolCall": false
  },
  {
    "id": "msg-002",
    "role": "assistant",
    "content": "Our return policy allows...",
    "timestamp": "2025-12-27T18:30:05Z",
    "isToolCall": false
  }
]
```

---

### 3.4 Session Features

#### 3.4.1 GetUserSessions Query

**File:** `ChatAI.Application/Features/Session/GetUserSessions/GetUserSessionsQuery.cs`

**Purpose:** List all chat sessions for a specific user.

**Query:**
```csharp
public class GetUserSessionsQuery : IRequest<List<SessionDto>>
{
    public string UserId { get; set; } = string.Empty;
    public bool OnlyActive { get; set; } = true;
}
```

**Handler:**
```csharp
public class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, List<SessionDto>>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly ITenantContext _tenantContext;

    public async Task<List<SessionDto>> Handle(GetUserSessionsQuery request, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequiredTenantId;
        
        var sessions = await _sessionRepository.GetByUserIdAsync(
            request.UserId, 
            request.OnlyActive, 
            ct);
        
        return sessions.Select(s => new SessionDto
        {
            SessionId = s.Id.ToString(),
            Title = s.Title,
            StartedAt = s.StartedAt,
            LastMessageAt = s.LastMessageAt,
            MessageCount = s.MessageCount,
            IsActive = s.IsActive
        }).ToList();
    }
}
```

**API Usage:**
```http
GET /api/chat/sessions?userId=user-123&onlyActive=true
X-API-Key: ch_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6

Response:
[
  {
    "sessionId": "session-001",
    "title": "Support Inquiry",
    "startedAt": "2025-12-27T10:00:00Z",
    "lastMessageAt": "2025-12-27T10:15:00Z",
    "messageCount": 8,
    "isActive": true
  },
  {
    "sessionId": "session-002",
    "title": "Product Question",
    "startedAt": "2025-12-26T14:30:00Z",
    "lastMessageAt": "2025-12-26T14:45:00Z",
    "messageCount": 5,
    "isActive": true
  }
]
```

---

### 3.5 Knowledge Features

#### 3.5.1 UploadKnowledge Command

**File:** `ChatAI.Application/Features/Knowledge/UploadKnowledge/UploadKnowledgeCommand.cs`

**Purpose:** Upload a document to the knowledge base and generate embeddings for RAG.

**Command:**
```csharp
public class UploadKnowledgeCommand : IRequest<KnowledgeDocumentDto>
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public string? Source { get; set; }
    public Guid UploadedBy { get; set; }
}
```

**Handler:**
```csharp
public class UploadKnowledgeCommandHandler : IRequestHandler<UploadKnowledgeCommand, KnowledgeDocumentDto>
{
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly ITenantContext _tenantContext;

    public async Task<KnowledgeDocumentDto> Handle(UploadKnowledgeCommand request, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequiredTenantId;
        
        // 1. Check tenant document limit
        var currentCount = await _knowledgeRepository.GetCountByTenantAsync(tenantId, ct);
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct);
        
        if (currentCount >= tenant.MaxDocuments)
        {
            throw new BusinessException($"Document limit reached ({tenant.MaxDocuments})");
        }
        
        // 2. Generate embedding
        var embedding = await _embeddingClient.GenerateEmbeddingAsync(request.Content, ct);
        
        // 3. Create document
        var document = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = request.Title,
            Content = request.Content,
            Category = request.Category,
            Tags = request.Tags,
            Source = request.Source,
            Embedding = embedding.ToArray(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.UploadedBy,
            IsActive = true
        };
        
        // 4. Save to database (and optionally Qdrant)
        await _knowledgeRepository.CreateAsync(document, ct);
        
        // 5. Update tenant document count
        tenant.CurrentDocumentCount++;
        await _tenantRepository.UpdateAsync(tenant, ct);
        
        return new KnowledgeDocumentDto
        {
            Id = document.Id,
            Title = document.Title,
            Category = document.Category,
            CreatedAt = document.CreatedAt
        };
    }
}
```

**Validation:**
```csharp
public class UploadKnowledgeValidator : AbstractValidator<UploadKnowledgeCommand>
{
    public UploadKnowledgeValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
        
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(100000).WithMessage("Content too large (max 100KB)");
        
        RuleFor(x => x.Category)
            .MaximumLength(50);
        
        RuleFor(x => x.Tags)
            .MaximumLength(200);
    }
}
```

**API Usage:**
```http
POST /api/knowledge/upload
Authorization: Bearer <admin-jwt>
Content-Type: application/json

{
  "title": "Product Return Policy",
  "content": "Our return policy allows customers to return items within 30 days...",
  "category": "Policies",
  "tags": "returns,refunds,policy",
  "source": "https://company.com/policies"
}

Response:
{
  "id": "doc-001",
  "title": "Product Return Policy",
  "category": "Policies",
  "createdAt": "2025-12-27T18:50:00Z"
}
```

---

#### 3.5.2 SearchKnowledge Query

**File:** `ChatAI.Application/Features/Knowledge/SearchKnowledge/SearchKnowledgeQuery.cs`

**Purpose:** Semantic search in knowledge base using vector similarity.

**Query:**
```csharp
public class SearchKnowledgeQuery : IRequest<List<KnowledgeSearchResult>>
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 3;  // Number of results
}
```

**Handler:**
```csharp
public class SearchKnowledgeQueryHandler : IRequestHandler<SearchKnowledgeQuery, List<KnowledgeSearchResult>>
{
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly ITenantContext _tenantContext;

    public async Task<List<KnowledgeSearchResult>> Handle(SearchKnowledgeQuery request, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequiredTenantId;
        
        // 1. Generate query embedding
        var queryEmbedding = await _embeddingClient.GenerateEmbeddingAsync(request.Query, ct);
        
        // 2. Vector similarity search
        var results = await _knowledgeRepository.SearchByVectorAsync(
            queryEmbedding.ToArray(), 
            request.TopK, 
            ct);
        
        // 3. Map to result DTOs with similarity scores
        return results.Select(doc => new KnowledgeSearchResult
        {
            DocumentId = doc.Id,
            Title = doc.Title,
            Content = doc.Content.Substring(0, Math.Min(500, doc.Content.Length)),  // Preview
            Category = doc.Category,
            SimilarityScore = CalculateCosineSimilarity(queryEmbedding, doc.Embedding)
        })
        .OrderByDescending(r => r.SimilarityScore)
        .ToList();
    }
    
    private float CalculateCosineSimilarity(float[] vec1, float[] vec2)
    {
        var dotProduct = vec1.Zip(vec2, (a, b) => a * b).Sum();
        var magnitude1 = Math.Sqrt(vec1.Sum(x => x * x));
        var magnitude2 = Math.Sqrt(vec2.Sum(x => x * x));
        return (float)(dotProduct / (magnitude1 * magnitude2));
    }
}
```

**API Usage:**
```http
POST /api/knowledge/search
Authorization: Bearer <admin-jwt>
Content-Type: application/json

{
  "query": "What is the return policy?",
  "topK": 3
}

Response:
[
  {
    "documentId": "doc-001",
    "title": "Product Return Policy",
    "content": "Our return policy allows customers to return items within 30 days...",
    "category": "Policies",
    "similarityScore": 0.92
  },
  {
    "documentId": "doc-015",
    "title": "Refund Processing",
    "content": "Refunds are processed within 5-7 business days...",
    "category": "Support",
    "similarityScore": 0.87
  }
]
```

---

### 3.6 Tenant Features

#### 3.6.1 CreateTenant Command

**File:** `ChatAI.Application/Features/Tenants/CreateTenant/CreateTenantCommand.cs`

**Purpose:** Create a new tenant with initial admin user and default settings.

**Command:**
```csharp
public class CreateTenantCommand : IRequest<TenantResponse>
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PlanTier { get; set; } = "Free";
    public string? CustomDomain { get; set; }
    
    // Initial admin user
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}
```

**Handler:**
```csharp
public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IAuthService _authService;

    public async Task<TenantResponse> Handle(CreateTenantCommand request, CancellationToken ct)
    {
        // 1. Validate slug uniqueness
        var existing = await _tenantRepository.GetBySlugAsync(request.Slug, ct);
        if (existing != null)
        {
            throw new BusinessException($"Tenant slug '{request.Slug}' already exists");
        }
        
        // 2. Create tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug.ToLower(),
            Name = request.Name,
            Email = request.Email,
            PlanTier = request.PlanTier,
            CustomDomain = request.CustomDomain,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MaxDocuments = GetDocumentLimit(request.PlanTier),
            MaxMonthlyMessages = GetMessageLimit(request.PlanTier),
            BillingPeriodStart = DateTime.UtcNow
        };
        
        await _tenantRepository.CreateAsync(tenant, ct);
        
        // 3. Create default settings
        var settings = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            WelcomeMessage = "Hi! How can I help you today?",
            EnableKnowledge = true,
            EnableToolCalling = true,
            Temperature = 0.7f,
            MaxTokens = 800
        };
        
        await _settingsRepository.CreateAsync(settings, ct);
        
        // 4. Create initial admin user
        var passwordHash = _authService.HashPassword(request.AdminPassword);
        
        var adminUser = new AdminUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Username = request.AdminUsername,
            PasswordHash = passwordHash,
            FullName = request.AdminFullName,
            Email = request.AdminEmail,
            Role = AdminRole.TenantAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        await _adminUserRepository.CreateAsync(adminUser, ct);
        
        return new TenantResponse
        {
            Id = tenant.Id,
            Slug = tenant.Slug,
            Name = tenant.Name,
            PlanTier = tenant.PlanTier,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt
        };
    }
    
    private int GetDocumentLimit(string planTier) => planTier switch
    {
        "Free" => 10,
        "Basic" => 50,
        "Pro" => 200,
        "Enterprise" => int.MaxValue,
        _ => 10
    };
    
    private int GetMessageLimit(string planTier) => planTier switch
    {
        "Free" => 1000,
        "Basic" => 10000,
        "Pro" => 50000,
        "Enterprise" => int.MaxValue,
        _ => 1000
    };
}
```

---

## 4. Infrastructure Layer

**Namespace:** `ChatAI.Infrastructure`

The Infrastructure layer contains concrete implementations of repository interfaces, AI service integrations, database configuration, and external service adapters.

### 4.1 Database Context

**File:** `ChatAI.Infrastructure/Data/ChatDbContext.cs`

**Purpose:** Entity Framework Core database context for SQL Server. Configures all entity mappings, relationships, and indexes.

**Configuration:**
```csharp
public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantSettings> TenantSettings { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<AdminConfiguration> AdminConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant Configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PlanTier).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomDomain).HasMaxLength(255);
            
            // Unique constraint on slug
            entity.HasIndex(e => e.Slug).IsUnique();
            
            // Index for active tenants
            entity.HasIndex(e => e.IsActive);
        });

        // TenantSettings Configuration
        modelBuilder.Entity<TenantSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WelcomeMessage).HasMaxLength(500);
            entity.Property(e => e.Temperature).HasPrecision(3, 2);
            
            // One-to-one with Tenant
            entity.HasOne<Tenant>()
                .WithOne()
                .HasForeignKey<TenantSettings>(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.TenantId).IsUnique();
        });

        // AdminUser Configuration
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50)
                .HasConversion<string>();  // Enum to string
            
            // Unique constraint
            entity.HasIndex(e => new { e.TenantId, e.Username }).IsUnique();
            
            // Foreign key to Tenant
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiKey Configuration
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            // Unique constraint on key hash
            entity.HasIndex(e => e.KeyHash).IsUnique();
            
            // Index for tenant lookups
            entity.HasIndex(e => e.TenantId);
            
            // Index for active keys
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
        });

        // ChatSession Configuration
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);
            
            // Indexes
            entity.HasIndex(e => new { e.TenantId, e.UserId });
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasIndex(e => e.StartedAt);
            
            // Foreign key to Tenant
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage Configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(50);
            
            // Indexes for efficient queries
            entity.HasIndex(e => new { e.TenantId, e.SessionId });
            entity.HasIndex(e => e.Timestamp);
            
            // Foreign key to ChatSession
            entity.HasOne<ChatSession>()
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .HasPrincipalKey(s => s.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // KnowledgeDocument Configuration
        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Tags).HasMaxLength(200);
            entity.Property(e => e.Source).HasMaxLength(500);
            
            // Embedding stored as binary
            entity.Property(e => e.Embedding).HasColumnType("varbinary(max)");
            
            // Indexes
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.CreatedAt);
            
            // Foreign key to Tenant
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Feedback Configuration
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MessageId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Rating).IsRequired();
            entity.Property(e => e.Comment).HasMaxLength(1000);
            
            // Indexes
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
            entity.HasIndex(e => e.Rating);
            
            // Foreign key to Tenant
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AdminConfiguration Configuration
        modelBuilder.Entity<AdminConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfigKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ConfigValue).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            
            // Unique constraint on key
            entity.HasIndex(e => e.ConfigKey).IsUnique();
        });
    }
}
```

**Connection String (appsettings.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ChatAI;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

---

### 4.2 Repositories

Repositories implement the repository pattern, providing data access abstraction over Entity Framework Core.

#### 4.2.1 TenantRepository

**File:** `ChatAI.Infrastructure/Repositories/TenantRepository.cs`

**Interface:** `ChatAI.Domain/Interfaces/ITenantRepository.cs`

**Implementation:**
```csharp
public class TenantRepository : ITenantRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<TenantRepository> _logger;

    public TenantRepository(ChatDbContext context, ILogger<TenantRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLower(), ct);
    }

    public async Task<List<Tenant>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task CreateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Tenant created: {TenantId} - {Slug}", tenant.Id, tenant.Slug);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Tenant updated: {TenantId}", tenant.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { id }, ct);
        if (tenant != null)
        {
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning("Tenant deleted: {TenantId}", id);
        }
    }
}
```

---

#### 4.2.2 ApiKeyRepository

**File:** `ChatAI.Infrastructure/Repositories/ApiKeyRepository.cs`

**Key Methods:**
```csharp
public class ApiKeyRepository : IApiKeyRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ApiKeyRepository> _logger;

    // Get by key hash (for authentication)
    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken ct = default)
    {
        return await _context.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);
    }

    // Get all keys for a tenant
    public async Task<List<ApiKey>> GetByTenantAsync(string tenantId, CancellationToken ct = default)
    {
        return await _context.ApiKeys
            .AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    // Create new key
    public async Task CreateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("API key created for Tenant: {TenantId}", apiKey.TenantId);
    }

    // Update key (for usage stats, deactivation)
    public async Task UpdateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        _context.ApiKeys.Update(apiKey);
        await _context.SaveChangesAsync(ct);
    }

    // Revoke key
    public async Task RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var apiKey = await _context.ApiKeys.FindAsync(new object[] { id }, ct);
        if (apiKey != null)
        {
            apiKey.IsActive = false;
            apiKey.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning("API key revoked: {ApiKeyId}", id);
        }
    }
}
```

---

#### 4.2.3 ChatMessageRepository

**File:** `ChatAI.Infrastructure/Repositories/ChatMessageRepository.cs`

**Key Methods:**
```csharp
public class ChatMessageRepository : IChatMessageRepository
{
    private readonly ChatDbContext _context;

    // Get messages for a session (with pagination)
    public async Task<List<ChatMessage>> GetBySessionIdAsync(
        string sessionId, 
        int limit = 20, 
        CancellationToken ct = default)
    {
        return await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .OrderBy(m => m.Timestamp)  // Reverse to chronological
            .ToListAsync(ct);
    }

    // Get recent messages across all sessions (for analytics)
    public async Task<List<ChatMessage>> GetRecentByTenantAsync(
        Guid tenantId, 
        int limit = 100, 
        CancellationToken ct = default)
    {
        return await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .ToListAsync(ct);
    }

    // Create message
    public async Task CreateAsync(ChatMessage message, CancellationToken ct = default)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(ct);
    }

    // Batch create (for efficiency)
    public async Task CreateRangeAsync(List<ChatMessage> messages, CancellationToken ct = default)
    {
        _context.ChatMessages.AddRange(messages);
        await _context.SaveChangesAsync(ct);
    }

    // Get token usage statistics
    public async Task<(int TotalInput, int TotalOutput)> GetTokenUsageAsync(
        Guid tenantId, 
        DateTime from, 
        DateTime to, 
        CancellationToken ct = default)
    {
        var result = await _context.ChatMessages
            .Where(m => m.TenantId == tenantId 
                && m.Timestamp >= from 
                && m.Timestamp <= to
                && m.Role == "assistant")
            .GroupBy(m => 1)
            .Select(g => new
            {
                InputTokens = g.Sum(m => m.InputTokens ?? 0),
                OutputTokens = g.Sum(m => m.OutputTokens ?? 0)
            })
            .FirstOrDefaultAsync(ct);

        return (result?.InputTokens ?? 0, result?.OutputTokens ?? 0);
    }
}
```

---

### 4.3 AI Services

#### 4.3.1 SemanticKernelChatService

**File:** `ChatAI.Infrastructure/AI/SemanticKernelChatService.cs`

**Purpose:** Integrates with Azure OpenAI using Microsoft Semantic Kernel for chat completions, tool calling, and streaming.

**Configuration:**
```csharp
public class SemanticKernelChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly ILogger<SemanticKernelChatService> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantSettingsRepository _settingsRepository;

    public SemanticKernelChatService(
        Kernel kernel,
        IChatCompletionService chatCompletion,
        ILogger<SemanticKernelChatService> logger,
        ITenantContext tenantContext,
        ITenantSettingsRepository settingsRepository)
    {
        _kernel = kernel;
        _chatCompletion = chatCompletion;
        _logger = logger;
        _tenantContext = tenantContext;
        _settingsRepository = settingsRepository;
    }

    public async Task<ChatResponseModel> SendMessageAsync(
        string userId,
        string message,
        string? sessionId,
        bool useTools,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.RequiredTenantId;
        
        // 1. Get tenant settings
        var settings = await _settingsRepository.GetByTenantIdAsync(tenantId, ct)
            ?? throw new BusinessException("Tenant settings not found");

        // 2. Build chat history
        var chatHistory = new ChatHistory();
        
        // Add system message
        if (!string.IsNullOrEmpty(settings.SystemPrompt))
        {
            chatHistory.AddSystemMessage(settings.SystemPrompt);
        }

        // Add conversation history (from DB - simplified here)
        // ... load previous messages and add to chatHistory

        // Add current user message
        chatHistory.AddUserMessage(message);

        // 3. Configure execution settings
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = settings.Temperature,
            MaxTokens = settings.MaxTokens,
            TopP = 0.9,
            FrequencyPenalty = 0.0,
            PresencePenalty = 0.0,
            ToolCallBehavior = useTools && settings.EnableToolCalling
                ? ToolCallBehavior.AutoInvokeKernelFunctions
                : ToolCallBehavior.DisableToolCalling
        };

        // 4. Call Azure OpenAI
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel,
                ct);

            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Chat completion success - Tenant: {TenantId}, User: {UserId}, Tokens: {Tokens}, Time: {Time}ms",
                tenantId, userId, result.Metadata?["Usage"], responseTime);

            // 5. Extract token usage
            var usage = result.Metadata?["Usage"] as CompletionsUsage;

            return new ChatResponseModel
            {
                SessionId = sessionId ?? Guid.NewGuid().ToString(),
                Message = result.Content ?? string.Empty,
                Model = result.ModelId,
                TokensUsed = usage != null ? new TokenUsage
                {
                    InputTokens = usage.PromptTokens,
                    OutputTokens = usage.CompletionTokens
                } : null,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat completion failed - Tenant: {TenantId}, User: {UserId}", 
                tenantId, userId);
            throw new InfrastructureException("AI service error", ex);
        }
    }
}
```

**Dependency Injection (Program.cs):**
```csharp
// Azure OpenAI configuration
var azureOpenAiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"]!;
var azureOpenAiKey = builder.Configuration["AzureOpenAI:ApiKey"]!;
var deploymentName = builder.Configuration["AzureOpenAI:DeploymentName"]!;

// Register Semantic Kernel
var kernelBuilder = builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(deploymentName, azureOpenAiEndpoint, azureOpenAiKey);

// Register plugins (tools)
kernelBuilder.Plugins.AddFromType<EmailPlugin>();
kernelBuilder.Plugins.AddFromType<KnowledgePlugin>();

builder.Services.AddScoped<IChatService, SemanticKernelChatService>();
```

---

#### 4.3.2 ChatStreamService

**File:** `ChatAI.Infrastructure/AI/ChatStreamService.cs`

**Purpose:** Streaming chat completions for real-time UI updates (Server-Sent Events).

**Key Method:**
```csharp
public async IAsyncEnumerable<string> StreamChatAsync(
    ChatHistory chatHistory,
    PromptExecutionSettings executionSettings,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    var streamingResult = _chatCompletion.GetStreamingChatMessageContentsAsync(
        chatHistory,
        executionSettings,
        _kernel,
        ct);

    await foreach (var chunk in streamingResult)
    {
        if (!string.IsNullOrEmpty(chunk.Content))
        {
            yield return chunk.Content;
        }
    }
}
```

---

#### 4.3.3 EmbeddingService

**File:** `ChatAI.Infrastructure/AI/EmbeddingService.cs`

**Purpose:** Generate text embeddings for knowledge documents using Azure OpenAI.

**Implementation:**
```csharp
public class EmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingGeneration;
    private readonly ILogger<EmbeddingService> _logger;

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text, 
        CancellationToken ct = default)
    {
        try
        {
            var embedding = await _embeddingGeneration.GenerateEmbeddingAsync(text, ct);
            
            _logger.LogInformation("Generated embedding - Dimension: {Dimension}", 
                embedding.Length);
            
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding");
            throw new InfrastructureException("Embedding generation failed", ex);
        }
    }

    public async Task<List<ReadOnlyMemory<float>>> GenerateBatchEmbeddingsAsync(
        List<string> texts, 
        CancellationToken ct = default)
    {
        var embeddings = new List<ReadOnlyMemory<float>>();
        
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, ct);
            embeddings.Add(embedding);
        }
        
        return embeddings;
    }
}
```

**Configuration:**
```csharp
// Register embedding service
builder.Services.AddAzureOpenAITextEmbeddingGeneration(
    deploymentName: builder.Configuration["AzureOpenAI:EmbeddingDeployment"]!,
    endpoint: builder.Configuration["AzureOpenAI:Endpoint"]!,
    apiKey: builder.Configuration["AzureOpenAI:ApiKey"]!);

builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
```

---

### 4.4 Authentication Services

#### 4.4.1 ApiKeyService

**File:** `ChatAI.Infrastructure/Services/ApiKeyService.cs`

**Purpose:** API key generation and hashing.

**Methods:**
```csharp
public class ApiKeyService : IApiKeyService
{
    private const string ApiKeyPrefix = "ch_";  // Chatify prefix
    private const int KeyLength = 32;  // 32 random bytes

    public string GenerateApiKey()
    {
        var randomBytes = new byte[KeyLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        var base64Key = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, KeyLength);
        
        return ApiKeyPrefix + base64Key;
    }

    public string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var keyBytes = Encoding.UTF8.GetBytes(apiKey);
        var hashBytes = sha256.ComputeHash(keyBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }
}
```

---

#### 4.4.2 AuthService

**File:** `ChatAI.Infrastructure/Services/AuthService.cs`

**Purpose:** JWT generation and password hashing for admin authentication.

**Methods:**
```csharp
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public string GenerateJwtToken(AdminUser admin)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.Username),
            new Claim(JwtRegisteredClaimNames.Email, admin.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenant_id", admin.TenantId.ToString()),
            new Claim("user_id", admin.Id.ToString()),
            new Claim(ClaimTypes.Role, admin.Role.ToString()),
            new Claim("full_name", admin.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

### 4.5 Health Checks

**File:** `ChatAI.Infrastructure/HealthChecks/QdrantHealthCheck.cs`

**Purpose:** Monitor health of Qdrant vector database connection.

**Implementation:**
```csharp
public class QdrantHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<QdrantHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken ct = default)
    {
        try
        {
            var qdrantUrl = _configuration["Qdrant:Url"];
            
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{qdrantUrl}/health", ct);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Qdrant is responsive");
            }
            
            return HealthCheckResult.Degraded($"Qdrant returned status {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Qdrant health check failed");
            return HealthCheckResult.Unhealthy("Qdrant is unreachable", ex);
        }
    }
}
```

**Registration (Program.cs):**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ChatDbContext>("database")
    .AddCheck<QdrantHealthCheck>("qdrant")
    .AddUrlGroup(new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!), "azure-openai");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

---

### 4.6 Tenant Context

**File:** `ChatAI.Infrastructure/Services/TenantContext.cs`

**Purpose:** Provides current tenant context throughout the request pipeline.

**Implementation:**
```csharp
public class TenantContext : ITenantContext
{
    private Guid? _tenantId;

    public Guid? TenantId => _tenantId;

    public Guid RequiredTenantId => _tenantId 
        ?? throw new BusinessException("Tenant context not resolved");

    public void SetTenantId(Guid tenantId)
    {
        if (_tenantId.HasValue && _tenantId.Value != tenantId)
        {
            throw new InvalidOperationException("Tenant ID already set");
        }
        
        _tenantId = tenantId;
    }

    public void Clear()
    {
        _tenantId = null;
    }
}
```

**Lifecycle:** Registered as scoped - unique instance per HTTP request.

**Usage:**
```csharp
// Injected into handlers/repositories
public class CreateApiKeyCommandHandler
{
    private readonly ITenantContext _tenantContext;
    
    public async Task<ApiKeyDto> Handle(CreateApiKeyCommand request, CancellationToken ct)
    {
        var tenantId = _tenantContext.RequiredTenantId;  // Throws if not set
        // ...
    }
}
```

---

## 5. API Layer (Presentation)

**Namespace:** `ChatAI.Api`

The API layer contains ASP.NET Core controllers, middleware, DTOs, and HTTP endpoints. All controllers follow RESTful conventions.

### 5.1 Controllers

#### 5.1.1 ChatController

**File:** `ChatAI.Api/Controllers/ChatController.cs`

**Purpose:** Handle synchronous chat message requests.

**Endpoints:**

```csharp
[ApiController]
[Route("api/chat")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _logger;

    // POST /api/chat/send
    [HttpPost("send")]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ChatResponseDto>> SendMessage(
        [FromBody] ChatRequestDto request,
        CancellationToken ct)
    {
        _logger.LogInformation("Chat request received - User: {UserId}", request.UserId);

        var command = new SendChatCommand
        {
            UserId = request.UserId,
            Message = request.Message,
            SessionId = request.SessionId,
            UseTools = request.UseTools
        };

        var response = await _mediator.Send(command, ct);

        return Ok(new ChatResponseDto
        {
            SessionId = response.SessionId,
            Message = response.Message,
            Model = response.Model,
            TokensUsed = response.TokensUsed,
            ToolCalls = response.ToolCalls?.Select(tc => new ToolCallInfoDto
            {
                ToolName = tc.ToolName,
                Arguments = tc.Arguments,
                Result = tc.Result
            }).ToList(),
            Timestamp = response.Timestamp
        });
    }

    // GET /api/chat/sessions/{sessionId}/messages
    [HttpGet("sessions/{sessionId}/messages")]
    [ProducesResponseType(typeof(List<ChatMessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChatMessageDto>>> GetConversationHistory(
        [FromRoute] string sessionId,
        [FromQuery] int maxMessages = 20,
        CancellationToken ct = default)
    {
        var query = new GetConversationHistoryQuery
        {
            SessionId = sessionId,
            MaxMessages = maxMessages
        };

        var messages = await _mediator.Send(query, ct);
        return Ok(messages);
    }
}
```

**Request/Response DTOs:**
```csharp
public class ChatRequestDto
{
    public string? UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public bool UseTools { get; set; } = true;
}

public class ChatResponseDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Model { get; set; }
    public TokenUsage? TokensUsed { get; set; }
    public List<ToolCallInfoDto>? ToolCalls { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

#### 5.1.2 ChatStreamController

**File:** `ChatAI.Api/Controllers/ChatStreamController.cs`

**Purpose:** Server-Sent Events (SSE) streaming for real-time chat responses.

**Endpoint:**
```csharp
[ApiController]
[Route("api/chat")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class ChatStreamController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatStreamController> _logger;

    // POST /api/chat/stream
    [HttpPost("stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task StreamChat(
        [FromBody] ChatRequestDto request,
        CancellationToken ct)
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        var command = new StreamChatCommand
        {
            UserId = request.UserId,
            Message = request.Message,
            SessionId = request.SessionId,
            UseTools = request.UseTools
        };

        await foreach (var chunk in _mediator.CreateStream(command, ct))
        {
            var sseMessage = $"data: {JsonSerializer.Serialize(chunk)}\n\n";
            await Response.WriteAsync(sseMessage, ct);
            await Response.Body.FlushAsync(ct);
        }

        await Response.WriteAsync("data: [DONE]\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
```

**Client Usage Example:**
```javascript
const eventSource = new EventSource('/api/chat/stream', {
    method: 'POST',
    headers: { 'X-API-Key': 'ch_xxxxx' },
    body: JSON.stringify({
        userId: 'user-123',
        message: 'Hello!',
        useTools: true
    })
});

eventSource.onmessage = (event) => {
    if (event.data === '[DONE]') {
        eventSource.close();
        return;
    }
    const chunk = JSON.parse(event.data);
    console.log(chunk.content);
};
```

---

#### 5.1.3 AuthController

**File:** `ChatAI.Api/Controllers/AuthController.cs`

**Purpose:** Admin authentication (JWT) and API key management.

**Endpoints:**

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginDto request,
        CancellationToken ct)
    {
        var command = new LoginCommand
        {
            Slug = request.Slug,
            Username = request.Username,
            Password = request.Password
        };

        var response = await _mediator.Send(command, ct);
        return Ok(response);
    }

    // POST /api/auth/api-keys (Create API key)
    [HttpPost("api-keys")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [TenantAdmin]  // Custom attribute
    [ProducesResponseType(typeof(ApiKeyResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiKeyResponseDto>> CreateApiKey(
        [FromBody] CreateApiKeyDto request,
        CancellationToken ct)
    {
        // Extract tenant_id from JWT
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Invalid tenant context");
        }

        var command = new CreateApiKeyCommand
        {
            ClientName = request.ClientName,
            Description = request.Description,
            RateLimitPerMinute = request.RateLimitPerMinute,
            RateLimitPerDay = request.RateLimitPerDay,
            ExpiresAt = request.ExpiresAt,
            CreatedBy = Guid.Parse(User.FindFirst("user_id")!.Value),
            TenantId = tenantId
        };

        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetApiKey), new { id = result.Id }, result);
    }

    // GET /api/auth/api-keys (List API keys)
    [HttpGet("api-keys")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [TenantAdmin]
    [ProducesResponseType(typeof(List<ApiKeyResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ApiKeyResponseDto>>> GetApiKeys(CancellationToken ct)
    {
        var query = new GetApiKeysQuery();
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    // DELETE /api/auth/api-keys/{id} (Revoke API key)
    [HttpDelete("api-keys/{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [TenantAdmin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeApiKey([FromRoute] Guid id, CancellationToken ct)
    {
        var command = new RevokeApiKeyCommand { ApiKeyId = id };
        await _mediator.Send(command, ct);
        return NoContent();
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst("user_id")!.Value);
        
        var command = new ResetPasswordCommand
        {
            UserId = userId,
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        await _mediator.Send(command, ct);
        return Ok(new { message = "Password updated successfully" });
    }
}
```

**DTOs:**
```csharp
public class LoginDto
{
    public string Slug { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class CreateApiKeyDto
{
    public string ClientName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RateLimitPerMinute { get; set; } = 60;
    public int RateLimitPerDay { get; set; } = 10000;
    public DateTime? ExpiresAt { get; set; }
}

public class ApiKeyResponseDto
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string? ApiKey { get; set; }  // Only returned on creation
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; }
}
```

---

#### 5.1.4 SessionController

**File:** `ChatAI.Api/Controllers/SessionController.cs`

**Purpose:** Manage chat sessions.

**Endpoints:**
```csharp
[ApiController]
[Route("api/sessions")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class SessionController : ControllerBase
{
    private readonly IMediator _mediator;

    // GET /api/sessions?userId=user-123
    [HttpGet]
    [ProducesResponseType(typeof(List<SessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SessionDto>>> GetUserSessions(
        [FromQuery] string userId,
        [FromQuery] bool onlyActive = true,
        CancellationToken ct = default)
    {
        var query = new GetUserSessionsQuery
        {
            UserId = userId,
            OnlyActive = onlyActive
        };

        var sessions = await _mediator.Send(query, ct);
        return Ok(sessions);
    }

    // DELETE /api/sessions/{sessionId}
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSession(
        [FromRoute] string sessionId,
        CancellationToken ct)
    {
        var command = new DeleteSessionCommand { SessionId = sessionId };
        await _mediator.Send(command, ct);
        return NoContent();
    }
}
```

---

#### 5.1.5 KnowledgeController

**File:** `ChatAI.Api/Controllers/KnowledgeController.cs`

**Purpose:** Knowledge base management (admin only).

**Endpoints:**
```csharp
[ApiController]
[Route("api/knowledge")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[TenantAdmin]
public class KnowledgeController : ControllerBase
{
    private readonly IMediator _mediator;

    // POST /api/knowledge/upload
    [HttpPost("upload")]
    [ProducesResponseType(typeof(KnowledgeDocumentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<KnowledgeDocumentDto>> UploadDocument(
        [FromBody] UploadKnowledgeCommand command,
        CancellationToken ct)
    {
        command.UploadedBy = Guid.Parse(User.FindFirst("user_id")!.Value);
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetDocument), new { id = result.Id }, result);
    }

    // GET /api/knowledge
    [HttpGet]
    [ProducesResponseType(typeof(List<KnowledgeDocumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KnowledgeDocumentDto>>> GetDocuments(
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        var query = new GetKnowledgeDocumentsQuery { Category = category };
        var documents = await _mediator.Send(query, ct);
        return Ok(documents);
    }

    // GET /api/knowledge/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(KnowledgeDocumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KnowledgeDocumentDto>> GetDocument(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var query = new GetKnowledgeDocumentQuery { Id = id };
        var document = await _mediator.Send(query, ct);
        return Ok(document);
    }

    // PUT /api/knowledge/{id}
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDocument(
        [FromRoute] Guid id,
        [FromBody] UpdateKnowledgeCommand command,
        CancellationToken ct)
    {
        command.Id = id;
        await _mediator.Send(command, ct);
        return NoContent();
    }

    // DELETE /api/knowledge/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var command = new DeleteKnowledgeDocumentCommand { Id = id };
        await _mediator.Send(command, ct);
        return NoContent();
    }

    // POST /api/knowledge/search
    [HttpPost("search")]
    [ProducesResponseType(typeof(List<KnowledgeSearchResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KnowledgeSearchResult>>> SearchKnowledge(
        [FromBody] SearchKnowledgeQuery query,
        CancellationToken ct)
    {
        var results = await _mediator.Send(query, ct);
        return Ok(results);
    }
}
```

---

#### 5.1.6 TenantController

**File:** `ChatAI.Api/Controllers/TenantController.cs`

**Purpose:** Tenant management (platform admin only).

**Endpoints:**
```csharp
[ApiController]
[Route("api/tenants")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TenantController : ControllerBase
{
    private readonly IMediator _mediator;

    // POST /api/tenants (Create tenant - Platform Admin only)
    [HttpPost]
    [PlatformAdmin]  // Custom attribute
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TenantResponse>> CreateTenant(
        [FromBody] CreateTenantCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetTenant), new { id = result.Id }, result);
    }

    // GET /api/tenants/{id}
    [HttpGet("{id}")]
    [TenantAdmin]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantResponse>> GetTenant(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var query = new GetTenantQuery { Id = id };
        var tenant = await _mediator.Send(query, ct);
        return Ok(tenant);
    }

    // PUT /api/tenants/{id}
    [HttpPut("{id}")]
    [TenantAdmin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateTenant(
        [FromRoute] Guid id,
        [FromBody] UpdateTenantCommand command,
        CancellationToken ct)
    {
        command.Id = id;
        await _mediator.Send(command, ct);
        return NoContent();
    }

    // GET /api/tenants (List all - Platform Admin only)
    [HttpGet]
    [PlatformAdmin]
    [ProducesResponseType(typeof(List<TenantResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TenantResponse>>> GetAllTenants(CancellationToken ct)
    {
        var query = new GetAllTenantsQuery();
        var tenants = await _mediator.Send(query, ct);
        return Ok(tenants);
    }
}
```

---

#### 5.1.7 FeedbackController

**File:** `ChatAI.Api/Controllers/FeedbackController.cs`

**Purpose:** User feedback collection and analytics.

**Endpoints:**
```csharp
[ApiController]
[Route("api/feedback")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class FeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    // POST /api/feedback
    [HttpPost]
    [ProducesResponseType(typeof(FeedbackResponseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<FeedbackResponseDto>> SubmitFeedback(
        [FromBody] SubmitFeedbackDto request,
        CancellationToken ct)
    {
        var command = new SubmitFeedbackCommand
        {
            SessionId = request.SessionId,
            MessageId = request.MessageId,
            UserId = request.UserId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetFeedback), new { id = result.Id }, result);
    }

    // GET /api/feedback/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FeedbackResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FeedbackResponseDto>> GetFeedback(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var query = new GetFeedbackQuery { Id = id };
        var feedback = await _mediator.Send(query, ct);
        return Ok(feedback);
    }

    // GET /api/feedback/stats (Admin only)
    [HttpGet("stats")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [TenantAdmin]
    [ProducesResponseType(typeof(FeedbackStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FeedbackStatsDto>> GetFeedbackStats(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = new GetFeedbackStatsQuery
        {
            From = from ?? DateTime.UtcNow.AddMonths(-1),
            To = to ?? DateTime.UtcNow
        };

        var stats = await _mediator.Send(query, ct);
        return Ok(stats);
    }
}
```

**DTOs:**
```csharp
public class SubmitFeedbackDto
{
    public string SessionId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }  // 1-5
    public string? Comment { get; set; }
}

public class FeedbackStatsDto
{
    public int TotalFeedbacks { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
    public int PositiveCount { get; set; }  // 4-5 stars
    public int NegativeCount { get; set; }  // 1-2 stars
}
```

---

#### 5.1.8 ConfigurationController

**File:** `ChatAI.Api/Controllers/ConfigurationController.cs`

**Purpose:** Tenant settings management (admin only).

**Endpoints:**
```csharp
[ApiController]
[Route("api/configuration")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[TenantAdmin]
public class ConfigurationController : ControllerBase
{
    private readonly IMediator _mediator;

    // GET /api/configuration
    [HttpGet]
    [ProducesResponseType(typeof(ConfigurationResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigurationResponseDto>> GetConfiguration(CancellationToken ct)
    {
        var query = new GetTenantSettingsQuery();
        var settings = await _mediator.Send(query, ct);
        return Ok(settings);
    }

    // PUT /api/configuration
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateConfiguration(
        [FromBody] UpdateConfigurationDto request,
        CancellationToken ct)
    {
        var command = new UpdateTenantSettingsCommand
        {
            WelcomeMessage = request.WelcomeMessage,
            SystemPrompt = request.SystemPrompt,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            EnableKnowledge = request.EnableKnowledge,
            EnableToolCalling = request.EnableToolCalling
        };

        await _mediator.Send(command, ct);
        return NoContent();
    }
}
```

---

### 5.2 Middleware

#### 5.2.1 TenantResolutionMiddleware

**File:** `ChatAI.Api/Middleware/TenantResolutionMiddleware.cs`

**Purpose:** Resolve tenant context from authentication claims or subdomain.

**Implementation:**
```csharp
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        Guid? tenantId = null;

        // Priority 1: tenant_id claim from API key or JWT
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var claimTenantId))
        {
            tenantId = claimTenantId;
            _logger.LogDebug("Tenant resolved from claim: {TenantId}", tenantId);
        }

        // Priority 2: Subdomain (e.g., acme.chatify.com → slug="acme")
        if (!tenantId.HasValue)
        {
            var host = context.Request.Host.Host;
            var parts = host.Split('.');
            
            if (parts.Length > 2)  // Has subdomain
            {
                var slug = parts[0];
                var tenantRepository = context.RequestServices.GetRequiredService<ITenantRepository>();
                var tenant = await tenantRepository.GetBySlugAsync(slug);
                
                if (tenant != null)
                {
                    tenantId = tenant.Id;
                    _logger.LogDebug("Tenant resolved from subdomain: {Slug} → {TenantId}", slug, tenantId);
                }
            }
        }

        // Set tenant context
        if (tenantId.HasValue)
        {
            tenantContext.SetTenantId(tenantId.Value);
        }
        else
        {
            _logger.LogWarning("Tenant could not be resolved for request: {Path}", context.Request.Path);
        }

        await _next(context);
    }
}
```

**Registration (Program.cs):**
```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();  // After auth, before endpoints
```

---

#### 5.2.2 GlobalExceptionMiddleware

**File:** `ChatAI.Api/Middleware/GlobalExceptionMiddleware.cs`

**Purpose:** Centralized exception handling with structured error responses.

**Implementation:**
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            BusinessException => (StatusCodes.Status400BadRequest, exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            error = message,
            details = exception is ValidationException valEx 
                ? valEx.Errors.Select(e => e.ErrorMessage).ToList() 
                : null,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
```

**Registration:**
```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();  // First in pipeline
```

---

### 5.3 Custom Attributes

#### 5.3.1 TenantAdminAttribute

**File:** `ChatAI.Api/Attributes/TenantAdminAttribute.cs`

**Purpose:** Authorization filter for tenant admin role.

**Implementation:**
```csharp
public class TenantAdminAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        
        if (roleClaim != AdminRole.TenantAdmin.ToString() && 
            roleClaim != AdminRole.PlatformAdmin.ToString())
        {
            context.Result = new ForbidResult();
        }
    }
}
```

**Usage:**
```csharp
[TenantAdmin]
[HttpPost("api-keys")]
public async Task<ActionResult> CreateApiKey(...) { }
```

---

#### 5.3.2 PlatformAdminAttribute

**File:** `ChatAI.Api/Attributes/PlatformAdminAttribute.cs`

**Purpose:** Authorization filter for platform admin role (highest privilege).

**Implementation:**
```csharp
public class PlatformAdminAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        
        if (roleClaim != AdminRole.PlatformAdmin.ToString())
        {
            context.Result = new ForbidResult();
        }
    }
}
```

**Usage:**
```csharp
[PlatformAdmin]
[HttpPost]
public async Task<ActionResult> CreateTenant(...) { }
```

---

### 5.4 Authentication Configuration

**File:** `ChatAI.Api/Program.cs` (excerpt)

**Multi-Scheme Authentication:**
```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "MultiScheme";
        options.DefaultChallengeScheme = "MultiScheme";
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
        };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "ChatAI.Auth";
        options.LoginPath = "/admin-login.html";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { })
    .AddPolicyScheme("MultiScheme", "Multi-Scheme Selector", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            // API key takes priority
            if (context.Request.Headers.ContainsKey("X-API-Key"))
                return "ApiKey";
            
            // Check for JWT in Authorization header
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return JwtBearerDefaults.AuthenticationScheme;
            
            // Default to cookie for admin UI
            return CookieAuthenticationDefaults.AuthenticationScheme;
        };
    });

builder.Services.AddAuthorization();
```

---

### 5.5 CORS Configuration

**File:** `ChatAI.Api/Program.cs` (excerpt)

**Purpose:** Allow cross-origin requests from client apps.

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClientApps", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",      // React dev server
                "http://localhost:5173",      // Vite dev server
                "https://*.chatify.com"       // Production domains
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Total-Count", "X-Page-Number");
    });
});

app.UseCors("AllowClientApps");
```

---

### 5.6 Rate Limiting

**File:** `ChatAI.Api/Program.cs` (excerpt)

**Purpose:** Prevent abuse with rate limiting policies.

```csharp
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/chat/send",
            Period = "1m",
            Limit = 60
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/chat/stream",
            Period = "1m",
            Limit = 30
        }
    };
});

builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();

app.UseIpRateLimiting();  // Before MVC middleware
```

---

## 6. Authentication & Security

### 6.1 Authentication Flows

#### 6.1.1 API Key Authentication Flow

**Purpose:** External client applications authenticate using API keys for chat endpoints.

**Flow Diagram:**
```
Client Request
    ↓
    X-API-Key: ch_abc123...
    ↓
ApiKeyAuthenticationHandler
    ↓
    Extract header → Hash key
    ↓
    Send ValidateApiKeyQuery (MediatR)
    ↓
ValidateApiKeyQueryHandler
    ↓
    Lookup by hash → Check IsActive → Check expiration
    ↓
    Update LastUsedAt, UsageCount
    ↓
    Return ApiKey entity
    ↓
ApiKeyAuthenticationHandler
    ↓
    Create ClaimsIdentity
    ↓
    Claims: [tenant_id, role=ApiClient]
    ↓
TenantResolutionMiddleware
    ↓
    Extract tenant_id claim
    ↓
    tenantContext.SetTenantId(...)
    ↓
Controller Action
    ↓
    ITenantContext.RequiredTenantId → Tenant-scoped data access
```

**Code Flow:**

1. **Request arrives with X-API-Key header:**
```http
POST /api/chat/send HTTP/1.1
Host: api.chatify.com
X-API-Key: ch_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
Content-Type: application/json
```

2. **ApiKeyAuthenticationHandler.HandleAuthenticateAsync():**
```csharp
// Extract header
var apiKey = Request.Headers["X-API-Key"].ToString();

// Hash the key
var keyHash = _apiKeyService.HashApiKey(apiKey);

// Validate via MediatR
var query = new ValidateApiKeyQuery { ApiKey = apiKey };
var validatedKey = await _mediator.Send(query);

if (validatedKey == null)
    return AuthenticateResult.Fail("Invalid API key");

// Create claims
var claims = new[]
{
    new Claim("tenant_id", validatedKey.TenantId),
    new Claim(ClaimTypes.Role, "ApiClient")
};

var identity = new ClaimsIdentity(claims, "ApiKey");
var principal = new ClaimsPrincipal(identity);
var ticket = new AuthenticationTicket(principal, "ApiKey");

return AuthenticateResult.Success(ticket);
```

3. **TenantResolutionMiddleware resolves tenant:**
```csharp
var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
if (Guid.TryParse(tenantIdClaim, out var tenantId))
{
    tenantContext.SetTenantId(tenantId);
}
```

4. **Controller uses tenant context:**
```csharp
var tenantId = _tenantContext.RequiredTenantId;
// All queries/commands now scoped to this tenant
```

---

#### 6.1.2 JWT Authentication Flow (Admin)

**Purpose:** Admin users authenticate with username/password to receive JWT token for management operations.

**Flow Diagram:**
```
Admin Login
    ↓
    POST /api/auth/login
    ↓
    { slug: "acme", username: "admin", password: "..." }
    ↓
LoginCommandHandler
    ↓
    Get Tenant by slug
    ↓
    Get AdminUser by username + tenantId
    ↓
    Verify password (BCrypt)
    ↓
    AuthService.GenerateJwtToken(adminUser)
    ↓
    Return JWT + User info
    ↓
Client stores JWT
    ↓
Subsequent requests:
    ↓
    Authorization: Bearer <jwt-token>
    ↓
JwtBearerHandler validates token
    ↓
    Verify signature, expiration, issuer, audience
    ↓
    Extract claims → Create ClaimsPrincipal
    ↓
TenantResolutionMiddleware
    ↓
    Extract tenant_id from JWT → Set context
    ↓
Authorization Attribute checks
    ↓
    [TenantAdmin] or [PlatformAdmin]
    ↓
Controller Action executes
```

**Login Implementation:**

```csharp
// LoginCommandHandler.cs
public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken ct)
{
    // 1. Get tenant by slug
    var tenant = await _tenantRepository.GetBySlugAsync(request.Slug.ToLower(), ct)
        ?? throw new UnauthorizedException("Invalid credentials");

    if (!tenant.IsActive)
        throw new BusinessException("Tenant is inactive");

    // 2. Get admin user
    var adminUser = await _adminUserRepository.GetByUsernameAsync(
        request.Username, 
        tenant.Id, 
        ct)
        ?? throw new UnauthorizedException("Invalid credentials");

    if (!adminUser.IsActive)
        throw new BusinessException("User is inactive");

    // 3. Verify password
    if (!_authService.VerifyPassword(request.Password, adminUser.PasswordHash))
        throw new UnauthorizedException("Invalid credentials");

    // 4. Update last login
    adminUser.LastLoginAt = DateTime.UtcNow;
    await _adminUserRepository.UpdateAsync(adminUser, ct);

    // 5. Generate JWT
    var token = _authService.GenerateJwtToken(adminUser);

    return new LoginResponseDto
    {
        Token = token,
        Username = adminUser.Username,
        FullName = adminUser.FullName,
        Role = adminUser.Role.ToString(),
        ExpiresAt = DateTime.UtcNow.AddMinutes(60)  // From config
    };
}
```

**JWT Token Structure:**
```json
{
  "sub": "admin",
  "email": "admin@acme.com",
  "jti": "unique-jwt-id",
  "tenant_id": "123e4567-e89b-12d3-a456-426614174000",
  "user_id": "admin-user-guid",
  "role": "TenantAdmin",
  "full_name": "John Doe",
  "exp": 1735350000,
  "iss": "ChatAI.Api",
  "aud": "ChatAI.Client"
}
```

---

#### 6.1.3 Cookie Authentication Flow (Admin UI)

**Purpose:** Browser-based admin UI uses cookies for seamless authentication.

**Flow:**
```
Admin visits /admin-login.html
    ↓
    Enter credentials
    ↓
    POST /api/auth/login
    ↓
Receive JWT token
    ↓
JavaScript sets cookie: ChatAI.Auth = <jwt-token>
    ↓
Subsequent page navigation
    ↓
Browser sends cookie automatically
    ↓
CookieAuthenticationHandler validates
    ↓
Extract claims → Set User context
    ↓
Admin pages accessible
```

**Cookie Configuration:**
```csharp
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "ChatAI.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/admin-login.html";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
```

---

### 6.2 Multi-Tenancy Security

#### 6.2.1 Tenant Isolation

**Principle:** All data is scoped to tenant ID. No cross-tenant data access is allowed.

**Implementation Patterns:**

1. **Repository Level Filtering:**
```csharp
public async Task<List<ChatMessage>> GetBySessionIdAsync(string sessionId, CancellationToken ct)
{
    var tenantId = _tenantContext.RequiredTenantId;
    
    return await _context.ChatMessages
        .AsNoTracking()
        .Where(m => m.TenantId == tenantId && m.SessionId == sessionId)  // CRITICAL
        .OrderBy(m => m.Timestamp)
        .ToListAsync(ct);
}
```

2. **Command/Query Handler Validation:**
```csharp
public async Task<ApiKeyDto> Handle(CreateApiKeyCommand request, CancellationToken ct)
{
    var tenantId = _tenantContext.RequiredTenantId;
    
    // Ensure API key is created for the authenticated tenant
    request.TenantId = tenantId;  // Override any client-provided value
    
    // ... rest of logic
}
```

3. **Database Indexes for Performance:**
```csharp
// Composite indexes on (TenantId, OtherColumn) for efficient queries
entity.HasIndex(e => new { e.TenantId, e.UserId });
entity.HasIndex(e => new { e.TenantId, e.IsActive });
entity.HasIndex(e => new { e.TenantId, e.SessionId });
```

---

#### 6.2.2 Tenant Context Lifecycle

**Scoped Service Pattern:**
```csharp
// Registration in Program.cs
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Lifecycle:
// 1. Request arrives
// 2. New TenantContext instance created
// 3. Authentication sets tenant_id claim
// 4. TenantResolutionMiddleware calls tenantContext.SetTenantId()
// 5. All services in request scope see same tenant ID
// 6. Request completes → TenantContext disposed
```

**Validation Example:**
```csharp
public class TenantContext : ITenantContext
{
    private Guid? _tenantId;

    public void SetTenantId(Guid tenantId)
    {
        // Prevent tenant switching mid-request
        if (_tenantId.HasValue && _tenantId.Value != tenantId)
        {
            throw new SecurityException("Tenant context already set");
        }
        
        _tenantId = tenantId;
    }

    public Guid RequiredTenantId
    {
        get
        {
            if (!_tenantId.HasValue)
            {
                // Force fail if tenant not resolved
                throw new SecurityException("Tenant context not resolved");
            }
            return _tenantId.Value;
        }
    }
}
```

---

### 6.3 Security Measures

#### 6.3.1 Password Security

**Hashing Algorithm:** BCrypt with salt rounds = 12

**Implementation:**
```csharp
public class AuthService : IAuthService
{
    public string HashPassword(string password)
    {
        // BCrypt automatically generates salt
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
```

**Password Requirements (via FluentValidation):**
```csharp
public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain digit")
            .Matches(@"[\W_]").WithMessage("Password must contain special character");
    }
}
```

---

#### 6.3.2 API Key Security

**Generation:** Cryptographically secure random bytes (32 bytes)

```csharp
public string GenerateApiKey()
{
    var randomBytes = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(randomBytes);
    }
    
    var base64Key = Convert.ToBase64String(randomBytes)
        .Replace("+", "")
        .Replace("/", "")
        .Replace("=", "")
        .Substring(0, 32);
    
    return "ch_" + base64Key;  // Prefix for identification
}
```

**Storage:** SHA256 hash only (plain key never stored)

```csharp
public string HashApiKey(string apiKey)
{
    using var sha256 = SHA256.Create();
    var keyBytes = Encoding.UTF8.GetBytes(apiKey);
    var hashBytes = sha256.ComputeHash(keyBytes);
    return Convert.ToHexString(hashBytes).ToLower();
}
```

**Rotation:** API keys can be revoked and recreated

```csharp
// Revoke old key
await _mediator.Send(new RevokeApiKeyCommand { ApiKeyId = oldKeyId });

// Create new key
var newKey = await _mediator.Send(new CreateApiKeyCommand { ... });
```

---

#### 6.3.3 Rate Limiting

**Strategy:** IP-based rate limiting per endpoint

**Configuration:**
```csharp
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/chat/send",
            Period = "1m",
            Limit = 60  // 60 requests per minute
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/chat/send",
            Period = "1d",
            Limit = 10000  // 10k requests per day
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Period = "1m",
            Limit = 5  // Prevent brute force
        }
    };
});
```

**Custom Rate Limit Per API Key:**
```csharp
public class ApiKey
{
    public int RateLimitPerMinute { get; set; } = 60;
    public int RateLimitPerDay { get; set; } = 10000;
    
    // Enforced in middleware or handler
}
```

---

#### 6.3.4 Input Validation

**FluentValidation Integration:**

All commands/queries validated before execution via MediatR pipeline behavior:

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken ct)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, ct)));
            
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();
            
            if (failures.Any())
                throw new ValidationException(failures);
        }
        
        return await next();
    }
}
```

**Example Validators:**
```csharp
public class SendChatCommandValidator : AbstractValidator<SendChatCommand>
{
    public SendChatCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message cannot be empty")
            .MaximumLength(4000).WithMessage("Message too long")
            .Must(NotContainMaliciousContent).WithMessage("Invalid content");
        
        RuleFor(x => x.UserId)
            .MaximumLength(100);
        
        RuleFor(x => x.SessionId)
            .Must(BeValidGuid).When(x => !string.IsNullOrEmpty(x.SessionId))
            .WithMessage("Invalid session ID format");
    }
    
    private bool NotContainMaliciousContent(string message)
    {
        // Basic XSS/injection prevention
        var dangerousPatterns = new[] { "<script", "javascript:", "onerror=" };
        return !dangerousPatterns.Any(p => message.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

#### 6.3.5 HTTPS Enforcement

**Production Configuration:**
```csharp
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.UseHsts();  // HTTP Strict Transport Security
}
```

**HSTS Configuration:**
```csharp
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});
```

---

### 6.4 Logging & Monitoring

#### 6.4.1 Serilog Configuration

**File:** `ChatAI.Api/Program.cs`

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ChatAI.Api")
    .Enrich.WithMachineName()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/chatai-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"]!)  // Centralized logging
    .CreateLogger();

builder.Host.UseSerilog();
```

**Structured Logging Examples:**
```csharp
_logger.LogInformation(
    "Chat completion - Tenant: {TenantId}, User: {UserId}, Tokens: {Tokens}, Time: {TimeMs}ms",
    tenantId, userId, tokensUsed, responseTime);

_logger.LogWarning(
    "API key validation failed - KeyHash: {KeyHash}, Reason: {Reason}",
    keyHash.Substring(0, 8), "Expired");

_logger.LogError(ex,
    "Database operation failed - Operation: {Operation}, Entity: {Entity}",
    "Create", "ApiKey");
```

---

#### 6.4.2 Health Checks

**Endpoints:**
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // Just checks if app is running
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")  // DB, Redis, etc.
});
```

**Response Format:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0234567"
    },
    "qdrant": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    },
    "azure-openai": {
      "status": "Healthy",
      "duration": "00:00:00.0876543"
    }
  }
}
```

---

## 7. Deployment Guide

### 7.1 Prerequisites

**Required Services:**
- SQL Server 2019+ (or Azure SQL Database)
- Azure OpenAI Service (GPT-4, text-embedding-ada-002)
- Qdrant Vector Database (optional for advanced RAG)
- .NET 10.0 Runtime
- IIS or Linux with reverse proxy (Nginx)

---

### 7.2 Configuration

#### 7.2.1 appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-sql.database.windows.net;Database=ChatAI_Prod;User Id=chatai_user;Password=<strong-password>;Encrypt=True;"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-openai.openai.azure.com/",
    "ApiKey": "<azure-openai-key>",
    "DeploymentName": "gpt-4",
    "EmbeddingDeployment": "text-embedding-ada-002"
  },
  "Qdrant": {
    "Url": "https://qdrant-prod.example.com:6333",
    "ApiKey": "<qdrant-api-key>",
    "CollectionName": "chatai_knowledge"
  },
  "JwtSettings": {
    "SecretKey": "<generate-secure-256-bit-key>",
    "Issuer": "ChatAI.Api",
    "Audience": "ChatAI.Client",
    "ExpirationMinutes": 60
  },
  "Seq": {
    "ServerUrl": "https://seq-logging.example.com"
  },
  "AllowedHosts": "*.chatify.com",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

---

### 7.3 Database Migration

**Apply migrations to production:**
```powershell
# From ChatAI.Infrastructure project directory
$env:ASPNETCORE_ENVIRONMENT="Production"

dotnet ef database update --project ChatAI.Infrastructure --startup-project ..\ChatAI.Api

# Or using SQL script:
dotnet ef migrations script --output migration.sql --idempotent
# Apply migration.sql to production database
```

**Verify migration:**
```sql
SELECT * FROM [ChatAI_Prod].[dbo].[__EFMigrationsHistory]
ORDER BY MigrationId DESC;
```

---

### 7.4 Docker Deployment

#### 7.4.1 Dockerfile

**File:** `Dockerfile` (root)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Chatify AI.sln", "./"]
COPY ["ChatAI.Api/ChatAI.Api.csproj", "ChatAI.Api/"]
COPY ["ChatAI.Application/ChatAI.Application.csproj", "ChatAI.Application/"]
COPY ["ChatAI.Domain/ChatAI.Domain.csproj", "ChatAI.Domain/"]
COPY ["ChatAI.Infrastructure/ChatAI.Infrastructure.csproj", "ChatAI.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "ChatAI.Api/ChatAI.Api.csproj"

# Copy all source files
COPY . .

# Build
WORKDIR "/src/ChatAI.Api"
RUN dotnet build "ChatAI.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatAI.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ChatAI.Api.dll"]
```

#### 7.4.2 docker-compose.yml

**File:** `docker-compose.yml`

```yaml
version: '3.8'

services:
  chatai-api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: chatai-api
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80;https://+:443
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/certificate.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=<cert-password>
    volumes:
      - ./logs:/app/logs
      - ./https:/https:ro
    depends_on:
      - sqlserver
      - qdrant
    restart: unless-stopped
    networks:
      - chatai-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: chatai-sqlserver
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong!Passw0rd
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    restart: unless-stopped
    networks:
      - chatai-network

  qdrant:
    image: qdrant/qdrant:latest
    container_name: chatai-qdrant
    ports:
      - "6333:6333"
    volumes:
      - qdrant-data:/qdrant/storage
    restart: unless-stopped
    networks:
      - chatai-network

volumes:
  sqlserver-data:
  qdrant-data:

networks:
  chatai-network:
    driver: bridge
```

**Start Services:**
```powershell
docker-compose up -d
docker-compose logs -f chatai-api  # View logs
```

---

### 7.5 Azure Deployment

#### 7.5.1 Azure Resources

**Required Services:**
- Azure App Service (Linux, .NET 10)
- Azure SQL Database
- Azure OpenAI Service
- Azure Key Vault (for secrets)
- Application Insights (monitoring)

**PowerShell Deployment Script:**

**File:** `deploy-azure.ps1`

```powershell
# Variables
$resourceGroup = "rg-chatai-prod"
$location = "eastus"
$appServicePlan = "asp-chatai-prod"
$webApp = "chatai-prod-api"
$sqlServer = "sql-chatai-prod"
$sqlDatabase = "chatai-prod-db"
$keyVault = "kv-chatai-prod"

# Create resource group
az group create --name $resourceGroup --location $location

# Create SQL Server
az sql server create `
    --name $sqlServer `
    --resource-group $resourceGroup `
    --location $location `
    --admin-user chatai_admin `
    --admin-password "<strong-password>"

# Create SQL Database
az sql db create `
    --resource-group $resourceGroup `
    --server $sqlServer `
    --name $sqlDatabase `
    --service-objective S1

# Create App Service Plan
az appservice plan create `
    --name $appServicePlan `
    --resource-group $resourceGroup `
    --sku P1V2 `
    --is-linux

# Create Web App
az webapp create `
    --resource-group $resourceGroup `
    --plan $appServicePlan `
    --name $webApp `
    --runtime "DOTNET|10.0"

# Configure App Settings
az webapp config appsettings set `
    --resource-group $resourceGroup `
    --name $webApp `
    --settings `
        ASPNETCORE_ENVIRONMENT=Production `
        ConnectionStrings__DefaultConnection="<sql-connection-string>" `
        AzureOpenAI__Endpoint="<openai-endpoint>" `
        AzureOpenAI__ApiKey="@Microsoft.KeyVault(SecretUri=<key-vault-uri>)"

# Deploy application
dotnet publish ChatAI.Api/ChatAI.Api.csproj -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./chatai-api.zip

az webapp deployment source config-zip `
    --resource-group $resourceGroup `
    --name $webApp `
    --src ./chatai-api.zip

Write-Host "Deployment complete! URL: https://$webApp.azurewebsites.net"
```

---

### 7.6 Post-Deployment Checklist

**Security:**
- ✅ HTTPS enabled and enforced
- ✅ HSTS configured (max-age 1 year)
- ✅ Secrets stored in Key Vault (not appsettings)
- ✅ CORS restricted to known origins
- ✅ Rate limiting enabled
- ✅ SQL firewall configured (specific IPs only)

**Monitoring:**
- ✅ Application Insights configured
- ✅ Health check endpoints responding
- ✅ Log aggregation to Seq/Azure Monitor
- ✅ Alerts configured (high error rates, downtime)

**Database:**
- ✅ Migrations applied successfully
- ✅ Indexes verified
- ✅ Backup policy configured (point-in-time restore)

**Performance:**
- ✅ Response caching enabled
- ✅ Connection pooling configured
- ✅ Static file compression enabled

**Testing:**
- ✅ Smoke tests passed (login, API key auth, chat)
- ✅ Load testing completed
- ✅ Health checks returning healthy status

---

### 7.7 Monitoring & Maintenance

**Key Metrics to Monitor:**
- Request rate (requests/minute)
- Response time (p50, p95, p99)
- Error rate (4xx, 5xx)
- Database query performance
- Azure OpenAI token usage
- API key usage per tenant
- Concurrent connections

**Log Queries (Seq/KQL):**
```sql
-- High error rate
select count(*) 
from stream 
where Level = 'Error' 
  and @Timestamp > Now()-1h
group by time(5m)

-- Slow chat responses
select avg(TimeMs) 
from stream 
where Message like '%Chat completion%' 
  and @Timestamp > Now()-1h

-- Top tenants by usage
select count(*) as RequestCount, TenantId
from stream
where Message like '%Chat request%'
  and @Timestamp > Now()-24h
group by TenantId
order by RequestCount desc
limit 10
```

---

## 8. Summary

**Chatify AI** is a production-ready, multi-tenant SaaS chatbot platform built with:

- **Clean Architecture** - Separation of concerns across Domain, Application, Infrastructure, API layers
- **CQRS Pattern** - Command/Query separation via MediatR
- **Multi-Tenancy** - Complete data isolation with tenant-scoped context
- **Multi-Scheme Authentication** - API Key, JWT, Cookie support
- **AI Integration** - Azure OpenAI with Semantic Kernel for chat and RAG
- **Vector Search** - Qdrant for knowledge base embeddings
- **Security** - BCrypt passwords, SHA256 API keys, rate limiting, input validation
- **Observability** - Structured logging, health checks, metrics

**Total Documentation:** ~4,200 lines covering every class, method, property, flow, and deployment scenario.

---

**END OF DOCUMENTATION**


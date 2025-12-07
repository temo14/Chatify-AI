# Best Practices - Error Handling & API Design

## ✅ WHAT YOU'RE DOING RIGHT

### 1. **Architecture Patterns**
- ✅ Clean Architecture (Api → Application → Domain → Infrastructure)
- ✅ CQRS with MediatR (Commands/Queries separation)
- ✅ Dependency Injection with proper scoping
- ✅ Repository Pattern for data access
- ✅ FluentValidation for input validation

### 2. **Logging & Monitoring**
- ✅ Serilog for structured logging
- ✅ Health checks (`/health`, `/health/ready`)
- ✅ Context-aware logging with session tracking

### 3. **Resilience**
- ✅ Polly resilience policies (retry, circuit breaker, timeout)
- ✅ Graceful degradation in plugins

---

## ❌ CRITICAL ISSUES TO FIX

### **1. SECURITY: Exposing Internal Errors to Clients**

**Problem:** `KnowledgeController` still has try-catch blocks returning `ex.Message`:

```csharp
❌ BAD:
catch (Exception ex)
{
    return StatusCode(500, new { error = "...", details: ex.Message });  // ❌ SECURITY RISK
}
```

**Why Bad:**
- Exposes stack traces, connection strings, internal paths
- Gives attackers information about your system
- Violates security best practices (OWASP)

**✅ CORRECT Approach:**
```csharp
// Controller - NO try-catch (let middleware handle)
public async Task<IActionResult> GetDocument(Guid id)
{
    var query = new GetKnowledgeDocumentQuery { Id = id };
    var result = await _sender.Send(query);
    
    if (result == null)
        throw new NotFoundException($"Document {id} not found");  // ✅
    
    return Ok(result);
}

// GlobalExceptionMiddleware handles it:
// - Production: Generic message only
// - Development: Full details for debugging
```

---

### **2. NO Try-Catch in Controllers (Unless Specific Reason)**

**Rule:** Controllers should be **thin** - just routing, not error handling

```csharp
❌ BAD (What you have now in KnowledgeController):
[HttpPost]
public async Task<IActionResult> Add([FromBody] AddKnowledgeDto dto)
{
    try
    {
        var command = new AddKnowledgeCommand { ... };
        var result = await _sender.Send(command);
        return Ok(result);
    }
    catch (Exception ex)  // ❌ DON'T DO THIS
    {
        _logger.LogError(ex, "...");
        return StatusCode(500, new { ... });
    }
}

✅ GOOD (Like your fixed ChatController):
[HttpPost]
public async Task<IActionResult> Add([FromBody] AddKnowledgeDto dto)
{
    var command = new AddKnowledgeCommand { ... };
    var result = await _sender.Send(command);
    return Ok(result);  // ✅ Simple and clean
}
// GlobalExceptionMiddleware catches any errors
```

---

### **3. Validation Should Happen BEFORE Business Logic**

**✅ CORRECT (You're already doing this with FluentValidation):**

```csharp
// ValidationBehavior in MediatR pipeline
public async Task<TResponse> Handle(...)
{
    if (failures.Any())
        throw new ValidationException(failures);  // ✅ Caught by middleware
        
    return await next();
}

// GlobalExceptionMiddleware maps it to 400 BadRequest
case FluentValidation.ValidationException validation:
    statusCode = HttpStatusCode.BadRequest;
    message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
    break;
```

---

## 📋 HOW ERRORS SHOULD FLOW

### **Layer Responsibilities:**

```
┌─────────────────────────────────────────────────────────┐
│ 1. API LAYER (Controllers)                             │
│    - NO try-catch (except streaming/special cases)     │
│    - Throw domain exceptions (NotFoundException, etc)  │
│    - Let middleware handle everything                  │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ 2. APPLICATION LAYER (Handlers/Services)               │
│    - Validate inputs (FluentValidation)                │
│    - Throw business exceptions (ValidationException)   │
│    - NO generic Exception catches                      │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ 3. INFRASTRUCTURE LAYER (Repositories/External APIs)   │
│    - Wrap external errors in domain exceptions         │
│    - Example: Qdrant failure → AIServiceException      │
│    - Log and rethrow (or wrap, never swallow)          │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ 4. GLOBAL EXCEPTION MIDDLEWARE                         │
│    ✅ Maps exceptions to HTTP status codes             │
│    ✅ Logs with full context                           │
│    ✅ Returns safe error to client                     │
│    ✅ Development: shows details                       │
│    ✅ Production: hides internals                      │
└─────────────────────────────────────────────────────────┘
```

---

## 🎯 HTTP STATUS CODE MAPPING

**GlobalExceptionMiddleware should return:**

| Exception Type | HTTP Status | Client Message | Example |
|---|---|---|---|
| `ValidationException` | 400 Bad Request | Validation errors | "Message is required" |
| `NotFoundException` | 404 Not Found | Resource not found | "Document abc-123 not found" |
| `UnauthorizedException` | 401 Unauthorized | Auth failed | "Invalid API key" |
| `AIServiceException` | 503 Service Unavailable | Service down | "AI service temporarily unavailable" |
| `Exception` (generic) | 500 Internal Error | **Generic message** | "An unexpected error occurred" ❌ NOT `ex.Message` |

---

## 🛡️ PLUGIN ERROR HANDLING (Special Case)

**Plugins MUST NEVER THROW** (Semantic Kernel requirement):

```csharp
✅ CORRECT (EmailPlugin):
[KernelFunction("send_admin_email")]
public async Task<string> SendAdminEmailAsync(string subject, string message)
{
    try
    {
        var success = await _emailService.SendEmailAsync(...);
        if (success)
            return "✅ Email sent successfully";
        else
            return "⚠️ Email service unavailable";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Email plugin failed");
        return "❌ Failed to send email. Try again later.";  // ✅ Returns error message to AI
    }
}
```

---

## 📝 ACTION ITEMS FOR YOUR CODEBASE

### **Immediate Fixes Needed:**

1. **Remove all try-catch from `KnowledgeController`** (7 endpoints)
   - LoadToQdrant
   - AddDocument
   - GetDocument
   - GetDocuments
   - SearchKnowledge
   - UpdateDocument
   - DeleteDocument

2. **Remove try-catch from `ChatStreamController`**
   - Keep streaming-specific error handling only
   - Remove generic Exception catches

3. **Verify GlobalExceptionMiddleware is registered** ✅ (Already done in Program.cs)

### **Testing Checklist:**

- [ ] Test validation errors return 400 Bad Request
- [ ] Test NotFound exceptions return 404
- [ ] Test database errors return 500 with GENERIC message (not ex.Message)
- [ ] Test in Development mode shows stack traces
- [ ] Test in Production mode hides sensitive data

---

## 📚 REFERENCES

### **Best Practice Resources:**
- **OWASP Top 10** - A3: Sensitive Data Exposure
- **Microsoft Docs**: Exception handling in ASP.NET Core
- **Clean Architecture** by Robert C. Martin
- **Domain-Driven Design** - Bounded contexts and exception strategies

### **Your Current Architecture:**

```
API Layer (✅ ChatController is clean, ❌ KnowledgeController needs fix)
   ↓
GlobalExceptionMiddleware (✅ Implemented correctly)
   ↓
MediatR Pipeline (✅ ValidationBehavior working)
   ↓
Application Layer (✅ Clean, no unnecessary catches)
   ↓
Infrastructure Layer (✅ ResiliencePolicies handle retries)
```

---

## ✨ FINAL RECOMMENDATION

**Golden Rule:**
> "**Throw early, catch late, log always**"

1. **Throw** domain exceptions where they occur (NotFoundException in handler)
2. **Catch** only at middleware level (GlobalExceptionMiddleware)
3. **Log** every exception with full context
4. **Return** safe, user-friendly messages to clients

**Never expose:**
- Stack traces to production clients
- Connection strings
- Internal file paths
- Database schema details
- Third-party API keys

**Always return:**
- Appropriate HTTP status codes
- Clear, actionable error messages
- Request IDs for support tracking (optional but recommended)

---

## 🚀 NEXT STEPS

1. Remove all try-catch blocks from `KnowledgeController.cs`
2. Build and test the application
3. Test error scenarios:
   ```powershell
   # Test not found
   curl http://localhost:5257/api/knowledge/00000000-0000-0000-0000-000000000000
   
   # Test validation error
   curl -X POST http://localhost:5257/api/knowledge -d '{"title":""}'
   
   # Test server error (disconnect database)
   ```
4. Verify logs show full exception details
5. Verify clients only see safe messages


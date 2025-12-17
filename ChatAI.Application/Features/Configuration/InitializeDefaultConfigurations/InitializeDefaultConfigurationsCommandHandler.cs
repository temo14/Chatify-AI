
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Configuration.InitializeDefaultConfigurations;

/// <summary>
/// Handler for InitializeDefaultConfigurationsCommand - creates default configuration settings
/// </summary>
public class InitializeDefaultConfigurationsCommandHandler : IRequestHandler<InitializeDefaultConfigurationsCommand, int>
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger<InitializeDefaultConfigurationsCommandHandler> _logger;

    public InitializeDefaultConfigurationsCommandHandler(
        IConfigurationRepository configurationRepository,
        ILogger<InitializeDefaultConfigurationsCommandHandler> logger)
    {
        _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> Handle(InitializeDefaultConfigurationsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔧 Initializing default configurations");

        var defaults = GetDefaultConfigurations(command.ModifiedBy);
        var createdCount = 0;

        foreach (var config in defaults)
        {
            var exists = await _configurationRepository.KeyExistsAsync(config.Key, cancellationToken);
            if (!exists)
            {
                await _configurationRepository.AddAsync(config, cancellationToken);
                createdCount++;
            }
        }

        _logger.LogInformation("✅ Initialized {Count} default configurations", createdCount);
        return createdCount;
    }

    private List<AdminConfiguration> GetDefaultConfigurations(string? modifiedBy)
    {
        var now = DateTime.UtcNow;
        return new List<AdminConfiguration>
        {
            // AI Settings - Core Parameters
            new() { Id = Guid.NewGuid(), Key = "AI.SystemPrompt", Value = @"You are Chatify AI, an intelligent conversational assistant with advanced capabilities.

## YOUR CORE ABILITIES
- **Knowledge Base Access**: You can search and retrieve information from an integrated knowledge repository to provide accurate, contextual answers
- **Email Support Tool**: When users need technical assistance or have issues requiring human intervention, you can send detailed support emails to administrators
- **Conversation Memory**: You maintain context across the conversation to provide coherent, personalized interactions

## YOUR OPERATING PRINCIPLES

### 1. ACCURACY FIRST
- Always base responses on available knowledge base information when relevant
- If uncertain or information is unavailable, clearly state limitations
- Cite knowledge sources when making specific claims
- Never fabricate information or make unsupported assumptions

### 2. CLARITY & STRUCTURE
- Organize complex information with clear formatting (lists, sections, emphasis)
- Use concise language while maintaining completeness
- Adapt technical depth to user's apparent expertise level
- Break down complex topics into digestible segments

### 3. PROACTIVE ASSISTANCE
- Anticipate follow-up questions and address them preemptively
- Suggest related topics or actions that might help the user
- When detecting user frustration or technical issues, offer to escalate via email to support team
- Guide users toward optimal solutions, not just answer the immediate question

### 4. PROFESSIONAL TONE
- Maintain a helpful, friendly, yet professional demeanor
- Be conversational without being overly casual
- Show empathy for user challenges while focusing on solutions
- Avoid jargon unless the user demonstrates technical familiarity

## RESPONSE GUIDELINES
- **Concise answers** for simple queries (2-3 sentences)
- **Detailed explanations** for complex topics (structured with headings)
- **Step-by-step instructions** for procedural questions
- **Code examples** with clear comments when relevant
- **Acknowledge** when redirecting to knowledge base or escalating to human support

## TOOL USAGE
- **Knowledge Search**: Automatically triggered when questions relate to stored documentation
- **Email Support**: Offer this when:
  - Technical issues require human investigation
  - User explicitly requests human assistance
  - Issue is beyond your knowledge scope
  - User expresses significant frustration

You represent a balance between autonomous problem-solving and knowing when to involve human expertise. Prioritize user success above all else.", DataType = "String", Category = "AI", 
                Description = "System prompt that defines AI behavior and personality", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "AI.Temperature", Value = "0.7", DataType = "Double", Category = "AI", 
                Description = "Controls randomness (0.0=focused, 2.0=creative). Recommended: 0.7 for balanced responses", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "0.0-2.0", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "AI.MaxTokens", Value = "1500", DataType = "Integer", Category = "AI", 
                Description = "Maximum response length in tokens (1 token ≈ 4 chars). Recommended: 1500 for detailed responses", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "100-8000", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "AI.TopP", Value = "0.95", DataType = "Double", Category = "AI", 
                Description = "Nucleus sampling (0.0-1.0). Higher = more diverse. Recommended: 0.95", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "0.0-1.0", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "AI.FrequencyPenalty", Value = "0.3", DataType = "Double", Category = "AI", 
                Description = "Reduces repetition (-2.0 to 2.0). Recommended: 0.3 to avoid redundancy", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "-2.0-2.0", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "AI.PresencePenalty", Value = "0.2", DataType = "Double", Category = "AI", 
                Description = "Encourages topic diversity (-2.0 to 2.0). Recommended: 0.2", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "-2.0-2.0", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "AI.ModelName", Value = "gpt-4o", DataType = "String", Category = "AI", 
                Description = "OpenAI deployment name (gpt-4o, gpt-4, gpt-35-turbo)", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },

            // RAG Settings - Retrieval-Augmented Generation
            new() { Id = Guid.NewGuid(), Key = "RAG.Enabled", Value = "true", DataType = "Boolean", Category = "RAG", 
                Description = "Enable knowledge base integration for AI responses", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "RAG.TopKResults", Value = "3", DataType = "Integer", Category = "RAG", 
                Description = "Number of knowledge base results to include in context. More = better accuracy but slower", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "1-10", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "RAG.ScoreThreshold", Value = "0.7", DataType = "Double", Category = "RAG", 
                Description = "Minimum similarity score (0.0-1.0). Higher = stricter matching. Recommended: 0.7", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "0.0-1.0", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "RAG.MaxContextLength", Value = "3000", DataType = "Integer", Category = "RAG", 
                Description = "Maximum characters from knowledge base to include. Prevents token overflow", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "500-8000", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "RAG.DocumentChunkSize", Value = "800", DataType = "Integer", Category = "RAG", 
                Description = "Characters per chunk when splitting documents. Recommended: 800 for embeddings", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "200-2000", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "RAG.ChunkOverlap", Value = "150", DataType = "Integer", Category = "RAG", 
                Description = "Overlap between chunks to preserve context across boundaries", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "0-500", CreatedAt = now, UpdatedAt = now },

            // Feature Flags - User-Facing Features
            new() { Id = Guid.NewGuid(), Key = "Features.EnableFileUpload", Value = "false", DataType = "Boolean", Category = "Features", 
                Description = "Allow users to upload documents to knowledge base (PDF, DOCX, TXT)", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Features.EnableExport", Value = "true", DataType = "Boolean", Category = "Features", 
                Description = "Allow users to export conversation history (JSON, Markdown, Text)", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Features.EnableFeedback", Value = "true", DataType = "Boolean", Category = "Features", 
                Description = "Show thumbs up/down buttons for user feedback on AI responses", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Features.EnableEmailTools", Value = "true", DataType = "Boolean", Category = "Features", 
                Description = "Allow AI to send emails to administrators (for support requests)", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Features.MaxConversationHistory", Value = "20", DataType = "Integer", Category = "Features", 
                Description = "Number of previous messages to include in AI context. More = better context but slower", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "5-50", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Features.StreamingEnabled", Value = "true", DataType = "Boolean", Category = "Features", 
                Description = "Enable real-time streaming responses (better UX, shows AI typing)", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },

            // Security & Limits
            new() { Id = Guid.NewGuid(), Key = "Security.SessionTimeoutMinutes", Value = "120", DataType = "Integer", Category = "Security", 
                Description = "Auto-logout inactive users after X minutes. Recommended: 120 (2 hours)", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "15-1440", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Security.RequireAuthentication", Value = "false", DataType = "Boolean", Category = "Security", 
                Description = "Require login before using chat. Enable for production environments", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Security.MaxConversationsPerUser", Value = "50", DataType = "Integer", Category = "Security", 
                Description = "Maximum active conversations per user. Prevents database bloat", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "10-1000", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Security.MaxMessageLength", Value = "4000", DataType = "Integer", Category = "Security", 
                Description = "Maximum characters per user message. Prevents abuse", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "100-10000", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Security.RateLimitPerMinute", Value = "20", DataType = "Integer", Category = "Security", 
                Description = "Maximum messages per user per minute. Prevents spam", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "5-100", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Security.EnableCORS", Value = "true", DataType = "Boolean", Category = "Security", 
                Description = "Allow cross-origin requests (needed for web clients)", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },

            // Branding & User Experience
            new() { Id = Guid.NewGuid(), Key = "Branding.ApplicationName", Value = "Chatify AI", DataType = "String", Category = "Branding", 
                Description = "Application name shown in UI and emails", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Branding.CompanyName", Value = "Your Company", DataType = "String", Category = "Branding", 
                Description = "Company/Organization name for branding", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Branding.WelcomeMessage", Value = "👋 Welcome! I'm your AI assistant. Ask me anything or upload documents to enhance my knowledge.", DataType = "String", Category = "Branding", 
                Description = "First message users see when starting a new conversation", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Branding.ThemeColor", Value = "#0066CC", DataType = "String", Category = "Branding", 
                Description = "Primary brand color in hex format (e.g., #0066CC for blue)", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "^#[0-9A-Fa-f]{6}$", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Branding.SupportEmail", Value = "t.baindurashvili.gm@gmail.com", DataType = "String", Category = "Branding", 
                Description = "Email where AI sends support requests", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = "^[^@]+@[^@]+\\.[^@]+$", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), Key = "Branding.LogoUrl", Value = "", DataType = "String", Category = "Branding", 
                Description = "URL to company logo (optional, shown in chat interface)", IsActive = true, ModifiedBy = modifiedBy, 
                ValidationRule = null, CreatedAt = now, UpdatedAt = now },
        };
    }
}

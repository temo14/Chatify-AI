using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Tenants.UpdateTenantSettings;

/// <summary>
/// Command for tenant admins to update their own tenant's chat settings
/// This is separate from UpdateTenantCommand which is for platform admins only
/// </summary>
public class UpdateTenantSettingsCommand : IRequest<TenantResponse>
{
    /// <summary>
    /// Tenant ID (set from authenticated user's context)
    /// </summary>
    public Guid TenantId { get; set; }
    
    // Chat Configuration Settings
    public string? SystemPrompt { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? ChatPlaceholder { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    
    // Vector/RAG Settings
    public string? VectorStorageMode { get; set; }
    public string? QdrantCollectionName { get; set; }
    public bool? EnableDocumentChunking { get; set; }
    public int? ChunkSize { get; set; }
    public int? ChunkOverlap { get; set; }
    
    // Feature Toggles
    public bool? EnableChatHistory { get; set; }
    public int? ChatHistoryRetentionDays { get; set; }
    public bool? EnableFeedback { get; set; }
    public bool? EnableOverview { get; set; }
    public bool? EnableTools { get; set; }
    
    // Support Configuration
    public string? SupportEmail { get; set; }
}

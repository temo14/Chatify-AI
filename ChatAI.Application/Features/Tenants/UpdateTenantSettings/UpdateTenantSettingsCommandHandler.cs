using ChatAI.Application.Exceptions;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Models.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Tenants.UpdateTenantSettings;

/// <summary>
/// Handler for UpdateTenantSettingsCommand
/// Allows tenant admins to update their own tenant's chat and RAG settings
/// Does NOT allow changing billing limits or subscription plans
/// </summary>
public class UpdateTenantSettingsCommandHandler : IRequestHandler<UpdateTenantSettingsCommand, TenantResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<UpdateTenantSettingsCommandHandler> _logger;

    public UpdateTenantSettingsCommandHandler(
        ITenantRepository tenantRepository,
        ILogger<UpdateTenantSettingsCommandHandler> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantResponse> Handle(UpdateTenantSettingsCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Updating tenant settings for tenant {TenantId}", request.TenantId);

        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, ct);
        if (tenant == null)
        {
            throw new NotFoundException($"Tenant with ID {request.TenantId} not found");
        }

        // Update settings only (not billing/plan info)
        if (tenant.Settings != null)
        {
            // Chat Configuration
            if (request.SystemPrompt != null) 
                tenant.Settings.SystemPrompt = request.SystemPrompt;
            if (request.WelcomeMessage != null) 
                tenant.Settings.WelcomeMessage = request.WelcomeMessage;
            if (request.ChatPlaceholder != null) 
                tenant.Settings.ChatPlaceholder = request.ChatPlaceholder;
            if (request.Temperature.HasValue) 
                tenant.Settings.Temperature = request.Temperature.Value;
            if (request.MaxTokens.HasValue) 
                tenant.Settings.MaxTokens = request.MaxTokens.Value;

            // Vector/RAG Settings
            if (request.VectorStorageMode != null) 
                tenant.Settings.VectorStorageMode = request.VectorStorageMode;
            if (request.QdrantCollectionName != null) 
                tenant.Settings.QdrantCollectionName = request.QdrantCollectionName;
            if (request.EnableDocumentChunking.HasValue) 
                tenant.Settings.EnableDocumentChunking = request.EnableDocumentChunking.Value;
            if (request.ChunkSize.HasValue) 
                tenant.Settings.ChunkSize = request.ChunkSize.Value;
            if (request.ChunkOverlap.HasValue) 
                tenant.Settings.ChunkOverlap = request.ChunkOverlap.Value;

            // Feature Toggles
            if (request.EnableChatHistory.HasValue) 
                tenant.Settings.EnableChatHistory = request.EnableChatHistory.Value;
            if (request.ChatHistoryRetentionDays.HasValue) 
                tenant.Settings.ChatHistoryRetentionDays = request.ChatHistoryRetentionDays.Value;
            if (request.EnableFeedback.HasValue) 
                tenant.Settings.EnableFeedback = request.EnableFeedback.Value;
            if (request.EnableOverview.HasValue) 
                tenant.Settings.EnableOverview = request.EnableOverview.Value;
            if (request.EnableTools.HasValue) 
                tenant.Settings.EnableTools = request.EnableTools.Value;

            // Support Configuration
            if (request.SupportEmail != null) 
                tenant.Settings.SupportEmail = request.SupportEmail;

            tenant.Settings.UpdatedAt = DateTime.UtcNow;
        }

        await _tenantRepository.UpdateAsync(tenant, ct);

        _logger.LogInformation("✅ Updated tenant settings for {TenantId}", tenant.Id);

        return MapToResponse(tenant);
    }

    private static TenantResponse MapToResponse(Domain.Entities.Tenant tenant)
    {
        return new TenantResponse
        {
            Id = tenant.Id,
            Slug = tenant.Slug,
            Name = tenant.Name,
            Email = tenant.Email,
            PlanTier = tenant.PlanTier,
            IsActive = tenant.IsActive,
            CustomDomain = tenant.CustomDomain,
            LogoUrl = tenant.LogoUrl,
            PrimaryColor = tenant.PrimaryColor,
            MaxDocuments = tenant.MaxDocuments,
            MaxMonthlyMessages = tenant.MaxMonthlyMessages,
            CurrentDocumentCount = tenant.CurrentDocumentCount,
            CurrentMonthMessages = tenant.CurrentMonthMessages,
            BillingPeriodStart = tenant.BillingPeriodStart,
            CreatedAt = tenant.CreatedAt,
            LastActivityAt = tenant.LastActivityAt,
            SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
            Settings = tenant.Settings == null ? null : new TenantSettingsResponse
            {
                Id = tenant.Settings.Id,
                VectorStorageMode = tenant.Settings.VectorStorageMode,
                QdrantCollectionName = tenant.Settings.QdrantCollectionName,
                EnableDocumentChunking = tenant.Settings.EnableDocumentChunking,
                ChunkSize = tenant.Settings.ChunkSize,
                ChunkOverlap = tenant.Settings.ChunkOverlap,
                EnableChatHistory = tenant.Settings.EnableChatHistory,
                ChatHistoryRetentionDays = tenant.Settings.ChatHistoryRetentionDays,
                EnableFeedback = tenant.Settings.EnableFeedback,
                EnableOverview = tenant.Settings.EnableOverview,
                EnableEmailSupport = tenant.Settings.EnableEmailSupport,
                SupportEmail = tenant.Settings.SupportEmail,
                WelcomeMessage = tenant.Settings.WelcomeMessage,
                ChatPlaceholder = tenant.Settings.ChatPlaceholder,
                EnableTools = tenant.Settings.EnableTools,
                Temperature = tenant.Settings.Temperature,
                MaxTokens = tenant.Settings.MaxTokens,
                SystemPrompt = tenant.Settings.SystemPrompt
            }
        };
    }
}

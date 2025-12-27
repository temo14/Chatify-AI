using ChatAI.Application.Exceptions;
using ChatAI.Domain.Models.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Tenants.UpdateTenant;

public class UpdateTenantCommand : IRequest<TenantResponse>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PlanTier { get; set; }
    public bool? IsActive { get; set; }
    public string? CustomDomain { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public int? MaxDocuments { get; set; }
    public int? MaxMonthlyMessages { get; set; }
    
    // Settings
    public string? VectorStorageMode { get; set; }
    public string? QdrantCollectionName { get; set; }
    public bool? EnableDocumentChunking { get; set; }
    public int? ChunkSize { get; set; }
    public string? WelcomeMessage { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public string? SystemPrompt { get; set; }
    public bool? EnableFeedback { get; set; }
    public bool? EnableOverview { get; set; }
    public bool? EnableEmailSupport { get; set; }
    public string? SupportEmail { get; set; }
}

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantResponse>
{
    private readonly Domain.Interfaces.Repositories.ITenantRepository _tenantRepository;
    private readonly ILogger<UpdateTenantCommandHandler> _logger;

    public UpdateTenantCommandHandler(
        Domain.Interfaces.Repositories.ITenantRepository tenantRepository,
        ILogger<UpdateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantResponse> Handle(UpdateTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, ct);
        if (tenant == null)
        {
            throw new NotFoundException($"Tenant with ID {request.Id} not found");
        }

        // Update tenant properties
        if (request.Name != null) tenant.Name = request.Name;
        if (request.Email != null) tenant.Email = request.Email;
        if (request.PlanTier != null) tenant.PlanTier = request.PlanTier;
        if (request.IsActive.HasValue) tenant.IsActive = request.IsActive.Value;
        if (request.CustomDomain != null) tenant.CustomDomain = request.CustomDomain.ToLower();
        if (request.LogoUrl != null) tenant.LogoUrl = request.LogoUrl;
        if (request.PrimaryColor != null) tenant.PrimaryColor = request.PrimaryColor;
        if (request.MaxDocuments.HasValue) tenant.MaxDocuments = request.MaxDocuments.Value;
        if (request.MaxMonthlyMessages.HasValue) tenant.MaxMonthlyMessages = request.MaxMonthlyMessages.Value;

        // Update settings
        if (tenant.Settings != null)
        {
            if (request.VectorStorageMode != null) tenant.Settings.VectorStorageMode = request.VectorStorageMode;
            if (request.QdrantCollectionName != null) tenant.Settings.QdrantCollectionName = request.QdrantCollectionName;
            if (request.EnableDocumentChunking.HasValue) tenant.Settings.EnableDocumentChunking = request.EnableDocumentChunking.Value;
            if (request.ChunkSize.HasValue) tenant.Settings.ChunkSize = request.ChunkSize.Value;
            if (request.WelcomeMessage != null) tenant.Settings.WelcomeMessage = request.WelcomeMessage;
            if (request.Temperature.HasValue) tenant.Settings.Temperature = request.Temperature.Value;
            if (request.MaxTokens.HasValue) tenant.Settings.MaxTokens = request.MaxTokens.Value;
            if (request.SystemPrompt != null) tenant.Settings.SystemPrompt = request.SystemPrompt;
            if (request.EnableFeedback.HasValue) tenant.Settings.EnableFeedback = request.EnableFeedback.Value;
            if (request.EnableOverview.HasValue) tenant.Settings.EnableOverview = request.EnableOverview.Value;
            if (request.EnableEmailSupport.HasValue) tenant.Settings.EnableEmailSupport = request.EnableEmailSupport.Value;
            if (request.SupportEmail != null) tenant.Settings.SupportEmail = request.SupportEmail;
            tenant.Settings.UpdatedAt = DateTime.UtcNow;
        }

        await _tenantRepository.UpdateAsync(tenant, ct);

        _logger.LogInformation("Updated tenant {TenantId}", tenant.Id);

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

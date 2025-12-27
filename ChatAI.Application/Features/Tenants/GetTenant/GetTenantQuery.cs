using ChatAI.Application.Exceptions;
using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Tenants.GetTenant;

public class GetTenantQuery : IRequest<TenantResponse>
{
    public Guid Id { get; set; }
}

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantResponse>
{
    private readonly Domain.Interfaces.Repositories.ITenantRepository _tenantRepository;

    public GetTenantQueryHandler(Domain.Interfaces.Repositories.ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
    }

    public async Task<TenantResponse> Handle(GetTenantQuery request, CancellationToken ct)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, ct);
        
        if (tenant == null)
        {
            throw new NotFoundException($"Tenant with ID {request.Id} not found");
        }

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

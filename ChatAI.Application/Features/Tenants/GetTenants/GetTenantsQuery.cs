using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Tenants.GetTenants;

public class GetTenantsQuery : IRequest<PagedResult<TenantResponse>>
{
    public string? SearchTerm { get; set; }
    public string? PlanTier { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, PagedResult<TenantResponse>>
{
    private readonly Domain.Interfaces.Repositories.ITenantRepository _tenantRepository;

    public GetTenantsQueryHandler(Domain.Interfaces.Repositories.ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
    }

    public async Task<PagedResult<TenantResponse>> Handle(GetTenantsQuery request, CancellationToken ct)
    {
        var (tenants, totalCount) = await _tenantRepository.GetPagedAsync(
            request.SearchTerm,
            request.PlanTier,
            request.IsActive,
            request.Page,
            request.PageSize,
            ct);

        var response = tenants.Select(MapToResponse);

        return new PagedResult<TenantResponse>
        {
            Items = response,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
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

using ChatAI.Application.Exceptions;
using ChatAI.Domain.Entities;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Domain.Models.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Tenants.CreateTenant;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IAdminUserRepository adminUserRepository,
        IAuthService authService,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _adminUserRepository = adminUserRepository ?? throw new ArgumentNullException(nameof(adminUserRepository));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantResponse> Handle(CreateTenantCommand request, CancellationToken ct)
    {
        // Validate slug is unique
        if (await _tenantRepository.SlugExistsAsync(request.Slug, ct))
        {
            throw new ValidationException($"Tenant slug '{request.Slug}' already exists");
        }

        // Validate email is not already used by another admin user
        var emailLower = request.Email.ToLower();
        if (await _adminUserRepository.UsernameExistsAsync(emailLower, ct))
        {
            throw new ValidationException($"Email '{request.Email}' is already in use by another admin user. Each admin must have a unique email.");
        }

        // Validate password is provided
        if (string.IsNullOrWhiteSpace(request.AdminPassword))
        {
            throw new ValidationException("Admin password is required to create a new tenant. This ensures the tenant has an admin user who can log in.");
        }

        // Validate password strength (minimum 8 characters)
        if (request.AdminPassword.Length < 8)
        {
            throw new ValidationException("Admin password must be at least 8 characters long.");
        }

        // Create tenant
        var tenant = new Tenant
        {
            Slug = request.Slug.ToLower(),
            Name = request.Name,
            Email = request.Email,
            PlanTier = request.PlanTier,
            CustomDomain = request.CustomDomain?.ToLower(),
            LogoUrl = request.LogoUrl,
            PrimaryColor = request.PrimaryColor ?? "#667eea",
            MaxDocuments = request.MaxDocuments ?? GetDefaultMaxDocuments(request.PlanTier),
            MaxMonthlyMessages = request.MaxMonthlyMessages ?? GetDefaultMaxMessages(request.PlanTier),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Create default settings with sensible defaults
        tenant.Settings = new TenantSettings
        {
            TenantId = tenant.Id,
            VectorStorageMode = request.VectorStorageMode ?? "SQL",
            QdrantCollectionName = request.QdrantCollectionName,
            EnableDocumentChunking = request.EnableDocumentChunking ?? true,
            ChunkSize = 512,
            ChunkOverlap = 50,
            WelcomeMessage = request.WelcomeMessage ?? $"Welcome to {tenant.Name}! How can I help you today?",
            ChatPlaceholder = "Ask me anything...",
            Temperature = 0.7,
            MaxTokens = 2000,
            ChatHistoryRetentionDays = 90,
            EnableFeedback = true,
            EnableOverview = true,
            EnableTools = false,
            CreatedAt = DateTime.UtcNow
        };

        // Create initial admin user for this tenant
        var adminUser = new AdminUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Username = emailLower, // Use email as username
            Email = request.Email,
            FullName = request.AdminFullName ?? tenant.Name,
            PasswordHash = _authService.HashPassword(request.AdminPassword),
            IsPlatformAdmin = false, // This is a Tenant Admin, NOT Platform Admin
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0,
            LockedUntil = null
        };

        // Save admin user first (it already has correct TenantId)
        await _adminUserRepository.CreateAsync(adminUser, ct);

        // Save tenant (admin user is already linked via TenantId)
        tenant = await _tenantRepository.AddAsync(tenant, ct);

        _logger.LogInformation(
            "✅ Created tenant {TenantId} ({TenantSlug}) with initial admin user {UserId} ({Username})",
            tenant.Id, tenant.Slug, adminUser.Id, adminUser.Username);

        return MapToResponse(tenant);
    }

    private static int GetDefaultMaxDocuments(string planTier) => planTier switch
    {
        "Free" => 10,
        "Starter" => 50,
        "Pro" => 200,
        "Enterprise" => 1000,
        _ => 10
    };

    private static int GetDefaultMaxMessages(string planTier) => planTier switch
    {
        "Free" => 1000,
        "Starter" => 5000,
        "Pro" => 20000,
        "Enterprise" => 100000,
        _ => 1000
    };

    private static TenantResponse MapToResponse(Tenant tenant)
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

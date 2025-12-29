using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using ChatAI.Infrastructure.AI;
using ChatAI.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatAI.Infrastructure.Services;

/// <summary>
/// Factory that creates the appropriate vector storage implementation based on tenant settings
/// </summary>
public class VectorStorageFactory : IVectorStorageFactory
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantRepository _tenantRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VectorStorageFactory> _logger;

    public VectorStorageFactory(
        ITenantContext tenantContext,
        ITenantRepository tenantRepository,
        IServiceProvider serviceProvider,
        ILogger<VectorStorageFactory> logger)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IVectorStorage> CreateForCurrentTenantAsync(CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
        {
            throw new InvalidOperationException("No tenant context available");
        }

        return await CreateForTenantAsync(_tenantContext.TenantId!.Value, ct);
    }

    public async Task<IVectorStorage> CreateForTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {tenantId} not found");
        }

        var settings = tenant.Settings;
        if (settings == null)
        {
            _logger.LogWarning("Tenant {TenantId} has no settings, defaulting to SQL vector storage", tenantId);
            return CreateSqlVectorStorage(tenantId);
        }

        return settings.VectorStorageMode.ToUpper() switch
        {
            //"QDRANT" => CreateQdrantVectorStorage(tenantId, settings.QdrantCollectionName),
            "SQL" => CreateSqlVectorStorage(tenantId),
            _ => CreateSqlVectorStorage(tenantId)
            //_ => throw new NotSupportedException($"Vector storage mode '{settings.VectorStorageMode}' is not supported")
        };
    }

    private IVectorStorage CreateSqlVectorStorage(Guid tenantId)
    {
        return (IVectorStorage)ActivatorUtilities.CreateInstance(
            _serviceProvider,
            typeof(SqlVectorStorage),
            tenantId);
    }

    private IVectorStorage CreateQdrantVectorStorage(Guid tenantId, string? collectionName)
    {
        var effectiveCollectionName = collectionName ?? $"tenant-{tenantId:N}";
        
        return (IVectorStorage)ActivatorUtilities.CreateInstance(
            _serviceProvider,
            typeof(QdrantVectorStorage),
            tenantId,
            effectiveCollectionName);
    }
}

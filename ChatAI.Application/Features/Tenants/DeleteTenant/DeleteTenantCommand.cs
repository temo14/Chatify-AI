using ChatAI.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Tenants.DeleteTenant;

public class DeleteTenantCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, Unit>
{
    private readonly Domain.Interfaces.Repositories.ITenantRepository _tenantRepository;
    private readonly ILogger<DeleteTenantCommandHandler> _logger;

    public DeleteTenantCommandHandler(
        Domain.Interfaces.Repositories.ITenantRepository tenantRepository,
        ILogger<DeleteTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> Handle(DeleteTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.Id, ct);
        if (tenant == null)
        {
            throw new NotFoundException($"Tenant with ID {request.Id} not found");
        }

        await _tenantRepository.DeleteAsync(request.Id, ct);

        _logger.LogInformation("Deleted tenant {TenantId} ({TenantSlug})", tenant.Id, tenant.Slug);

        return Unit.Value;
    }
}

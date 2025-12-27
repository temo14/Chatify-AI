using ChatAI.Domain.Interfaces.Repositories;
using MediatR;

namespace ChatAI.Application.Features.AdminUsers.GetAdminUserByTenantId;

public class GetAdminUserByTenantIdQueryHandler : IRequestHandler<GetAdminUserByTenantIdQuery, AdminUserResponse?>
{
    private readonly IAdminUserRepository _adminUserRepository;

    public GetAdminUserByTenantIdQueryHandler(IAdminUserRepository adminUserRepository)
    {
        _adminUserRepository = adminUserRepository;
    }

    public async Task<AdminUserResponse?> Handle(GetAdminUserByTenantIdQuery request, CancellationToken cancellationToken)
    {
        var adminUser = await _adminUserRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);

        if (adminUser == null)
        {
            return null;
        }

        return new AdminUserResponse
        {
            Id = adminUser.Id,
            Email = adminUser.Email ?? string.Empty
        };
    }
}

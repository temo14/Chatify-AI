using MediatR;

namespace ChatAI.Application.Features.AdminUsers.GetAdminUserByTenantId;

public class GetAdminUserByTenantIdQuery : IRequest<AdminUserResponse?>
{
    public Guid TenantId { get; set; }
}

public class AdminUserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
}

using Microsoft.AspNetCore.Authorization;

namespace ChatAI.Api.Attributes;

/// <summary>
/// Authorization attribute for Tenant Admin role (customers + platform admins)
/// Tenant admins can manage their own tenant's knowledge base and settings
/// Platform admins automatically have tenant admin access for support purposes
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TenantAdminAttribute : AuthorizeAttribute
{
    public TenantAdminAttribute()
    {
        Policy = "TenantAdmin";
    }
}

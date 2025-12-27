using Microsoft.AspNetCore.Authorization;

namespace ChatAI.Api.Attributes;

/// <summary>
/// Authorization attribute for Platform Admin role (Dott staff only)
/// Platform admins can manage all tenants and system-wide configuration
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class PlatformAdminAttribute : AuthorizeAttribute
{
    public PlatformAdminAttribute()
    {
        Policy = "PlatformAdmin";
    }
}

using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ChatAI.Api.Middleware;

/// <summary>
/// Middleware to resolve tenant from subdomain, custom domain, header, or JWT claim
/// Priority: JWT claim > Custom Domain > Subdomain > X-Tenant-Slug header
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantRepository tenantRepository)
    {
        try
        {
            // 1. Try to resolve from JWT claim (highest priority for admin users)
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantIdFromJwt))
            {
                var tenantFromJwt = await tenantRepository.GetByIdAsync(tenantIdFromJwt);
                if (tenantFromJwt != null && tenantFromJwt.IsActive)
                {
                    tenantContext.SetTenant(tenantFromJwt.Id, tenantFromJwt.Slug);
                    _logger.LogDebug("Tenant resolved from JWT: {TenantSlug} ({TenantId})", 
                        tenantFromJwt.Slug, tenantFromJwt.Id);
                    await _next(context);
                    return;
                }
            }

            // 2. Try to resolve from custom domain
            var host = context.Request.Host.Host.ToLower();
            var tenantFromDomain = await tenantRepository.GetByCustomDomainAsync(host);
            if (tenantFromDomain != null && tenantFromDomain.IsActive)
            {
                tenantContext.SetTenant(tenantFromDomain.Id, tenantFromDomain.Slug);
                _logger.LogDebug("Tenant resolved from custom domain: {TenantSlug} ({Domain})", 
                    tenantFromDomain.Slug, host);
                await _next(context);
                return;
            }

            // 3. Try to resolve from subdomain (e.g., studio1.yourapp.com)
            var hostParts = host.Split('.');
            if (hostParts.Length >= 3) // Has subdomain
            {
                var subdomain = hostParts[0];
                
                // Skip common subdomains
                if (!IsReservedSubdomain(subdomain))
                {
                    var tenantFromSubdomain = await tenantRepository.GetBySlugAsync(subdomain);
                    if (tenantFromSubdomain != null && tenantFromSubdomain.IsActive)
                    {
                        tenantContext.SetTenant(tenantFromSubdomain.Id, tenantFromSubdomain.Slug);
                        _logger.LogDebug("Tenant resolved from subdomain: {TenantSlug}", subdomain);
                        await _next(context);
                        return;
                    }
                }
            }

            // 4. Try to resolve from X-Tenant-Slug header (for API clients)
            if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var slugHeader))
            {
                var slug = slugHeader.ToString().ToLower();
                var tenantFromHeader = await tenantRepository.GetBySlugAsync(slug);
                if (tenantFromHeader != null && tenantFromHeader.IsActive)
                {
                    tenantContext.SetTenant(tenantFromHeader.Id, tenantFromHeader.Slug);
                    _logger.LogDebug("Tenant resolved from header: {TenantSlug}", slug);
                    await _next(context);
                    return;
                }
            }

            // No tenant resolved - this is OK for admin login, health checks, etc.
            _logger.LogDebug("No tenant resolved for path: {Path}", context.Request.Path);
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant");
            throw;
        }
    }

    private static bool IsReservedSubdomain(string subdomain)
    {
        var reserved = new[] { "www", "api", "admin", "app", "mail", "ftp", "localhost" };
        return reserved.Contains(subdomain.ToLower());
    }
}

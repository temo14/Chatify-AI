using ChatAI.Api.Attributes;
using ChatAI.Application.Exceptions;
using ChatAI.Application.Features.AdminUsers.GetAdminUserByTenantId;
using ChatAI.Application.Features.Tenants.CreateTenant;
using ChatAI.Application.Features.Tenants.DeleteTenant;
using ChatAI.Application.Features.Tenants.GetTenant;
using ChatAI.Application.Features.Tenants.GetTenants;
using ChatAI.Application.Features.Tenants.UpdateTenant;
using ChatAI.Application.Features.Tenants.UpdateTenantSettings;
using ChatAI.Domain.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Tenant management endpoints for multi-tenancy administration
/// Thin controller - delegates all business logic to Application layer via CQRS
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Require authentication, specific roles defined per-endpoint
public class TenantController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ISender sender, ILogger<TenantController> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all tenants with pagination, search, and filtering
    /// </summary>
    /// <param name="searchTerm">Search in tenant name, email, or slug</param>
    /// <param name="planTier">Filter by plan tier (Free, Basic, Pro, Enterprise)</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <response code="200">Returns paginated tenant list</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpGet]
    [PlatformAdmin]
    [ProducesResponseType(typeof(PagedResult<TenantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTenants(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? planTier = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetTenantsQuery
        {
            SearchTerm = searchTerm,
            PlanTier = planTier,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize
        };

        var result = await _sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific tenant by ID
    /// </summary>
    /// <param name="id">Tenant unique identifier</param>
    /// <response code="200">Returns tenant details</response>
    /// <response code="404">Tenant not found</response>
    [HttpGet("{id:guid}")]
    [PlatformAdmin]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var query = new GetTenantQuery { Id = id };
        var result = await _sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new tenant with initial admin user
    /// </summary>
    /// <remarks>
    /// This endpoint atomically creates both:
    /// 1. A new tenant (customer company) with default settings
    /// 2. The first admin user for that tenant (using email as username)
    /// 
    /// The tenant's email will be used as the admin username for login.
    /// The admin will have IsPlatformAdmin=false (Tenant Admin role).
    /// 
    /// This prevents "orphan tenants" - every tenant has an admin from day one.
    /// </remarks>
    /// <param name="command">Tenant creation details including admin password</param>
    /// <response code="201">Tenant and admin user created successfully</response>
    /// <response code="400">Validation failed or email already in use</response>
    /// <response code="409">Slug already exists</response>
    [HttpPost]
    [PlatformAdmin]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand command)
    {
        var result = await _sender.Send(command);
        return CreatedAtAction(
            nameof(GetTenant),
            new { id = result.Id },
            result);
    }

    /// <summary>
    /// Update an existing tenant
    /// </summary>
    /// <param name="id">Tenant unique identifier</param>
    /// <param name="command">Updated tenant details</param>
    /// <response code="200">Tenant updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Tenant not found</response>
    [HttpPut("{id:guid}")]
    [PlatformAdmin]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantCommand command)
    {
        command.Id = id; // Ensure ID matches route parameter
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete (soft delete) a tenant
    /// </summary>
    /// <param name="id">Tenant unique identifier</param>
    /// <response code="204">Tenant deleted successfully</response>
    /// <response code="404">Tenant not found</response>
    [HttpDelete("{id:guid}")]
    [PlatformAdmin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        var command = new DeleteTenantCommand { Id = id };
        await _sender.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Get current tenant's usage statistics
    /// </summary>
    /// <param name="id">Tenant unique identifier</param>
    /// <response code="200">Returns tenant usage stats</response>
    /// <response code="404">Tenant not found</response>
    [HttpGet("{id:guid}/stats")]
    [PlatformAdmin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantStats(Guid id)
    {
        var query = new GetTenantQuery { Id = id };
        var tenant = await _sender.Send(query);

        var stats = new
        {
            tenant.CurrentDocumentCount,
            CurrentMonthlyMessages = tenant.CurrentMonthMessages,
            tenant.MaxDocuments,
            tenant.MaxMonthlyMessages,
            DocumentUsagePercentage = tenant.MaxDocuments > 0
                ? (double)tenant.CurrentDocumentCount / tenant.MaxDocuments * 100
                : 0,
            MessageUsagePercentage = tenant.MaxMonthlyMessages > 0
                ? (double)tenant.CurrentMonthMessages / tenant.MaxMonthlyMessages * 100
                : 0,
            tenant.BillingPeriodStart,
            DaysUntilBillingReset = (tenant.BillingPeriodStart.AddMonths(1) - DateTime.UtcNow).Days
        };

        return Ok(stats);
    }

    /// <summary>
    /// Update current tenant's chat settings (Tenant Admin endpoint)
    /// Allows tenant admins to configure their own chat experience
    /// </summary>
    /// <param name="command">Settings to update</param>
    /// <response code="200">Settings updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Tenant not found</response>
    [HttpPut("settings")]
    [TenantAdmin] // Both tenant admins and platform admins can access
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenantSettings([FromBody] UpdateTenantSettingsCommand command)
    {
        // Get tenant ID from authenticated user's claims
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest(new { message = "Invalid tenant context" });
        }

        command.TenantId = tenantId;
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Get current tenant's settings (Tenant Admin endpoint)
    /// </summary>
    /// <response code="200">Returns tenant settings</response>
    /// <response code="404">Tenant not found</response>
    [HttpGet("settings")]
    [TenantAdmin]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentTenantSettings()
    {
        // Get tenant ID from authenticated user's claims
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest(new { message = "Invalid tenant context" });
        }

        var query = new GetTenantQuery { Id = tenantId };
        var result = await _sender.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get the admin user for a specific tenant (Platform Admin only)
    /// Used for password reset functionality
    /// </summary>
    /// <param name="id">Tenant unique identifier</param>
    /// <response code="200">Returns admin user ID and email</response>
    /// <response code="404">Tenant or admin user not found</response>
    [HttpGet("{id:guid}/admin-user")]
    [PlatformAdmin]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantAdminUser(Guid id)
    {
        // Query admin user by TenantId
        var adminUser = await _sender.Send(new GetAdminUserByTenantIdQuery { TenantId = id });

        if (adminUser == null)
        {
            return NotFound(new { message = "Admin user not found for this tenant" });
        }

        return Ok(new { id = adminUser.Id, email = adminUser.Email });
    }
}
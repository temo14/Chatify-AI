using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Tenants.CreateTenant;

public class CreateTenantCommand : IRequest<TenantResponse>
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PlanTier { get; set; } = "Free";
    public string? CustomDomain { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public int? MaxDocuments { get; set; }
    public int? MaxMonthlyMessages { get; set; }
    
    // Settings
    public string? VectorStorageMode { get; set; }
    public string? QdrantCollectionName { get; set; }
    public bool? EnableDocumentChunking { get; set; }
    public string? WelcomeMessage { get; set; }
    
    // Admin User Provisioning
    /// <summary>
    /// Password for the initial tenant admin user (required)
    /// This admin will have full control over the tenant's settings and knowledge base
    /// </summary>
    public string AdminPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Full name for the admin user (defaults to tenant name)
    /// </summary>
    public string? AdminFullName { get; set; }
}

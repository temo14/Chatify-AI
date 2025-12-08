using ChatAI.Api.DTOs;
using ChatAI.Application.Commands;
using ChatAI.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Controller for admin configuration management
/// Thin controller - delegates all logic to Application layer via CQRS (MediatR)
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "Admin")]
public class ConfigurationController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(ISender sender, ILogger<ConfigurationController> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all configurations with optional filters
    /// </summary>
    /// <param name="category">Filter by category</param>
    /// <param name="isActive">Filter by active status</param>
    /// <response code="200">Returns configuration list</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllConfigurations([FromQuery] string? category, [FromQuery] bool? isActive)
    {
        var query = new GetConfigurationListQuery
        {
            Category = category,
            IsActive = isActive
        };

        var configurations = await _sender.Send(query);

        var dtos = configurations.Select(c => new ConfigurationResponseDto
        {
            Id = c.Id,
            Key = c.Key,
            Value = c.Value,
            DataType = c.DataType,
            Category = c.Category,
            Description = c.Description,
            IsActive = c.IsActive,
            ModifiedBy = c.ModifiedBy,
            ValidationRule = c.ValidationRule,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Get configuration by key
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <response code="200">Returns the configuration</response>
    /// <response code="404">Configuration not found</response>
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfiguration(string key)
    {
        var query = new GetConfigurationQuery { Key = key };
        var config = await _sender.Send(query);

        if (config == null)
        {
            return NotFound();
        }

        var dto = new ConfigurationResponseDto
        {
            Id = config.Id,
            Key = config.Key,
            Value = config.Value,
            DataType = config.DataType,
            Category = config.Category,
            Description = config.Description,
            IsActive = config.IsActive,
            ModifiedBy = config.ModifiedBy,
            ValidationRule = config.ValidationRule,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Get configurations by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <response code="200">Returns configurations in the category</response>
    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var query = new GetConfigurationListQuery { Category = category };
        var configurations = await _sender.Send(query);

        var dtos = configurations.Select(c => new ConfigurationResponseDto
        {
            Id = c.Id,
            Key = c.Key,
            Value = c.Value,
            DataType = c.DataType,
            Category = c.Category,
            Description = c.Description,
            IsActive = c.IsActive,
            ModifiedBy = c.ModifiedBy,
            ValidationRule = c.ValidationRule,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Create or update configuration
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="dto">Configuration details</param>
    /// <response code="200">Configuration updated successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateConfiguration(string key, [FromBody] UpdateConfigurationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new UpdateConfigurationCommand
        {
            Key = key,
            Value = dto.Value,
            DataType = dto.DataType,
            Category = dto.Category,
            Description = dto.Description,
            IsActive = dto.IsActive,
            ModifiedBy = dto.ModifiedBy,
            ValidationRule = dto.ValidationRule
        };

        var configId = await _sender.Send(command);

        _logger.LogInformation("Configuration '{Key}' updated", key);

        return Ok(new { id = configId });
    }

    /// <summary>
    /// Delete configuration
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <response code="204">Configuration deleted successfully</response>
    /// <response code="404">Configuration not found</response>
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConfiguration(string key)
    {
        var command = new DeleteConfigurationCommand { Key = key };
        var result = await _sender.Send(command);

        if (!result)
        {
            return NotFound();
        }

        _logger.LogInformation("Configuration '{Key}' deleted", key);

        return NoContent();
    }

    /// <summary>
    /// Initialize default configuration settings
    /// </summary>
    /// <param name="modifiedBy">Name of the user initializing defaults</param>
    /// <response code="200">Returns count of configurations created</response>
    [HttpPost("initialize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InitializeDefaults([FromQuery] string? modifiedBy = "System")
    {
        var command = new InitializeDefaultConfigurationsCommand { ModifiedBy = modifiedBy };
        var count = await _sender.Send(command);

        _logger.LogInformation("Initialized {Count} default configurations", count);

        return Ok(new { created = count });
    }

    /// <summary>
    /// Get all configuration categories
    /// </summary>
    /// <response code="200">Returns list of categories</response>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var query = new GetConfigurationCategoriesQuery();
        var categories = await _sender.Send(query);

        return Ok(categories);
    }
}

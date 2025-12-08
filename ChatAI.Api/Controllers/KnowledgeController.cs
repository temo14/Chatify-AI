using ChatAI.Api.DTOs.Knowledge;
using ChatAI.Application.Commands;
using ChatAI.Application.Exceptions;
using ChatAI.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatAI.Api.Controllers;

/// <summary>
/// Knowledge management endpoints for RAG (Retrieval-Augmented Generation)
/// Thin controller - delegates all business logic to Application layer via CQRS
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "Admin")]
public class KnowledgeController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<KnowledgeController> _logger;

    public KnowledgeController(ISender sender, ILogger<KnowledgeController> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Batch load existing knowledge documents to Qdrant vector database
    /// </summary>
    /// <response code="200">Returns load statistics</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("load-to-qdrant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoadToQdrant()
    {
        var command = new LoadDocumentsToQdrantCommand();
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Add a new knowledge document with automatic embedding generation
    /// </summary>
    /// <param name="request">Document details</param>
    /// <response code="201">Document created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddDocument([FromBody] AddKnowledgeDocumentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new AddKnowledgeDocumentCommand
        {
            Title = request.Title,
            Content = request.Content,
            Source = request.Source,
            Category = request.Category,
            MetadataJson = request.MetadataJson,
            IsActive = request.IsActive
        };

        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetDocument), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get a specific knowledge document by ID
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <response code="200">Document found</response>
    /// <response code="404">Document not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        var query = new GetKnowledgeDocumentQuery { Id = id };
        var result = await _sender.Send(query);
        
        if (result == null)
        {
            throw new NotFoundException($"Document {id} not found");
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all knowledge documents with optional filtering and pagination
    /// </summary>
    /// <param name="onlyActive">Only return active documents</param>
    /// <param name="category">Filter by category</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20, max 100)</param>
    /// <response code="200">Returns paginated document list</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] bool onlyActive = false,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = new GetKnowledgeDocumentsQuery
        {
            OnlyActive = onlyActive,
            Category = category
        };

        var allDocuments = await _sender.Send(query);
        var totalCount = allDocuments.Count();

        var paginatedDocs = allDocuments
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            data = paginatedDocs
        });
    }

    /// <summary>
    /// Search knowledge documents using semantic similarity
    /// </summary>
    /// <param name="query">Search query text</param>
    /// <param name="limit">Maximum results to return (1-50)</param>
    /// <param name="category">Filter by category (optional)</param>
    /// <response code="200">Returns search results</response>
    /// <response code="400">Invalid search parameters</response>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 5,
        [FromQuery] string? category = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required" });
        }

        if (limit < 1 || limit > 50)
        {
            return BadRequest(new { error = "Limit must be between 1 and 50" });
        }

        var searchQuery = new SearchKnowledgeQuery
        {
            Query = query,
            Limit = limit,
            Category = category
        };

        var result = await _sender.Send(searchQuery);
        return Ok(result);
    }

    /// <summary>
    /// Update an existing knowledge document (regenerates embeddings)
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="request">Updated document details</param>
    /// <response code="200">Document updated successfully</response>
    /// <response code="404">Document not found</response>
    /// <response code="400">Invalid request data</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] AddKnowledgeDocumentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new UpdateKnowledgeDocumentCommand
        {
            Id = id,
            Title = request.Title,
            Content = request.Content,
            Source = request.Source,
            Category = request.Category,
            MetadataJson = request.MetadataJson,
            IsActive = request.IsActive
        };

        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete a knowledge document and its embeddings
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <response code="204">Document deleted successfully</response>
    /// <response code="404">Document not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var command = new DeleteKnowledgeDocumentCommand { Id = id };
        await _sender.Send(command);
        return NoContent();
    }
}

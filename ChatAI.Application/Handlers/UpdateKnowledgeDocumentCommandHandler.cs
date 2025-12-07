using ChatAI.Application.Commands;
using ChatAI.Application.Exceptions;
using ChatAI.Application.Interfaces;
using ChatAI.Application.Models.Response;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Handlers;

public class UpdateKnowledgeDocumentCommandHandler : IRequestHandler<UpdateKnowledgeDocumentCommand, KnowledgeDocumentResponse>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<UpdateKnowledgeDocumentCommandHandler> _logger;

    public UpdateKnowledgeDocumentCommandHandler(
        IKnowledgeRepository repository,
        ILogger<UpdateKnowledgeDocumentCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<KnowledgeDocumentResponse> Handle(UpdateKnowledgeDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating knowledge document {DocumentId}", request.Id);

        var existing = await _repository.GetByIdAsync(request.Id);
        
        if (existing == null)
        {
            throw new NotFoundException($"Knowledge document {request.Id} not found");
        }

        // Update properties
        existing.Title = request.Title;
        existing.Content = request.Content;
        existing.Source = request.Source;
        existing.Category = request.Category;
        existing.MetadataJson = request.MetadataJson;
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        // Repository will regenerate embeddings if content changed
        await _repository.UpdateAsync(existing);

        _logger.LogInformation("✅ Updated knowledge document {DocumentId}: {Title}", request.Id, existing.Title);

        return KnowledgeDocumentResponse.FromEntity(existing);
    }
}

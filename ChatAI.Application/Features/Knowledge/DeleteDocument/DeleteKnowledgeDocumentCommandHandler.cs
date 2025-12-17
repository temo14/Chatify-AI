
using ChatAI.Application.Exceptions;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatAI.Application.Features.Knowledge.DeleteDocument;

public class DeleteKnowledgeDocumentCommandHandler : IRequestHandler<DeleteKnowledgeDocumentCommand, Unit>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ILogger<DeleteKnowledgeDocumentCommandHandler> _logger;

    public DeleteKnowledgeDocumentCommandHandler(
        IKnowledgeRepository repository,
        ILogger<DeleteKnowledgeDocumentCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> Handle(DeleteKnowledgeDocumentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting knowledge document {DocumentId}", request.Id);

        var existing = await _repository.GetByIdAsync(request.Id);
        
        if (existing == null)
        {
            throw new NotFoundException($"Knowledge document {request.Id} not found");
        }

        await _repository.DeleteAsync(request.Id);

        _logger.LogInformation("Deleted knowledge document {DocumentId}: {Title}", request.Id, existing.Title);

        return Unit.Value;
    }
}

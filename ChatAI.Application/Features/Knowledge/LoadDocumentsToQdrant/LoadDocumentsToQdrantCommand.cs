using ChatAI.Domain.Models.Response;
using MediatR;

namespace ChatAI.Application.Features.Knowledge.LoadDocumentsToQdrant;

/// <summary>
/// Command to batch load existing knowledge documents to Qdrant vector database
/// </summary>
public class LoadDocumentsToQdrantCommand : IRequest<LoadToQdrantResponse>
{
}

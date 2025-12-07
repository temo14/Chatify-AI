using ChatAI.Application.Models.Response;
using MediatR;

namespace ChatAI.Application.Commands;

/// <summary>
/// Command to batch load existing knowledge documents to Qdrant vector database
/// </summary>
public class LoadDocumentsToQdrantCommand : IRequest<LoadToQdrantResponse>
{
}

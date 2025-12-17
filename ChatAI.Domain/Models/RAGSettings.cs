namespace ChatAI.Domain.Models;

/// <summary>
/// RAG settings from database configuration
/// </summary>
public class RAGSettings
{
    public bool Enabled { get; set; }
    public int TopKResults { get; set; }
    public double ScoreThreshold { get; set; }
    public int MaxContextLength { get; set; }
    public int DocumentChunkSize { get; set; }
    public int ChunkOverlap { get; set; }
}

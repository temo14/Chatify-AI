namespace ChatAI.Application.Models.Response;

/// <summary>
/// Response from batch loading documents to Qdrant
/// </summary>
public class LoadToQdrantResponse
{
    public int TotalDocuments { get; set; }
    public int LoadedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Message => $"Loaded {LoadedCount} documents, skipped {SkippedCount}, {ErrorCount} errors";
    public bool Success => ErrorCount == 0;
}

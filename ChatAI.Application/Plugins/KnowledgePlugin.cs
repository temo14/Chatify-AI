using ChatAI.Application.Services;
using ChatAI.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;

namespace ChatAI.Application.Plugins;

/// <summary>
/// Semantic Kernel plugin for searching the knowledge base (RAG)
/// Allows AI to retrieve relevant documents to answer user questions
/// </summary>
public class KnowledgePlugin
{
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly ChatContext _chatContext;
    private readonly ILogger<KnowledgePlugin> _logger;

    public KnowledgePlugin(
        IKnowledgeRepository knowledgeRepository,
        ChatContext chatContext,
        ILogger<KnowledgePlugin> logger)
    {
        _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
        _chatContext = chatContext ?? throw new ArgumentNullException(nameof(chatContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Search the company's knowledge base for relevant information
    /// Use this to answer questions about company policies, products, services, or any domain-specific information
    /// </summary>
    /// <param name="query">The search query - should be the user's question or key terms to find relevant information</param>
    /// <param name="topK">Number of most relevant documents to retrieve (default: 5, max: 10)</param>
    /// <returns>Relevant documents from the knowledge base</returns>
    [KernelFunction("search_knowledge")]
    [Description("Search the company's knowledge base to find relevant information. Use this to answer questions about company policies, return policies, products, services, FAQs, or any company-specific information. Always use this when users ask about company information.")]
    public async Task<string> SearchKnowledgeAsync(
        [Description("The search query - user's question or key terms")] string query,
        [Description("Number of documents to retrieve (1-10)")] int topK = 5)
    {
        try
        {
            _logger.LogInformation("🔍 [{Context}] TOOL CALLED: search_knowledge | Query: {Query} | TopK: {TopK}", 
                _chatContext.GetContextInfo(), query, topK);

            // Clamp topK to reasonable limits
            topK = Math.Clamp(topK, 1, 10);

            // Search the knowledge base
            var documents = await _knowledgeRepository.SearchAsync(query, topK);
            var docList = documents.ToList();

            if (!docList.Any())
            {
                _logger.LogWarning("⚠️ [{Context}] No documents found for query: {Query}", 
                    _chatContext.GetContextInfo(), query);
                return "No relevant information found in the knowledge base.";
            }

            // Format the results
            var result = new StringBuilder();
            result.AppendLine($"Found {docList.Count} relevant document(s):");
            result.AppendLine();

            for (int i = 0; i < docList.Count; i++)
            {
                var doc = docList[i];
                result.AppendLine($"--- Document {i + 1}: {doc.Title} ---");
                
                if (!string.IsNullOrEmpty(doc.Category))
                {
                    result.AppendLine($"Category: {doc.Category}");
                }
                
                if (!string.IsNullOrEmpty(doc.Source))
                {
                    result.AppendLine($"Source: {doc.Source}");
                }
                
                result.AppendLine();
                result.AppendLine(doc.Content);
                result.AppendLine();
            }

            _logger.LogInformation("✅ [{Context}] Retrieved {Count} documents successfully", 
                _chatContext.GetContextInfo(), docList.Count);

            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [{Context}] Error searching knowledge base: {Error}", 
                _chatContext.GetContextInfo(), ex.Message);
            return $"Error searching knowledge base: {ex.Message}";
        }
    }

    /// <summary>
    /// Get documents by category from the knowledge base
    /// Use this when users ask for information from a specific category (e.g., "FAQ", "Product", "Policy")
    /// </summary>
    /// <param name="category">The category to filter by (e.g., "FAQ", "Product", "Policy", "Technical")</param>
    /// <returns>All documents in the specified category</returns>
    [KernelFunction("get_documents_by_category")]
    [Description("Get all documents from a specific category in the knowledge base. Use this when users want to browse a specific category like FAQs, policies, or product information.")]
    public async Task<string> GetDocumentsByCategoryAsync(
        [Description("The category name (e.g., 'FAQ', 'Product', 'Policy')")] string category)
    {
        try
        {
            _logger.LogInformation("📁 [{Context}] TOOL CALLED: get_documents_by_category | Category: {Category}", 
                _chatContext.GetContextInfo(), category);

            var documents = await _knowledgeRepository.GetByCategoryAsync(category);
            var docList = documents.ToList();

            if (!docList.Any())
            {
                _logger.LogWarning("⚠️ [{Context}] No documents found in category: {Category}", 
                    _chatContext.GetContextInfo(), category);
                return $"No documents found in category '{category}'.";
            }

            // Format the results
            var result = new StringBuilder();
            result.AppendLine($"Found {docList.Count} document(s) in category '{category}':");
            result.AppendLine();

            foreach (var doc in docList)
            {
                result.AppendLine($"--- {doc.Title} ---");
                result.AppendLine(doc.Content);
                result.AppendLine();
            }

            _logger.LogInformation("✅ [{Context}] Retrieved {Count} documents from category {Category}", 
                _chatContext.GetContextInfo(), docList.Count, category);

            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [{Context}] Error getting documents by category: {Error}", 
                _chatContext.GetContextInfo(), ex.Message);
            return $"Error getting documents: {ex.Message}";
        }
    }
}

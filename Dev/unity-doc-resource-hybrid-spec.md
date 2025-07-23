[McpServerResourceType]
public class UnityDocumentationResource
{
    private readonly UnityInstallationService _installationService;
    private readonly IDocumentationIndexer _indexer;
    private readonly ILogger<UnityDocumentationResource> _logger;

    // 1. DISCOVERY - Let AI explore the documentation structure
    [McpServerResource(Name = "unity_docs_index")]
    public Task<ResourceResult> GetDocumentationIndex(
        [Description("The project path to resolve Unity version")] string projectPath,
        [Description("Optional filter by type (class, method, namespace, etc.)")] string? type = null,
        [Description("Optional namespace filter")] string? namespaceFilter = null)
    {
        var index = _indexer.GetDocumentationIndex(projectPath, type, namespaceFilter);
        var jsonContent = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
        
        return Task.FromResult(ResourceResult.Success(
            new ResourceContent(jsonContent, "application/json")));
    }

    // 2. SEARCH - AI-friendly search across documentation
    [McpServerResource(Name = "unity_docs_search")]
    public Task<ResourceResult> SearchDocumentation(
        [Description("The project path to resolve Unity version")] string projectPath,
        [Description("Search query (class name, method, concept, etc.)")] string query,
        [Description("Maximum results to return")] int maxResults = 10,
        [Description("Include content snippets in results")] bool includeSnippets = true)
    {
        var results = _indexer.SearchDocumentation(projectPath, query, maxResults, includeSnippets);
        var jsonContent = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        
        return Task.FromResult(ResourceResult.Success(
            new ResourceContent(jsonContent, "application/json")));
    }

    // 3. CONTENT RETRIEVAL - Structured, AI-friendly content
    [McpServerResource(Name = "unity_docs_content")]
    public Task<ResourceResult> GetDocumentationContent(
        [Description("The project path to resolve Unity version")] string projectPath,
        [Description("The relative path to the documentation")] string relativePath,
        [Description("Return format: 'structured', 'markdown', or 'html'")] string format = "structured")
    {
        try
        {
            var docRoot = _installationService.GetDocumentationPath(projectPath);
            var fullPath = Path.GetFullPath(Path.Combine(docRoot, relativePath));
            
            if (!fullPath.StartsWith(Path.GetFullPath(docRoot)))
                return Task.FromResult(ResourceResult.Error(403, "Forbidden path."));
            
            if (!File.Exists(fullPath))
                return Task.FromResult(ResourceResult.NotFound());

            var content = format.ToLower() switch
            {
                "structured" => _indexer.ParseToStructuredData(fullPath),
                "markdown" => _indexer.ConvertToMarkdown(fullPath), 
                "html" => File.ReadAllText(fullPath),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };

            var contentType = format.ToLower() switch
            {
                "structured" => "application/json",
                "markdown" => "text/markdown",
                "html" => "text/html",
                _ => "text/plain"
            };

            return Task.FromResult(ResourceResult.Success(
                new ResourceContent(content, contentType)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documentation content");
            return Task.FromResult(ResourceResult.Error(500, ex.Message));
        }
    }

    // 4. BATCH RETRIEVAL - For AI agents that want multiple pages
    [McpServerResource(Name = "unity_docs_batch")]
    public Task<ResourceResult> GetDocumentationBatch(
        [Description("The project path to resolve Unity version")] string projectPath,
        [Description("Array of relative paths to retrieve")] string[] relativePaths,
        [Description("Return format for all documents")] string format = "structured")
    {
        var results = new List<BatchDocumentResult>();
        
        foreach (var path in relativePaths.Take(20)) // Limit batch size
        {
            try
            {
                var content = GetDocumentationContent(projectPath, path, format).Result;
                results.Add(new BatchDocumentResult 
                { 
                    Path = path, 
                    Success = content.IsSuccess,
                    Content = content.IsSuccess ? content.Content : null,
                    Error = content.IsSuccess ? null : content.Error
                });
            }
            catch (Exception ex)
            {
                results.Add(new BatchDocumentResult 
                { 
                    Path = path, 
                    Success = false, 
                    Error = ex.Message 
                });
            }
        }

        var jsonContent = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(ResourceResult.Success(
            new ResourceContent(jsonContent, "application/json")));
    }
}

// Supporting data structures
public class DocumentationIndex
{
    public string UnityVersion { get; set; }
    public List<DocumentationEntry> Entries { get; set; }
    public Dictionary<string, List<string>> NamespaceMap { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class DocumentationEntry
{
    public string Title { get; set; }
    public string RelativePath { get; set; }
    public string Type { get; set; }
    public string Namespace { get; set; }
    public string Description { get; set; }
    public List<string> Keywords { get; set; }
}

public class SearchResult
{
    public List<DocumentationMatch> Matches { get; set; }
    public int TotalCount { get; set; }
    public string Query { get; set; }
}

public class DocumentationMatch
{
    public DocumentationEntry Entry { get; set; }
    public double Score { get; set; }
    public List<string> Snippets { get; set; }
}

public class BatchDocumentResult
{
    public string Path { get; set; }
    public bool Success { get; set; }
    public string Content { get; set; }
    public string Error { get; set; }
}

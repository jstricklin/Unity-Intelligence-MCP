using System.Numerics;
using Microsoft.Extensions.AI;

public class SemanticUnityDocumentationService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IVectorDatabase _vectorDb;
    private readonly IDocumentChunker _chunker;
    private readonly ILogger<SemanticUnityDocumentationService> _logger;

    public SemanticUnityDocumentationService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IVectorDatabase vectorDb,
        IDocumentChunker chunker,
        ILogger<SemanticUnityDocumentationService> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _vectorDb = vectorDb;
        _chunker = chunker;
        _logger = logger;
    }

    // 1. INDEXING PROCESS - Run this when documentation changes
    public async Task IndexDocumentationAsync(string projectPath)
    {
        var docRoot = GetDocumentationPath(projectPath);
        var htmlFiles = Directory.GetFiles(docRoot, "*.html", SearchOption.AllDirectories);

        foreach (var filePath in htmlFiles)
        {
            await IndexSingleDocumentAsync(filePath);
        }
    }

    private async Task IndexSingleDocumentAsync(string filePath)
    {
        try
        {
            // Step 1: Parse and extract meaningful content
            var docContent = await ParseDocumentAsync(filePath);
            
            // Step 2: Split into chunks (important for large docs)
            var chunks = _chunker.ChunkDocument(docContent);
            
            // Step 3: Generate embeddings for each chunk
            foreach (var chunk in chunks)
            {
                var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(chunk.Text);
                
                var vectorRecord = new DocumentVector
                {
                    Id = Guid.NewGuid().ToString(),
                    FilePath = filePath,
                    ChunkIndex = chunk.Index,
                    Text = chunk.Text,
                    Embedding = embedding.Vector.ToArray(),
                    Metadata = new DocumentMetadata
                    {
                        Title = docContent.Title,
                        Type = docContent.Type,
                        Namespace = docContent.Namespace,
                        UnityVersion = docContent.UnityVersion,
                        Keywords = ExtractKeywords(chunk.Text),
                        Section = chunk.Section // e.g., "Description", "Parameters", "Example"
                    }
                };
                
                await _vectorDb.UpsertAsync(vectorRecord);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document: {FilePath}", filePath);
        }
    }

    // 2. SEMANTIC SEARCH - This is where the magic happens
    public async Task<SemanticSearchResult> SearchAsync(
        string query, 
        int maxResults = 10,
        double similarityThreshold = 0.7,
        string? typeFilter = null,
        string? namespaceFilter = null)
    {
        // Step 1: Convert query to embedding
        var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query);
        
        // Step 2: Vector similarity search
        var searchRequest = new VectorSearchRequest
        {
            QueryVector = queryEmbedding.Vector.ToArray(),
            MaxResults = maxResults * 2, // Get more results for filtering
            SimilarityThreshold = similarityThreshold,
            Filters = BuildFilters(typeFilter, namespaceFilter)
        };
        
        var vectorResults = await _vectorDb.SearchAsync(searchRequest);
        
        // Step 3: Re-rank and group results
        var groupedResults = GroupAndRankResults(vectorResults, query);
        
        return new SemanticSearchResult
        {
            Query = query,
            Results = groupedResults.Take(maxResults).ToList(),
            TotalMatches = vectorResults.Count,
            SearchTime = DateTime.UtcNow
        };
    }

    // 3. HYBRID SEARCH - Combines semantic + keyword search
    public async Task<SemanticSearchResult> HybridSearchAsync(
        string query,
        int maxResults = 10)
    {
        // Run both semantic and keyword searches in parallel
        var semanticTask = SearchAsync(query, maxResults);
        var keywordTask = KeywordSearchAsync(query, maxResults);
        
        await Task.WhenAll(semanticTask, keywordTask);
        
        var semanticResults = await semanticTask;
        var keywordResults = await keywordTask;
        
        // Merge and re-rank results using reciprocal rank fusion
        return MergeSearchResults(semanticResults, keywordResults, maxResults);
    }

    // 4. CONTEXTUAL RECOMMENDATIONS - "Related Documentation"
    public async Task<List<DocumentChunk>> GetRelatedDocumentationAsync(
        string currentDocPath,
        int maxResults = 5)
    {
        // Get embedding for current document
        var currentDoc = await ParseDocumentAsync(currentDocPath);
        var currentEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(
            $"{currentDoc.Title} {currentDoc.Description}");
        
        // Find similar documents
        var searchRequest = new VectorSearchRequest
        {
            QueryVector = currentEmbedding.Vector.ToArray(),
            MaxResults = maxResults,
            SimilarityThreshold = 0.6,
            ExcludeIds = new[] { GetDocumentId(currentDocPath) } // Don't include self
        };
        
        var results = await _vectorDb.SearchAsync(searchRequest);
        return results.Select(MapToDocumentChunk).ToList();
    }

    // 5. QUESTION ANSWERING - AI can ask natural questions
    public async Task<DocumentationAnswer> AnswerQuestionAsync(string question)
    {
        // Step 1: Find relevant documentation chunks
        var relevantChunks = await SearchAsync(question, maxResults: 5);
        
        // Step 2: Combine chunks as context
        var context = string.Join("\n\n", relevantChunks.Results.Select(r => r.Text));
        
        // Step 3: Generate answer using context (this would use an LLM)
        var answer = await GenerateAnswerFromContext(question, context);
        
        return new DocumentationAnswer
        {
            Question = question,
            Answer = answer,
            SourceChunks = relevantChunks.Results,
            Confidence = CalculateConfidence(relevantChunks.Results)
        };
    }

    private List<DocumentGroup> GroupAndRankResults(List<VectorSearchResult> vectorResults, string query)
    {
        // Group by document and combine chunk scores
        return vectorResults
            .GroupBy(r => r.Metadata.FilePath)
            .Select(g => new DocumentGroup
            {
                FilePath = g.Key,
                Title = g.First().Metadata.Title,
                TotalScore = g.Sum(x => x.Similarity),
                BestChunks = g.OrderByDescending(x => x.Similarity).Take(3).ToList(),
                RelevantSections = g.Select(x => x.Metadata.Section).Distinct().ToList()
            })
            .OrderByDescending(d => d.TotalScore)
            .ToList();
    }

    private async Task<ParsedDocument> ParseDocumentAsync(string filePath)
    {
        var html = await File.ReadAllTextAsync(filePath);
        
        // Parse Unity documentation structure
        // Unity docs have specific patterns we can extract
        return new ParsedDocument
        {
            FilePath = filePath,
            Title = ExtractTitle(html),
            Description = ExtractDescription(html),
            Type = DetermineDocumentationType(html),
            Namespace = ExtractNamespace(html),
            UnityVersion = ExtractUnityVersion(html),
            MainContent = ExtractMainContent(html),
            CodeExamples = ExtractCodeExamples(html),
            Parameters = ExtractParameters(html)
        };
    }
}

// Data structures for semantic search
public class DocumentVector
{
    public string Id { get; set; }
    public string FilePath { get; set; }
    public int ChunkIndex { get; set; }
    public string Text { get; set; }
    public float[] Embedding { get; set; }
    public DocumentMetadata Metadata { get; set; }
}

public class DocumentChunk
{
    public int Index { get; set; }
    public string Text { get; set; }
    public string Section { get; set; } // "Description", "Parameters", "Example", etc.
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}

public class SemanticSearchResult
{
    public string Query { get; set; }
    public List<DocumentGroup> Results { get; set; }
    public int TotalMatches { get; set; }
    public DateTime SearchTime { get; set; }
}

public class DocumentGroup
{
    public string FilePath { get; set; }
    public string Title { get; set; }
    public double TotalScore { get; set; }
    public List<VectorSearchResult> BestChunks { get; set; }
    public List<string> RelevantSections { get; set; }
}

public class DocumentationAnswer
{
    public string Question { get; set; }
    public string Answer { get; set; }
    public List<DocumentGroup> SourceChunks { get; set; }
    public double Confidence { get; set; }
}

// MCP Resource integration
[McpServerResource(Name = "unity_docs_semantic_search")]
public async Task<ResourceResult> SemanticSearchResource(
    [Description("Natural language query")] string query,
    [Description("Maximum results")] int maxResults = 10,
    [Description("Document type filter")] string? typeFilter = null)
{
    var results = await _semanticService.SearchAsync(query, maxResults, typeFilter: typeFilter);
    var jsonContent = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
    
    return ResourceResult.Success(new ResourceContent(jsonContent, "application/json"));
}

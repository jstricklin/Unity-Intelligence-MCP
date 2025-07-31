// ============================================================================
// DEPENDENCY INJECTION SETUP
// ============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentationServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register ChromaDB client
        services.AddHttpClient<IChromaDbClient, ChromaDbClient>((serviceProvider, client) =>
        {
            var chromaBaseUrl = configuration.GetConnectionString("ChromaDB") ?? "http://localhost:8000";
            return new ChromaDbClient(client, chromaBaseUrl);
        });
        
        // Register database
        services.AddScoped<IApplicationDatabase, ApplicationDatabase>();
        
        // Register semantic documentation repository
        services.AddScoped<ISemanticDocumentationRepository, SemanticDocumentationRepository>();
        
        // Register documentation service
        services.AddScoped<DocumentationService>();
        
        return services;
    }
}

// ============================================================================
// CONFIGURATION
// ============================================================================

// appsettings.json
/*
{
  "ConnectionStrings": {
    "DuckDB": "path/to/your/database.db",
    "ChromaDB": "http://localhost:8000"
  },
  "Documentation": {
    "BatchSize": 100,
    "MaxSearchResults": 50,
    "DefaultUnityVersion": "2023.3"
  }
}
*/

// ============================================================================
// COMPLETE WORKFLOW EXAMPLE
// ============================================================================

public class DocumentationWorkflow
{
    private readonly ISemanticDocumentationRepository _repository;
    private readonly DocumentationService _service;
    
    public DocumentationWorkflow(
        ISemanticDocumentationRepository repository,
        DocumentationService service)
    {
        _repository = repository;
        _service = service;
    }
    
    /// <summary>
    /// Complete workflow: Insert documents into DuckDB and index in ChromaDB
    /// </summary>
    public async Task ProcessDocumentationBatchAsync(
        IReadOnlyList<SemanticDocumentRecord> records,
        string unityVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"Processing {records.Count} documents for Unity {unityVersion}...");
            
            // 1. Insert into DuckDB (your existing bulk insert)
            await _repository.InsertDocumentsInBulkAsync(records, cancellationToken);
            
            // 2. Index in ChromaDB for semantic search
            await _repository.IndexDocumentsAsync(unityVersion, cancellationToken);
            
            Console.WriteLine($"Successfully processed {records.Count} documents");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Workflow failed: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Update workflow: Replace existing version
    /// </summary>
    public async Task UpdateVersionAsync(
        IReadOnlyList<SemanticDocumentRecord> records,
        string unityVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"Updating Unity {unityVersion} documentation...");
            
            // 1. Delete existing data (both DuckDB and ChromaDB)
            await _repository.DeleteDocsByVersionAsync(unityVersion, cancellationToken);
            
            // 2. Insert new data
            await ProcessDocumentationBatchAsync(records, unityVersion, cancellationToken);
            
            Console.WriteLine($"Successfully updated Unity {unityVersion} documentation");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Update failed: {ex.Message}");
            throw;
        }
    }
}

// ============================================================================
// USAGE EXAMPLES
// ============================================================================

public class DocumentationUsageExamples
{
    private readonly DocumentationService _documentationService;
    
    public DocumentationUsageExamples(DocumentationService documentationService)
    {
        _documentationService = documentationService;
    }
    
    /// <summary>
    /// Example 1: Basic semantic search
    /// </summary>
    public async Task BasicSemanticSearchExample()
    {
        var results = await _documentationService.SearchUnityDocumentationAsync(
            query: "How to move a GameObject in Unity?",
            unityVersion: "2023.3",
            maxResults: 5
        );
        
        Console.WriteLine($"Found {results.Count} results:");
        foreach (var result in results)
        {
            Console.WriteLine($"[{result.Distance:F3}] {result.DocumentTitle}");
            Console.WriteLine($"  Type: {result.ElementType}");
            Console.WriteLine($"  Class: {result.ClassName}");
            Console.WriteLine($"  Content: {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
            Console.WriteLine();
        }
    }
    
    /// <summary>
    /// Example 2: Search by specific element type
    /// </summary>
    public async Task SearchByTypeExample()
    {
        // Search only for methods related to physics
        var methodResults = await _documentationService.SearchUnityDocumentationAsync(
            query: "physics collision detection",
            unityVersion: "2023.3",
            elementType: "public_method",
            maxResults: 10
        );
        
        Console.WriteLine("Physics Methods:");
        foreach (var result in methodResults.Take(5))
        {
            Console.WriteLine($"  {result.ClassName}.{result.Content.Split('\n')[0]}");
        }
        
        // Search only for properties related to transform
        var propertyResults = await _documentationService.SearchUnityDocumentationAsync(
            query: "transform position rotation",
            unityVersion: "2023.3",
            elementType: "property",
            maxResults: 10
        );
        
        Console.WriteLine("\nTransform Properties:");
        foreach (var result in propertyResults.Take(5))
        {
            Console.WriteLine($"  {result.ClassName}.{result.Content.Split('\n')[0]}");
        }
    }
    
    /// <summary>
    /// Example 3: Compare search approaches
    /// </summary>
    public async Task CompareSearchApproachesExample()
    {
        var query = "instantiate prefab";
        
        // Semantic search (ChromaDB)
        var semanticResults = await _documentationService.SearchUnityDocumentationAsync(
            query: query,
            unityVersion: "2023.3",
            maxResults: 5
        );
        
        Console.WriteLine("=== SEMANTIC SEARCH RESULTS ===");
        foreach (var result in semanticResults)
        {
            Console.WriteLine($"[{result.Distance:F3}] {result.DocumentTitle}");
            Console.WriteLine($"  {result.Content.Substring(0, Math.Min(80, result.Content.Length))}...");
        }
        
        // You could also implement traditional keyword search in DuckDB for comparison
        Console.WriteLine("\n=== KEYWORD SEARCH RESULTS (for comparison) ===");
        // This would be a separate method using DuckDB's full-text search
        // var keywordResults = await SearchKeywordAsync(query, "2023.3");
    }
    
    /// <summary>
    /// Example 4: Advanced filtering and hybrid search
    /// </summary>
    public async Task HybridSearchExample()
    {
        // Step 1: Use semantic search to find relevant content
        var semanticResults = await _documentationService.SearchUnityDocumentationAsync(
            query: "audio music sound effects",
            unityVersion: "2023.3",
            maxResults: 20
        );
        
        // Step 2: Filter results by specific criteria (could extend this in the repository)
        var audioSystemResults = semanticResults
            .Where(r => r.Namespace?.Contains("Audio") == true || 
                       r.ClassName?.Contains("Audio") == true)
            .Take(10)
            .ToList();
        
        Console.WriteLine("Audio System Results:");

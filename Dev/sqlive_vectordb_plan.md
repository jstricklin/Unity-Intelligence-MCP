# SQLite Multi-Source Documentation Database Implementation Plan

## Database Schema Reference

This implementation plan is built around the **Multi-Source Unity Documentation Schema** defined above, which provides:

- **Universal document storage** with source-specific metadata preservation
- **Multi-level vector embeddings** (document, element, and chunk levels)
- **Cross-source relationship mapping** for intelligent content discovery
- **Backward-compatible views** that preserve existing query patterns
- **Extensible design** supporting additional documentation sources

The schema's key innovation is using JSON metadata storage (`doc_metadata` table) to preserve your existing `UnityDocumentationData` structure while enabling unified semantic search across all documentation types.

**Reference the complete schema code above for:**
- Exact table definitions and relationships
- Vector index configurations  
- Performance optimization indices
- Source-specific view definitions
- Example queries for different use cases

## Phase 1: Database Foundation (Week 1-2)

### 1.1 SQLite Setup
- Install SQLite with vector extension (sqlite-vss or sqlite-vec)
- Configure vector similarity functions (cosine similarity)
- Set embedding dimensions to 768 (compatible with text-embedding-3-small)

### 1.2 Core Schema Implementation
**Reference the complete Multi-Source Unity Documentation Schema created above**, which includes:

```sql
-- Execute schema creation scripts in order:
1. doc_sources table (source registry)
2. unity_docs table (universal document container) 
3. doc_metadata table (source-specific JSON data)
4. content_elements table (flexible element storage)
5. content_chunks table (granular content pieces)
6. doc_relationships table (cross-document references)
7. Vector indices (vec_docs_index, vec_elements_index, vec_chunks_index)
8. Performance indices (idx_docs_source_type, idx_elements_type, etc.)
9. Source-specific views (scripting_api_docs, manual_hierarchy, api_elements)
```

**Key Schema Features:**
- **Multi-source support**: `doc_sources` table registers different documentation types
- **Flexible metadata**: JSON storage in `doc_metadata` preserves your existing `UnityDocumentationData` structure
- **Universal search**: Vector indices enable semantic search across all sources
- **Backward compatibility**: Views maintain existing query patterns for scripting API

### 1.3 Source Registration
```sql
INSERT INTO doc_sources VALUES
('scripting_api', 'Unity Scripting API', '2023.3', '1.0'),
('editor_manual', 'Unity User Manual', '2023.3', '1.0'),
('tutorial', 'Unity Learn Tutorials', 'current', '1.0');
```

## Phase 2: Data Model Integration (Week 2-3)

### 2.1 Interface Design
```csharp
public interface IDocumentationSource
{
    string SourceType { get; }
    Task<UniversalDocumentRecord> ToUniversalRecord(IEmbeddingService embeddings);
}
```

### 2.2 Extend Existing Scripting API Class
```csharp
public class ScriptingApiDocumentation : UnityDocumentationData, IDocumentationSource
{
    public string SourceType => "scripting_api";
    
    public async Task<UniversalDocumentRecord> ToUniversalRecord(IEmbeddingService embeddings)
    {
        // Convert existing structured data to universal format
        // Store UnityDocumentationData as JSON metadata
        // Generate contextual embeddings for class, methods, properties
    }
}
```

### 2.3 Data Access Layer
```csharp
public class DocumentationRepository
{
    Task<int> InsertDocument(UniversalDocumentRecord record);
    Task<int> InsertElements(List<ContentElement> elements);
    Task<List<SearchResult>> SemanticSearch(float[] embedding, string sourceType = null);
    Task<List<SearchResult>> HybridSearch(string query, Dictionary<string, object> filters);
}
```

## Phase 3: Embedding Pipeline (Week 3-4)

### 3.1 Embedding Service Integration
```csharp
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text);
    Task<List<float[]>> EmbedBatchAsync(List<string> texts);
}
```

### 3.2 Contextual Embedding Generation
- **Document Level**: `"Unity {ClassName} class: {Title}. {Description}"`
- **Element Level**: `"Unity {ClassName}.{ElementName} {ElementType}: {Description}"`
- **Chunk Level**: Include surrounding context for better semantic understanding

### 3.3 Batch Processing Pipeline
```csharp
public class EmbeddingPipeline
{
    Task ProcessDocumentationSource(IDocumentationSource source);
    Task UpdateEmbeddings(List<string> changedDocuments);
    Task<ProcessingStats> BatchProcess(List<IDocumentationSource> sources);
}
```

## Phase 4: Query System (Week 4-5)

### 4.1 Basic Semantic Search
```csharp
public async Task<List<SearchResult>> SearchSemantic(string query, SearchOptions options)
{
    var queryEmbedding = await _embeddings.EmbedAsync(query);
    
    var sql = @"
        SELECT d.title, d.doc_type, s.source_name, 
               vss_search(v.embedding, ?) as similarity
        FROM unity_docs d
        JOIN doc_sources s ON d.source_id = s.id
        JOIN vec_docs_index v ON d.rowid = v.rowid
        WHERE vss_search(v.embedding, ?) > ?
        ORDER BY similarity DESC
        LIMIT ?";
    
    return await ExecuteSearchQuery(sql, queryEmbedding, options.Threshold, options.Limit);
}
```

### 4.2 Source-Specific Queries
**Using the views defined in the schema above:**

```csharp
// Use scripting_api_docs view (maintains compatibility with existing queries)
public async Task<List<ApiElement>> SearchScriptingApi(string query)
{
    var sql = @"
        SELECT title, class_name, description, namespace
        FROM scripting_api_docs 
        WHERE title LIKE ? OR description LIKE ?";
    // Maintains existing query patterns while using new schema
}

// Use manual_hierarchy view for structured manual content
public async Task<List<ManualSection>> SearchManual(string query, string category = null)
{
    var sql = @"
        SELECT title, category, section_type, difficulty_level
        FROM manual_hierarchy 
        WHERE category = ? AND title_embedding semantically matches ?";
}

// Use api_elements view for element-level searches
public async Task<List<ElementResult>> SearchApiElements(string query)
{
    var sql = @"
        SELECT title, class_name, element_type, is_inherited
        FROM api_elements ae
        JOIN vec_elements_index v ON ae.rowid = v.rowid
        WHERE vss_search(v.embedding, ?) > 0.75";
}
```

### 4.3 Cross-Source Relationships
```csharp
public async Task BuildCrossReferences()
{
    // Find API classes referenced in manual sections
    // Create relationship records for enhanced context
}
```

## Phase 5: Smart Routing (Week 5-6)

### 5.1 Query Classification
```csharp
public class QueryClassifier
{
    Task<QueryClassification> ClassifyQuery(string query, ConversationContext context);
    // Pattern matching + lightweight semantic classification
}
```

### 5.2 Adaptive Search Strategy
```csharp
public class SmartDocumentationRouter
{
    // High confidence: Single source
    // Medium confidence: Primary + secondary sources  
    // Low confidence: Comprehensive search with balancing
}
```

## Phase 6: Performance Optimization (Week 6-7)

### 6.1 Index Optimization
- Tune vector similarity indices for query patterns
- Add composite indices for common filter combinations
- Implement query result caching

### 6.2 Batch Operations
- Bulk embedding updates for documentation changes
- Efficient cross-reference rebuilding
- Connection pooling for concurrent access

## Phase 7: API Integration (Week 7-8)

### 7.1 REST API Endpoints
```csharp
[ApiController]
public class DocumentationController
{
    [HttpPost("search/semantic")]
    Task<SearchResponse> SemanticSearch(SemanticSearchRequest request);
    
    [HttpPost("search/hybrid")]  
    Task<SearchResponse> HybridSearch(HybridSearchRequest request);
    
    [HttpGet("sources/{sourceType}/browse")]
    Task<BrowseResponse> BrowseSource(string sourceType, BrowseOptions options);
}
```

### 7.2 LLM Integration Points
```csharp
public class RAGContextBuilder
{
    Task<string> BuildContext(SearchResults results, string queryType);
    Task<List<CrossReference>> FindRelatedContent(List<SearchResult> primary);
}
```

## Implementation Checklist

### Database Setup ✓
- [ ] SQLite + vector extension installed
- [ ] Schema created and tested
- [ ] Sample data inserted and verified
- [ ] Vector indices performance tested

### Data Integration ✓  
- [ ] IDocumentationSource interface implemented
- [ ] Existing UnityDocumentationData extended
- [ ] Manual/tutorial source classes created
- [ ] JSON metadata storage working

### Embedding Pipeline ✓
- [ ] Embedding service integrated
- [ ] Contextual embedding generation implemented
- [ ] Batch processing pipeline created
- [ ] Update mechanisms in place

### Query System ✓
- [ ] Basic semantic search working
- [ ] Source-specific queries implemented
- [ ] Cross-reference system built
- [ ] Performance benchmarks met

### Smart Routing ✓
- [ ] Query classification implemented
- [ ] Adaptive routing strategy working
- [ ] Fallback mechanisms tested
- [ ] Context-aware improvements added

### Production Ready ✓
- [ ] API endpoints implemented
- [ ] Error handling and logging
- [ ] Performance monitoring
- [ ] Documentation and tests

## Success Metrics

### Performance Targets
- **Query Response Time**: <100ms for single-source, <500ms for multi-source
- **Embedding Generation**: <10ms per document chunk
- **Database Size**: Efficiently handle 10K+ documents with minimal storage overhead

### Quality Metrics
- **Semantic Relevance**: >85% user satisfaction with search results
- **Cross-Source Discovery**: Users find relevant content across different source types
- **API Compatibility**: Existing scripting API queries continue to work without modification

### Scalability Goals
- Support incremental updates without full reprocessing
- Handle multiple Unity versions simultaneously
- Extend to additional documentation sources without schema changes

## Risk Mitigation

### Technical Risks
- **Vector Extension Compatibility**: Test sqlite-vss across deployment environments
- **Embedding Model Changes**: Abstract embedding service for easy model swapping
- **Query Performance**: Implement caching and optimize indices proactively

### Data Quality Risks
- **Embedding Quality**: Validate embeddings with known similar/dissimilar pairs
- **Cross-Reference Accuracy**: Manual validation of relationship discovery
- **Source Consistency**: Version management for documentation updates

## Deployment Strategy

### Development Environment
- Local SQLite with sample Unity documentation subset
- Embedding generation using OpenAI API or local model
- Unit tests for all major components

### Staging Environment  
- Full documentation corpus loading and testing
- Performance benchmarking under realistic load
- Integration testing with LLM services

### Production Deployment
- Incremental rollout with existing system fallback
- Monitoring and alerting for query performance
- User feedback collection for continuous improvement

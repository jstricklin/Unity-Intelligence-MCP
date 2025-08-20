// ==== CORE ABSTRACTIONS ====
// These interfaces make everything swappable

public interface IEmbeddingProvider
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    int EmbeddingDimensions { get; }
    string ModelName { get; }
    bool IsLocal { get; }
    Task InitializeAsync();
}

public interface IVectorDatabase
{
    Task<string> StoreAsync(DocumentVector vector);
    Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request);
    Task<bool> DeleteAsync(string id);
    Task<long> GetCountAsync();
    Task OptimizeAsync(); // For maintenance
}

public interface IDocumentParser
{
    Task<ParsedDocument> ParseAsync(string filePath);
    bool CanParse(string filePath);
    string[] SupportedExtensions { get; }
}

public interface IDocumentChunker
{
    List<DocumentChunk> ChunkDocument(ParsedDocument document);
    int MaxChunkSize { get; set; }
    int ChunkOverlap { get; set; }
}

public interface ISearchRanker
{
    List<RankedResult> RankResults(List<VectorSearchResult> semanticResults, 
                                  List<KeywordSearchResult> keywordResults, 
                                  string query);
}

// ==== EMBEDDING PROVIDERS ====
// Easy to swap between different embedding approaches

public class LocalSentenceTransformerProvider : IEmbeddingProvider
{
    private readonly string _modelName;
    private InferenceSession _session;
    private Tokenizer _tokenizer;

    public LocalSentenceTransformerProvider(string modelName = "all-MiniLM-L6-v2")
    {
        _modelName = modelName;
    }

    public string ModelName => _modelName;
    public bool IsLocal => true;
    public int EmbeddingDimensions => _modelName switch
    {
        "all-MiniLM-L6-v2" => 384,
        "all-MiniLM-L12-v2" => 384,
        "all-mpnet-base-v2" => 768,
        _ => 384
    };

    public async Task InitializeAsync()
    {
        var modelPath = await DownloadModelIfNeeded(_modelName);
        _session = new InferenceSession(modelPath);
        _tokenizer = new Tokenizer(_modelName);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var tokens = _tokenizer.Encode(text);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", tokens.InputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", tokens.AttentionMask)
        };

        using var results = _session.Run(inputs);
        var embeddings = results.First().AsTensor<float>();
        return MeanPooling(embeddings, tokens.AttentionMask);
    }
}

public class OpenAIEmbeddingProvider : IEmbeddingProvider
{
    private readonly OpenAIClient _client;
    private readonly string _model;

    public OpenAIEmbeddingProvider(string apiKey, string model = "text-embedding-3-small")
    {
        _client = new OpenAIClient(apiKey);
        _model = model;
    }

    public string ModelName => _model;
    public bool IsLocal => false;
    public int EmbeddingDimensions => _model switch
    {
        "text-embedding-3-small" => 1536,
        "text-embedding-3-large" => 3072,
        "text-embedding-ada-002" => 1536,
        _ => 1536
    };

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var response = await _client.GetEmbeddingsAsync(_model, new[] { text });
        return response.Value.Data[0].Embedding.ToArray();
    }
}

// ==== VECTOR DATABASES ====
// Start with SQLite, upgrade to specialized DBs later

public class SQLiteVectorDatabase : IVectorDatabase
{
    private readonly string _connectionString;
    private readonly IEmbeddingProvider _embeddingProvider;

    public SQLiteVectorDatabase(string dbPath, IEmbeddingProvider embeddingProvider)
    {
        _connectionString = $"Data Source={dbPath}";
        _embeddingProvider = embeddingProvider;
        InitializeSchema();
    }

    public async Task<string> StoreAsync(DocumentVector vector)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        var id = Guid.NewGuid().ToString();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO document_vectors (id, file_path, chunk_index, content, embedding, metadata)
            VALUES (@id, @filePath, @chunkIndex, @content, @embedding, @metadata)";

        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("filePath", vector.FilePath);
        command.Parameters.AddWithValue("chunkIndex", vector.ChunkIndex);
        command.Parameters.AddWithValue("content", vector.Text);
        command.Parameters.AddWithValue("embedding", SerializeEmbedding(vector.Embedding));
        command.Parameters.AddWithValue("metadata", JsonSerializer.Serialize(vector.Metadata));

        await command.ExecuteNonQueryAsync();
        return id;
    }

    public async Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request)
    {
        // Simplified SQLite vector search
        // For production, consider sqlite-vss extension
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        var results = new List<VectorSearchResult>();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, file_path, chunk_index, content, embedding, metadata
            FROM document_vectors
            LIMIT 1000"; // Pre-filter for performance

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var embedding = DeserializeEmbedding((byte[])reader["embedding"]);
            var similarity = CosineSimilarity(request.QueryVector, embedding);

            if (similarity >= request.SimilarityThreshold)
            {
                results.Add(new VectorSearchResult
                {
                    Id = reader.GetString("id"),
                    FilePath = reader.GetString("file_path"),
                    ChunkIndex = reader.GetInt32("chunk_index"),
                    Content = reader.GetString("content"),
                    Similarity = similarity,
                    Metadata = JsonSerializer.Deserialize<DocumentMetadata>(reader.GetString("metadata"))
                });
            }
        }

        return results.OrderByDescending(r => r.Similarity).Take(request.MaxResults).ToList();
    }

    // Other IVectorDatabase methods...
}

public class PineconeVectorDatabase : IVectorDatabase
{
    private readonly PineconeClient _client;
    private readonly string _indexName;

    public PineconeVectorDatabase(string apiKey, string indexName)
    {
        _client = new PineconeClient(apiKey);
        _indexName = indexName;
    }

    public async Task<string> StoreAsync(DocumentVector vector)
    {
        var id = Guid.NewGuid().ToString();
        
        await _client.UpsertAsync(_indexName, new[]
        {
            new Vector
            {
                Id = id,
                Values = vector.Embedding,
                Metadata = new Dictionary<string, object>
                {
                    ["file_path"] = vector.FilePath,
                    ["chunk_index"] = vector.ChunkIndex,
                    ["content"] = vector.Text,
                    ["metadata"] = JsonSerializer.Serialize(vector.Metadata)
                }
            }
        });

        return id;
    }

    public async Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request)
    {
        var response = await _client.QueryAsync(_indexName, new QueryRequest
        {
            Vector = request.QueryVector,
            TopK = request.MaxResults,
            IncludeMetadata = true
        });

        return response.Matches.Select(match => new VectorSearchResult
        {
            Id = match.Id,
            FilePath = match.Metadata["file_path"].ToString(),
            ChunkIndex = (int)match.Metadata["chunk_index"],
            Content = match.Metadata["content"].ToString(),
            Similarity = match.Score,
            Metadata = JsonSerializer.Deserialize<DocumentMetadata>(match.Metadata["metadata"].ToString())
        }).ToList();
    }

    // Other methods...
}

// ==== CONFIGURATION-DRIVEN SETUP ====
// This is where the magic happens - everything is swappable via config

public class DocumentationServiceFactory
{
    public static IUnityDocumentationService Create(DocumentationConfig config)
    {
        // Create embedding provider
        var embeddingProvider = config.EmbeddingProvider.Type switch
        {
            "local" => new LocalSentenceTransformerProvider(config.EmbeddingProvider.Model),
            "openai" => new OpenAIEmbeddingProvider(config.EmbeddingProvider.ApiKey, config.EmbeddingProvider.Model),
            "azure" => new AzureOpenAIEmbeddingProvider(config.EmbeddingProvider.Endpoint, config.EmbeddingProvider.ApiKey),
            "cohere" => new CohereEmbeddingProvider(config.EmbeddingProvider.ApiKey),
            _ => throw new ArgumentException($"Unknown embedding provider: {config.EmbeddingProvider.Type}")
        };

        // Create vector database
        var vectorDb = config.VectorDatabase.Type switch
        {
            "sqlite" => new SQLiteVectorDatabase(config.VectorDatabase.ConnectionString, embeddingProvider),
            "pinecone" => new PineconeVectorDatabase(config.VectorDatabase.ApiKey, config.VectorDatabase.IndexName),
            "weaviate" => new WeaviateVectorDatabase(config.VectorDatabase.Endpoint, config.VectorDatabase.ApiKey),
            "chroma" => new ChromaVectorDatabase(config.VectorDatabase.Endpoint),
            _ => throw new ArgumentException($"Unknown vector database: {config.VectorDatabase.Type}")
        };

        // Create document parser
        var parser = config.DocumentParser.Type switch
        {
            "unity" => new UnityHtmlParser(),
            "generic" => new GenericHtmlParser(),
            "markdown" => new MarkdownParser(),
            _ => new UnityHtmlParser() // Default
        };

        // Create chunker
        var chunker = new ConfigurableDocumentChunker
        {
            MaxChunkSize = config.Chunking.MaxSize,
            ChunkOverlap = config.Chunking.Overlap,
            ChunkingStrategy = config.Chunking.Strategy
        };

        // Create search ranker
        var ranker = config.SearchRanking.Algorithm switch
        {
            "rrf" => new ReciprocalRankFusionRanker(),
            "weighted" => new WeightedRanker(config.SearchRanking.SemanticWeight, config.SearchRanking.KeywordWeight),
            "simple" => new SimpleRanker(),
            _ => new ReciprocalRankFusionRanker()
        };

        return new UnityDocumentationService(embeddingProvider, vectorDb, parser, chunker, ranker);
    }
}

// ==== CONFIGURATION MODEL ====
public class DocumentationConfig
{
    public EmbeddingProviderConfig EmbeddingProvider { get; set; }
    public VectorDatabaseConfig VectorDatabase { get; set; }
    public DocumentParserConfig DocumentParser { get; set; }
    public ChunkingConfig Chunking { get; set; }
    public SearchRankingConfig SearchRanking { get; set; }
}

public class EmbeddingProviderConfig
{
    public string Type { get; set; } // "local", "openai", "azure", "cohere"
    public string Model { get; set; }
    public string ApiKey { get; set; }
    public string Endpoint { get; set; }
}

public class VectorDatabaseConfig
{
    public string Type { get; set; } // "sqlite", "pinecone", "weaviate", "chroma"
    public string ConnectionString { get; set; }
    public string ApiKey { get; set; }
    public string IndexName { get; set; }
    public string Endpoint { get; set; }
}

// ==== MAIN SERVICE ====
public class UnityDocumentationService : IUnityDocumentationService
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorDatabase _vectorDb;
    private readonly IDocumentParser _parser;
    private readonly IDocumentChunker _chunker;
    private readonly ISearchRanker _ranker;

    public UnityDocumentationService(
        IEmbeddingProvider embeddingProvider,
        IVectorDatabase vectorDb,
        IDocumentParser parser,
        IDocumentChunker chunker,
        ISearchRanker ranker)
    {
        _embeddingProvider = embeddingProvider;
        _vectorDb = vectorDb;
        _parser = parser;
        _chunker = chunker;
        _ranker = ranker;
    }

    public async Task IndexDocumentationAsync(string docsPath)
    {
        await _embeddingProvider.InitializeAsync();
        
        var files = Directory.GetFiles(docsPath, "*.*", SearchOption.AllDirectories)
            .Where(_parser.CanParse);

        foreach (var file in files)
        {
            var parsed = await _parser.ParseAsync(file);
            var chunks = _chunker.ChunkDocument(parsed);

            foreach (var chunk in chunks)
            {
                var embedding = await _embeddingProvider.GenerateEmbeddingAsync(chunk.Text);
                
                var vector = new DocumentVector
                {
                    FilePath = file,
                    ChunkIndex = chunk.Index,
                    Text = chunk.Text,
                    Embedding = embedding,
                    Metadata = chunk.Metadata
                };

                await _vectorDb.StoreAsync(vector);
            }
        }
    }

    public async Task<SearchResults> SearchAsync(string query, SearchOptions options = null)
    {
        options ??= new SearchOptions();

        // Generate query embedding
        var queryEmbedding = await _embeddingProvider.GenerateEmbeddingAsync(query);

        // Semantic search
        var semanticResults = await _vectorDb.SearchAsync(new VectorSearchRequest
        {
            QueryVector = queryEmbedding,
            MaxResults = options.MaxResults * 2,
            SimilarityThreshold = options.SimilarityThreshold
        });

        // Keyword search (if supported by vector DB or separate service)
        var keywordResults = await PerformKeywordSearch(query, options);

        // Rank and merge results
        var rankedResults = _ranker.RankResults(semanticResults, keywordResults, query);

        return new SearchResults
        {
            Query = query,
            Results = rankedResults.Take(options.MaxResults).ToList(),
            TotalFound = rankedResults.Count
        };
    }
}

// ==== EXAMPLE CONFIGURATIONS ====
public static class ConfigurationExamples
{
    // Lightweight local setup
    public static DocumentationConfig LocalConfig => new()
    {
        EmbeddingProvider = new()
        {
            Type = "local",
            Model = "all-MiniLM-L6-v2"
        },
        VectorDatabase = new()
        {
            Type = "sqlite",
            ConnectionString = "./unity_docs.db"
        },
        DocumentParser = new() { Type = "unity" },
        Chunking = new()
        {
            MaxSize = 1000,
            Overlap = 100,
            Strategy = "semantic"
        },
        SearchRanking = new()
        {
            Algorithm = "rrf",
            SemanticWeight = 0.7,
            KeywordWeight = 0.3
        }
    };

    // Cloud production setup
    public static DocumentationConfig CloudConfig => new()
    {
        EmbeddingProvider = new()
        {
            Type = "openai",
            Model = "text-embedding-3-small",
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        },
        VectorDatabase = new()
        {
            Type = "pinecone",
            ApiKey = Environment.GetEnvironmentVariable("PINECONE_API_KEY"),
            IndexName = "unity-docs"
        },
        // ... rest same as local
    };
}

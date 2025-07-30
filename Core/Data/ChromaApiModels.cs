using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityIntelligenceMCP.Core.Data
{
    // For /api/v1/collections
    public record CreateCollectionRequest(string Name);

    // For /api/v1/collections/{name}/add
    public record AddEmbeddingsRequest
    {
        [JsonPropertyName("ids")]
        public List<string> Ids { get; init; } = new();

        [JsonPropertyName("embeddings")]
        public List<float[]> Embeddings { get; init; } = new();

        [JsonPropertyName("metadatas")]
        public List<Dictionary<string, object>> Metadatas { get; init; } = new();
    }

    // For /api/v1/collections/{name}/query
    public record QueryRequest
    {
        [JsonPropertyName("query_embeddings")]
        public List<float[]> QueryEmbeddings { get; init; } = new();

        [JsonPropertyName("n_results")]
        public int NResults { get; init; }
    }

    public record QueryResponse
    {
        [JsonPropertyName("ids")]
        public List<List<string>> Ids { get; init; } = new();

        [JsonPropertyName("distances")]
        public List<List<float>> Distances { get; init; } = new();

        [JsonPropertyName("metadatas")]
        public List<List<Dictionary<string, object>>> Metadatas { get; init; } = new();
    }
    
    // For /api/v1/collections/{name}/delete
    public record DeleteRequest(
        [JsonPropertyName("where")]
        Dictionary<string, object> Where
    );
}

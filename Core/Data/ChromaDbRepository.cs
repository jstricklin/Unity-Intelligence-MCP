using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Data
{
    public class ChromaDbRepository : IVectorRepository
    {
        private readonly HttpClient _httpClient;
        private const string CollectionName = "unity_docs";

        public ChromaDbRepository(HttpClient httpClient, ConfigurationService configService)
        {
            var chromaUrl = configService.UnitySettings.ChromaDbUrl ?? "http://localhost:8000";
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(chromaUrl);
        }

        public async Task InitializeAsync()
        {
            try
            {
                var collectionsResponse = await _httpClient.GetAsync("/api/v1/collections");
                if (collectionsResponse.IsSuccessStatusCode)
                {
                    var collections = await collectionsResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
                    if (collections != null && collections.Any(c => c.ContainsKey("name") && c["name"].ToString() == CollectionName))
                    {
                        Console.Error.WriteLine($"[ChromaDB] Collection '{CollectionName}' already exists.");
                        return;
                    }
                }

                Console.Error.WriteLine($"[ChromaDB] Creating collection: {CollectionName}");
                var response = await _httpClient.PostAsJsonAsync("/api/v1/collections", new CreateCollectionRequest(CollectionName));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to initialize ChromaDB collection: {ex.Message}");
            }
        }
        
        public async Task AddEmbeddingsAsync(IEnumerable<VectorRecord> records)
        {
            var request = new AddEmbeddingsRequest
            {
                Ids = records.Select(r => r.Id).ToList(),
                Embeddings = records.Select(r => r.Vector).ToList(),
                Metadatas = records.Select(r => r.Metadata).ToList()
            };
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/collections/{CollectionName}/add", request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(float[] queryVector, int topK)
        {
            var request = new QueryRequest
            {
                QueryEmbeddings = new List<float[]> { queryVector },
                NResults = topK
            };

            var response = await _httpClient.PostAsJsonAsync($"/api/v1/collections/{CollectionName}/query", request);
            response.EnsureSuccessStatusCode();

            var queryResponse = await response.Content.ReadFromJsonAsync<QueryResponse>();
            if (queryResponse?.Ids.FirstOrDefault() == null)
            {
                return Enumerable.Empty<SearchResult>();
            }
            
            return queryResponse.Ids.First()
                .Zip(queryResponse.Distances.First(), (id, distance) => new { Id = id, Distance = distance })
                .Zip(queryResponse.Metadatas.First(), (pair, metadata) => new SearchResult
                {
                    Title = metadata.GetValueOrDefault("title", string.Empty)?.ToString() ?? string.Empty,
                    Content = metadata.GetValueOrDefault("content", string.Empty)?.ToString() ?? string.Empty,
                    ElementType = metadata.GetValueOrDefault("element_type", string.Empty)?.ToString() ?? string.Empty,
                    ClassName = metadata.GetValueOrDefault("class_name", string.Empty)?.ToString() ?? string.Empty,
                    Similarity = 1 - pair.Distance
                })
                .ToList();
        }

        public async Task DeleteByVersionAsync(string unityVersion)
        {
            var deleteRequest = new DeleteRequest(new Dictionary<string, object>
            {
                { "unity_version", unityVersion }
            });
            var response = await _httpClient.PostAsJsonAsync($"/api/v1/collections/{CollectionName}/delete", deleteRequest);
            response.EnsureSuccessStatusCode();
            Console.Error.WriteLine($"[ChromaDB] Deleted embeddings for Unity version {unityVersion}.");
        }
    }
}

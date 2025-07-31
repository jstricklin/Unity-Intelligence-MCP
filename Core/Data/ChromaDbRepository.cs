using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ChromaDB.Client;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Data
{
    public class ChromaDbRepository : IVectorRepository
    {
        // private readonly ChromaClient _client;
        // private readonly ChromaClient _client;
        private readonly HttpClient _httpClient;
        private readonly ChromaConfigurationOptions _configOptions;
        private const string CollectionName = "unity_docs";
        private ChromaCollectionClient? _collectionClient = null;

        public ChromaDbRepository(HttpClient httpClient, ConfigurationService configService)
        {
            var chromaUrl = configService.UnitySettings.ChromaDbUrl ?? "http://localhost:8000";
            _httpClient = httpClient;
            _configOptions = new ChromaConfigurationOptions(uri: chromaUrl);
        }

        public async Task InitializeChromaDbAsync()
        {
            try
            {
                Console.Error.WriteLine($"[ChromaDB] Creating collection: {CollectionName}");
                var client = new ChromaClient(_configOptions, _httpClient);
                var collection = await client.GetOrCreateCollection(CollectionName);
                _collectionClient = new ChromaCollectionClient(collection, _configOptions, _httpClient);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to initialize ChromaDB collection: {ex.Message}");
            }
        }
        
        public async Task AddEmbeddingsAsync(IEnumerable<VectorRecord> records)
        {
            if (_collectionClient == null)
            {
                Console.Error.WriteLine($"[ERROR] Failed to add embedding! ChromaDB not initialized.");
                return;
            }
            var request = new AddEmbeddingsRequest
            {
                Ids = records.Select(r => r.Id).ToList(),
                Metadatas = records.Select(r => r.Metadata).ToList()
            };
            // var response = await _httpClient.PostAsJsonAsync($"/api/v1/collections/{CollectionName}/add", request);
            // response.EnsureSuccessStatusCode();
            await _collectionClient.Add(request.Ids, metadatas: request.Metadatas);
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(ReadOnlyMemory<float> queryVector, int topK)
        {
            if (_collectionClient == null)
            {
                Console.Error.WriteLine($"[ERROR] Failed to add embedding! ChromaDB not initialized.");
                return Enumerable.Empty<SearchResult>();
            }
            var request = new QueryRequest
            {
                QueryEmbeddings = new List<ReadOnlyMemory<float>> { queryVector },
                NResults = topK
            };


            var queryResponse = await _collectionClient.Query(request.QueryEmbeddings, request.NResults);
            if (queryResponse?.FirstOrDefault() == null)
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

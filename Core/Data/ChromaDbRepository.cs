using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
            var chromaUrl = configService.UnitySettings.ChromaDbUrl ?? "http://localhost:8000/api/v1/";
            _httpClient = httpClient;
            _configOptions = new ChromaConfigurationOptions(uri: chromaUrl);
        }

        public async Task InitializeChromaDbAsync()
        {
            try
            {
                Console.Error.WriteLine($"[ChromaDB] Initializing collection: {CollectionName}");
                var client = new ChromaClient(_configOptions, _httpClient);
                var collection = await client.GetOrCreateCollection(CollectionName);
                Console.Error.WriteLine($"[ERROR] Failed to initialize ChromaDB collection: {collection.Name}");
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
            var resultsForFirstQuery = queryResponse?.FirstOrDefault();
            if (resultsForFirstQuery == null)
            {
                return Enumerable.Empty<SearchResult>();
            }

            return resultsForFirstQuery.Select(entry => new SearchResult
            {
                Title = entry.Metadata?.GetValueOrDefault("title", string.Empty)?.ToString() ?? string.Empty,
                Content = entry.Metadata?.GetValueOrDefault("content", string.Empty)?.ToString() ?? string.Empty,
                ElementType = entry.Metadata?.GetValueOrDefault("element_type", string.Empty)?.ToString() ?? string.Empty,
                ClassName = entry.Metadata?.GetValueOrDefault("class_name", string.Empty)?.ToString() ?? string.Empty,
                Similarity = 1 - entry.Distance
            }).ToList();
        }

        public async Task DeleteByVersionAsync(string unityVersion)
        {
            if (_collectionClient == null)
            {
                Console.Error.WriteLine($"[ERROR] Failed to delete embeddings! ChromaDB not initialized.");
                return;
            }

            try
            {
                // 1. Create a filter to find documents by version.
                var whereFilter = ChromaWhereOperator.In("unity_version", unityVersion);
                // 2. Get the entries to find their IDs.
                var entries = await _collectionClient.Get(where: whereFilter);
                if (entries == null || !entries.Any())
                {
                    Console.Error.WriteLine($"[ChromaDB] No embeddings found for Unity version {unityVersion} to delete.");
                    return;
                }

                // 3. Extract the IDs required for the delete operation.
                var idsToDelete = entries.Select(e => e.Id).ToList();

                // 4. Delete the entries by their specific IDs.
                await _collectionClient.Delete(idsToDelete);
                Console.Error.WriteLine($"[ChromaDB] Deleted {idsToDelete.Count} embeddings for Unity version {unityVersion}.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to delete embeddings by version: {ex.Message}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class SemanticSearchService : ISemanticSearchService
    {
        private readonly IEmbeddingService _embedding;
        private readonly IDuckDbConnectionFactory _dbFactory;

        public SemanticSearchService(
            IEmbeddingService embeddingService,
            IDuckDbConnectionFactory connectionFactory)
        {
            _embedding = embeddingService;
            _dbFactory = connectionFactory;
        }

        public async Task<IEnumerable<SemanticSearchResult>> SearchAsync(string query, int limit = 5, string sourceType = "scripting_api")
        {
            try
            {
                var vector = await _embedding.EmbedAsync(query);
                return await _dbFactory.ExecuteWithConnectionAsync(async connection =>
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                    SELECT 
                        d.id,
                        d.title,
                        d.url,
                        s.source_name,
                        ce.content,
                        1 - array_distance(ce.embedding, CAST($query AS FLOAT[384])) AS relevance
                    FROM content_elements ce
                    JOIN unity_docs d ON ce.doc_id = d.id
                    JOIN doc_sources s ON d.source_id = s.id
                    WHERE s.source_type = $sourceType
                    ORDER BY relevance DESC
                    LIMIT $limit;
                    ";

                    cmd.Parameters.AddRange(new[] { 
                        new DuckDBParameter("query", vector),
                        new DuckDBParameter("limit", limit),
                        new DuckDBParameter("sourceType", sourceType)
                    });

                    var results = new List<SemanticSearchResult>();
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        results.Add(new SemanticSearchResult(
                            docId: reader.GetInt64(0),
                            title: reader.GetString(1),
                            url: reader.IsDBNull(2) ? null : reader.GetString(2),
                            source: reader.GetString(3),
                            contentSnippet: reader.GetString(4),
                            relevanceScore: reader.GetDouble(5)
                        ));
                    }
                    return results;
                });
            }
            catch (DuckDBException ex) when (ex.Message.Contains("vss_search"))
            {
                throw new InvalidOperationException(
                    "Semantic search unavailable. Please rebuild documentation index.",
                    ex
                );
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public interface ISemanticSearchService
    {
        Task<IEnumerable<SemanticSearchResult>> SearchAsync(string query, int limit = 5, string sourceType = "scripting_api");
    }

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
                        d.id AS DocId,
                        d.title,
                        d.url,
                        s.source_name AS Source,
                        1 - vss_distance(d.embedding, @query) AS RelevanceScore
                    FROM vss_search(
                        (SELECT embedding, _rowid FROM unity_docs), 
                        @query, 
                        k:=@limit
                    ) vd
                    JOIN unity_docs d ON vd._rowid = d._rowid
                    JOIN doc_sources s ON d.source_id = s.id
                    WHERE s.source_type = @sourceType
                    ORDER BY RelevanceScore DESC
                    LIMIT @limit;";

                    cmd.Parameters
                        .Add(new DuckDBParameter("query", vector))
                        .Add(new DuckDBParameter("limit", limit))
                        .Add(new DuckDBParameter("sourceType", sourceType));

                    var results = new List<SemanticSearchResult>();
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        results.Add(new SemanticSearchResult(
                            DocId: reader.GetInt64(0),
                            Title: reader.GetString(1),
                            Url: reader.GetString(2),
                            Source: reader.GetString(3),
                            RelevanceScore: reader.GetDouble(4)
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

using System.Collections.Generic;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class SemanticRecommendationService
    {
        private readonly IDuckDbConnectionFactory _dbFactory;
        
        public SemanticRecommendationService(IDuckDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }
        
        public async Task<List<DocumentResult>> GetRelatedDocsAsync(
            long currentDocId, int limit = 5, string sourceType = "scripting_api")
        {
            return await _dbFactory.ExecuteWithConnectionAsync(async connection => 
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    WITH current_doc AS (
                        SELECT embedding 
                        FROM unity_docs 
                        WHERE id = $currentDocId
                    ),
                    similar_docs AS (
                        SELECT
                            d.id,
                            d.title,
                            d.url,
                            s.source_name,
                            1 - array_distance(d.embedding, cd.embedding) AS relevance
                        FROM unity_docs d
                        CROSS JOIN current_doc cd
                        JOIN doc_sources s ON d.source_id = s.id
                        WHERE d.id != $currentDocId
                            AND s.source_type = $sourceType
                        ORDER BY relevance DESC
                        LIMIT $limit
                    )
                    SELECT * FROM similar_docs;
                ";

                cmd.Parameters.AddRange(new[] {
                    new DuckDBParameter("currentDocId", currentDocId),
                    new DuckDBParameter("sourceType", sourceType),
                    new DuckDBParameter("limit", limit)
                });

                var results = new List<DocumentResult>();
                using var reader = await cmd.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    results.Add(new DocumentResult(
                        reader.GetInt64(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetDouble(4)
                    ));
                }
                return results;
            });
        }
    }
    
    public record DocumentResult(long DocId, string Title, string Url, string Source, double Relevance);
}

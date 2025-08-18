using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        public async Task<List<DocumentGroup>> GetHierarchicalResultsAsync(
            string query, 
            int docLimit = 5, 
            int chunksPerDoc = 3,
            string sourceType = "scripting_api")
        {
            var vector = await _embedding.EmbedAsync(query);
            
            return await _dbFactory.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    WITH ranked_chunks AS (
                        SELECT
                            d.id AS doc_id,
                            d.title,
                            d.url,
                            s.source_name,
                            ce.id AS chunk_id,
                            ce.content,
                            1 - array_distance(ce.embedding, CAST($query AS FLOAT[384])) AS relevance,
                            ce.element_type AS section
                        FROM content_elements ce
                        JOIN unity_docs d ON ce.doc_id = d.id
                        JOIN doc_sources s ON d.source_id = s.id
                        WHERE s.source_type = $sourceType
                    ),
                    ranked_results AS (
                        SELECT *,
                            ROW_NUMBER() OVER(
                                PARTITION BY doc_id 
                                ORDER BY relevance DESC
                            ) AS chunk_rank,
                            MAX(relevance) OVER(PARTITION BY doc_id) AS doc_relevance
                        FROM ranked_chunks
                    )
                    SELECT 
                        doc_id,
                        title,
                        url,
                        source_name,
                        MAX(doc_relevance) AS max_relevance,
                        ARRAY_AGG(chunk_id ORDER BY relevance DESC) AS chunk_ids,
                        ARRAY_AGG(content ORDER BY relevance DESC) AS contents,
                        ARRAY_AGG(relevance ORDER BY relevance DESC) AS relevances,
                        ARRAY_AGG(section ORDER BY relevance DESC) AS sections
                    FROM ranked_results
                    WHERE chunk_rank <= $chunksPerDoc
                    GROUP BY doc_id, title, url, source_name
                    ORDER BY max_relevance DESC
                    LIMIT $docLimit;
                ";

                cmd.Parameters.AddRange(new[] {
                    new DuckDBParameter("query", vector),
                    new DuckDBParameter("sourceType", sourceType),
                    new DuckDBParameter("chunksPerDoc", chunksPerDoc),
                    new DuckDBParameter("docLimit", docLimit)
                });

                return await ProcessDocumentGroups(cmd);
            });
        }

        public async Task<List<DocumentGroup>> HybridSearchAsync(
            string query, 
            int docLimit = 5,
            int chunksPerDoc = 3,
            double semanticWeight = 0.75,
            string sourceType = "scripting_api")
        {
            // Extract meaningful terms from query
            var terms = query.Split()
                .Where(t => t.Length > 2)
                .Distinct()
                .ToArray();

            var vector = await _embedding.EmbedAsync(query);
            var keywordThresholdWeight = 1 - semanticWeight; // Default is 0.25

            return await _dbFactory.ExecuteWithConnectionAsync(async connection =>
            {
                // Only create keyword scoring function if we have terms
                if (terms.Any())
                {
                    connection.RegisterScalarFunction<string, double>("keyword_score", (readers, writer, rowCount) => {
                        // Precompute lowercase terms once
                        var tokens = terms.Select(t => t.ToLower()).ToArray();
                        var weight = keywordThresholdWeight;
                        
                        for (ulong idx = 0; idx < rowCount; idx++) {
                            var content = readers[0].GetValue<string>(idx)?.ToLower() ?? "";
                            double score = 0;
                            
                            foreach (var term in tokens) {
                                if (content.Contains(term)) {
                                    score += weight;
                                }
                            }
                            writer.WriteValue(score, idx);
                        }
                    });
                }
                else
                {
                    // // Fallback: Return zero if no valid terms
                     connection.RegisterScalarFunction<string, double>("keyword_score", (readers, writer, rowCount) =>
                    {
                        for (ulong i = 0; i < rowCount; i++)
                        {
                            writer.WriteValue(0.0, i);
                        }
                    });
                }

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    WITH base_chunks AS (
                        SELECT
                            d.id AS doc_id,
                            d.title,
                            d.url,
                            s.source_name,
                            ce.id AS chunk_id,
                            ce.content,
                            ce.element_type AS section,
                            1 - array_distance(ce.embedding, CAST($query AS FLOAT[384])) AS semantic_score,
                            keyword_score(ce.content) AS keyword_score
                        FROM content_elements ce
                        JOIN unity_docs d ON ce.doc_id = d.id
                        JOIN doc_sources s ON d.source_id = s.id
                        WHERE s.source_type = $sourceType
                    ),
                    scored_chunks AS (
                        SELECT *,
                            ($semanticWeight * semantic_score) + 
                            keyword_score AS combined_score
                        FROM base_chunks
                    ),
                    ranked_results AS (
                        SELECT *,
                            ROW_NUMBER() OVER(
                                PARTITION BY doc_id 
                                ORDER BY combined_score DESC
                            ) AS chunk_rank,
                            MAX(combined_score) OVER(PARTITION BY doc_id) AS doc_score
                        FROM scored_chunks
                    )
                    SELECT 
                        doc_id,
                        title,
                        url,
                        source_name,
                        MAX(doc_score) AS max_relevance,
                        ARRAY_AGG(chunk_id ORDER BY combined_score DESC) AS chunk_ids,
                        ARRAY_AGG(content ORDER BY combined_score DESC) AS contents,
                        ARRAY_AGG(combined_score ORDER BY combined_score DESC) AS relevances,
                        ARRAY_AGG(section ORDER BY combined_score DESC) AS sections
                    FROM ranked_results
                    WHERE chunk_rank <= $chunksPerDoc
                    GROUP BY doc_id, title, url, source_name
                    ORDER BY max_relevance DESC
                    LIMIT $docLimit;
                ";

                cmd.Parameters.AddRange(new[] {
                    new DuckDBParameter("query", vector),
                    new DuckDBParameter("sourceType", sourceType),
                    new DuckDBParameter("semanticWeight", semanticWeight),
                    new DuckDBParameter("chunksPerDoc", chunksPerDoc),
                    new DuckDBParameter("docLimit", docLimit)
                });

                return await ProcessDocumentGroups(cmd);
            });
        }

        public async Task<IEnumerable<SemanticSearchResult>> SearchAsync(string query, int limit = 5, string sourceType = "scripting_api")
        {
            // Existing flat search implementation as a fallback
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
                        url: reader.GetString(2),
                        source: reader.GetString(3),
                        contentSnippet: reader.GetString(4),
                        relevanceScore: reader.GetFloat(5)
                    ));
                }
                return results;
            });
        }

        private async Task<List<DocumentGroup>> ProcessDocumentGroups(DbCommand cmd)
        {
            var results = new List<DocumentGroup>();
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var group = new DocumentGroup {
                    DocId = reader.GetInt64(0),
                    Title = reader.GetString(1),
                    Url = reader.GetString(2),
                    Source = reader.GetString(3),
                    MaxRelevance = reader.GetDouble(4),
                    TopChunks = new List<ChunkResult>()
                };

                var chunkIds = GetArray<long>(reader, 5);
                var contents = GetArray<string>(reader, 6);
                var relevances = GetArray<double>(reader, 7);
                var sections = GetArray<string>(reader, 8);

                Console.Error.WriteLine($"[INFO] Chunk Check: {chunkIds.Count()}");
                for (int i = 0; i < chunkIds.Length; i++)
                {
                    group.TopChunks.Add(new ChunkResult {
                        ChunkId = chunkIds[i],
                        ContentSnippet = contents[i],
                        Relevance = relevances[i],
                        SectionType = sections[i]
                    });
                }
                
                results.Add(group);
            }
            return results;
        }

        private T[] GetArray<T>(DbDataReader reader, int ordinal)
        {
            return reader.GetValue(ordinal) as T[] ?? Array.Empty<T>();
        }
    }
}

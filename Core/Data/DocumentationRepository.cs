using System;
using System.Collections.Generic;
using DuckDB.NET.Data;
using System.Runtime.InteropServices;
using System.Text.Json;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;
using UnityIntelligenceMCP.Configuration;

namespace UnityIntelligenceMCP.Core.Data
{
    public class DocumentationRepository : IDocumentationRepository
    {
        private readonly IApplicationDatabase _database;

        public DocumentationRepository(IApplicationDatabase database)
        {
            _database = database;
        }

        // public async Task IndexDocumentationAsync(string projectPath)
        // {
        //     var docRoot = ConfigurationService.GetDocumentationPath(projectPath);
        //     var htmlFiles = Directory.GetFiles(docRoot, "*.html", SearchOption.AllDirectories);

        //     foreach (var filePath in htmlFiles)
        //     {
        //         await IndexSingleDocumentAsync(filePath);
        //     }
        // }

        public async Task<int> InsertDocumentAsync(SemanticDocumentRecord record, CancellationToken cancellationToken = default)
        {
            await using var connection = new DuckDBConnection($"DataSource = {_database.GetConnectionString()}");
            await connection.OpenAsync(cancellationToken);

            // await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var transaction = connection.BeginTransaction();

            // Insert into unity_docs
            var docCommand = connection.CreateCommand();
            docCommand.Transaction = transaction;
            docCommand.CommandText = @"
                INSERT INTO unity_docs (source_id, doc_key, title, url, doc_type, category, unity_version, content_hash, title_embedding, summary_embedding)
                VALUES ((SELECT id FROM doc_sources WHERE source_type = $source_type), $doc_key, $title, $url, $doc_type, $category, $unity_version, $content_hash, $title_embedding, $summary_embedding)
                RETURNING id;
            ";

            docCommand.Parameters.Add(new DuckDBParameter("source_type", record.Metadata.FirstOrDefault()?.MetadataType ?? "scripting_api"));
            docCommand.Parameters.Add(new DuckDBParameter("doc_key", record.DocKey));
            docCommand.Parameters.Add(new DuckDBParameter("title", record.Title));
            docCommand.Parameters.Add(new DuckDBParameter("url", record.Url ?? (object)DBNull.Value));
            docCommand.Parameters.Add(new DuckDBParameter("doc_type", record.DocType ?? (object)DBNull.Value));
            docCommand.Parameters.Add(new DuckDBParameter("category", record.Category ?? (object)DBNull.Value));
            docCommand.Parameters.Add(new DuckDBParameter("unity_version", record.UnityVersion ?? (object)DBNull.Value));
            docCommand.Parameters.Add(new DuckDBParameter("content_hash", record.ContentHash ?? (object)DBNull.Value));
            docCommand.Parameters.Add(new DuckDBParameter("title_embedding", record.TitleEmbedding ?? (object)DBNull.Value));
            docCommand.Parameters.Add(new DuckDBParameter("summary_embedding", record.SummaryEmbedding ?? (object)DBNull.Value));
            
            var docId = Convert.ToInt32(await docCommand.ExecuteScalarAsync(cancellationToken));

            // Insert into doc_metadata
            foreach (var meta in record.Metadata)
            {
                var metaCommand = connection.CreateCommand();
                metaCommand.Transaction = transaction;
                metaCommand.CommandText = @"
                    INSERT INTO doc_metadata (doc_id, metadata_type, metadata_json)
                    VALUES ($doc_id, $metadata_type, $metadata_json);
                ";
                metaCommand.Parameters.Add(new DuckDBParameter("doc_id", docId));
                metaCommand.Parameters.Add(new DuckDBParameter("metadata_type", meta.MetadataType));
                metaCommand.Parameters.Add(new DuckDBParameter("metadata_json", meta.MetadataJson));
                await metaCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            // Insert into content_elements
            foreach (var element in record.Elements)
            {
                var elementCommand = connection.CreateCommand();
                elementCommand.Transaction = transaction;
                elementCommand.CommandText = @"
                    INSERT INTO content_elements (doc_id, element_type, title, content, attributes_json, element_embedding)
                    VALUES ($doc_id, $element_type, $title, $content, $attributes_json, $element_embedding);
                ";
                elementCommand.Parameters.Add(new DuckDBParameter("doc_id", docId));
                elementCommand.Parameters.Add(new DuckDBParameter("element_type", element.ElementType));
                elementCommand.Parameters.Add(new DuckDBParameter("title", element.Title ?? (object)DBNull.Value));
                elementCommand.Parameters.Add(new DuckDBParameter("content", element.Content ?? (object)DBNull.Value));
                elementCommand.Parameters.Add(new DuckDBParameter("attributes_json", element.AttributesJson ?? (object)DBNull.Value));
                
                object embeddingParam = DBNull.Value;
                if (element.ElementEmbedding.HasValue)
                {
                    embeddingParam = MemoryMarshal.AsBytes(element.ElementEmbedding.Value.Span).ToArray();
                }
                elementCommand.Parameters.Add(new DuckDBParameter("element_embedding", embeddingParam));
                await elementCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            
            await transaction.CommitAsync(cancellationToken);
            
            Console.Error.WriteLine($"[DB] Successfully inserted document '{record.Title}' with ID {docId}.");
            return docId;
        }

        public async Task<IEnumerable<SearchResult>> SemanticSearchAsync(float[] embedding, int limit = 10, CancellationToken cancellationToken = default)
        {
            var results = new List<SearchResult>();
            await using var connection = new DuckDBConnection($"DataSource = {_database.GetConnectionString()}");
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    ce.title,
                    ce.content,
                    ce.element_type,
                    d.title as ClassName,
                    v.distance
                FROM vec_elements_index v
                JOIN content_elements ce ON v.rowid = ce.id
                JOIN unity_docs d ON ce.doc_id = d.id
                WHERE vss_search(v.embedding, $embedding)
                ORDER BY v.distance
                LIMIT $limit;
            ";
            command.Parameters.Add(new DuckDBParameter("embedding", FloatArrayToByteArray(embedding)));
            command.Parameters.Add(new DuckDBParameter("limit", limit));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new SearchResult
                {
                    Title = reader.GetString(0),
                    Content = reader.IsDBNull(1) ? null : reader.GetString(1),
                    ElementType = reader.GetString(2),
                    ClassName = reader.GetString(3),
                    Similarity = 1 - reader.GetFloat(4) // Convert distance to similarity (0=exact, 1=opposite)
                });
            }
            return results;
        }

        public async Task<int> GetDocCountForVersionAsync(string unityVersion, CancellationToken cancellationToken = default)
        {
            await using var connection = new DuckDBConnection($"DataSource = {_database.GetConnectionString()}");
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM unity_docs WHERE unity_version = $unity_version;";
            command.Parameters.Add(new DuckDBParameter("unity_version", unityVersion));

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }

        public async Task DeleteDocsByVersionAsync(string unityVersion, CancellationToken cancellationToken = default)
        {
            await using var connection = new DuckDBConnection($"DataSource = {_database.GetConnectionString()}");
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM unity_docs WHERE unity_version = $unity_version;";
            command.Parameters.Add(new DuckDBParameter("unity_version", unityVersion));
            await command.ExecuteNonQueryAsync(cancellationToken);
            Console.Error.WriteLine($"[DB] Deleted existing documentation for Unity version {unityVersion}.");
        }

        private static byte[]? FloatArrayToByteArray(IReadOnlyCollection<float> floats)
        {
            if (floats == null || floats.Count == 0) return null;
            var byteArray = new byte[floats.Count * sizeof(float)];
            Buffer.BlockCopy(floats.ToArray(), 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }
    }
}

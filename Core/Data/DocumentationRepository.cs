using Microsoft.Data.Sqlite;
using System.Text.Json;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Core.Data
{
    public class DocumentationRepository : IDocumentationRepository
    {
        private readonly IDocumentationDatabase _database;

        public DocumentationRepository(IDocumentationDatabase database)
        {
            _database = database;
        }

        public async Task<int> InsertDocumentAsync(UniversalDocumentRecord record, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_database.GetConnectionString());
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            // Insert into unity_docs
            var docCommand = connection.CreateCommand();
            docCommand.Transaction = transaction;
            docCommand.CommandText = @"
                INSERT INTO unity_docs (source_id, doc_key, title, url, doc_type, category, unity_version, content_hash, title_embedding, summary_embedding)
                VALUES ((SELECT id FROM doc_sources WHERE source_type = @source_type), @doc_key, @title, @url, @doc_type, @category, @unity_version, @content_hash, @title_embedding, @summary_embedding)
                RETURNING id;
            ";

            docCommand.Parameters.AddWithValue("@source_type", record.Metadata.FirstOrDefault()?.MetadataType ?? "scripting_api");
            docCommand.Parameters.AddWithValue("@doc_key", record.DocKey);
            docCommand.Parameters.AddWithValue("@title", record.Title);
            docCommand.Parameters.AddWithValue("@url", record.Url ?? (object)DBNull.Value);
            docCommand.Parameters.AddWithValue("@doc_type", record.DocType ?? (object)DBNull.Value);
            docCommand.Parameters.AddWithValue("@category", record.Category ?? (object)DBNull.Value);
            docCommand.Parameters.AddWithValue("@unity_version", record.UnityVersion ?? (object)DBNull.Value);
            docCommand.Parameters.AddWithValue("@content_hash", record.ContentHash ?? (object)DBNull.Value);
            docCommand.Parameters.AddWithValue("@title_embedding", record.TitleEmbedding ?? (object)DBNull.Value);
            docCommand.Parameters.AddWithValue("@summary_embedding", record.SummaryEmbedding ?? (object)DBNull.Value);
            
            var docId = Convert.ToInt32(await docCommand.ExecuteScalarAsync(cancellationToken));

            // Insert into doc_metadata
            foreach (var meta in record.Metadata)
            {
                var metaCommand = connection.CreateCommand();
                metaCommand.Transaction = transaction;
                metaCommand.CommandText = @"
                    INSERT INTO doc_metadata (doc_id, metadata_type, metadata_json)
                    VALUES (@doc_id, @metadata_type, @metadata_json);
                ";
                metaCommand.Parameters.AddWithValue("@doc_id", docId);
                metaCommand.Parameters.AddWithValue("@metadata_type", meta.MetadataType);
                metaCommand.Parameters.AddWithValue("@metadata_json", meta.MetadataJson);
                await metaCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            // Insert into content_elements
            foreach (var element in record.Elements)
            {
                var elementCommand = connection.CreateCommand();
                elementCommand.Transaction = transaction;
                elementCommand.CommandText = @"
                    INSERT INTO content_elements (doc_id, element_type, title, content, attributes_json, element_embedding)
                    VALUES (@doc_id, @element_type, @title, @content, @attributes_json, @element_embedding);
                ";
                elementCommand.Parameters.AddWithValue("@doc_id", docId);
                elementCommand.Parameters.AddWithValue("@element_type", element.ElementType);
                elementCommand.Parameters.AddWithValue("@title", element.Title ?? (object)DBNull.Value);
                elementCommand.Parameters.AddWithValue("@content", element.Content ?? (object)DBNull.Value);
                elementCommand.Parameters.AddWithValue("@attributes_json", element.AttributesJson ?? (object)DBNull.Value);
                elementCommand.Parameters.AddWithValue("@element_embedding", element.ElementEmbedding ?? (object)DBNull.Value);
                await elementCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            
            await transaction.CommitAsync(cancellationToken);
            
            Console.Error.WriteLine($"[DB] Successfully inserted document '{record.Title}' with ID {docId}.");
            return docId;
        }

        public Task<ResourceResult> SemanticSearchAsync(float[] embedding, string? sourceType = null, CancellationToken cancellationToken = default)
        {
            // Placeholder for future implementation of semantic vector search.
            Console.Error.WriteLine("[DB] SemanticSearchAsync is not yet implemented.");
            return Task.FromResult(ResourceResult.Success(new List<object>(), "Not Implemented"));
        }
    }
}

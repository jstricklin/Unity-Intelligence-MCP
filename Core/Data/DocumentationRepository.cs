using System;
using System.Collections.Generic;
using System.Linq;
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
                INSERT INTO unity_docs (source_id, doc_key, title, url, doc_type, category, unity_version, content_hash)
                VALUES ((SELECT id FROM doc_sources WHERE source_type = $source_type), $doc_key, $title, $url, $doc_type, $category, $unity_version, $content_hash)
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
                    INSERT INTO content_elements (doc_id, element_type, title, content, attributes_json)
                    VALUES ($doc_id, $element_type, $title, $content, $attributes_json);
                ";
                elementCommand.Parameters.Add(new DuckDBParameter("doc_id", docId));
                elementCommand.Parameters.Add(new DuckDBParameter("element_type", element.ElementType));
                elementCommand.Parameters.Add(new DuckDBParameter("title", element.Title ?? (object)DBNull.Value));
                elementCommand.Parameters.Add(new DuckDBParameter("content", element.Content ?? (object)DBNull.Value));
                elementCommand.Parameters.Add(new DuckDBParameter("attributes_json", element.AttributesJson ?? (object)DBNull.Value));
                await elementCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            
            await transaction.CommitAsync(cancellationToken);
            
            Console.Error.WriteLine($"[DB] Successfully inserted document '{record.Title}' with ID {docId}.");
            return docId;
        }

        public async Task InsertDocumentsInBulkAsync(IReadOnlyList<SemanticDocumentRecord> records, CancellationToken cancellationToken = default)
        {
            if (records == null || records.Count == 0) return;

            await using var connection = new DuckDBConnection($"DataSource = {_database.GetConnectionString()}");
            await connection.OpenAsync(cancellationToken);
            await using var transaction = connection.BeginTransaction();

            try
            {
                async Task<List<long>> GetNextIdsAsync(string sequenceName, int count)
                {
                    if (count == 0) return new List<long>();
                    var ids = new List<long>(count);
                    var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = $"SELECT nextval('{sequenceName}') FROM generate_series(1, {count})";
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            ids.Add(reader.GetInt64(0));
                        }
                    }
                    return ids;
                }

                var sourceIdCommand = connection.CreateCommand();
                sourceIdCommand.Transaction = transaction;
                sourceIdCommand.CommandText = "SELECT id FROM doc_sources WHERE source_type = 'scripting_api' LIMIT 1;";
                var sourceId = Convert.ToInt64(await sourceIdCommand.ExecuteScalarAsync(cancellationToken));
                
                var docsCount = records.Count;
                var metadataCount = records.Sum(r => r.Metadata.Count);
                var elementsCount = records.Sum(r => r.Elements.Count);

                var docIds = await GetNextIdsAsync("unity_docs_id_seq", docsCount);
                var metadataIds = await GetNextIdsAsync("doc_metadata_id_seq", metadataCount);
                var elementIds = await GetNextIdsAsync("content_elements_id_seq", elementsCount);

                var docIdMap = new Dictionary<string, long>();
                int docIdIndex = 0;
                using (var appender = connection.CreateAppender("unity_docs"))
                {
                    foreach (var record in records)
                    {
                        var newDocId = docIds[docIdIndex];
                        docIdMap[record.DocKey] = newDocId;
                        appender.CreateRow()
                            .AppendValue(newDocId)
                            .AppendValue(sourceId)
                            .AppendValue(record.DocKey)
                            .AppendValue(record.Title)
                            .AppendValue(record.Url)
                            .AppendValue(record.DocType)
                            .AppendValue(record.Category)
                            .AppendValue(record.UnityVersion)
                            .AppendValue(record.ContentHash)
                            .EndRow();
                        docIdIndex++;
                    }
                }

                int metadataIdIndex = 0;
                using (var appender = connection.CreateAppender("doc_metadata"))
                {
                    foreach (var record in records)
                    {
                        if (!docIdMap.TryGetValue(record.DocKey, out var docId)) continue;
                        foreach (var meta in record.Metadata)
                        {
                            appender.CreateRow()
                                .AppendValue(metadataIds[metadataIdIndex++])
                                .AppendValue(docId)
                                .AppendValue(meta.MetadataType)
                                .AppendValue(meta.MetadataJson)
                                .EndRow();
                        }
                    }
                }

                int elementIdIndex = 0;
                using (var appender = connection.CreateAppender("content_elements"))
                {
                    foreach (var record in records)
                    {
                        if (!docIdMap.TryGetValue(record.DocKey, out var docId)) continue;
                        foreach (var element in record.Elements)
                        {
                            appender.CreateRow()
                                .AppendValue(elementIds[elementIdIndex++])
                                .AppendValue(docId)
                                .AppendValue(element.ElementType)
                                .AppendValue(element.Title)
                                .AppendValue(element.Content)
                                .AppendValue(element.AttributesJson)
                                .EndRow();
                        }
                    }
                }
                
                await transaction.CommitAsync(cancellationToken);
                Console.Error.WriteLine($"[DB] Successfully inserted a batch of {records.Count} documents.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                Console.Error.WriteLine($"[ERROR] Failed to insert document batch: {ex.Message}");
                throw;
            }
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

    }
}

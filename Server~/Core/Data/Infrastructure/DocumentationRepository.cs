using System;
using System.Collections.Generic;
using System.Linq;
using DuckDB.NET.Data;
using System.Runtime.InteropServices;
using System.Text.Json;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Data.Infrastructure
{
    public class DocumentationRepository : IDocumentationRepository
    {

        private readonly IDuckDbConnectionFactory _connectionFactory;
        private readonly ILogger<DocumentationRepository> _logger;

        public DocumentationRepository(
            IDuckDbConnectionFactory connectionFactory,
            ILogger<DocumentationRepository> logger
            )
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public Task<int> InsertDocumentAsync(SemanticDocumentRecord record, CancellationToken cancellationToken = default)
        {
            return _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                await using var transaction = connection.BeginTransaction();

                // Insert into unity_docs
                var docCommand = connection.CreateCommand();
                docCommand.Transaction = transaction;
                docCommand.CommandText = @"
                INSERT INTO unity_docs (source_id, doc_key, title, url, construct_type, category, unity_version, content_hash, embedding)
                VALUES ((SELECT id FROM doc_sources WHERE source_type = $source_type), $doc_key, $title, $url, $type, $category, $unity_version, $content_hash, $embedding)
                RETURNING id;
            ";

                docCommand.Parameters.Add(new DuckDBParameter("source_type", record.Metadata.FirstOrDefault()?.MetadataType ?? "scripting_api"));
                docCommand.Parameters.Add(new DuckDBParameter("doc_key", record.DocKey));
                docCommand.Parameters.Add(new DuckDBParameter("title", record.Title));
                docCommand.Parameters.Add(new DuckDBParameter("url", record.Url ?? (object)DBNull.Value));
                docCommand.Parameters.Add(new DuckDBParameter("type", record.ConstructType ?? (object)DBNull.Value));
                docCommand.Parameters.Add(new DuckDBParameter("category", record.Category ?? (object)DBNull.Value));
                docCommand.Parameters.Add(new DuckDBParameter("unity_version", record.UnityVersion ?? (object)DBNull.Value));
                docCommand.Parameters.Add(new DuckDBParameter("content_hash", record.ContentHash ?? (object)DBNull.Value));
                docCommand.Parameters.Add(new DuckDBParameter("embedding", record.Embedding ?? (object)DBNull.Value));

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
                    INSERT INTO content_elements (doc_id, element_type, title, content, attributes_json, embedding)
                    VALUES ($doc_id, $element_type, $title, $content, $attributes_json, $embedding);
                ";
                    elementCommand.Parameters.Add(new DuckDBParameter("doc_id", docId));
                    elementCommand.Parameters.Add(new DuckDBParameter("element_type", element.ElementType));
                    elementCommand.Parameters.Add(new DuckDBParameter("title", element.Title ?? (object)DBNull.Value));
                    elementCommand.Parameters.Add(new DuckDBParameter("content", element.Content ?? (object)DBNull.Value));
                    elementCommand.Parameters.Add(new DuckDBParameter("attributes_json", element.AttributesJson ?? (object)DBNull.Value));
                    elementCommand.Parameters.Add(new DuckDBParameter("embedding", record.Embedding ?? (object)DBNull.Value));
                    await elementCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation($"[DB] Successfully inserted document '{record.Title}' with ID {docId}.");
                return docId;
            });
        }

        public Task<Dictionary<string, long>> InsertDocumentsInBulkAsync(IReadOnlyList<SemanticDocumentRecord> records, CancellationToken cancellationToken = default)
        {
            if (records == null || !records.Any()) return Task.FromResult(new Dictionary<string, long>());

            return _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                var docIdMap = new Dictionary<string, long>();
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

                    var docIds = await GetNextIdsAsync("unity_docs_id_seq", docsCount);
                    var metadataIds = await GetNextIdsAsync("doc_metadata_id_seq", metadataCount);

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
                                .AppendValue(record.ConstructType)
                                .AppendValue(record.Category)
                                .AppendValue(record.UnityVersion)
                                .AppendValue(record.ContentHash)
                                .AppendValue(record.Embedding)
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
                
                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogInformation($"[DB] Successfully inserted a batch of {records.Count} documents.");
                    return docIdMap;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError($"[ERROR] Failed to insert document batch: {ex.Message}");
                    throw;
                }
            });
        }

        public Task InsertContentElementsInBulkAsync(IReadOnlyList<ContentElementRecord> elements, CancellationToken cancellationToken = default)
        {
            if (elements == null || !elements.Any()) return Task.CompletedTask;
        
            return _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
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
                    
                    var elementIds = await GetNextIdsAsync("content_elements_id_seq", elements.Count);
        
                    using var appender = connection.CreateAppender("content_elements");
                    int elementIdIndex = 0;
                    foreach (var element in elements)
                    {
                        appender.CreateRow()
                            .AppendValue(elementIds[elementIdIndex++])
                            .AppendValue(element.DocId)
                            .AppendValue(element.ElementType)
                            .AppendValue(element.Title)
                            .AppendValue(element.Content)
                            .AppendValue(element.AttributesJson)
                            .AppendValue(element.Embedding)
                            .EndRow();
                    }
                    
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError($"[ERROR] Failed to insert content elements batch: {ex.Message}");
                    throw;
                }
            });
        }

        public Task InsertRelationshipsInBulkAsync(IReadOnlyList<object> relationships, CancellationToken cancellationToken = default)
        {
            if (relationships == null || !relationships.Any()) return Task.CompletedTask;

            return _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
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

                    var relIds = await GetNextIdsAsync("doc_relationships_id_seq", relationships.Count);

                    using var appender = connection.CreateAppender("doc_relationships");
                    int relIdIndex = 0;
                    foreach (var rel in relationships)
                    {
                        var type = rel.GetType();
                        var sourceDocId = (long)type.GetProperty("SourceDocId")!.GetValue(rel, null)!;
                        var targetDocId = (long)type.GetProperty("TargetDocId")!.GetValue(rel, null)!;
                        var relationshipType = (string)type.GetProperty("RelationshipType")!.GetValue(rel, null)!;
                        var context = (string)type.GetProperty("Context")!.GetValue(rel, null)!;

                        appender.CreateRow()
                            .AppendValue(relIds[relIdIndex++])
                            .AppendValue(sourceDocId)
                            .AppendValue(targetDocId)
                            .AppendValue(relationshipType)
                            .AppendValue(context)
                            .EndRow();
                    }
                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogInformation($"[DB] Successfully inserted a batch of {relationships.Count} relationships.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[ERROR] Failed to insert relationships batch: {ex.Message}");
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }


        public Task<int> GetDocCountForVersionAsync(string unityVersion, CancellationToken cancellationToken = default)
        {
            return _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM unity_docs WHERE unity_version = $unity_version;";
                command.Parameters.Add(new DuckDBParameter("unity_version", unityVersion));

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt32(result);
            });
        }

        public Task DeleteDocsByVersionAsync(string unityVersion, CancellationToken cancellationToken = default)
        {
            return _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM unity_docs WHERE unity_version = $unity_version;";
                command.Parameters.Add(new DuckDBParameter("unity_version", unityVersion));
                await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation($"[DB] Deleted existing documentation for Unity version {unityVersion}.");
            });
        }

        public async Task InitializeDocumentTrackingAsync(string unityVersion, IEnumerable<FileStatus> fileStatuses)
        {
            await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                // Initialize records
                foreach (var status in fileStatuses)
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO doc_processing_state (file_path, unity_version, content_hash, state)
                        VALUES ($path, $version, $hash, $state)
                        ON CONFLICT (file_path) DO UPDATE SET
                            content_hash = EXCLUDED.content_hash,
                            state = EXCLUDED.state
                    ";
                    cmd.Parameters.AddRange(new[] {
                        new DuckDBParameter("path", status.FilePath),
                        new DuckDBParameter("version", unityVersion),
                        new DuckDBParameter("hash", status.ContentHash),
                        new DuckDBParameter("state", status.State.ToString())
                    });
                    await cmd.ExecuteNonQueryAsync();
                }
            });
        }

        public async Task<Dictionary<string, FileStatus>> GetDocumentTrackingAsync(string unityVersion)
        {
            return await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT file_path, content_hash, state FROM doc_processing_state WHERE unity_version = $version";
                cmd.Parameters.Add(new DuckDBParameter("version", unityVersion));
                
                var results = new Dictionary<string, FileStatus>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results[reader.GetString(0)] = new FileStatus {
                        FilePath = reader.GetString(0),
                        ContentHash = reader.GetString(1),
                        State = Enum.Parse<DocumentState>(reader.GetString(2))
                    };
                }
                return results;
            });
        }

        public async Task MarkDocumentProcessingAsync(string filePath, string unityVersion)
            => await UpdateState(filePath, unityVersion, DocumentState.Processing);

        public async Task MarkDocumentProcessedAsync(string filePath, string unityVersion)
            => await UpdateState(filePath, unityVersion, DocumentState.Processed);

        public async Task MarkDocumentFailedAsync(string filePath, string unityVersion)
            => await UpdateState(filePath, unityVersion, DocumentState.Failed);

        private async Task UpdateState(string filePath, string unityVersion, DocumentState state)
        {
            await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE doc_processing_state
                    SET state = $state
                    WHERE file_path = $path AND unity_version = $version
                ";
                cmd.Parameters.AddRange(new[] {
                    new DuckDBParameter("state", state.ToString()),
                    new DuckDBParameter("path", filePath),
                    new DuckDBParameter("version", unityVersion)
                });
                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task RemoveDeprecatedDocumentsAsync(string unityVersion)
        {
            await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM doc_processing_state WHERE state = 'Deprecated' AND unity_version = $version";
                cmd.Parameters.Add(new DuckDBParameter("version", unityVersion));
                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task ResetTrackingStateAsync(string unityVersion)
        {
            await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE doc_processing_state
                    SET state = 'Pending'
                    WHERE unity_version = $version
                ";
                cmd.Parameters.Add(new DuckDBParameter("version", unityVersion));
                await cmd.ExecuteNonQueryAsync();
            });
        }

        public async Task RemoveOrphanedTrackingAsync(string unityVersion, IEnumerable<string> orphanedPaths)
        {
            await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    DELETE FROM doc_processing_state
                    WHERE unity_version = $version
                    AND file_path = ANY($paths)
                ";
                cmd.Parameters.Add(new DuckDBParameter("version", unityVersion));
                cmd.Parameters.Add(new DuckDBParameter("paths", orphanedPaths.ToArray()));
                await cmd.ExecuteNonQueryAsync();
            });
        }
    }
}

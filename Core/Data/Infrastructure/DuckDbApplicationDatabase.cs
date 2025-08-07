using DuckDB.NET.Data;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data.Contracts;

namespace UnityIntelligenceMCP.Core.Data.Infrastructure
{
    public class DuckDbApplicationDatabase : IApplicationDatabase
    {
        private readonly IDuckDbConnectionFactory _connectionFactory;
        private readonly SemaphoreSlim _schemaLock = new(1, 1);

        public DuckDbApplicationDatabase(IDuckDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }


        public async Task InitializeDatabaseAsync(string unityVersion)
        {
            await _schemaLock.WaitAsync();
            try
            {
                // await _connectionFactory.TryRecoverDatabaseAsync();
            
                // Get connection through factory
                await using var connection = await _connectionFactory.GetConnectionAsync();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    LOAD vss;";
                await cmd.ExecuteNonQueryAsync();
                cmd.CommandText = @"
                    SET hnsw_enable_experimental_persistence = true;";
                await cmd.ExecuteNonQueryAsync();
                if (await IsSchemaInitializedAsync(connection))
                {
                    Console.Error.WriteLine("[Database] Schema is already initialized - skipping initialization");
                    return;
                }

                await InitializeSchemaTransactionallyAsync(connection, unityVersion);
            }
            finally
            {
                _schemaLock.Release();
            }
        }

        private const string SchemaBaseTables = @"
            CREATE SEQUENCE doc_sources_id_seq START 1;
            CREATE TABLE IF NOT EXISTS doc_sources (
                id BIGINT PRIMARY KEY DEFAULT nextval('doc_sources_id_seq'),
                source_type VARCHAR NOT NULL UNIQUE,
                source_name VARCHAR NOT NULL,
                version VARCHAR,
                base_url VARCHAR,
                schema_version VARCHAR,
                last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            CREATE SEQUENCE unity_docs_id_seq START 1;
            CREATE TABLE IF NOT EXISTS unity_docs (
                id BIGINT PRIMARY KEY DEFAULT nextval('unity_docs_id_seq'),
                source_id BIGINT NOT NULL,
                doc_key VARCHAR NOT NULL,
                title VARCHAR NOT NULL,
                url VARCHAR,
                doc_type VARCHAR,
                category VARCHAR,
                unity_version VARCHAR,
                content_hash VARCHAR,
                embedding FLOAT[384],
                FOREIGN KEY (source_id) REFERENCES doc_sources (id),
                UNIQUE(source_id, doc_key)
            );

            CREATE SEQUENCE doc_metadata_id_seq START 1;
            CREATE TABLE IF NOT EXISTS doc_metadata (
                id BIGINT PRIMARY KEY DEFAULT nextval('doc_metadata_id_seq'),
                doc_id BIGINT NOT NULL,
                metadata_type VARCHAR NOT NULL,
                metadata_json VARCHAR NOT NULL,
                FOREIGN KEY (doc_id) REFERENCES unity_docs (id),
                UNIQUE(doc_id, metadata_type)
            );

            CREATE SEQUENCE content_elements_id_seq START 1;
            CREATE TABLE IF NOT EXISTS content_elements (
                id BIGINT PRIMARY KEY DEFAULT nextval('content_elements_id_seq'),
                doc_id BIGINT NOT NULL,
                element_type VARCHAR NOT NULL,
                title VARCHAR,
                content VARCHAR,
                attributes_json VARCHAR,
                embedding FLOAT[384],
                FOREIGN KEY (doc_id) REFERENCES unity_docs (id)
            );

            CREATE SEQUENCE doc_relationships_id_seq START 1;
            CREATE TABLE IF NOT EXISTS doc_relationships (
                id BIGINT PRIMARY KEY DEFAULT nextval('doc_relationships_id_seq'),
                source_doc_id BIGINT NOT NULL,
                target_doc_id BIGINT NOT NULL,
                relationship_type VARCHAR NOT NULL,
                context VARCHAR,
                FOREIGN KEY (source_doc_id) REFERENCES unity_docs (id),
                FOREIGN KEY (target_doc_id) REFERENCES unity_docs (id),
                UNIQUE(source_doc_id, target_doc_id, relationship_type, context)
            );
        ";

        private const string SchemaStandardIndexes = @"
            CREATE INDEX idx_elements_doc_type ON content_elements(doc_id, element_type);
            CREATE INDEX idx_metadata_doc ON doc_metadata(doc_id);
        ";

        private const string SchemaHnswIndexes = @"
            CREATE INDEX idx_unity_docs_embedding ON unity_docs USING HNSW (embedding)
            WITH (
                metric = 'cosine'
            );
            CREATE INDEX idx_docs_source_type ON unity_docs(source_id, doc_type);
            CREATE INDEX idx_content_elements_embedding ON content_elements USING HNSW (embedding);
        ";

        private const string SchemaViews = @"
            CREATE VIEW scripting_api_docs AS
            SELECT 
                d.id,
                d.title,
                d.doc_key as file_path,
                dm.metadata_json ->> '$.description' as description,
                dm.metadata_json ->> '$.class_name' as class_name,
                dm.metadata_json ->> '$.namespace' as namespace
            FROM unity_docs d
            JOIN doc_sources s ON d.source_id = s.id  
            LEFT JOIN doc_metadata dm ON d.id = dm.doc_id AND dm.metadata_type = 'scripting_api'
            WHERE s.source_type = 'scripting_api';

            CREATE VIEW api_elements AS
            SELECT 
                ce.id,
                ce.title,
                ce.content as description,
                ce.element_type,
                ce.attributes_json ->> '$.is_inherited' as is_inherited,
                d.title as class_name,
                dm.metadata_json ->> '$.namespace' as namespace
            FROM content_elements ce
            JOIN unity_docs d ON ce.doc_id = d.id
            JOIN doc_sources s ON d.source_id = s.id
            LEFT JOIN doc_metadata dm ON d.id = dm.doc_id AND dm.metadata_type = 'scripting_api'
            WHERE s.source_type = 'scripting_api'
            AND ce.element_type IN ('property', 'public_method', 'static_method', 'message');
        ";
        private const string ProcessingTable = @"
        CREATE TABLE IF NOT EXISTS doc_processing_state (
            file_path VARCHAR PRIMARY KEY,
            unity_version VARCHAR NOT NULL,
            content_hash VARCHAR NOT NULL,
            state VARCHAR(20) NOT NULL,
            last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        ";

        private const string InitialData = @"
            INSERT INTO doc_sources (id, source_type, source_name, version, schema_version) VALUES
            (1, 'scripting_api', 'Unity Scripting API', '{0}', '1.0'),
            (2, 'editor_manual', 'Unity User Manual', '{0}', '1.0'),
            (3, 'tutorial', 'Unity Learn Tutorials', 'current', '1.0');
        ";
        
        // private async Task<DuckDBConnection> GetVssConnectionAsync()
        // {
        //     var connection = new DuckDBConnection($"DataSource = {GetConnectionString()}");
        //     await connection.OpenAsync();
        //     using var cmd = connection.CreateCommand();
        //     cmd.CommandText = @"
        //         LOAD vss;
        //         SET hnsw_enable_experimental_persistence = true;
        //         SET enable_progress_bar = false;";
        //     await cmd.ExecuteNonQueryAsync();
        //     return connection;
        // }
        private async Task<bool> IsSchemaInitializedAsync(DuckDBConnection connection)
        {
            // TODO: prepare better initialization fix 
            // var command = connection.CreateCommand();
            // // Split objects into tables and sequences
            // var tables = _requiredSchemaObjects.Where(o => !o.EndsWith("_seq")).ToList();
            // var sequences = _requiredSchemaObjects.Where(o => o.EndsWith("_seq")).ToList();

            // // Check existence of tables and sequences
            // command.CommandText = $@"
            //     SELECT 
            //         (SELECT COUNT(*) = {tables.Count} FROM information_schema.tables WHERE table_name IN ({GenerateSqlList(tables)})) AND
            //         (SELECT COUNT(*) = {sequences.Count} FROM information_schema.sequences WHERE sequence_name IN ({GenerateSqlList(sequences)}))
            // ";

            // try
            // {
            //     var result = await command.ExecuteScalarAsync();
            //     return Convert.ToBoolean(result);
            // }
            // catch
            // {
            //     // Schema is not initialized if query fails
            //     return false;
            // }
            var command = connection.CreateCommand();
            command.CommandText = @"SELECT COUNT(*) FROM information_schema.tables WHERE table_name =
            'doc_sources'";
            var count = (long?)await command.ExecuteScalarAsync();
            return count > 0;
        }

        // private string GenerateSqlList(IEnumerable<string> items)
        // {
        //     return string.Join(", ", items.Select(n => $"'{n}'"));
        // }

        private async Task InitializeSchemaTransactionallyAsync(DuckDBConnection connection, string unityVersion)
        {
            await using var transaction = await connection.BeginTransactionAsync();
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            try
            {
                // Remove VSS loading - factory handles this
                // Processing table
                command.CommandText = ProcessingTable;
                await command.ExecuteNonQueryAsync();
                

                // Create tables, sequences with identity
                command.CommandText = SchemaBaseTables;
                await command.ExecuteNonQueryAsync();

                // Create HNSW indexes using VSS connection
                // await using var vssConn = await GetVssConnectionAsync();
                // await using var vssCmd = connection.CreateCommand();
                command.CommandText = SchemaHnswIndexes;
                await command.ExecuteNonQueryAsync();
                Console.Error.WriteLine("[HNSW] Created indexes");

                // Create standard indexes and views
                command.CommandText = SchemaStandardIndexes;
                await command.ExecuteNonQueryAsync();

                command.CommandText = SchemaViews;
                await command.ExecuteNonQueryAsync();
                
                // Insert initial data with actual Unity version
                command.CommandText = string.Format(InitialData, unityVersion);
                await command.ExecuteNonQueryAsync();

                // Add schema info
                command.CommandText = "CREATE TABLE IF NOT EXISTS mcp_schema_info (schema_version INT DEFAULT 1)";
                await command.ExecuteNonQueryAsync();
                command.CommandText = "INSERT INTO mcp_schema_info (schema_version) VALUES (1)";
                await command.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                Console.Error.WriteLine("[Database] Complete");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.Error.WriteLine($"[Database] Initialization failed: {ex}");
                throw;
            }
        }

        // private async Task TryRecoverDatabase()
        // {
        //     try
        //     {
        //         await _connectionFactory.ExecuteWithConnectionAsync(async connection => 
        //         {
        //             using var cmd = connection.CreateCommand();
        //             cmd.CommandText = "CHECKPOINT";
        //             await cmd.ExecuteNonQueryAsync();
        //         });
        //     }
        //     catch (DuckDBException ex)
        //     {
        //         Debug.WriteLine($"Pre-init recovery failed: {ex.Message}");
        //         await ForceWalCleanup();
        //     }
        // }

        // private async Task ForceWalCleanup()
        // {
        //     var dbPath = GetConnectionString();
        //     var files = new[] { dbPath + ".wal", dbPath + ".tmp", dbPath + ".lock" };
        //     foreach (var file in files)
        //     {
        //         try 
        //         { 
        //             if (File.Exists(file)) File.Delete(file); 
        //         }
        //         catch { /* Ignore */ }
        //     }
        //     await Task.Delay(100);
        // }
    }
}

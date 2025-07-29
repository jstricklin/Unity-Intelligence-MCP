using DuckDB.NET.Data;
using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data
{
    public class DuckDbApplicationDatabase : IDocumentationDatabase
    {
        private readonly string _databasePath;
        private static bool _isInitialized = false;

        public DuckDbApplicationDatabase(string databasePath = "application.duckdb")
        {
            _databasePath = Path.Combine(AppContext.BaseDirectory, databasePath);
        }

        public string GetConnectionString() => _databasePath;

        public async Task InitializeDatabaseAsync()
        {
            if (_isInitialized || File.Exists(_databasePath))
            {
                _isInitialized = true;
                return;
            }

            Console.Error.WriteLine("[Database] Initializing new DuckDB application database...");
            await using var connection = new DuckDBConnection($"DataSource = {GetConnectionString()}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = SchemaV1;
            await command.ExecuteNonQueryAsync();

            command.CommandText = InitialData;
            await command.ExecuteNonQueryAsync();
            
            _isInitialized = true;
            Console.Error.WriteLine("[Database] Database initialized successfully.");
        }

        private const string SchemaV1 = @"
            -- Source registry for different documentation types
            CREATE TABLE doc_sources (
                id BIGINT PRIMARY KEY,
                source_type VARCHAR NOT NULL UNIQUE,
                source_name VARCHAR NOT NULL,
                version VARCHAR,
                base_url VARCHAR,
                schema_version VARCHAR,
                last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            -- Universal document container
            CREATE TABLE unity_docs (
                id BIGINT PRIMARY KEY,
                source_id BIGINT NOT NULL,
                doc_key VARCHAR NOT NULL,
                title VARCHAR NOT NULL,
                url VARCHAR,
                doc_type VARCHAR,
                category VARCHAR,
                unity_version VARCHAR,
                content_hash VARCHAR,
                title_embedding BLOB,
                summary_embedding BLOB,
                FOREIGN KEY (source_id) REFERENCES doc_sources (id),
                UNIQUE(source_id, doc_key)
            );

            -- Source-specific structured data (JSON for flexibility)
            CREATE TABLE doc_metadata (
                id BIGINT PRIMARY KEY,
                doc_id BIGINT NOT NULL,
                metadata_type VARCHAR NOT NULL,
                metadata_json VARCHAR NOT NULL,
                FOREIGN KEY (doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
                UNIQUE(doc_id, metadata_type)
            );

            -- Flexible content elements
            CREATE TABLE content_elements (
                id BIGINT PRIMARY KEY,
                doc_id BIGINT NOT NULL,
                element_type VARCHAR NOT NULL,
                title VARCHAR,
                content VARCHAR,
                attributes_json VARCHAR,
                element_embedding BLOB,
                FOREIGN KEY (doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE
            );

            -- Granular content chunks for detailed semantic search
            CREATE TABLE content_chunks (
                id BIGINT PRIMARY KEY,
                doc_id BIGINT NOT NULL,
                element_id BIGINT,
                content VARCHAR NOT NULL,
                token_count INTEGER,
                embedding BLOB,
                FOREIGN KEY (doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
                FOREIGN KEY (element_id) REFERENCES content_elements (id) ON DELETE CASCADE
            );

            -- Cross-document relationships
            CREATE TABLE doc_relationships (
                id BIGINT PRIMARY KEY,
                source_doc_id BIGINT NOT NULL,
                target_doc_id BIGINT NOT NULL,
                relationship_type VARCHAR NOT NULL,
                FOREIGN KEY (source_doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
                FOREIGN KEY (target_doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
                UNIQUE(source_doc_id, target_doc_id, relationship_type)
            );

            -- Performance indices
            CREATE INDEX idx_docs_source_type ON unity_docs(source_id, doc_type);
            CREATE INDEX idx_elements_doc_type ON content_elements(doc_id, element_type);
            CREATE INDEX idx_metadata_doc ON doc_metadata(doc_id);
            CREATE INDEX idx_chunks_doc ON content_chunks(doc_id);

            -- Source-specific views for common queries
            CREATE VIEW scripting_api_docs AS
            SELECT 
                d.id,
                d.title,
                d.doc_key as file_path,
                json_extract_string(dm.metadata_json, '$.description') as description,
                json_extract_string(dm.metadata_json, '$.class_name') as class_name,
                json_extract_string(dm.metadata_json, '$.namespace') as namespace,
                d.title_embedding,
                d.summary_embedding
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
                json_extract_string(ce.attributes_json, '$.is_inherited') as is_inherited,
                d.title as class_name,
                json_extract_string(dm.metadata_json, '$.namespace') as namespace,
                ce.element_embedding
            FROM content_elements ce
            JOIN unity_docs d ON ce.doc_id = d.id
            JOIN doc_sources s ON d.source_id = s.id
            LEFT JOIN doc_metadata dm ON d.id = dm.doc_id AND dm.metadata_type = 'scripting_api'
            WHERE s.source_type = 'scripting_api'
            AND ce.element_type IN ('property', 'public_method', 'static_method', 'message');
        ";

        private const string InitialData = @"
            INSERT INTO doc_sources (id, source_type, source_name, version, schema_version) VALUES
            (1, 'scripting_api', 'Unity Scripting API', '2023.3', '1.0'),
            (2, 'editor_manual', 'Unity User Manual', '2023.3', '1.0'),
            (3, 'tutorial', 'Unity Learn Tutorials', 'current', '1.0');
        ";
    }
}

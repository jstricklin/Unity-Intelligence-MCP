using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data
{
    public class SqliteDocumentationDatabase : IDocumentationDatabase
    {
        private readonly string _databasePath;
        private readonly string _connectionString;
        private static bool _isInitialized = false;

        public SqliteDocumentationDatabase(string databasePath = "documentation.db")
        {
            _databasePath = Path.Combine(AppContext.BaseDirectory, databasePath);
            _connectionString = $"Data Source={_databasePath}";
        }

        public string GetConnectionString() => _connectionString;

        public async Task InitializeDatabaseAsync()
        {
            if (_isInitialized || File.Exists(_databasePath))
            {
                _isInitialized = true;
                return;
            }

            Console.Error.WriteLine("[Database] Initializing new documentation database...");
            await using var connection = new SqliteConnection(_connectionString);
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
            CREATE TABLE doc_sources (
                id INTEGER PRIMARY KEY,
                source_type TEXT NOT NULL UNIQUE,
                source_name TEXT NOT NULL,
                version TEXT,
                base_url TEXT,
                schema_version TEXT,
                last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            CREATE TABLE unity_docs (
                id INTEGER PRIMARY KEY,
                source_id INTEGER NOT NULL,
                doc_key TEXT NOT NULL,
                title TEXT NOT NULL,
                url TEXT,
                doc_type TEXT,
                category TEXT,
                unity_version TEXT,
                content_hash TEXT,
                title_embedding BLOB,
                summary_embedding BLOB,
                FOREIGN KEY (source_id) REFERENCES doc_sources (id),
                UNIQUE(source_id, doc_key)
            );
            CREATE TABLE doc_metadata (
                id INTEGER PRIMARY KEY,
                doc_id INTEGER NOT NULL,
                metadata_type TEXT NOT NULL,
                metadata_json TEXT NOT NULL,
                FOREIGN KEY (doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
                UNIQUE(doc_id, metadata_type)
            );
            CREATE TABLE content_elements (
                id INTEGER PRIMARY KEY,
                doc_id INTEGER NOT NULL,
                element_type TEXT NOT NULL,
                title TEXT,
                content TEXT,
                attributes_json TEXT,
                element_embedding BLOB,
                FOREIGN KEY (doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE
            );
            CREATE VIRTUAL TABLE vec_elements_index USING vss0(
                embedding(768)
            );
        ";

        private const string InitialData = @"
            INSERT INTO doc_sources (source_type, source_name, version, schema_version) VALUES
            ('scripting_api', 'Unity Scripting API', '2023.3', '1.0'),
            ('editor_manual', 'Unity User Manual', '2023.3', '1.0'),
            ('tutorial', 'Unity Learn Tutorials', 'current', '1.0');
        ";
    }
}

using DuckDB.NET.Data;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data.Contracts;

namespace UnityIntelligenceMCP.Core.Data.Infrastructure
{
    public class DuckDbConnectionFactory : IDuckDbConnectionFactory
    {
        private readonly string _dbPath;
        private readonly ILogger<DuckDbConnectionFactory> _logger;

        public DuckDbConnectionFactory(string dbPath, ILogger<DuckDbConnectionFactory> logger)
        {
            _dbPath = dbPath;
            _logger = logger;
        }

        public async Task<DuckDBConnection> GetConnectionAsync()
        {
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var connection = new DuckDBConnection($"DataSource={_dbPath}");
                    connection.Open();
                    return connection;
                }
                catch (DuckDBException ex) when (IsLockedDatabaseError(ex))
                {
                    _logger.LogWarning("Database locked (attempt {Attempt}/{MaxAttempts}): {Message}", 
                        attempt, maxAttempts, ex.Message);
                    
                    if (attempt == maxAttempts)
                    {
                        _logger.LogError("Failed to unlock database after {MaxAttempts} attempts", maxAttempts);
                        throw;
                    }
                    
                    await TryRecoverDatabaseAsync();
                    await Task.Delay(500 * attempt); // Backoff delay
                }
            }
            throw new InvalidOperationException("Unexpected connection flow");
        }

        private bool IsLockedDatabaseError(DuckDBException ex)
        {
            return ex.Message.Contains("database is locked") ||
                   ex.Message.Contains("Could not set lock") ||
                   ex.Message.Contains("out of memory");
        }

        private async Task TryRecoverDatabaseAsync()
        {
            try
            {
                // Try graceful checkpoint first
                _logger.LogInformation("Attempting checkpoint...");
                using var conn = new DuckDBConnection($"DataSource={_dbPath};access_mode=READ_ONLY");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CHECKPOINT";
                cmd.ExecuteNonQuery();
                _logger.LogInformation("Checkpoint successful");
            }
            catch (Exception checkpointEx)
            {
                _logger.LogWarning("Checkpoint failed: {Message}", checkpointEx.Message);
                await ForceWalCleanup();
            }
        }

        private async Task ForceWalCleanup()
        {
            try
            {
                _logger.LogWarning("Forcing WAL cleanup...");
                var filesToDelete = new[] { _dbPath + ".wal", _dbPath + ".tmp", _dbPath + ".lock" };
                
                foreach (var file in filesToDelete)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        _logger.LogInformation("Deleted {File}", Path.GetFileName(file));
                    }
                }
                await Task.Delay(300); // Allow OS to release resources
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WAL cleanup failed");
            }
        }

        public async Task<T> ExecuteWithConnectionAsync<T>(Func<DuckDBConnection, Task<T>> operation)
        {
            await using var connection = await GetConnectionAsync();
            return await operation(connection);
        }

        public async Task ExecuteWithConnectionAsync(Func<DuckDBConnection, Task> operation)
        {
            await using var connection = await GetConnectionAsync();
            await operation(connection);
        }
    }
}

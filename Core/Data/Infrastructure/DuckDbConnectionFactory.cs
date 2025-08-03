using DuckDB.NET.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data.Contracts;

namespace UnityIntelligenceMCP.Core.Data.Infrastructure
{
    public class DuckDbConnectionFactory : IDuckDbConnectionFactory
    {
        private readonly IApplicationDatabase _appDb;
        private int _connectionCounter = 0;

        public DuckDbConnectionFactory(IApplicationDatabase appDb)
        {
            _appDb = appDb;
        }

        public async Task<DuckDBConnection> GetConnectionAsync()
        {
            var connId = Interlocked.Increment(ref _connectionCounter);
            // Console.Error.WriteLine($"[Conn #{connId}] Opening connection...");
            var connection = new DuckDBConnection($"DataSource = {_appDb.GetConnectionString()}");
            await connection.OpenAsync();
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                LOAD vss;";
            await cmd.ExecuteNonQueryAsync();
            // Console.Error.WriteLine($"[Conn #{connId}] VSS loaded and validated");
            return connection;
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

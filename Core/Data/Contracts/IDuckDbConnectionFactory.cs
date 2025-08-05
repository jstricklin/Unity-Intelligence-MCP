using DuckDB.NET.Data;
using System;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data.Contracts
{
    public interface IDuckDbConnectionFactory
    {
        Task<DuckDBConnection> GetConnectionAsync();
        string GetConnectionString();
        Task TryRecoverDatabaseAsync();
        Task<T> ExecuteWithConnectionAsync<T>(Func<DuckDBConnection, Task<T>> operation);
        
        Task ExecuteWithConnectionAsync(Func<DuckDBConnection, Task> operation);
    }
}

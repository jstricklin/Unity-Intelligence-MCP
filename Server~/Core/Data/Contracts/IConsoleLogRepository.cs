using System.Collections.Generic;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Data.Contracts
{
    public interface IConsoleLogRepository
    {
        Task InsertBatchAsync(IEnumerable<ConsoleLogEntry> logs, CancellationToken cancellationToken);
        Task<IEnumerable<ConsoleLogEntry>> GetLogsAsync(string logTypeFilter, int page, int pageSize);
        Task PruneOldLogsAsync(int retentionHours);
    }
}

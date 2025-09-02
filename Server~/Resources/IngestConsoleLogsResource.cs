using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class IngestConsoleLogsResource
    {
        private readonly IConsoleLogRepository _logRepository;

        public IngestConsoleLogsResource(IConsoleLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        [McpServerResource(Name = "ingest_console_logs"), Description("Accepts a batch of console logs from the Unity Editor for caching.")]
        public async Task<string> IngestLogs(List<ConsoleLogEntry> logs)
        {
            if (logs == null || logs.Count == 0) return "No logs received.";

            await _logRepository.InsertBatchAsync(logs);
            return $"Successfully ingested {logs.Count} log entries.";
        }
    }
}

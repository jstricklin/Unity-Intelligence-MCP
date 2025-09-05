using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Utilities;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class ConsoleLogsResource
    {
        private readonly IConsoleLogRepository _logRepository;

        public ConsoleLogsResource(IConsoleLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        [McpServerResource(Name = "get_console_logs"), Description("Retrieves cached console logs from the Unity Editor.")]
        public async Task<TextResourceContents> GetConsoleLogs(
            [Description("Filter logs by type (e.g., Error, Warning, Log)")] 
            string logType = "",
            [Description("Page number for pagination")] 
            int page = 1,
            [Description("Number of results per page")] 
            int pageSize = 100)
        {
            var logs = await _logRepository.GetLogsAsync(logType, page, pageSize);
            return ResourceParser.ParseTextResourceContents(JsonSerializer.Serialize(logs));
        }
    }
}

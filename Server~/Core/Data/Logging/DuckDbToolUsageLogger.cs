using System.Text.Json;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Data.Logging
{
    public class DuckDbToolUsageLogger : IToolUsageLogger
    {
        private readonly IDuckDbConnectionFactory _dbFactory;
        public DuckDbToolUsageLogger(IDuckDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public ToolUsageLog Parse(string toolData, ToolUsageLog? log = null)
        {
            var usageLog = log ?? new ToolUsageLog();
            using var doc = JsonDocument.Parse(toolData);
            var root = doc.RootElement;

            if (log == null) // This is a request
            {
                usageLog.UsageType = root.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "resource"
                    ? "resource"
                    : "tool";
                if (usageLog.UsageType == "resource" && root.TryGetProperty("resource_uri", out var uriElement))
                {
                    usageLog.ResourceUri = uriElement.GetString();
                }
            }

            foreach (var prop in root.EnumerateObject())
            {
                switch (prop.Name)
                {
                    case "command":
                        usageLog.OperationName = prop.Value.ToString();
                        break;
                    case "parameters":
                        usageLog.ParametersJson = prop.Value.ToString();
                        break;
                    case "data":
                        if (usageLog.UsageType == "resource")
                        {
                            var summary = new
                            {
                                MimeType = "application/json",
                                TextLength = prop.Value.ToString().Length,
                                ResourceUri = usageLog.ResourceUri
                            };
                            usageLog.ResultSummaryJson = JsonSerializer.Serialize(summary);
                        }
                        else
                        {
                            var summary = new
                            {
                                MimeType = "application/json",
                                TextLength = prop.Value.ToString().Length,
                            };
                            usageLog.ResultSummaryJson = JsonSerializer.Serialize(summary);
                            // usageLog.ResultSummaryJson = prop.Value.ToString();
                        }
                        break;
                    case "success":
                        usageLog.WasSuccessful = prop.Value.GetBoolean();
                        break;
                    case "execution_time":
                        usageLog.ExecutionTimeMs = (long)prop.Value.GetDouble();
                        break;
                    default: break;
                }
            }
            return usageLog;
        }

        public async Task LogAsync(ToolUsageLog log)
        {
            await _dbFactory.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO tool_usage_log (
                        tool_name, 
                        parameters_json, 
                        result_summary_json, 
                        execution_time_ms, 
                        was_successful, 
                    ) VALUES ($tool_name, $parameters, $summary, $execution_time, $success);
                ";
                cmd.Parameters.Add(new DuckDBParameter("tool_name", log.ToolName));
                cmd.Parameters.Add(new DuckDBParameter("parameters", log.ParametersJson));
                cmd.Parameters.Add(new DuckDBParameter("summary", log.ResultSummaryJson));
                cmd.Parameters.Add(new DuckDBParameter("execution_time", log.ExecutionTimeMs));
                cmd.Parameters.Add(new DuckDBParameter("success", log.WasSuccessful));
                
                await cmd.ExecuteNonQueryAsync();
            });
        }
    }
}

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
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                switch (prop.Name)
                {
                    case "type":
                        usageLog.UsageType = prop.Value.ToString();
                        break;
                    case "command":
                        usageLog.OperationName = prop.Value.ToString();
                        break;
                    case "resource_uri":
                        usageLog.ResourceUri = prop.Value.ToString();
                        break;
                    case "parameters":
                        usageLog.ParametersJson = prop.Value.ToString();
                        break;
                    case "data":
                        usageLog.ResultSummaryJson = prop.Value.ToString();
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
                        usage_type,
                        operation_name,
                        resource_uri,
                        parameters_json, 
                        result_summary_json, 
                        execution_time_ms, 
                        was_successful
                    ) VALUES ($usage_type, $operation_name, $resource_uri, $parameters, $summary, $execution_time, $success);
                ";
                cmd.Parameters.Add(new DuckDBParameter("usage_type", log.UsageType));
                cmd.Parameters.Add(new DuckDBParameter("operation_name", log.OperationName));
                cmd.Parameters.Add(new DuckDBParameter("resource_uri", log.ResourceUri));
                cmd.Parameters.Add(new DuckDBParameter("parameters", log.ParametersJson));
                cmd.Parameters.Add(new DuckDBParameter("summary", log.ResultSummaryJson));
                cmd.Parameters.Add(new DuckDBParameter("execution_time", log.ExecutionTimeMs));
                cmd.Parameters.Add(new DuckDBParameter("success", log.WasSuccessful));
                
                await cmd.ExecuteNonQueryAsync();
            });
        }
    }
}

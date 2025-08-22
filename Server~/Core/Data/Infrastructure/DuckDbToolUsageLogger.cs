using System.Threading.Tasks;
using DuckDB.NET.Data;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Data.Infrastructure
{
    public class DuckDbToolUsageLogger : IToolUsageLogger
    {
        private readonly IDuckDbConnectionFactory _dbFactory;

        public DuckDbToolUsageLogger(IDuckDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
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
                        peak_process_memory_mb
                    ) VALUES ($tool_name, $parameters, $summary, $execution_time, $success, $memory);
                ";
                cmd.Parameters.Add(new DuckDBParameter("tool_name", log.ToolName));
                cmd.Parameters.Add(new DuckDBParameter("parameters", log.ParametersJson));
                cmd.Parameters.Add(new DuckDBParameter("summary", log.ResultSummaryJson));
                cmd.Parameters.Add(new DuckDBParameter("execution_time", log.ExecutionTimeMs));
                cmd.Parameters.Add(new DuckDBParameter("success", log.WasSuccessful));
                cmd.Parameters.Add(new DuckDBParameter("memory", (object?)log.PeakProcessMemoryMb ?? System.DBNull.Value));
                
                await cmd.ExecuteNonQueryAsync();
            });
        }
    }
}

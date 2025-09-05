using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models.Database;
using DuckDB.NET.Data;
using System.Text.Json;

namespace UnityIntelligenceMCP.Core.Data.Infrastructure
{
    public class ConsoleLogRepository : IConsoleLogRepository
    {
        private readonly IDuckDbConnectionFactory _connectionFactory;
        private readonly IDbWorkQueue _workQueue;
        private readonly IMCPUsageLogger _usageLogger;

        public ConsoleLogRepository(IDbWorkQueue workQueue, IMCPUsageLogger usageLogger, IDuckDbConnectionFactory connectionFactory)
        {
            _workQueue = workQueue;
            _usageLogger = usageLogger;
            _connectionFactory = connectionFactory;
        }

        public async Task InsertBatchAsync(IEnumerable<ConsoleLogEntry> logs, CancellationToken cancellationToken)
        {
            await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                using var appender = connection.CreateAppender("unity_console_logs");
                foreach (var log in logs)
                {
                    var row = appender.CreateRow();
                    row.AppendValue(log.Timestamp);
                    row.AppendValue(log.LogType);
                    row.AppendValue(log.Message);
                    row.AppendValue(log.SourceFile);
                    row.AppendValue(log.Line);
                    row.AppendValue(log.StackTrace);
                    row.EndRow();
                }
                await transaction.CommitAsync(cancellationToken);
            });
        }

        //TODO Enhance with log deduplication
        public async Task<IEnumerable<ConsoleLogEntry>> GetLogsAsync(string logTypeFilter, int page, int pageSize)
        {
            var offset = (page - 1) * pageSize;
            var sql = "SELECT * FROM unity_console_logs";
            var parameters = new Dictionary<string, object>
            {
                { "limit", pageSize },
                { "offset", offset }
            };

            if (!string.IsNullOrEmpty(logTypeFilter))
            {
                sql += " WHERE log_type = $logType";
                parameters.Add("logType", logTypeFilter);
            }
            
            sql += " ORDER BY timestamp DESC LIMIT $limit OFFSET $offset";

            return await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                foreach(var p in parameters)
                {
                    command.Parameters.Add(new DuckDBParameter(p.Key, p.Value));
                }

                var reader = await command.ExecuteReaderAsync();
                var results = new List<ConsoleLogEntry>();
                while (await reader.ReadAsync())
                {
                    results.Add(new ConsoleLogEntry
                    {
                        Timestamp = reader.GetDateTime(0),
                        LogType = reader.GetString(1),
                        Message = reader.GetString(2),
                        SourceFile = reader.GetString(3),
                        Line = reader.GetInt32(4),
                        StackTrace = reader.GetString(5)
                    });
                }
                await _usageLogger.LogAsync(new MCPUsageLog
                {
                    CommandName = "get_console_logs",
                    UsageType = "resource",
                    ParametersJson = JsonSerializer.Serialize(parameters),
                    ResultSummaryJson = JsonSerializer.Serialize(results),
                    // ExecutionTimeMs = 0,
                    WasSuccessful = true,
                });

                return results;
            });
        }

        public async Task PruneOldLogsAsync(int retentionHours)
        {
            var cutoff = DateTime.UtcNow.AddHours(-retentionHours);
            var sql = "DELETE FROM unity_console_logs WHERE Timestamp < $cutoff";
            
            await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.Add(new DuckDBParameter("cutoff", cutoff));
                await command.ExecuteNonQueryAsync();
            });
        }
    }
}

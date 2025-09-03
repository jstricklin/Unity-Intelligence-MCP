using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Commands.Contracts;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Commands
{
    public class IngestConsoleLogsCommand : IServerCommand
    {

        public string CommandName { get; } = "ingest_console_logs";
        IDbWorkQueue _workQueue;
        public IngestConsoleLogsCommand(IDbWorkQueue workQueue)
        {
            _workQueue = workQueue;
        }

        public async Task<object> ExecuteCommand(string? data = null)
        {
            var logs = JsonSerializer.Deserialize<ConsoleLogEntryBatch>(data!)?.logs;
            if (logs == null || logs.Count == 0) return new
            {
                type = "response",
                success = false,
                error = new
                {
                    message = "No logs received."
                }
            };

            foreach (ConsoleLogEntry log in logs)
            {
                await _workQueue.EnqueueAsync(log);
            }
            // Console.Error.WriteLine($"Succesfully added {logs.Count} log {(logs.Count > 1 ? "entries" : "entry")} to be processed");
            return new
            {
                type = "response",
                success = true,
                message = $"Successfully added {logs.Count} log {(logs.Count > 1 ? "entries" : "entry")} to be processed."
            };
        }
    }
}
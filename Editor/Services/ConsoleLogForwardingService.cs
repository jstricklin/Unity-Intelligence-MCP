using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Unity;

namespace UnityIntelligenceMCP.Editor.Services
{
    public static class ConsoleLogForwardingService
    {
        private static readonly ConcurrentQueue<Models.ConsoleLogEntry> _logBuffer = new ConcurrentQueue<Models.ConsoleLogEntry>();
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private const int BatchSize = 50;
        private static readonly TimeSpan BatchInterval = TimeSpan.FromSeconds(2);

        public static void Initialize()
        {
            Application.logMessageReceivedThreaded += HandleLog;
            Task.Run(() => ProcessBuffer(_cancellationTokenSource.Token));
            EditorApplication.quitting += OnEditorQuitting;
        }

        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            var firstLineOfStack = stackTrace.Split('\n')[0];
            var fileInfo = firstLineOfStack.Contains(" (at ") ? firstLineOfStack.Substring(firstLineOfStack.IndexOf(" (at ", StringComparison.Ordinal) + 5) : "N/A";
            if (fileInfo.EndsWith(")"))
            {
                fileInfo = fileInfo.Substring(0, fileInfo.Length - 1);
            }
            var parts = fileInfo.Split(':');
            var sourceFile = parts.Length > 0 ? parts[0] : "N/A";
            int.TryParse(parts.Length > 1 ? parts[1] : "0", out var line);

            _logBuffer.Enqueue(new Models.ConsoleLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = logString,
                StackTrace = stackTrace,
                LogType = type.ToString(),
                SourceFile = sourceFile,
                Line = line
            });
        }

        private static async Task ProcessBuffer(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(BatchInterval, token);
                await SendBatch();
            }
        }

        private static async Task SendBatch()
        {
            var logsToSend = new List<Models.ConsoleLogEntry>();
            while (logsToSend.Count < BatchSize && _logBuffer.TryDequeue(out var log))
            {
                logsToSend.Add(log);
            }

            if (logsToSend.Count > 0)
            {
                var request = new
                {
                    command = "ingest_console_logs",
                    type = "resource",
                    parameters = new { logs = logsToSend }
                };
                
                var jsonPayload = JsonConvert.SerializeObject(request);
                await UnityIntelligenceMCPServer.Instance.Send(jsonPayload);
            }
        }

        private static void OnEditorQuitting()
        {
            _cancellationTokenSource.Cancel();
            SendBatch().Wait();
            Application.logMessageReceivedThreaded -= HandleLog;
        }
    }
}

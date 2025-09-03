using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Commands.Contracts;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Core.Handlers.Contracts;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Handlers
{
    public class MessageHandler : IMessageHandler
    {
        private readonly ILogger<MessageHandler> _logger;
        private readonly ICommandService _commandService;
        private readonly IToolUsageLogger _toolLogger;

        public MessageHandler(
        ICommandService commandService, 
        IToolUsageLogger toolLogger, 
        ILogger<MessageHandler> logger)
        {
            _commandService = commandService;
            _toolLogger = toolLogger;
            _logger = logger;
        }

        public async Task<string> ProcessMessageAsync(string message, ConcurrentDictionary<string, (ToolUsageLog usageLog, TaskCompletionSource<string> tcs)> pendingRequests)
        {
            // _logger.LogInformation("Processing message from Unity Editor: {message}", message);
            // Message processing logic will go here.

            using var doc = JsonDocument.Parse(message);
            if (doc.RootElement.TryGetProperty("request_id", out var requestIdElement))
            {
                // Message is response data from MCP request to Editor
                var requestId = requestIdElement.GetString();
                if (requestId != null && pendingRequests.TryRemove(requestId, out var request))
                {
                    request.tcs.SetResult(message);
                    _toolLogger.Parse(message, request.usageLog);
                    await _toolLogger.LogAsync(request.usageLog);
                }
            }
            else 
            {
                // TODO refactor this method to handle proper message type (response/request) to handle eventual tool requests from the Editor
                // Message is a request from Editor to MCP Server
                if (doc.RootElement.TryGetProperty("command", out var commandElement))
                {
                    doc.RootElement.TryGetProperty("parameters", out var parametersElement);
                    var response = await _commandService.ExecuteCommand(commandElement.ToString(), parametersElement.ToString());
                    // _logger.LogInformation($"Sending Command response to Unity Editor: {JsonSerializer.Serialize(response)}");
                    await EditorBridgeClientService.SendResponseToUnity(JsonSerializer.Serialize(response));
                }
                else 
                {
                    _logger.LogInformation($"Message from Unity Editor: {message}");
                }
            }
            return JsonSerializer.Serialize(new
            {
                success = true
            });
        }
    }
}

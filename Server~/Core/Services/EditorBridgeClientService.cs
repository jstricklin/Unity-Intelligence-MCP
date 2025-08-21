using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UnityIntelligenceMCP.Core.Services
{
    // This class exists as an alternative to the current architecture where this MCP server acts as a client to Unity Engine Websocket Server
    public class EditorBridgeClientService : BackgroundService
    {
        private readonly ILogger<EditorBridgeClientService> _logger;
        private ClientWebSocket _ws = new();
        
        public EditorBridgeClientService(ILogger<EditorBridgeClientService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync(stoppingToken);
                    await ProcessMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Editor bridge connection failed");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private async Task ConnectAsync(CancellationToken ct)
        {
            var uri = new Uri("ws://localhost:4649/mcp-bridge");
            await _ws.ConnectAsync(uri, ct);
            _logger.LogInformation("Connected to Unity Editor bridge");
        }

        private async Task ProcessMessagesAsync(CancellationToken ct)
        {
            var buffer = new byte[4096];
            while (_ws.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    _logger.LogInformation("Received: {0}", 
                        Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
            }
        }

        public async Task SendAsync(string jsonPayload, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonPayload);
            await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }
    }
}

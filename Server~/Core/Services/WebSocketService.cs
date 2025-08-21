using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Services.Contracts;

namespace UnityIntelligenceMCP.Core.Services
{
    public class WebSocketService
    {
        private readonly IMessageHandler _messageHandler;
        private readonly ILogger<WebSocketService> _logger;

        public WebSocketService(IMessageHandler messageHandler, ILogger<WebSocketService> logger)
        {
            _messageHandler = messageHandler;
            _logger = logger;
        }

        public async Task HandleConnectionAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation("WebSocket connection established.");
            
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        receiveResult.CloseStatus.Value,
                        receiveResult.CloseStatusDescription,
                        CancellationToken.None);
                    _logger.LogInformation("WebSocket connection closed by client.");
                    break;
                }

                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                    await _messageHandler.ProcessMessageAsync(message, webSocket);
                }
            }
        }
    }
}

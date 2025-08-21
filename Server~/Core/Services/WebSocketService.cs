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
        private WebSocket? _activeSocket;

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

            _activeSocket = await context.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation("WebSocket connection established.");
            
            var buffer = new byte[1024 * 4];

            try
            {
                while (_activeSocket.State == WebSocketState.Open)
                {
                    var receiveResult = await _activeSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await _activeSocket.CloseAsync(
                            receiveResult.CloseStatus.Value,
                            receiveResult.CloseStatusDescription,
                            CancellationToken.None);
                        _logger.LogInformation("WebSocket connection closed by client.");
                        break;
                    }

                    if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                        await _messageHandler.ProcessMessageAsync(message, _activeSocket);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the WebSocket connection.");
            }
            finally
            {
                _activeSocket?.Dispose();
                _activeSocket = null;
                _logger.LogInformation("WebSocket connection terminated.");
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (_activeSocket == null || _activeSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning("Attempted to send message, but no active connection is available.");
                return;
            }

            var messageBytes = Encoding.UTF8.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(messageBytes, 0, messageBytes.Length);
            await _activeSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

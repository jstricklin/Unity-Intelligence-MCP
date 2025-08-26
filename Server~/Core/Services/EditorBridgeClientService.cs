using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnityIntelligenceMCP.Configuration;

namespace UnityIntelligenceMCP.Core.Services
{
    public class EditorBridgeClientService : BackgroundService
    {
        private readonly ILogger<EditorBridgeClientService> _logger;
        private readonly ConfigurationService _configurationService;
        private ClientWebSocket _ws = new();
        private static EditorBridgeClientService? _instance;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
        public EditorBridgeClientService(
            ConfigurationService configurationService,
            ILogger<EditorBridgeClientService> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
            _instance = this;
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

            var uri = new Uri($"ws://localhost:{_configurationService.UnitySettings.SERVER_PORT}/mcp-bridge");
            _ws?.Dispose();
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(uri, ct);
            _logger.LogInformation("Connected to Unity Editor bridge");
        }

        private async Task ProcessMessagesAsync(CancellationToken ct)
        {
            var buffer = new byte[4096];
            var stringBuilder = new StringBuilder();
            WebSocketReceiveResult result;
            string responseJson = "";
            while (_ws.State == WebSocketState.Open)
            {
                do
                {
                    result = await _ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                } while (!result.EndOfMessage);
                try
                {
                    responseJson = stringBuilder.ToString();
                    _logger.LogInformation("Received: {0}", responseJson);
                    using var doc = JsonDocument.Parse(responseJson);
                    if (doc.RootElement.TryGetProperty("request_id", out var requestIdElement))
                    {
                        var requestId = requestIdElement.GetString();
                        if (requestId != null && _pendingRequests.TryRemove(requestId, out var tcs))
                        {
                            tcs.SetResult(responseJson);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse incoming JSON message from Unity.");
                }
                finally
                {
                    stringBuilder.Clear();
                }
            }
        }
        public static Task<string> SendMessageToUnity(string jsonPayload)
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("Editor Bridge is not initialized.");
            }
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return _instance.SendRequestAsync(jsonPayload, cts.Token);
        }
        public async Task<string> SendRequestAsync(string jsonPayload, CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string>();

            if (!_pendingRequests.TryAdd(requestId, tcs))
            {
                throw new InvalidOperationException("Could not register a pending request.");
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonPayload);
                using var ms = new System.IO.MemoryStream();
                using (var writer = new Utf8JsonWriter(ms))
                {
                    writer.WriteStartObject();
                    writer.WriteString("request_id", requestId);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        prop.WriteTo(writer);
                    }
                    writer.WriteEndObject();
                }
                
                var bytes = ms.ToArray();
                var messageWithId = Encoding.UTF8.GetString(bytes);

                _logger.LogInformation("Sending: {0}", messageWithId);
                await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);

                using (ct.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task;
                }
            }
            finally
            {
                _pendingRequests.TryRemove(requestId, out _);
            }
        }

        public override void Dispose()
        {
            _ws?.Dispose();
            base.Dispose();
        }
    }
}

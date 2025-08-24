using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Tools;

namespace UnityIntelligenceMCP.Core.Services
{
    public class EditorBridgeClientService : BackgroundService
    {
        private readonly ILogger<EditorBridgeClientService> _logger;
        private readonly ConfigurationService _configurationService;
        private ClientWebSocket _ws = new();
        delegate Task UnityMessageHandler(string jsonPayload, CancellationToken cts);
        static event UnityMessageHandler? HandleMessageToUnity;
        public EditorBridgeClientService(
            ConfigurationService configurationService,
            ILogger<EditorBridgeClientService> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
            HandleMessageToUnity += SendAsync;
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
        public static Task SendMessageToUnity(string jsonPayload)
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return HandleMessageToUnity?.Invoke(jsonPayload, cts.Token) ?? throw new Exception("Unity Editor Bridge has not been configured.");
        }
        public async Task SendAsync(string jsonPayload, CancellationToken ct)
        {
            _logger.LogInformation("Sending: {0}", jsonPayload);
            var bytes = Encoding.UTF8.GetBytes(jsonPayload);
            await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
        }

        public override void Dispose()
        {
            _ws?.Dispose();
            base.Dispose();
        }
    }
}

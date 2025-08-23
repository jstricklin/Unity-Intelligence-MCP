using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Services.Contracts;

namespace UnityIntelligenceMCP.Core.Services
{
    public class MessageHandler : IMessageHandler
    {
        private readonly ILogger<MessageHandler> _logger;

        public MessageHandler(ILogger<MessageHandler> logger)
        {
            _logger = logger;
        }

        public Task ProcessMessageAsync(string message, WebSocket socket)
        {
            _logger.LogInformation("Processing message from server: {message}", message);
            // Message processing logic will go here.
            return Task.CompletedTask;
        }
    }
}

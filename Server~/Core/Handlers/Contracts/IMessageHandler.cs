using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Handlers.Contracts
{
    public interface IMessageHandler
    {
        Task<string> ProcessMessageAsync(string message, ConcurrentDictionary<string, (ToolUsageLog usageLog, TaskCompletionSource<string> tcs)> pendingRequests);
        // Task ProcessMessageAsync(string message, WebSocket socket);
    }
}

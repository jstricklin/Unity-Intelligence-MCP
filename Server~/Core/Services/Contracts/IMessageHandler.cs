using System.Net.WebSockets;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Services.Contracts
{
    public interface IMessageHandler
    {
        Task ProcessMessageAsync(string message, WebSocket socket);
    }
}

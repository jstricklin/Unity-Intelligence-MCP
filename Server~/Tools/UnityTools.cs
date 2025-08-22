using System.ComponentModel;
using UnityIntelligenceMCP.Core.Services;
using ModelContextProtocol.Server;

namespace UnityIntelligenceMCP.Tools
{

    [McpServerToolType]
    public class UnityTools
    {
        private readonly WebSocketService _webSocketService;

        public UnityTools(WebSocketService webSocketService)
        {
            _webSocketService = webSocketService;
        }

        [McpServerTool(Name = "create_primitive"), Description("Create a primitive object in Unity.")]
        public async Task CreatePrimitive(
            String type = "Sphere",
            CancellationToken cancellationToken = default)
        {
            await _webSocketService.SendMessageAsync("{ 'command': 'create_primitive', 'parameters': { 'type':'Sphere', 'name':'MCP sphere', 'position': '[0,0,0]'} }");
        }
    }
}
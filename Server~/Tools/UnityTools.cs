using System.ComponentModel;
using UnityIntelligenceMCP.Core.Services;
using ModelContextProtocol.Server;
using System.Text.Json;
using System.Numerics;

namespace UnityIntelligenceMCP.Tools
{

    [McpServerToolType]
    public class UnityTools
    {
        private readonly EditorBridgeClientService _editorBridgeClientService;

        public UnityTools(EditorBridgeClientService editorBridgeClientService)
        {
            _editorBridgeClientService = editorBridgeClientService;
        }

        [McpServerTool(Name = "create_primitive"), Description("Create a primitive object in Unity.")]
        public async Task CreatePrimitive(
            String type = "Sphere",
            CancellationToken cancellationToken = default)
        {
            var command = new ToolRequest();
            command.command = "create_primitive";
            command.parameters = new Dictionary<string, string> {
                { "type", "Sphere" },
                { "name", "MCP Sphere" },
                { "position", Vector3.Zero.ToString() },
            };
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await _editorBridgeClientService.SendAsync(JsonSerializer.Serialize(command), cts.Token);
        }
        class ToolRequest 
        {
            public string command = "";
            public Dictionary<string,string> parameters = null;
        }
    }
}
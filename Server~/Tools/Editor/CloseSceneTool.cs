using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools.Editor
{
    [McpServerToolType]
    public class CloseSceneTool
    {
        [McpServerTool(Name = "close_scene"), Description("Closes/unloads an open scene in the Unity Editor.")]
        public async Task<string> CloseScene(
            [Description("The name of the scene to close (e.g., 'MyScene').")] string sceneName)
        {
            var command = new UnityToolRequest
            {
                command = "close_scene",
            };
            command.parameters["sceneName"] = sceneName;

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }
    }
}

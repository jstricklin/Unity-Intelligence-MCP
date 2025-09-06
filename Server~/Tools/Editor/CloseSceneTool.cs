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
        [McpServerTool(Name = "close_scene"), Description("Closes an open scene in the Unity Editor. Can optionally save changes in the scene before closing.")]
        public async Task<string> CloseScene(
            [Description("The name of the scene to close (e.g., 'MyScene').")] string sceneName,
            [Description("If true, saves any unsaved changes in the scene before closing.")] bool saveChanges = false)
        {
            var command = new UnityToolRequest
            {
                command = "close_scene",
            };
            command.parameters["sceneName"] = sceneName;
            command.parameters["saveChanges"] = saveChanges;

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }
    }
}

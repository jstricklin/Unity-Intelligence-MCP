using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools.Editor
{
    [McpServerToolType]
    public class ChangeSceneTool
    {
        [McpServerTool(Name = "change_scene"), Description("Changes the current active scene in the Unity Editor. Can optionally save changes in the current scene before switching.")]
        public async Task<string> ChangeScene(
            [Description("The project-relative path to the scene file (e.g., 'Assets/Scenes/MyScene.unity').")] string scenePath,
            [Description("If true, saves any unsaved changes in the current scene before changing.")] bool saveChanges = false)
        {
            var command = new UnityToolRequest
            {
                command = "change_scene",
            };
            command.parameters["scenePath"] = scenePath;
            command.parameters["saveChanges"] = saveChanges;

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }
    }
}

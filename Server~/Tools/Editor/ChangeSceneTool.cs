using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools.Editor
{
    [McpServerToolType]
    public class OpenSceneTool
    {
        [McpServerTool(Name = "open_scene"), Description("Opens a scene in the Unity Editor. Can optionally save changes in the current scene and/or load the new scene additively.")]
        public async Task<string> OpenScene(
            [Description("The name of the scene to open (e.g., 'MyScene'). The scene must exist in the project assets.")] string sceneName,
            [Description("If true, saves any unsaved changes in the current scene before changing.")] bool saveChanges = false,
            [Description("If true, loads the scene additively on top of the current scene(s).")] bool additive = false)
        {
            var command = new UnityToolRequest
            {
                command = "open_scene",
            };
            command.parameters["sceneName"] = sceneName;
            command.parameters["saveChanges"] = saveChanges;
            command.parameters["additive"] = additive;

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }
    }
}

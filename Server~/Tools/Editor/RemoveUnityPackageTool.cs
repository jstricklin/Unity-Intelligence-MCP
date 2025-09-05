using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools.Editor
{
    [McpServerToolType]
    public class RemoveUnityPackageTool
    {
        [McpServerTool(Name = "remove_unity_package"), Description("Removes a package from the Unity project using its name (e.g., 'com.unity.vectorgraphics').")]
        public async Task<string> RemovePackage([Description("The package name to remove.")] string name)
        {
            var command = new UnityToolRequest
            {
                command = "remove_package",
            };
            command.parameters["name"] = name;

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }
    }
}

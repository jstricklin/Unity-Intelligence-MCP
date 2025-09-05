using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools.Editor
{
    [McpServerToolType]
    public class AddUnityPackageTool
    {
        [McpServerTool(Name = "add_unity_package"), Description("Adds a package to the Unity project using its identifier (e.g., 'com.unity.vectorgraphics' or a git URL).")]
        public async Task<string> AddPackage([Description("The package identifier to add.")] string identifier)
        {
            var command = new UnityToolRequest
            {
                command = "add_package",
            };
            command.parameters["identifier"] = identifier;

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }
    }
}

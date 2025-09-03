using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Services;

namespace UnityIntelligenceMCP.Tools.Editor
{
    [McpServerToolType]
    public class AddUnityPackageTool
    {
        [McpServerTool(Name = "add_unity_package"), Description("Adds a package to the Unity project using its identifier (e.g., 'com.unity.vectorgraphics' or a git URL).")]
        public async Task<string> AddPackage([Description("The package identifier to add.")] string identifier)
        {
            var toolRequest = new JObject
            {
                ["command"] = "add_package",
                ["parameters"] = new JObject { ["identifier"] = identifier }
            };

            return await EditorBridgeClientService.SendMessageToUnity(toolRequest.ToString());
        }
    }
}

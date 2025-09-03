using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Services;

namespace UnityIntelligenceMCP.Tools.Editor
{
    [McpServerToolType]
    public class RemoveUnityPackageTool
    {
        [McpServerTool(Name = "remove_unity_package"), Description("Removes a package from the Unity project using its name (e.g., 'com.unity.vectorgraphics').")]
        public async Task<string> RemovePackage([Description("The package name to remove.")] string name)
        {
            var toolRequest = new JObject
            {
                ["command"] = "remove_package",
                ["parameters"] = new JObject { ["name"] = name }
            };

            return await EditorBridgeClientService.SendMessageToUnity(toolRequest.ToString());
        }
    }
}

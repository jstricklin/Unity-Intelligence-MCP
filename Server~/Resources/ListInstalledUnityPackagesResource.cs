using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Services;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerToolType]
    public class ListInstalledUnityPackagesResource
    {
        [McpServerTool(Name = "list_installed_unity_packages"), Description("Lists all packages currently installed in the Unity project.")]
        public async Task<string> ListInstalledPackages()
        {
            var resourceRequest = new JObject
            {
                ["resourceUri"] = "packages/installed",
                ["parameters"] = new JObject()
            };

            return await EditorBridgeClientService.SendMessageToUnity(resourceRequest.ToString());
        }
    }
}

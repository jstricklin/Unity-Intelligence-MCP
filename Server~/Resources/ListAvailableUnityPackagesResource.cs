using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Services;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerToolType]
    public class ListAvailableUnityPackagesResource
    {
        [McpServerTool(Name = "list_available_unity_packages"), Description("Lists all available packages from the Unity package registry.")]
        public async Task<string> ListAvailablePackages()
        {
            var resourceRequest = new JObject
            {
                ["resourceUri"] = "packages/available",
                ["parameters"] = new JObject()
            };

            return await EditorBridgeClientService.SendMessageToUnity(resourceRequest.ToString());
        }
    }
}

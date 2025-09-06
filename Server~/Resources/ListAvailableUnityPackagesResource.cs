using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Utilities;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class ListAvailableUnityPackagesResource
    {
        [McpServerResource(Name = "list_available_unity_packages"), Description("Lists all available packages from the Unity package registry.")]
        public async Task<string> ListAvailablePackages()
        {
            var resourceRequest = new UnityResourceRequest
            {
                command = "list_available_unity_packages",
                resource_uri = "unity://packages/available"
            };

            var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(resourceRequest));
            return ResourceParser.ParseTextResourceContents(jsonResponse);
        }
    }
}

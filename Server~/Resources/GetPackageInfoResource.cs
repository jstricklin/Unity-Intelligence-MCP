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
    public class GetPackageInfoResource
    {
        // TODO enhance with proper URI template processing
        [McpServerResource(Name = "get_package_info"), Description("Get package from the Unity package registry.")]
        public async Task<string> GetPackageInfo(
            [Description("Package name (e.g., 'com.unity.2d.animation')")] 
            string packageName
        )
        {
            var resourceRequest = new UnityResourceRequest
            {
                command = "get_package_info",
                resource_uri = "unity://packages/info/{package_name}"
            };
            resourceRequest.parameters["package_name"] = packageName;
            var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(resourceRequest));
            return ResourceParser.ParseTextResourceContents(jsonResponse);
            // return jsonResponse;
        }
    }
}

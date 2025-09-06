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
    public interface IResource
    {
        public const string UriTemplate = "";
    }
    [McpServerResourceType]
    public class ListInstalledUnityPackagesResource : IResource
    {
        //TODO update remaining resources to meet proper template config as seen here
        public const string UriTemplate = "unity://packages/installed";
        [McpServerResource(Name = "list_installed_unity_packages", UriTemplate = UriTemplate, MimeType = "application/json"), Description("Lists all packages currently installed in the Unity project.")]
        public async Task<string> ListInstalledPackages()
        {
            var resourceRequest = new UnityResourceRequest
            {
                command = "list_installed_unity_packes",
                resource_uri = UriTemplate
            };

            var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(resourceRequest));
            return ResourceParser.ParseTextResourceContents(jsonResponse);
            // return jsonResponse;
        }
    }
}

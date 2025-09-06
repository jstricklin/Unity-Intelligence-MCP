using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Utilities;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class AssetInfoResource
    {
        [McpServerResource(Name = "get_assets_info"), Description("Searches the AssetDatabase for assets matching a filter and returns their information.")]
        public async Task<string> GetAssetsInfo(
            [Description("The space-separated filter string for searching assets. Can include names, types (t:), and labels (l:). If empty, returns all assets. One 'type' max per query.")] 
            string filter = "",
            [Description("The project area to search. Defaults to 'assets' folder. (e.g., 'assets', 'packages', 'all')")] 
            string area = "assets",
            [Description("A list of folders to search in. If empty, searches the entire project.")] string[]? 
            searchInFolders = null,
            CancellationToken cancellationToken = default)
        {
            var request = new UnityResourceRequest
            {
                command = "get_assets_info",
                resource_uri = "unity://asset/info",
            };

            if (!string.IsNullOrEmpty(filter))
            {
                request.parameters["filter"] = $"{filter},a:{area}";
            }

            if (searchInFolders != null && searchInFolders.Length > 0)
            {
                request.parameters["searchInFolders"] = searchInFolders;
            }

            var jsonPayload = JsonSerializer.Serialize(request);
            var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(jsonPayload);

            return ResourceParser.ParseTextResourceContents(jsonResponse);
        }
    }
}

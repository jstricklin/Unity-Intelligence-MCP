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
        [McpServerResource(Name = "get_asset_info"), Description("Searches the AssetDatabase for assets matching a filter and returns their information.")]
        public async Task<TextResourceContents> GetAssetInfo(
            [Description("The filter string for searching assets. Can include names, types (t:), and labels (l:). If empty, returns all assets.")] string filter = null,
            [Description("A list of folders to search in. If empty, searches the entire project.")] string[] searchInFolders = null,
            CancellationToken cancellationToken = default)
        {
            var request = new UnityResourceRequest
            {
                command = "get_asset_info",
                type = "resource",
                resource_uri = "unity://asset/info",
                parameters = new Dictionary<string, object>()
            };

            if (!string.IsNullOrEmpty(filter))
            {
                request.parameters["filter"] = filter;
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

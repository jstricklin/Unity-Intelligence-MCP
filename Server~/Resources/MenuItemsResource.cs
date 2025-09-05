using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
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
    public class MenuItemsResource
    {
        [McpServerResource(Name = "get_editor_menu_items"), Description("Retrieves a list of all available menu items from the Unity Editor.")]
        public async Task<TextResourceContents> GetMenuItems(CancellationToken cancellationToken = default)
        {
            var request = new UnityResourceRequest
            {
                command = "get_editor_menu_items",
                type = "resource",
                resource_uri = "unity://editor/menuitems"
            };
            var jsonPayload = JsonSerializer.Serialize(request);
            var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(jsonPayload);

            return ResourceParser.ParseTextResourceContents(jsonResponse);
        }
    }
}

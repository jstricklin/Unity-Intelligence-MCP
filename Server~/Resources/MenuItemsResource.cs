using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Services;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class MenuItemsResource
    {
        [McpServerResource(Name = "get_editor_menu_items"), Description("Retrieves a list of all available menu items from the Unity Editor.")]
        public async Task<string> GetMenuItems(CancellationToken cancellationToken = default)
        {
            var request = new
            {
                type = "resource",
                resource_uri = "unity://editor/menuitems"
            };
            var jsonPayload = JsonSerializer.Serialize(request);
            return await EditorBridgeClientService.SendMessageToUnity(jsonPayload);
        }
    }
}

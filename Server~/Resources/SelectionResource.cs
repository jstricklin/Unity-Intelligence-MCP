using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Services;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class SelectionResource
    {
        [McpServerResource(Name = "get_selection"), Description("Retrieves the currently selected GameObject(s) in the Unity Editor.")]
        public async Task<string> GetSelection(CancellationToken cancellationToken = default)
        {
            var request = new
            {
                command = "get_selection",
                type = "resource",
                resource_uri = "unity://selection/current"
            };
            var jsonPayload = JsonSerializer.Serialize(request);
            return await EditorBridgeClientService.SendMessageToUnity(jsonPayload);
        }
    }
}

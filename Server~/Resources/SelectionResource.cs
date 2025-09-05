using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Utilities;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class SelectionResource
    {
        [McpServerResource(Name = "get_selection"), Description("Retrieves the currently selected GameObject(s) in the Unity Editor.")]
        public async Task<TextResourceContents> GetSelection(CancellationToken cancellationToken = default)
        {
            var request = new
            {
                command = "get_selection",
                type = "resource",
                resource_uri = "unity://selection/current"
            };
            var jsonPayload = JsonSerializer.Serialize(request);
            // return await EditorBridgeClientService.SendMessageToUnity(jsonPayload);
            var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(jsonPayload);
            return ResourceParser.ParseTextResourceContents(jsonResponse);
        }
    }
}

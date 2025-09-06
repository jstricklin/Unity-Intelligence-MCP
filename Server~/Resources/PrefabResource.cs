using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Utilities;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class PrefabResource
    {
        // private readonly ILogger<PrefabResource> _logger;

        // public PrefabResource(ILogger<PrefabResource> logger)
        // {
        //     _logger = logger;
        // }

        [McpServerResource(Name = "list_prefabs")]
        [Description("Lists prefabs in the Unity project, optionally filtering by a search path.")]
        public async Task<string> ListPrefabsAsync(
        [Description("The folder path to search within, e.g., 'Assets/Prefabs'. Searches the entire project if omitted.")] 
        string searchPath = "Assets")
        {
                var request = new UnityResourceRequest
                {
                    command = "list_prefabs",
                    resource_uri = "unity://prefabs",
                };
                request.parameters["search_path"] = searchPath;

                var jsonPayload = JsonSerializer.Serialize(request);
                var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(jsonPayload);
            return ResourceParser.ParseTextResourceContents(jsonResponse);
            // using var doc = JsonDocument.Parse(jsonResponse);
            // var root = doc.RootElement;

            // string data = "";
            // if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
            // {
            //     data = root.GetProperty("data").GetRawText();
            // }
            // else
            //     data = root.TryGetProperty("error", out var msgEl) ? msgEl.GetString()! : "Unknown error from Unity Editor.";
            // return new TextResourceContents
            // {
            //     Uri  = request.resource_uri,
            //     Text = data,
            //     MimeType = "application/json"
            // };
        }
    }
}

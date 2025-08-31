using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Services;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class PrefabResource
    {
        private readonly ILogger<PrefabResource> _logger;

        public PrefabResource(ILogger<PrefabResource> logger)
        {
            _logger = logger;
        }

        [McpServerResource(Name = "list_prefabs")]
        [Description("Lists prefabs in the Unity project, optionally filtering by a search path.")]
        public async Task<TextResourceContents> ListPrefabsAsync(
            [Description("The folder path to search within, e.g., 'Assets/Prefabs'. Searches the entire project if omitted.")] string searchPath = "Assets")
        {
            try
            {
                var request = new JObject
                {
                    ["type"] = "resource",
                    ["resource_uri"] = "unity://prefabs/",
                    ["parameters"] = new JObject
                    {
                        ["search_path"] = searchPath
                    }
                };

                var jsonPayload = request.ToString();
                var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(jsonPayload);

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                {
                    var data = root.GetProperty("data").GetRawText();
                    return new TextResourceContents
                    {
                        Uri  = (string)request["resource_uri"],
                        Text = data,
                        MimeType = "application/json"
                    };
                }

                var message = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error from Unity Editor.";
                throw new InvalidOperationException($"Failed to list prefabs from Unity: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list prefabs from Unity Editor.");
                throw new InvalidOperationException($"Error communicating with Unity Editor: {ex.Message}", ex);
            }
        }
    }
}

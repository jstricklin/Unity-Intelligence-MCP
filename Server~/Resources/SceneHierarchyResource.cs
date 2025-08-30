using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Services;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class SceneHierarchyResource
    {
        private readonly ILogger<UnityProjectResource> _logger;

        public SceneHierarchyResource(ILogger<UnityProjectResource> logger)
        {
            _logger = logger;
        }

        [McpServerResource(Name = "get_scene_hierarchy")]
        [Description("Retrieves current scene hierarchy (GameObjects and their relationships) from the Unity Editor.")]
        public async Task<TextResourceContents> GetSceneHierarchyAsync()
        {
            try
            {
                var request = new
                {
                    type = "resource",
                    command = "get_scene_hierarchy",
                    resource_uri = "unity://scene/hierarchy"
                };
                var jsonPayload = JsonSerializer.Serialize(request);
                var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(jsonPayload);

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                {
                    var data = root.GetProperty("data").GetRawText();
                    return new TextResourceContents
                    {
                        Uri  = request.resource_uri,
                        Text = data,
                        MimeType = "application/json"
                    };
                }
                
                var message = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error from Unity Editor.";
                throw new InvalidOperationException($"Failed to get scene hierarchy from Unity: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get project info from Unity Editor.");
                throw new InvalidOperationException($"Error communicating with Unity Editor: {ex.Message}", ex);
            }
        }
    }
}
    

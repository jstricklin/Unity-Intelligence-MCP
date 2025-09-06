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
    public class SceneHierarchyResource
    {
        private readonly ILogger<UnityProjectResource> _logger;

        public SceneHierarchyResource(ILogger<UnityProjectResource> logger)
        {
            _logger = logger;
        }

        [McpServerResource(Name = "get_scene_hierarchy")]
        [Description("Retrieves current scene hierarchy (GameObjects and their relationships) from the Unity Editor.")]
        public async Task<string> GetSceneHierarchyAsync()
        {
            var request = new UnityResourceRequest
            {
                command = "get_scene_hierarchy",
                type = "resource",
                resource_uri = "unity://scene/hierarchy"
            };
            var jsonPayload = JsonSerializer.Serialize(request);
            var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(jsonPayload);

            return ResourceParser.ParseTextResourceContents(jsonResponse);
        }
    }
}
    

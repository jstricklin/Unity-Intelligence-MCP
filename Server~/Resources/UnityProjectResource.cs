using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class UnityProjectResource
    {
        private readonly ILogger<UnityProjectResource> _logger;
        private readonly IToolUsageLogger _usageLogger;

        public UnityProjectResource(ILogger<UnityProjectResource> logger, IToolUsageLogger usageLogger)
        {
            _logger = logger;
            _usageLogger = usageLogger;
        }

        [McpServerResource(Name = "get_project_info")]
        [Description("Retrieves information about the current Unity project from the editor.")]
        public async Task<TextResourceContents> GetProjectInfoAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            TextResourceContents? result = null;

            try
            {
                var request = new
                {
                    type = "resource",
                    command = "get_project_info",
                    resource_uri = "unity://project/info"
                };
                var jsonPayload = JsonSerializer.Serialize(request);
                var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(jsonPayload);

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                {
                    var data = root.GetProperty("data").GetRawText();
                    result = new TextResourceContents
                    {
                        Uri  = request.resource_uri,
                        Text = data,
                        MimeType = "application/json"
                    };
                    return result;
                }
                
                var message = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error from Unity Editor.";
                throw new InvalidOperationException($"Failed to get project info from Unity: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get project info from Unity Editor.");
                throw new InvalidOperationException($"Error communicating with Unity Editor: {ex.Message}", ex);
            }
        }
    }
}
    
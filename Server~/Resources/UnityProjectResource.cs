using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Database;
using UnityIntelligenceMCP.Utilities;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class UnityProjectResource
    {
        private readonly ILogger<UnityProjectResource> _logger;
        public UnityProjectResource(ILogger<UnityProjectResource> logger, IMCPUsageLogger usageLogger)
        {
            _logger = logger;
        }

        [McpServerResource(Name = "get_project_info")]
        [Description("Retrieves information about the current Unity project from the editor.")]
        public async Task<TextResourceContents> GetProjectInfoAsync()
        {
            var request = new UnityResourceRequest
            {
                command = "get_project_info",
                resource_uri = "unity://project/info"
            };
            var jsonPayload = JsonSerializer.Serialize(request);
            var jsonResponse = await EditorBridgeClientService.SendMessageToUnity(jsonPayload);

            return ResourceParser.ParseTextResourceContents(jsonResponse);
        }
    }
}
    
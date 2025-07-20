using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityCodeIntelligence.Core.Abstractions;
using UnityCodeIntelligence.Core.Analysis;
using UnityCodeIntelligence.Core.Models;

namespace UnityCodeIntelligence.Core.Server;

public class ResourceProvider : IResourceProvider
{
    private readonly UnityProjectAnalyzer _projectAnalyzer;

    public ResourceProvider(UnityProjectAnalyzer projectAnalyzer)
    {
        _projectAnalyzer = projectAnalyzer;
    }

    // Simplified provider logic. A full implementation would use a dictionary.
    public async Task<ResourceContent> GetResource(string uri, CancellationToken cancellationToken)
    {
        if (uri == "project://overview")
        {
            // Assume project path is known from configuration.
            var projectPath = "/path/to/unity/project"; // TODO: Get from a config service.
            var context = await _projectAnalyzer.AnalyzeProjectAsync(projectPath);

            var overview = new
            {
                context.RootPath,
                ScriptCount = context.Scripts.Count,
                // Add other high-level stats here.
            };
            return new ResourceContent(JsonSerializer.Serialize(overview), "application/json");
        }

        return null; // Or throw a not found exception.
    }
}

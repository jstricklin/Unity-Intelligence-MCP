using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityCodeIntelligence.Core.Abstractions;
using UnityCodeIntelligence.Core.Analysis;
using UnityCodeIntelligence.Core.Models;
using UnityCodeIntelligence.Core.Server;
using UnityCodeIntelligence.Tools;

namespace UnityCodeIntelligence.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Register Core Services
        builder.Services.AddSingleton<IMcpServer, McpServer>(); // Assume McpServer orchestrates the app lifecycle
        builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
        builder.Services.AddSingleton<IResourceProvider, ResourceProvider>();
        builder.Services.AddSingleton<IPluginManager, PluginManager>(); // Basic implementation for now

        // Register Analysis Services
        builder.Services.AddSingleton<RoslynAnalysisService>();
        builder.Services.AddSingleton<UnityProjectAnalyzer>();
        builder.Services.AddSingleton<DependencyGraphBuilder>(); // Add this for dependency analysis

        // Register Tool Handlers (assuming tools are in a dedicated class)
        builder.Services.AddSingleton<AnalysisTools>();

        var host = builder.Build();

        // Logic to register tools and resources from the DI container
        var toolRegistry = host.Services.GetRequiredService<IToolRegistry>();
        var analysisTools = host.Services.GetRequiredService<AnalysisTools>();
        toolRegistry.RegisterTool(
            "analyze_project_structure",
            "Analyzes the entire Unity project structure.",
            // Define input schema if needed, can be null for now
            null,
            analysisTools.AnalyzeProjectStructure
        );
        toolRegistry.RegisterTool(
            "find_script_dependencies",
            "Finds all dependencies for a given C# script.",
            // Define input schema
            null,
            analysisTools.FindScriptDependencies
        );


        await host.RunAsync();
    }
}

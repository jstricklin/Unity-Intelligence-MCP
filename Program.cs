using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UnityCodeIntelligence.Analysis;

var builder = Host.CreateApplicationBuilder(args);

// Configure the MCP server and discover tools/resources from the assembly.
builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "Unity Code Intelligence MCP Server" };
    })
    .WithStdioServerTransport()
    .WithToolsAndResourcesFromAssembly();

// Register our custom analysis services for dependency injection.
// These can be injected into tools and resources.
builder.Services.AddSingleton<UnityAnalyzersService>();
builder.Services.AddSingleton<UnityRoslynAnalysisService>();
builder.Services.AddSingleton<UnityProjectAnalyzer>();

var host = builder.Build();
await host.RunAsync();

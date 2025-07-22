using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityCodeIntelligence.Core.Analysis;

var builder = Host.CreateApplicationBuilder(args);

// Keep stdout clean for MCP stdio transport by redirecting all logs to stderr.
builder.Logging.ClearProviders();
// This can be changed to LogLevel.Trace for verbose debugging output.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// Configure the MCP server and discover tools/resources from the assembly.
builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "Unity Code Intelligence MCP Server", Version = "1.0.0" };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Register our custom analysis services for dependency injection.
// These can be injected into tools and resources.
builder.Services.AddSingleton<UnityRoslynAnalysisService>();
builder.Services.AddSingleton<PatternDetectorRegistry>();
builder.Services.AddSingleton<UnityComponentRelationshipAnalyzer>();
builder.Services.AddSingleton<UnityProjectAnalyzer>();
builder.Services.AddSingleton<UnityPatternAnalyzer>();
builder.Services.AddSingleton<PatternMetricsAnalyzer>();

var host = builder.Build();
await host.RunAsync();

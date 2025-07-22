using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityCodeIntelligence.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Keep stdout clean for MCP stdio transport by redirecting all logs to stderr.
builder.Logging.ClearProviders();
// This can be changed to LogLevel.Trace for verbose debugging output.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer(options => 
    {
        options.ServerInfo = new() { Name = "Unity Code Intelligence MCP Server", Version = "1.0.0" };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
// Clean registration using extension method below
builder.Services.AddUnityAnalysisServices();

var host = builder.Build();
await host.RunAsync();

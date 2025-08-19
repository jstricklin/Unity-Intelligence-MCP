using UnityIntelligenceMCP.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Extensions;
using UnityIntelligenceMCP.Configuration;

var builder = Host.CreateEmptyApplicationBuilder(null);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
// Add configuration sources to the builder
builder.Services.AddSingleton<ConfigurationService>();

builder.Services
    .AddMcpServer(options => 
    {
        options.ServerInfo = new() { Name = "Unity Intelligence MCP Server", Version = "1.0.0" };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly(); 

builder.Services.AddUnityAnalysisServices();
builder.Services.AddUnityDocumentationServices();

var host = builder.Build();

await host.Services.InitializeServicesAsync();
await host.RunAsync();
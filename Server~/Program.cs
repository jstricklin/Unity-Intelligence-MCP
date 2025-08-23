using UnityIntelligenceMCP.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Extensions;
using UnityIntelligenceMCP.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using UnityIntelligenceMCP.Core.Services;

var builder = WebApplication.CreateBuilder();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
// Add configuration sources to the builder
builder.Services.AddSingleton<ConfigurationService>();

var mcpServerBuilder = builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "Unity Intelligence MCP Server", Version = "1.0.0" };
    })
    .WithStdioServerTransport();

builder.Services.AddCoreUnityServices();
builder.Services.AddDatabaseServices();
builder.Services.AddEditorBridgeServices();
builder.Services.AddUnityAnalysisServices();
builder.Services.AddUnityDocumentationServices();

mcpServerBuilder.WithToolsFromAssembly();
mcpServerBuilder.WithResourcesFromAssembly();

var app = builder.Build();

await app.Services.InitializeServicesAsync();
await app.RunAsync();

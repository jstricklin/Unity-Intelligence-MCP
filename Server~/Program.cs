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
builder.Services.AddWebSocketServices();

// Temporarily build service provider to access configuration for conditional registration
using (var tempServiceProvider = builder.Services.BuildServiceProvider())
{
    var configService = tempServiceProvider.GetRequiredService<ConfigurationService>();
    var logger = tempServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

    if (!string.IsNullOrEmpty(configService.UnitySettings.PROJECT_PATH))
    {
        builder.Services.AddUnityAnalysisServices();
        mcpServerBuilder.WithToolsFromAssembly();
        logger.LogInformation($"Project Path: {configService.UnitySettings.PROJECT_PATH}");
    }
    else
    {
        logger.LogInformation("PROJECT_PATH not configured, Unity analysis tools disabled.");
    }
    if (!string.IsNullOrEmpty(configService.UnitySettings.EDITOR_PATH))
    {
        builder.Services.AddUnityDocumentationServices();
        logger.LogInformation($"Editor Path: {configService.UnitySettings.EDITOR_PATH}");
    }
    else
    {
        logger.LogInformation("EDITOR_PATH not configured, Unity Documentation RAG disabled.");
    }
}

mcpServerBuilder.WithResourcesFromAssembly();

var app = builder.Build();

var config = app.Services.GetRequiredService<ConfigurationService>();
var port = config.UnitySettings.MCP_SERVER_PORT;

app.UseWebSockets();
app.MapGet("/mcp-bridge", async (HttpContext context, WebSocketService service) =>
{
    await service.HandleConnectionAsync(context);
});
await app.Services.InitializeServicesAsync();
await app.RunAsync($"http://localhost:{port}");

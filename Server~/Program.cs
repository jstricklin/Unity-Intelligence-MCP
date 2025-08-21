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
builder.Services.AddWebSocketServices();

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
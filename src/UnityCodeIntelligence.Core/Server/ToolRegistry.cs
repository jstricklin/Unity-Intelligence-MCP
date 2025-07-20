using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityCodeIntelligence.Core.Abstractions;
using UnityCodeIntelligence.Core.Models;

namespace UnityCodeIntelligence.Core.Server;

public class ToolRegistry : IToolRegistry
{
    public void RegisterTool(string name, string description, object? schema, Func<JsonElement, CancellationToken, Task<ToolResult>> handler)
    {
        // Placeholder implementation for Phase 1.
    }
}

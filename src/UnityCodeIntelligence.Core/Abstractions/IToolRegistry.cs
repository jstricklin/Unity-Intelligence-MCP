using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityCodeIntelligence.Core.Models;

namespace UnityCodeIntelligence.Core.Abstractions;

public interface IToolRegistry
{
    void RegisterTool(string name, string description, object? schema, Func<JsonElement, CancellationToken, Task<ToolResult>> handler);
}

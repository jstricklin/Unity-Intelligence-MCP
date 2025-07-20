using System.Threading.Tasks;
using UnityCodeIntelligence.Core.Abstractions;

namespace UnityCodeIntelligence.Core.Server;

public class PluginManager : IPluginManager
{
    // These methods will be empty for Phase 1.
    public Task LoadPluginAsync(string assemblyPath) => Task.CompletedTask;
    public Task ReloadPluginAsync(string pluginName) => Task.CompletedTask;
    public Task UnloadPluginAsync(string pluginName) => Task.CompletedTask;
}

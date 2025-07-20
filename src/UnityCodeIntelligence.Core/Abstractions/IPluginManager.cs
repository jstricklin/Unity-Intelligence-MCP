using System.Threading.Tasks;

namespace UnityCodeIntelligence.Core.Abstractions;

public interface IPluginManager
{
    Task LoadPluginAsync(string assemblyPath);
    Task ReloadPluginAsync(string pluginName);
    Task UnloadPluginAsync(string pluginName);
}

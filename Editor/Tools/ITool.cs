using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace UnityIntelligenceMCP.Tools
{
    public interface ITool
    {
        string CommandName { get; }
        Task<ToolResponse> ExecuteAsync(JObject parameters);
    }
}

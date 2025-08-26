
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Editor.Models
{
    public interface IResourceHandler
    {
        string ResourceURI { get; }
        Task<ToolResponse> HandleRequest(JObject parameters);
    }
}
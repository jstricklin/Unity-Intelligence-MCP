
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Editor.Models;

namespace UnityIntelligenceMCP.Editor.Resources.Contracts
{
    public interface IResourceHandler
    {
        string ResourceURI { get; }
        Task<ResourceResponse> HandleRequest(JObject parameters);
    }
}
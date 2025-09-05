using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Contracts;

namespace UnityIntelligenceMCP.Tools.Base
{
    public abstract class EditorTool : ITool
    {
        public abstract string CommandName { get; }
        public abstract Task<ToolResponse> ExecuteAsync(JObject parameters);
    }
}

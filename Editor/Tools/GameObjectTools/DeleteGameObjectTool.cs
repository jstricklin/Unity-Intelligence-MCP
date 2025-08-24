using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;
using UnityEditor;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class DeleteGameObjectTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "delete_gameobject";
        
        public DeleteGameObjectTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!parameters.TryGetValue("target", out var targetToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'target' parameter"));
            if (!parameters.TryGetValue("searchBy", out var searchByToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'searchBy' parameter. Use 'name' or 'instanceId'."));

            GameObject target = _service.Find(targetToken.Value<string>(), searchByToken.Value<string>());
            if (target == null)
                return Task.FromResult(ToolResponse.ErrorResponse("Target GameObject not found"));
            
            string name = target.name;
            _service.Delete(target);
            
            return Task.FromResult(ToolResponse.SuccessResponse(
                $"Deleted GameObject: {name}",
                new { deleted = true }
            ));
        }
    }
}

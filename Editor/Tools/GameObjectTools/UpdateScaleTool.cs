using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;
using UnityEditor;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class UpdateScaleTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "update_scale";
        
        public UpdateScaleTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!parameters.TryGetValue("target", out var targetToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'target' parameter"));
            if (!parameters.TryGetValue("searchBy", out var searchByToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'searchBy' parameter. Use 'name' or 'instanceId'."));

            GameObject target = _service.Find(targetToken.Value<string>(), searchByToken.Value<string>());
            if (target == null)
                return Task.FromResult(ToolResponse.ErrorResponse("Target GameObject not found"));
            
            if (!VectorParser.TryParseScale(parameters["scale"] as JObject, out Vector3 newScale))
                return Task.FromResult(ToolResponse.ErrorResponse("Invalid scale format"));
            
            if (newScale.x <= 0 || newScale.y <= 0 || newScale.z <= 0)
                return Task.FromResult(ToolResponse.ErrorResponse("Scale values must be positive"));
            
            _service.UpdateScale(target, newScale);
            
            return Task.FromResult(ToolResponse.SuccessResponse(
                $"Updated scale of {target.name}",
                new {
                    scale = new {
                        x = target.transform.localScale.x,
                        y = target.transform.localScale.y,
                        z = target.transform.localScale.z
                    }
                }
            ));
        }
    }
}

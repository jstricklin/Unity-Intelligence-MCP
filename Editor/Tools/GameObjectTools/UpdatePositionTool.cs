using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;
using UnityEditor;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class UpdatePositionTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "update_position";
        
        public UpdatePositionTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!parameters.TryGetValue("target", out var targetToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'target' parameter"));
            if (!parameters.TryGetValue("searchBy", out var searchByToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'searchBy' parameter. Use 'name' or 'instanceId'."));

            var target = _service.Find(targetToken.Value<string>(), searchByToken.Value<string>());
            if (target == null)
                return Task.FromResult(ToolResponse.ErrorResponse("Target GameObject not found"));
            
            if (!VectorParser.TryParsePosition(parameters["position"] as JObject, out Vector3 newPosition))
                return Task.FromResult(ToolResponse.ErrorResponse("Invalid position format"));
            
            _service.UpdatePosition(target, newPosition);
            
            return Task.FromResult(ToolResponse.SuccessResponse(
                $"Updated position of {target.name}",
                new {
                    position = new {
                        x = target.transform.position.x,
                        y = target.transform.position.y,
                        z = target.transform.position.z
                    }
                }
            ));
        }
    }
}

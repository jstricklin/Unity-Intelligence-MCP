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
            if (!ToolValidator.TryFindTarget(parameters, _service, out var target, out var errorResponse))
                return Task.FromResult(errorResponse);
            
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

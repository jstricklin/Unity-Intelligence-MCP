using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;
using UnityEditor;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class UpdateRotationTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "update_rotation";
        
        public UpdateRotationTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!parameters.TryGetValue("target", out var targetToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'target' parameter"));
            if (!parameters.TryGetValue("searchBy", out var searchByToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'searchBy' parameter. Use 'name' or 'instanceId'."));

            GameObject target = _service.Find(targetToken.Value<string>(), searchByToken.Value<string>());
            if (target == null)
                return Task.FromResult(ToolResponse.ErrorResponse("Target GameObject not found"));
            
            if (!VectorParser.TryParseRotation(parameters["rotation"] as JObject, out Quaternion newRotation))
                return Task.FromResult(ToolResponse.ErrorResponse("Invalid rotation format"));
            
            _service.UpdateRotation(target, newRotation);
            
            return Task.FromResult(ToolResponse.SuccessResponse(
                $"Updated rotation of {target.name}",
                new {
                    rotation = new {
                        x = target.transform.rotation.x,
                        y = target.transform.rotation.y,
                        z = target.transform.rotation.z,
                        w = target.transform.rotation.w
                    }
                }
            ));
        }
    }
}

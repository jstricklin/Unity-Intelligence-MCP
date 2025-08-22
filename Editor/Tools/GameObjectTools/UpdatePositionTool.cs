using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class UpdatePositionTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "update_position";
        
        public UpdatePositionTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!parameters.TryGetValue("target", out JToken targetToken))
                return Task.FromResult(ToolResponse.Error("Missing 'target' parameter"));
            
            // Support both name and instance ID lookup
            GameObject target = null;
            if (targetToken.Type == JTokenType.String)
            {
                target = _service.Find(targetToken.Value<string>());
            }
            else if (targetToken.Type == JTokenType.Integer)
            {
                target = GameObject.FindObjectFromInstanceID(targetToken.Value<int>());
            }
            
            if (target == null)
                return Task.FromResult(ToolResponse.Error("Target GameObject not found"));
            
            if (!VectorParser.TryParsePosition(parameters["position"] as JObject, out Vector3 newPosition))
                return Task.FromResult(ToolResponse.Error("Invalid position format"));
            
            _service.UpdatePosition(target, newPosition);
            
            return Task.FromResult(ToolResponse.Success(
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

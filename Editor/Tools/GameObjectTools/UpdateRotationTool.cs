using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class UpdateRotationTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "update_rotation";
        
        public UpdateRotationTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!parameters.TryGetValue("target", out JToken targetToken))
                return Task.FromResult(ToolResponse.Error("Missing 'target' parameter"));
            
            GameObject target = FindTarget(targetToken);
            if (target == null)
                return Task.FromResult(ToolResponse.Error("Target GameObject not found"));
            
            if (!VectorParser.TryParseRotation(parameters["rotation"] as JObject, out Quaternion newRotation))
                return Task.FromResult(ToolResponse.Error("Invalid rotation format"));
            
            _service.UpdateRotation(target, newRotation);
            
            return Task.FromResult(ToolResponse.Success(
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
        
        private GameObject FindTarget(JToken targetToken)
        {
            if (targetToken.Type == JTokenType.String)
                return _service.Find(targetToken.Value<string>());
            
            if (targetToken.Type == JTokenType.Integer)
                return GameObject.FindObjectFromInstanceID(targetToken.Value<int>());
            
            return null;
        }
    }
}

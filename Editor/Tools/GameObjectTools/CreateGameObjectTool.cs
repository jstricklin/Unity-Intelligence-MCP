using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class CreateGameObjectTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "create_gameobject";
        
        public CreateGameObjectTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            string name = parameters["name"]?.Value<string>()?.Trim();
            if (string.IsNullOrEmpty(name))
                return Task.FromResult(ToolResponse.ErrorResponse("Name parameter is required and cannot be empty"));
            
            if (!VectorParser.TryParsePosition(parameters["position"] as JObject, out Vector3 position))
                position = Vector3.zero;
                
            var obj = _service.Create(name, position);
            
            return Task.FromResult(ToolResponse.SuccessResponse(
                $"Created {name}", 
                new { 
                    instanceId = obj.GetInstanceID(),
                    name = obj.name
                }
            ));
        }
    }
}

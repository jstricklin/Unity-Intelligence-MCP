using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class CreatePrimitiveTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "create_primitive";
        
        public CreatePrimitiveTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            // Parse primitive type with fallback to Cube
            PrimitiveType type;
            if (parameters["type"] is null || 
                !System.Enum.TryParse(parameters["type"].Value<string>(), true, out type))
            {
                type = PrimitiveType.Cube;
            }

            string name = parameters["name"]?.Value<string>()?.Trim();
            if (string.IsNullOrEmpty(name))
                name = type.ToString();

            if (!VectorParser.TryParsePosition(parameters["position"] as JObject, out Vector3 position))
                position = Vector3.zero;
                
            var obj = _service.CreatePrimitive(type, name, position);
            
            return Task.FromResult(ToolResponse.SuccessResponse(
                $"Created primitive: {name}", 
                new { 
                    instanceId = obj.GetInstanceID(),
                    name = obj.name,
                    type = type.ToString()
                }
            ));
        }
    }
}

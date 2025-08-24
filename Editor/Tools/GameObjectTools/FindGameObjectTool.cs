using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class FindGameObjectTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "find_gameobject";
        
        public FindGameObjectTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!parameters.TryGetValue("target", out var targetToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'target' parameter"));
            if (!parameters.TryGetValue("searchBy", out var searchByToken))
                return Task.FromResult(ToolResponse.ErrorResponse("Missing 'searchBy' parameter. Use 'name' or 'instanceId'."));

            var targetValue = targetToken.Value<string>();
            var obj = _service.Find(targetValue, searchByToken.Value<string>());
            if (obj == null)
                return Task.FromResult(ToolResponse.ErrorResponse($"GameObject '{targetValue}' not found"));
                
            return Task.FromResult(ToolResponse.SuccessResponse(
                $"Found {obj.name}",
                new {
                    instanceId = obj.GetInstanceID(),
                    position = new {
                        x = obj.transform.position.x,
                        y = obj.transform.position.y,
                        z = obj.transform.position.z
                    },
                    scale = new {
                        x = obj.transform.localScale.x,
                        y = obj.transform.localScale.y,
                        z = obj.transform.localScale.z
                    },
                    rotation = new {
                        x = obj.transform.rotation.x,
                        y = obj.transform.rotation.y,
                        z = obj.transform.rotation.z,
                        w = obj.transform.rotation.w
                    }
                }
            ));
        }
    }
}

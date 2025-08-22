using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class DeleteGameObjectTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "delete_gameobject";
        
        public DeleteGameObjectTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!parameters.TryGetValue("target", out JToken targetToken))
                return Task.FromResult(ToolResponse.Error("Missing 'target' parameter"));
            
            GameObject target = FindTarget(targetToken);
            if (target == null)
                return Task.FromResult(ToolResponse.Error("Target GameObject not found"));
            
            string name = target.name;
            _service.Delete(target);
            
            return Task.FromResult(ToolResponse.Success(
                $"Deleted GameObject: {name}",
                new { deleted = true }
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

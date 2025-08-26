using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Editor.Models;
using UnityEngine;
using UnityEditor;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class DeleteGameObjectTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "delete_gameobject";
        
        public DeleteGameObjectTool(IGameObjectService service) => _service = service;
        
        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!ToolValidator.TryFindTarget(parameters, _service, out var target, out var errorResponse))
                return Task.FromResult(errorResponse);
            
            string name = target.name;
            _service.Delete(target);
            
            return Task.FromResult(ToolResponse.SuccessResponse(
                $"Deleted GameObject: {name}",
                new { deleted = true }
            ));
        }
    }
}

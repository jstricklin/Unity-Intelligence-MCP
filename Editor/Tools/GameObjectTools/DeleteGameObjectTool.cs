using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Editor.Models;
using UnityEngine;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class DeleteGameObjectTool : GameObjectTool
    {
        public override string CommandName => "delete_gameobject";

        public DeleteGameObjectTool(IGameObjectService service)
        {
            GameObjectService = service;
        }

        protected override ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters)
        {
            var name = target.name;
            GameObjectService.Delete(target);

            return ToolResponse.SuccessResponse(
                $"Deleted GameObject: {name}",
                new { deleted = true }
            );
        }
    }
}

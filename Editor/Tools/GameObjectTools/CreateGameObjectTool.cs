using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class CreateGameObjectTool : GameObjectTool
    {
        public override string CommandName => "create_gameobject";

        public CreateGameObjectTool(IGameObjectService service)
        {
            GameObjectService = service;
        }

        protected override ToolResponse ExecuteOnMainThread(JObject parameters)
        {
            var name = parameters["name"]?.Value<string>()?.Trim();
            if (string.IsNullOrEmpty(name))
                return ToolResponse.ErrorResponse("Name parameter is required and cannot be empty");

            if (!VectorParser.TryParsePosition(parameters["position"] as JObject, out var position))
                position = Vector3.zero;

            GameObject parent = null;
            if (parameters.TryGetValue("parent", out var parentToken) && parentToken is JObject parentParams)
            {
                if (!ToolValidator.TryFindTarget(parentParams, GameObjectService, out parent, out var errorResponse))
                {
                    return errorResponse;
                }
            }

            var obj = GameObjectService.Create(name, position, parent);

            return ToolResponse.SuccessResponse(
                $"Created {name}",
                new
                {
                    instanceId = obj.GetInstanceID(),
                    name = obj.name
                }
            );
        }

        protected override ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters)
        {
            // This tool creates a new object, so this method is not applicable.
            throw new System.NotImplementedException();
        }
    }
}

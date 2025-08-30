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
        private readonly IComponentService _componentService;

        public CreateGameObjectTool(IGameObjectService gameObjectService, IComponentService componentService)
        {
            GameObjectService = gameObjectService;
            _componentService = componentService;
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

            if (parameters.TryGetValue("components", out var componentsToken) && componentsToken is JArray components)
            {
                foreach (var componentNameToken in components)
                {
                    var componentName = componentNameToken.Value<string>();
                    if (!string.IsNullOrEmpty(componentName))
                    {
                        try
                        {
                            _componentService.GetOrAddComponent(obj, componentName.Trim());
                        }
                        catch (System.InvalidOperationException ex)
                        {
                            Object.DestroyImmediate(obj);
                            return ToolResponse.ErrorResponse($"Failed to add component '{componentName}': {ex.Message}");
                        }
                    }
                }
            }

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

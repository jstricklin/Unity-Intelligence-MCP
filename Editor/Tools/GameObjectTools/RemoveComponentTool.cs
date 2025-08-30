using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class RemoveComponentTool : GameObjectTool
    {
        public override string CommandName => "remove_component";

        public RemoveComponentTool(IGameObjectService service, IComponentService componentService)
        {
            GameObjectService = service;
            ComponentService = componentService;
        }

        protected override ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters)
        {
            var name = target.name;
            var componentTypeName = parameters["component_type"]?.ToString();
            if (string.IsNullOrWhiteSpace(componentTypeName))
            {
                return ToolResponse.ErrorResponse("Parameter 'component_type' is required.");
            }

            if (ComponentService.RemoveComponent(target, componentTypeName))
            {
                return ToolResponse.SuccessResponse(
                    $"Removed Component: {componentTypeName} from {name}",
                    new { removed = true }
                );
            } else {
                return ToolResponse.ErrorResponse($"Component '{componentTypeName}' not found on '{name}' GameObject.");
            }
        }
    }
}

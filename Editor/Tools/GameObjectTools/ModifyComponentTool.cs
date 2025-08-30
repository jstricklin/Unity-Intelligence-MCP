using System;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class ModifyComponentTool : GameObjectTool
    {
        private readonly IComponentService _componentService;
        public override string CommandName => "modify_component";

        public ModifyComponentTool(IGameObjectService gameObjectService, IComponentService componentService)
        {
            GameObjectService = gameObjectService;
            _componentService = componentService;
        }

        protected override ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters)
        {
            var componentTypeName = parameters["component_type"]?.ToString();
            if (string.IsNullOrWhiteSpace(componentTypeName))
            {
                return ToolResponse.ErrorResponse("Parameter 'component_type' is required.");
            }

            var properties = parameters["properties"] as JObject;
            if (properties == null)
            {
                return ToolResponse.ErrorResponse("Parameter 'properties' must be a valid JSON object.");
            }

            var componentType = _componentService.FindType(componentTypeName);
            if (componentType == null)
            {
                throw new InvalidOperationException($"Component type '{componentTypeName}' not found.");
            }

            var component = _componentService.GetOrAddComponent(target, componentType);
            _componentService.ApplyProperties(component, properties);

            return ToolResponse.SuccessResponse(
                $"Successfully modified component '{componentTypeName}' on GameObject '{target.name}'.");
        }
    }
}

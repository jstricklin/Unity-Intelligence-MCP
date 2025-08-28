using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class ModifyComponentTool : ITool
    {
        private readonly IGameObjectService _gameObjectService;
        private readonly IComponentService _componentService;
        public string CommandName => "modify_component";

        public ModifyComponentTool(IGameObjectService gameObjectService, IComponentService componentService)
        {
            _gameObjectService = gameObjectService;
            _componentService = componentService;
        }

        public async Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!ToolValidator.TryFindTarget(parameters, _gameObjectService, out var target, out var errorResponse))
            {
                return errorResponse;
            }

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

            var tcs = new TaskCompletionSource<bool>();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    var componentType = _componentService.FindType(componentTypeName);
                    if (componentType == null)
                    {
                        throw new InvalidOperationException($"Component type '{componentTypeName}' not found.");
                    }

                    var component = _componentService.GetOrAddComponent(target, componentType);
                    _componentService.ApplyProperties(component, properties);

                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            await tcs.Task;

            return ToolResponse.SuccessResponse($"Successfully modified component '{componentTypeName}' on GameObject '{target.name}'.", null);
        }
    }
}

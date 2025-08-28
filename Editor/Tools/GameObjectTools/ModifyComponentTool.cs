using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class ModifyComponentTool : ITool
    {
        private readonly IGameObjectService _gameObjectService;
        public string CommandName => "modify_component";

        public ModifyComponentTool(IGameObjectService gameObjectService)
        {
            _gameObjectService = gameObjectService;
        }

        private static Type FindType(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName != null && t.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
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
                    var componentType = FindType(componentTypeName);
                    if (componentType == null)
                    {
                        throw new InvalidOperationException($"Component type '{componentTypeName}' not found.");
                    }

                    var component = target.GetComponent(componentType);
                    if (component == null)
                    {
                        component = target.AddComponent(componentType);
                    }

                    foreach (var property in properties.Properties())
                    {
                        var propInfo = component.GetType().GetProperty(property.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (propInfo != null && propInfo.CanWrite)
                        {
                            var value = property.Value.ToObject(propInfo.PropertyType);
                            propInfo.SetValue(component, value);
                        }
                        else
                        {
                            var fieldInfo = component.GetType().GetField(property.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (fieldInfo != null)
                            {
                                var value = property.Value.ToObject(fieldInfo.FieldType);
                                fieldInfo.SetValue(component, value);
                            }
                        }
                    }
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

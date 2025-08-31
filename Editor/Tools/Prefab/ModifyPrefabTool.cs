using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Editor.Utils;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Prefab
{
    public class ModifyPrefabTool : GameObjectTool
    {
        // This tool operates on a prefab asset, not a scene object.
        protected override bool findTarget { get; set; } = false;

        protected override ToolResponse ExecuteOnMainThread(JObject parameters)
        {
            var prefabPath = parameters["prefab_path"]?.Value<string>();
            if (string.IsNullOrEmpty(prefabPath) || !AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath))
            {
                return ToolResponse.ErrorResponse($"Prefab not found or invalid path: {prefabPath}");
            }

            var modifications = parameters["modifications"] as JArray;
            if (modifications == null || modifications.Count == 0)
            {
                return ToolResponse.ErrorResponse("No modifications provided.");
            }

            var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            var results = new List<object>();
            int successCount = 0;
            
            try
            {
                foreach (var mod in modifications)
                {
                    var result = ApplyModification(prefabContents, mod as JObject);
                    if ((bool)result.GetType().GetProperty("success").GetValue(result))
                    {
                        successCount++;
                    }
                    results.Add(result);
                }

                if (successCount > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                }
            }
            catch (Exception e)
            {
                return ToolResponse.ErrorResponse($"An unexpected error occurred during modification: {e.Message}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }

            return ToolResponse.SuccessResponse("Modification process completed.", new
            {
                modificationsApplied = successCount,
                modificationsFailed = modifications.Count - successCount,
                results
            });
        }
        
        private object ApplyModification(GameObject root, JObject mod)
        {
            var operation = mod?["operation"]?.Value<string>();
            var data = mod?["data"] as JObject;

            if (string.IsNullOrEmpty(operation) || data == null)
            {
                return new { success = false, operation, error = "Invalid modification format." };
            }

            try
            {
                switch (operation)
                {
                    case "updateTransform":
                        if (JsonUtils.TryParseVector3(data["position"], out var pos)) root.transform.localPosition = pos;
                        if (JsonUtils.TryParseVector3(data["rotation"], out var rot)) root.transform.localEulerAngles = rot;
                        if (JsonUtils.TryParseVector3(data["scale"], out var scale)) root.transform.localScale = scale;
                        return new { success = true, operation };

                    case "addComponent":
                        var componentTypeStr = data["componentType"]?.Value<string>();
                        var type = Type.GetType(componentTypeStr);
                        if (type == null) return new { success = false, operation, error = $"Component type '{componentTypeStr}' not found." };
                        var newComponent = root.AddComponent(type);
                        ApplyProperties(newComponent, data["properties"] as JObject);
                        return new { success = true, operation, componentType = componentTypeStr };
                    
                    case "removeComponent":
                        componentTypeStr = data["componentType"]?.Value<string>();
                        type = Type.GetType(componentTypeStr);
                        if (type == null) return new { success = false, operation, error = $"Component type '{componentTypeStr}' not found." };
                        var componentToRemove = root.GetComponent(type);
                        if (componentToRemove == null) return new { success = false, operation, error = $"Component '{componentTypeStr}' not found on prefab." };
                        Undo.DestroyObjectImmediate(componentToRemove);
                        return new { success = true, operation, componentType = componentTypeStr };
                    
                    // Other operations like 'modifyComponent', 'renameGameObject', 'setActive' would go here.
                        
                    default:
                        return new { success = false, operation, error = "Unsupported operation." };
                }
            }
            catch (Exception e)
            {
                return new { success = false, operation, error = e.Message };
            }
        }

        private void ApplyProperties(Component component, JObject properties)
        {
            if (properties == null) return;
            
            foreach (var prop in properties.Properties())
            {
                var pi = component.GetType().GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance);
                if (pi != null && pi.CanWrite)
                {
                    try
                    {
                        var value = prop.Value.ToObject(pi.PropertyType);
                        pi.SetValue(component, value);
                    }
                    catch (Exception) { /* Value conversion failed, skip */ }
                }
            }
        }
    }
}

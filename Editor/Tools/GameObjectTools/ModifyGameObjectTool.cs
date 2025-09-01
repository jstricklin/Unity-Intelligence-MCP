using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Tools.GameObjects
{
    public class ModifyGameObjectTool : GameObjectTool
    {
        public override string CommandName => "modify_gameobject";

        public ModifyGameObjectTool(IGameObjectService gameObjectService)
        {
            GameObjectService = gameObjectService;
        }

        protected override ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters)
        {
            Undo.RecordObject(target, $"GameObject Updates '{target.name}'");
            // Update Name
            if (parameters.TryGetValue("name", StringComparison.OrdinalIgnoreCase, out var nameToken) &&
                !string.IsNullOrEmpty(nameToken.ToString()) && !string.IsNullOrWhiteSpace(nameToken.ToString()))
            {
                // Undo.RecordObject(target, $"Name '{target.name}'");
                target.name = nameToken.ToString();
            }

            // Update Tag
            if (parameters.TryGetValue("tag", StringComparison.OrdinalIgnoreCase, out var tagToken) &&
                !string.IsNullOrEmpty(tagToken.ToString()) && !string.IsNullOrWhiteSpace(tagToken.ToString()))
            {
                // Undo.RecordObject(target, $"Tag '{target.name}'");
                target.tag = tagToken.ToString();
            }

            // Update Layer
            if (parameters.TryGetValue("layer", StringComparison.OrdinalIgnoreCase, out var layerToken) &&
                layerToken.Type == JTokenType.Integer)
            {
                // Undo.RecordObject(target.layer, $"Tag '{target.layer}'");
                target.layer = layerToken.ToObject<int>();
            }

            // Update Active State
            if (parameters.TryGetValue("is_active", StringComparison.OrdinalIgnoreCase, out var activeToken) &&
                activeToken.Type == JTokenType.Boolean)
            {
                // Undo.RecordObject(target.activeSelf, $"Active Self '{target.activeSelf}'");
                target.SetActive(activeToken.ToObject<bool>());
            }

            // Update Static State
            if (parameters.TryGetValue("is_static", StringComparison.OrdinalIgnoreCase, out var staticToken) &&
                staticToken.Type == JTokenType.Boolean)
            {
                // Undo.RecordObject(target.isStatic, $"Is Static '{target.isStatic}'");
                target.isStatic = staticToken.ToObject<bool>();
            }

            // Move to a different scene
            if (parameters.TryGetValue("scene_path", StringComparison.OrdinalIgnoreCase, out var scenePathToken) &&
                !string.IsNullOrEmpty(scenePathToken.ToString()) &&
                !string.IsNullOrWhiteSpace(scenePathToken.ToString()))
            {
                var scenePath = scenePathToken.ToString();
                var destinationScene = SceneManager.GetSceneByPath(scenePath);
                if (destinationScene.IsValid() && destinationScene.isLoaded)
                {
                    // Undo.RecordObject(target.scene, $"Change GameObject Scene '{target.scene}'");
                    SceneManager.MoveGameObjectToScene(target, destinationScene);
                }
            }

            return ToolResponse.SuccessResponse(
                $"Successfully modified GameObject '{target.name}'.",
                new
                {
                    instance_id = target.GetInstanceID()
                }
            );
        }
    }
}

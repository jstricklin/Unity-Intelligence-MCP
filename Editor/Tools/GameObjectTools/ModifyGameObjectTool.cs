using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class ModifyGameObjectTool : ITool
    {
        private readonly IGameObjectService _gameObjectService;
        public string CommandName => "modify_gameobject";

        public ModifyGameObjectTool(IGameObjectService gameObjectService)
        {
            _gameObjectService = gameObjectService;
        }

        public async Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!ToolValidator.TryFindTarget(parameters, _gameObjectService, out var target, out var errorResponse))
            {
                return errorResponse;
            }

            var tcs = new TaskCompletionSource<bool>();

            // Defer property modifications to the main thread
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Update Name
                    if (parameters.TryGetValue("name", StringComparison.OrdinalIgnoreCase, out var nameToken) && !string.IsNullOrEmpty(nameToken.ToString()) && !string.IsNullOrWhiteSpace(nameToken.ToString()))
                    {
                        target.name = nameToken.ToString();
                    }

                    // Update Tag
                    if (parameters.TryGetValue("tag", StringComparison.OrdinalIgnoreCase, out var tagToken) && !string.IsNullOrEmpty(tagToken.ToString()) && !string.IsNullOrWhiteSpace(tagToken.ToString()))
                    {
                        target.tag = tagToken.ToString();
                    }

                    // Update Layer
                    if (parameters.TryGetValue("layer", StringComparison.OrdinalIgnoreCase, out var layerToken) && layerToken.Type == JTokenType.Integer)
                    {
                        target.layer = layerToken.ToObject<int>();
                    }

                    // Update Active State
                    if (parameters.TryGetValue("is_active", StringComparison.OrdinalIgnoreCase, out var activeToken) && activeToken.Type == JTokenType.Boolean)
                    {
                        target.SetActive(activeToken.ToObject<bool>());
                    }

                    // Update Static State
                    if (parameters.TryGetValue("is_static", StringComparison.OrdinalIgnoreCase, out var staticToken) && staticToken.Type == JTokenType.Boolean)
                    {
                        target.isStatic = staticToken.ToObject<bool>();
                    }

                    // Move to a different scene
                    if (parameters.TryGetValue("scene_path", StringComparison.OrdinalIgnoreCase, out var scenePathToken) && !string.IsNullOrEmpty(scenePathToken.ToString()) && !string.IsNullOrWhiteSpace(scenePathToken.ToString()))
                    {
                        var scenePath = scenePathToken.ToString();
                        var destinationScene = SceneManager.GetSceneByPath(scenePath);
                        if (destinationScene.IsValid() && destinationScene.isLoaded)
                        {
                            SceneManager.MoveGameObjectToScene(target, destinationScene);
                        }
                        else
                        {
                            // If scene is not valid/loaded, we can't move the object.
                            // The response will simply show the object's current (old) scene.
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

            return ToolResponse.SuccessResponse(
                $"Successfully modified GameObject '{target.name}'.",
                new
                {
                    instance_id = target.GetInstanceID(),
                    name = target.name,
                    tag = target.tag,
                    layer = target.layer,
                    is_active = target.activeSelf,
                    is_static = target.isStatic,
                    scene = target.scene.path
                }
            );
        }
    }
}
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Editor.Utils;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Tools.Prefab
{
    public class SpawnPrefabTool : GameObjectTool
    {
        // This tool does not require a pre-existing target GameObject.
        protected override bool findTarget { get; set; } = false;

        public SpawnPrefabTool(IGameObjectService gameObjectService)
        {
            this.GameObjectService = gameObjectService;
        }

        protected override ToolResponse ExecuteOnMainThread(JObject parameters)
        {
            var prefabPath = parameters["prefab_path"]?.Value<string>();
            if (string.IsNullOrEmpty(prefabPath))
            {
                return ToolResponse.ErrorResponse("Missing required parameter: prefab_path");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return ToolResponse.ErrorResponse($"Prefab not found at path: {prefabPath}");
            }

            GameObject parent = null;
            if (parameters.TryGetValue("parent_game_object_id", out var parentIdToken) &&
                int.TryParse(parentIdToken.ToString(), out var parentId))
            {
                parent = GameObjectService.FindById(parentId);
                if (parent == null)
                {
                    return ToolResponse.ErrorResponse($"Parent GameObject with ID {parentId} not found.");
                }
            }
            
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent?.transform);
            Undo.RegisterCreatedObjectUndo(instance, $"Spawn {instance.name}");

            var instanceName = parameters["instance_name"]?.Value<string>();
            if (!string.IsNullOrEmpty(instanceName))
            {
                instance.name = instanceName;
            }

            if (JsonUtils.TryParseVector3(parameters["position"], out var pos))
            {
                instance.transform.localPosition = pos;
            }

            if (JsonUtils.TryParseVector3(parameters["rotation"], out var rot))
            {
                instance.transform.localEulerAngles = rot;
            }

            if (JsonUtils.TryParseVector3(parameters["scale"], out var scale))
            {
                instance.transform.localScale = scale;
            } else if (parent == null)
            {
                // Only apply prefab scale if not parented, otherwise it inherits
                instance.transform.localScale = prefab.transform.localScale;
            }

            if (parameters["select_after_spawn"]?.Value<bool>() == true)
            {
                Selection.activeGameObject = instance;
            }

            var responseData = new
            {
                success = true,
                gameObjectId = instance.GetInstanceID(),
                instanceName = instance.name
            };

            return ToolResponse.SuccessResponse("Prefab spawned successfully.", responseData);
        }
    }
}

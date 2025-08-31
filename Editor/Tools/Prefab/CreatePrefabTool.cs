using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Prefab
{
    public class CreatePrefabTool : GameObjectTool
    {
        // This tool requires a target GameObject from the scene.
        protected override bool findTarget { get; set; } = true;
        
        protected override ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters)
        {
            var savePath = parameters["save_path"]?.Value<string>();
            if (string.IsNullOrEmpty(savePath))
            {
                return ToolResponse.ErrorResponse("Missing required parameter: save_path");
            }
            if (!savePath.EndsWith(".prefab"))
            {
                return ToolResponse.ErrorResponse("Save path must end with .prefab");
            }

            var directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var replaceOriginal = parameters["replace_original"]?.Value<bool>() ?? false;
            GameObject prefabAsset;
            bool success;

            try
            {
                if (replaceOriginal)
                {
                    prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(target, savePath, InteractionMode.AutomatedAction);
                }
                else
                {
                    prefabAsset = PrefabUtility.SaveAsPrefabAsset(target, savePath);
                }
                success = prefabAsset != null;
            }
            catch (System.Exception e)
            {
                return ToolResponse.ErrorResponse($"Failed to create prefab: {e.Message}");
            }

            if (!success)
            {
                return ToolResponse.ErrorResponse($"Failed to save prefab at '{savePath}'. Check for read-only files or invalid paths.");
            }

            AssetDatabase.Refresh();
            var guid = AssetDatabase.AssetPathToGUID(savePath);

            var responseData = new
            {
                success = true,
                prefabPath = savePath,
                prefabGuid = guid,
                replacedOriginal = replaceOriginal
            };

            return ToolResponse.SuccessResponse("Prefab created successfully.", responseData);
        }
    }
}

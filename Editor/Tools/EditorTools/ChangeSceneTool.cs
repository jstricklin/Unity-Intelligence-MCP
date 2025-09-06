using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Editor
{
    public class ChangeSceneTool : EditorTool
    {
        public override string CommandName => "change_scene";

        protected override ToolResponse ExecuteOnMainThread(JObject parameters)
        {
            var scenePath = parameters["scenePath"]?.Value<string>();
            if (string.IsNullOrEmpty(scenePath))
            {
                return ToolResponse.ErrorResponse("scenePath parameter is required.");
            }

            if (!File.Exists(scenePath))
            {
                return ToolResponse.ErrorResponse($"Scene file not found at path: {scenePath}");
            }

            var saveChanges = parameters["saveChanges"]?.Value<bool>() ?? false;

            if (EditorSceneManager.GetActiveScene().isDirty && saveChanges)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return ToolResponse.ErrorResponse($"Failed to open scene: {scenePath}");
            }

            return ToolResponse.SuccessResponse($"Successfully changed to scene: {scenePath}", new { sceneName = scene.name });
        }
    }
}

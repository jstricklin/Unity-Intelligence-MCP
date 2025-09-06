using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Editor
{
    public class OpenSceneTool : EditorTool
    {
        public override string CommandName => "open_scene";

        public override Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            var sceneName = parameters["sceneName"]?.Value<string>();
            if (string.IsNullOrEmpty(sceneName))
            {
                return Task.FromResult(ToolResponse.ErrorResponse("sceneName parameter is required."));
            }

            var sceneGuids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            if (sceneGuids.Length == 0)
            {
                return Task.FromResult(ToolResponse.ErrorResponse($"Scene '{sceneName}' not found in the project."));
            }

            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids.First());

            var saveChanges = parameters["saveChanges"]?.Value<bool>() ?? false;
            var additive = parameters["additive"]?.Value<bool>() ?? false;

            if (EditorSceneManager.GetActiveScene().isDirty && saveChanges)
            {
                EditorSceneManager.SaveOpenScenes();
            }
            
            var openMode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(scenePath, openMode);

            if (!scene.IsValid())
            {
                return Task.FromResult(ToolResponse.ErrorResponse($"Failed to open scene: {sceneName}"));
            }

            return Task.FromResult(ToolResponse.SuccessResponse($"Successfully opened scene: {sceneName}", new { sceneName = scene.name }));
        }
    }
}

using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Editor.Services.Contracts;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Editor
{
    public class OpenSceneTool : EditorTool
    {
        private readonly ISceneService _sceneService;
        public override string CommandName => "open_scene";

        public OpenSceneTool(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        protected override ToolResponse ExecuteOnMainThread(JObject parameters)
        {
            var sceneName = parameters["sceneName"]?.Value<string>();
            if (string.IsNullOrEmpty(sceneName))
            {
                return ToolResponse.ErrorResponse("sceneName parameter is required.");
            }

            var scenePath = _sceneService.FindScenePathByName(sceneName);
            if (string.IsNullOrEmpty(scenePath))
            {
                return ToolResponse.ErrorResponse($"Scene '{sceneName}' not found in the project.");
            }

            var saveChanges = parameters["saveChanges"]?.Value<bool>() ?? false;
            if (saveChanges)
            {
                _sceneService.SaveCurrentSceneIfDirty();
            }
            
            var additive = parameters["additive"]?.Value<bool>() ?? false;
            
            if (!_sceneService.OpenScene(scenePath, additive))
            {
                return ToolResponse.ErrorResponse($"Failed to open scene: {sceneName}");
            }

            return ToolResponse.SuccessResponse($"Successfully opened scene: {sceneName}", new { sceneName });
        }
    }
}

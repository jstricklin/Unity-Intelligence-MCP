using System.Threading.Tasks;
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

        public override Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            // Note: This tool still uses main-thread-only APIs from EditorSceneManager.
            // The EditorTool base class currently dispatches ExecuteAsync to the main thread.
            // If that behavior changes, this logic will need to be explicitly marshaled.
            
            var sceneName = parameters["sceneName"]?.Value<string>();
            if (string.IsNullOrEmpty(sceneName))
            {
                return Task.FromResult(ToolResponse.ErrorResponse("sceneName parameter is required."));
            }

            var scenePath = _sceneService.FindScenePathByName(sceneName);
            if (string.IsNullOrEmpty(scenePath))
            {
                return Task.FromResult(ToolResponse.ErrorResponse($"Scene '{sceneName}' not found in the project."));
            }

            var saveChanges = parameters["saveChanges"]?.Value<bool>() ?? false;
            if (saveChanges)
            {
                _sceneService.SaveCurrentSceneIfDirty();
            }
            
            var additive = parameters["additive"]?.Value<bool>() ?? false;
            
            if (!_sceneService.OpenScene(scenePath, additive))
            {
                return Task.FromResult(ToolResponse.ErrorResponse($"Failed to open scene: {sceneName}"));
            }

            return Task.FromResult(ToolResponse.SuccessResponse($"Successfully opened scene: {sceneName}", new { sceneName }));
        }
    }
}

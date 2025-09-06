using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Editor.Services.Contracts;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Editor
{
    public class CloseSceneTool : EditorTool
    {
        private readonly ISceneService _sceneService;
        public override string CommandName => "close_scene";

        public CloseSceneTool(ISceneService sceneService)
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

            var saveChanges = parameters["saveChanges"]?.Value<bool>() ?? false;

            if (!_sceneService.CloseScene(sceneName, saveChanges))
            {
                return Task.FromResult(ToolResponse.ErrorResponse($"Scene '{sceneName}' could not be closed."));
            }

            return Task.FromResult(ToolResponse.SuccessResponse($"Successfully closed scene: {sceneName}"));
        }
    }
}

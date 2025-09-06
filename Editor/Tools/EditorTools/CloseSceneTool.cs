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

        protected override ToolResponse ExecuteOnMainThread(JObject parameters)
        {
            var sceneName = parameters["sceneName"]?.Value<string>();
            if (string.IsNullOrEmpty(sceneName))
            {
                return ToolResponse.ErrorResponse("sceneName parameter is required.");
            }

            if (!_sceneService.CloseScene(sceneName))
            {
                return ToolResponse.ErrorResponse($"Scene '{sceneName}' could not be closed.");
            }

            return ToolResponse.SuccessResponse($"Successfully closed scene: {sceneName}");
        }
    }
}

using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Utils;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class CreatePrimitiveTool : GameObjectTool
    {
        public override string CommandName => "create_primitive";
        protected override bool findTarget { get; set; } = false;
        public CreatePrimitiveTool(IGameObjectService service)
        {
            GameObjectService = service;
        }

        protected override ToolResponse ExecuteOnMainThread(JObject parameters)
        {
            // Parse primitive type with fallback to Cube
            PrimitiveType type;
            if (parameters["type"] is null ||
                !System.Enum.TryParse(parameters["type"].Value<string>(), true, out type))
            {
                type = PrimitiveType.Cube;
            }

            var name = parameters["name"]?.Value<string>()?.Trim();
            if (string.IsNullOrEmpty(name))
                name = type.ToString();

            if (!VectorParser.TryParsePosition(parameters["position"] as JObject, out var position))
                position = Vector3.zero;

            GameObject parent = null;
            if (parameters.TryGetValue("parent", out var parentToken) && parentToken is JObject parentParams)
            {
                if (!ToolValidator.TryFindTarget(parentParams, GameObjectService, out parent, out var errorResponse))
                {
                    return errorResponse;
                }
            }

            var obj = GameObjectService.CreatePrimitive(type, name, position, parent);

            return ToolResponse.SuccessResponse(
                $"Created primitive: {name}",
                new
                {
                    instanceId = obj.GetInstanceID(),
                    name = obj.name,
                    type = type.ToString()
                }
            );
        }
    }
}
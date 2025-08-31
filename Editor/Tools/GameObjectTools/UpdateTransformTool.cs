
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Utils;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class UpdateTransformTool : GameObjectTool
    {
        public override string CommandName => "update_transform";

        public UpdateTransformTool(IGameObjectService service)
        {
            GameObjectService = service;
        }

        protected override ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters)
        {
            if (parameters.ContainsKey("position"))
            {
                if (!VectorParser.TryParsePosition(parameters["position"] as JObject, out var newPosition))
                    return ToolResponse.ErrorResponse("Invalid position format");
                GameObjectService.UpdatePosition(target, newPosition);
            }

            if (parameters.ContainsKey("rotation"))
            {
                if (!VectorParser.TryParseRotation(parameters["rotation"] as JObject, out var newRotation))
                    return ToolResponse.ErrorResponse("Invalid rotation format");
                GameObjectService.UpdateRotation(target, newRotation);
            }

            if (parameters.ContainsKey("scale"))
            {
                if (!VectorParser.TryParseScale(parameters["scale"] as JObject, out var newScale))
                    return ToolResponse.ErrorResponse("Invalid scale format");

                if (newScale.x <= 0 || newScale.y <= 0 || newScale.z <= 0)
                    return ToolResponse.ErrorResponse("Scale values must be positive");

                GameObjectService.UpdateScale(target, newScale);
            }

            if (parameters.TryGetValue("clearParent", out var clearParentToken) && clearParentToken.Type == JTokenType.Boolean && clearParentToken.Value<bool>())
            {
                GameObjectService.ClearParent(target);
            }
            else if (parameters.TryGetValue("parent", out var parentToken) && parentToken is JObject parentParams)
            {
                if (!ToolValidator.TryFindTarget(parentParams, GameObjectService, out var parent, out var errorResponse))
                {
                    return errorResponse;
                }
                GameObjectService.UpdateParent(target, parent);
            }

            return ToolResponse.SuccessResponse(
                $"Updated transform of {target.name}",
                new
                {
                    position = new
                    {
                        x = target.transform.position.x,
                        y = target.transform.position.y,
                        z = target.transform.position.z
                    },
                    rotation = new
                    {
                        x = target.transform.rotation.x,
                        y = target.transform.rotation.y,
                        z = target.transform.rotation.z,
                        w = target.transform.rotation.w
                    },
                    scale = new
                    {
                        x = target.transform.localScale.x,
                        y = target.transform.localScale.y,
                        z = target.transform.localScale.z
                    }
                }
            );
        }
    }
}

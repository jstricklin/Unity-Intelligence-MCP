
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Editor.Models;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools.GameObjectTools
{
    public class UpdateTransformTool : ITool
    {
        private readonly IGameObjectService _service;
        public string CommandName => "update_transform";

        public UpdateTransformTool(IGameObjectService service) => _service = service;

        public Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!ToolValidator.TryFindTarget(parameters, _service, out var target, out var errorResponse))
                return Task.FromResult(errorResponse);

            if (parameters.ContainsKey("position"))
            {
                if (!VectorParser.TryParsePosition(parameters["position"] as JObject, out Vector3 newPosition))
                    return Task.FromResult(ToolResponse.ErrorResponse("Invalid position format"));
                _service.UpdatePosition(target, newPosition);
            }

            if (parameters.ContainsKey("rotation"))
            {
                if (!VectorParser.TryParseRotation(parameters["rotation"] as JObject, out Quaternion newRotation))
                    return Task.FromResult(ToolResponse.ErrorResponse("Invalid rotation format"));
                _service.UpdateRotation(target, newRotation);
            }

            if (parameters.ContainsKey("scale"))
            {
                if (!VectorParser.TryParseScale(parameters["scale"] as JObject, out Vector3 newScale))
                    return Task.FromResult(ToolResponse.ErrorResponse("Invalid scale format"));

                if (newScale.x <= 0 || newScale.y <= 0 || newScale.z <= 0)
                    return Task.FromResult(ToolResponse.ErrorResponse("Scale values must be positive"));

                _service.UpdateScale(target, newScale);
            }

            return Task.FromResult(ToolResponse.SuccessResponse(
                $"Updated transform of {target.name}",
                new {
                    position = new {
                        x = target.transform.position.x,
                        y = target.transform.position.y,
                        z = target.transform.position.z
                    },
                    rotation = new {
                        x = target.transform.rotation.x,
                        y = target.transform.rotation.y,
                        z = target.transform.rotation.z,
                        w = target.transform.rotation.w
                    },
                    scale = new {
                        x = target.transform.localScale.x,
                        y = target.transform.localScale.y,
                        z = target.transform.localScale.z
                    }
                }
            ));
        }
    }
}

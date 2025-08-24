using System.ComponentModel;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Core.Services;
using ModelContextProtocol.Server;
using System.Text.Json;
using System.Numerics;

namespace UnityIntelligenceMCP.Tools
{

    [McpServerToolType]
    public class UnityTools
    {
        [McpServerTool(Name = "create_primitive"), Description("Create a primitive object in Unity.")]
        public async Task<string> CreatePrimitive(
            [Description("Primitive Type to create: Sphere, Capsule, Cylinder, Cube, Plane, Quad")]
            String type,
            [Description("New GameObject Name")]
            String name,
            [Description("Position: 0,0,0")]
            String position,
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest();
            command.command = "create_primitive";
            command.parameters["type"] = type;
            command.parameters["name"] = name;
            try {
                var splitPos = position.Split(',');
                command.parameters["position"] = new { x = float.Parse(splitPos[0]), y = float.Parse(splitPos[1]), z = float.Parse(splitPos[2]) };
            } catch {
                command.parameters["position"] = new { x = 0, y = 0, z = 0 };
            }
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }

        [McpServerTool(Name = "find_gameobject"), Description("Find a GameObject in the scene by name or instance ID.")]
        public async Task<string> FindGameObject(
            [Description("Name or path of the GameObject, e.g., 'MyObject' or 'Parent/Child'.")]
            string target = null,
            [Description("Instance ID of the GameObject to find.")]
            string instanceId = null,
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "find_gameobject"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }

        [McpServerTool(Name = "update_position"), Description("Update the position of a GameObject.")]
        public async Task<string> UpdatePosition(
            [Description("New position: x,y,z")]
            string position,
            [Description("Name or path of the target GameObject.")]
            string target = null,
            [Description("Instance ID of the target GameObject.")]
            string instanceId = null,
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "update_position"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;
            try
            {
                var splitPos = position.Split(',');
                command.parameters["position"] = new { x = float.Parse(splitPos[0]), y = float.Parse(splitPos[1]), z = float.Parse(splitPos[2]) };
            }
            catch
            {
                command.parameters["position"] = new { x = 0, y = 0, z = 0 };
            }
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }

        [McpServerTool(Name = "update_scale"), Description("Update the scale of a GameObject.")]
        public async Task<string> UpdateScale(
            [Description("New scale: x,y,z")]
            string scale,
            [Description("Name or path of the target GameObject.")]
            string target = null,
            [Description("Instance ID of the target GameObject.")]
            string instanceId = null,
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "update_scale"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;
            try
            {
                var splitScale = scale.Split(',');
                command.parameters["scale"] = new { x = float.Parse(splitScale[0]), y = float.Parse(splitScale[1]), z = float.Parse(splitScale[2]) };
            }
            catch
            {
                command.parameters["scale"] = new { x = 1, y = 1, z = 1 };
            }
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }

        [McpServerTool(Name = "update_rotation"), Description("Update the rotation of a GameObject using Euler angles or a Quaternion.")]
        public async Task<string> UpdateRotation(
            [Description("New rotation. Euler angles: 'x,y,z'. Quaternion: 'x,y,z,w'.")]
            string rotation,
            [Description("Name or path of the target GameObject.")]
            string target = null,
            [Description("Instance ID of the target GameObject.")]
            string instanceId = null,
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "update_rotation"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;
            try
            {
                var splitRot = rotation.Split(',');
                if (splitRot.Length == 4) // Quaternion
                {
                    command.parameters["rotation"] = new {
                        x = float.Parse(splitRot[0]),
                        y = float.Parse(splitRot[1]),
                        z = float.Parse(splitRot[2]),
                        w = float.Parse(splitRot[3])
                    };
                }
                else if (splitRot.Length == 3) // Euler angles
                {
                    command.parameters["rotation"] = new {
                        x = float.Parse(splitRot[0]),
                        y = float.Parse(splitRot[1]),
                        z = float.Parse(splitRot[2])
                    };
                }
                else
                {
                    command.parameters["rotation"] = new { x = 0f, y = 0f, z = 0f, w = 1f };
                }
            }
            catch
            {
                command.parameters["rotation"] = new { x = 0f, y = 0f, z = 0f, w = 1f };
            }
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }

        [McpServerTool(Name = "delete_gameobject"), Description("Delete a GameObject from the scene.")]
        public async Task<string> DeleteGameObject(
            [Description("Name or path of the GameObject to delete.")]
            string target = null,
            [Description("Instance ID of the GameObject to delete.")]
            string instanceId = null,
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "delete_gameobject"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }
    }
}

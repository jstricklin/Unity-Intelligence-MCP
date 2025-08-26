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
        // TODO extract these to individual files and build new tool request error and response models
        [McpServerTool(Name = "create_primitive"), Description("Create a primitive object in Unity.")]
        public async Task<string> CreatePrimitive(
            [Description("Primitive Type to create: Sphere, Capsule, Cylinder, Cube, Plane, Quad")]
            String type = "",
            [Description("New GameObject Name")]
            String name = "",
            [Description("Position: 0,0,0")]
            String position = "",
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
            string target = "",
            [Description("Instance ID of the GameObject to find.")]
            string instanceId = "",
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

        [McpServerTool(Name = "modify_gameobject"), Description("Modify properties of a GameObject, such as its name, tag, layer, and active state.")]
        public async Task<string> ModifyGameObject(
            [Description("Name or path of the target GameObject.")]
            string target = "",
            [Description("Instance ID of the target GameObject.")]
            string instanceId = "",
            [Description("Optional. The new name for the GameObject.")]
            string name = "",
            [Description("Optional. The new tag for the GameObject.")]
            string tag = "",
            [Description("Optional. The new layer for the GameObject.")]
            int? layer = null,
            [Description("Optional. The new active state for the GameObject.")]
            bool? isActive = null,
            [Description("Optional. The new static state for the GameObject.")]
            bool? isStatic = null,
            [Description("Optional. The path of the scene to move the GameObject to.")]
            string scenePath = "",
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "modify_gameobject"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;

            if (name != null) command.parameters["name"] = name;
            if (tag != null) command.parameters["tag"] = tag;
            if (layer.HasValue) command.parameters["layer"] = layer.Value;
            if (isActive.HasValue) command.parameters["is_active"] = isActive.Value;
            if (isStatic.HasValue) command.parameters["is_static"] = isStatic.Value;
            if (scenePath != null) command.parameters["scene_path"] = scenePath;

            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }

        [McpServerTool(Name = "update_transform"), Description("Update the transform (position, rotation, scale) of a GameObject by name or instance ID.")]
        public async Task<string> UpdateTransform(
            [Description("Name or path of the target GameObject.")]
            string target = "",
            [Description("Instance ID of the target GameObject.")]
            string instanceId = "",
            [Description("Optional. New position: x,y,z")]
            string position = "",
            [Description("Optional. New rotation. Euler angles: 'x,y,z', or Quaternion: 'x,y,z,w'.")]
            string rotation = "",
            [Description("Optional. New scale: x,y,z")]
            string scale = "",
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "update_transform"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;
            if (!string.IsNullOrWhiteSpace(position))
            {
                try
                {
                    var splitPos = position.Split(',');
                    command.parameters["position"] = new { x = float.Parse(splitPos[0]), y = float.Parse(splitPos[1]), z
= float.Parse(splitPos[2]) };
                }
                catch 
                {
                    return JsonSerializer.Serialize(new { status = "error", message = "Malformed 'position' received. Expected 'x,y,z'" });
                }
            }

            if (!string.IsNullOrWhiteSpace(scale))
            {
                try
                {
                    var splitScale = scale.Split(',');
                    command.parameters["scale"] = new { x = float.Parse(splitScale[0]), y = float.Parse(splitScale[1]),
z = float.Parse(splitScale[2]) };
                }
                catch 
                { 
                    return JsonSerializer.Serialize(new { status = "error", message = "Malformed 'scale' received. Expected 'x,y,z'" });
                }
            }

            if (!string.IsNullOrWhiteSpace(rotation))
            {
                try
                {
                    var splitRot = rotation.Split(',');
                    if (splitRot.Length == 4) // Quaternion
                    {
                        command.parameters["rotation"] = new
                        {
                            x = float.Parse(splitRot[0]),
                            y = float.Parse(splitRot[1]),
                            z = float.Parse(splitRot[2]),
                            w = float.Parse(splitRot[3])
                        };
                    }
                    else if (splitRot.Length == 3) // Euler angles
                    {
                        command.parameters["rotation"] = new
                        {
                            x = float.Parse(splitRot[0]),
                            y = float.Parse(splitRot[1]),
                            z = float.Parse(splitRot[2])
                        };
                    }
                }
                catch 
                { 
                    return JsonSerializer.Serialize(new { status = "error", message = "Malformed 'rotation' received. Expected 'x,y,z' or 'x,y,z,w'" });
                }
            }
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }

        [McpServerTool(Name = "execute_menu_item"), Description("Executes a Unity Editor menu item by its path, e.g., 'File/Save Project'.")]
        public async Task<string> ExecuteMenuItem(
            [Description("The path of the menu item to execute.")]
            string path,
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "execute_menu_item"
            };
            command.parameters["path"] = path;
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }


        [McpServerTool(Name = "delete_gameobject"), Description("Delete a GameObject from the scene.")]
        public async Task<string> DeleteGameObject(
            [Description("Name or path of the GameObject to delete.")]
            string target = "",
            [Description("Instance ID of the GameObject to delete.")]
            string instanceId = "",
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

using System.ComponentModel;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Core.Services;
using ModelContextProtocol.Server;
using System.Text.Json;
using System.Numerics;
using System.Linq;

namespace UnityIntelligenceMCP.Tools
{

    [McpServerToolType]
    public class UnityTools
    {
    // TODO extract these to individual files and build new tool request error and response models with proper optional usagetype var 
        [McpServerTool(Name = "create_primitive"), Description("Create a primitive object in Unity.")]
        public async Task<string> CreatePrimitive(
            [Description("Primitive Type to create: Sphere, Capsule, Cylinder, Cube, Plane, Quad")]
            String type = "",
            [Description("New GameObject Name")]
            String name = "",
            [Description("Position: 0,0,0")]
            String position = "",
            [Description("Optional. Name or path of the parent GameObject.")]
            string parentTarget = "",
            [Description("Optional. Instance ID of the parent GameObject.")]
            string parentInstanceId = "",
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
            if (!string.IsNullOrWhiteSpace(parentTarget) || !string.IsNullOrWhiteSpace(parentInstanceId))
            {
                command.parameters["parent"] = new { target = parentTarget, instanceId = parentInstanceId };
            }

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }

        [McpServerTool(Name = "create_gameobject"), Description("Create a new, empty GameObject in Unity.")]
        public async Task<string> CreateGameObject(
            [Description("New GameObject Name")]
            string name = "",
            [Description("Position: 0,0,0")]
            string position = "",
            [Description("Optional. Name or path of the parent GameObject.")]
            string parentTarget = "",
            [Description("Optional. Instance ID of the parent GameObject.")]
            string parentInstanceId = "",
            [Description("Optional. A comma-separated list of component names to add.")]
            string components = "",
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest();
            command.command = "create_gameobject";
            command.parameters["name"] = name;
            try
            {
                var splitPos = position.Split(',');
                command.parameters["position"] = new { x = float.Parse(splitPos[0]), y = float.Parse(splitPos[1]), z = float.Parse(splitPos[2]) };
            }
            catch
            {
                command.parameters["position"] = new { x = 0, y = 0, z = 0 };
            }

            if (!string.IsNullOrWhiteSpace(parentTarget) || !string.IsNullOrWhiteSpace(parentInstanceId))
            {
                command.parameters["parent"] = new { target = parentTarget, instanceId = parentInstanceId };
            }
            
            if (!string.IsNullOrWhiteSpace(components))
            {
                command.parameters["components"] = components.Split(',').Select(c => c.Trim()).ToArray();
            }

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }

        // TODO enhance this to highly multiple found options, with optional inputs for other queryables (component ownership, tags)
        [McpServerTool(Name = "select_gameobjects"), Description("Select GameObjects in the scene by comma-separated list of names or instance IDs.")]
        public async Task<string> SelectGameObjects(
            [Description("Names or paths of the GameObject(s), e.g., 'MyObject' or 'Parent/Child,MyObject'.")]
            string targets = "",
            [Description("Instance IDs of the GameObject(s) to find.")]
            string instanceIds = "",
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "select_gameobjects"
            };
            command.parameters["targets"] = targets;
            command.parameters["instanceIds"] = instanceIds;

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
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

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }

        [McpServerTool(Name = "modify_component"), Description("Modify properties of a component on a GameObject. Adds the component if it doesn't exist.")]
        public async Task<string> ModifyComponent(
            [Description("Name or path of the target GameObject.")]
            string target = "",
            [Description("Instance ID of the target GameObject.")]
            string instanceId = "",
            [Description("The name of the component type (full name optional for specificity), e.g., 'UnityEngine.Rigidbody'.")]
            string componentType = "",
            [Description("A JSON string of properties to set, e.g., '{\"mass\": 5, \"useGravity\": false}'.")]
            string properties = "{}",
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "modify_component"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;
            command.parameters["component_type"] = componentType;
            try
            {
                command.parameters["properties"] = JsonSerializer.Deserialize<object>(properties)!;
            }
            catch (JsonException ex)
            {
                return JsonSerializer.Serialize(new { status = "error", message = $"Invalid JSON in 'properties' parameter: {ex.Message}" });
            }

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }

        [McpServerTool(Name = "remove_component"), Description("Removes a component from a GameObject.")]
        public async Task<string> RemoveComponent(
            [Description("Name or path of the target GameObject.")]
            string target = "",
            [Description("Instance ID of the target GameObject.")]
            string instanceId = "",
            [Description("The name of the component type (full name optional for specificity), e.g., 'UnityEngine.Rigidbody'.")]
            string componentType = "",
            CancellationToken cancellationToken = default)
        {
            var command = new UnityToolRequest
            {
                command = "remove_component"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;
            command.parameters["component_type"] = componentType;

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }

        [McpServerTool(Name = "update_transform"), Description("Update the transform (position, rotation, scale, parent) of a GameObject by name or instance ID.")]
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
            [Description("Optional. Name or path of the new parent GameObject.")]
            string parentTarget = "",
            [Description("Optional. Instance ID of the new parent GameObject.")]
            string parentInstanceId = "",
            [Description("Optional. Set to true to clear the GameObject's parent.")]
            bool clearParent = false,
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
                    command.parameters["position"] = new { x = float.Parse(splitPos[0]), y = float.Parse(splitPos[1]), z = float.Parse(splitPos[2]) };
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
                    command.parameters["scale"] = new { x = float.Parse(splitScale[0]), y = float.Parse(splitScale[1]), z = float.Parse(splitScale[2]) };
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
            if (clearParent)
            {
                command.parameters["clearParent"] = true;
            }
            else if (!string.IsNullOrWhiteSpace(parentTarget) || !string.IsNullOrWhiteSpace(parentInstanceId))
            {
                command.parameters["parent"] = new { target = parentTarget, instanceId = parentInstanceId };
            }

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
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

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
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

            var response = await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            return UnityToolResponse.ParseResponse(response);
        }
    }
}

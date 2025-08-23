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
        public async Task CreatePrimitive(
            [Description("Primitive Type to create: Sphere, Capsule, Cylinder, Cube, Plane, Quad")]
            String type = "Sphere",
            [Description("New GameObject Name")]
            String name = "MCP Sphere",
            [Description("Position: 0,0,0")]
            String position = "0,0,0",
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
            await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }
    }
}
using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Services;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Models;
using System.Text.Json;

namespace UnityIntelligenceMCP.Tools.Prefab
{
    [McpServerToolType]
    public class SpawnPrefabTool
    {
        [McpServerTool(Name = "spawn_prefab"), Description("Spawns a prefab instance in the current scene.")]
        public async Task<string> SpawnPrefab(
            [Description("The asset path of the prefab to spawn.")]
            string prefabPath,
            [Description("Optional name for the new instance.")]
            string instanceName = "",
            [Description("Optional. Name or path of the new parent GameObject.")]
            string parentTarget = "",
            [Description("Optional instance ID of the parent GameObject.")]
            string parentInstanceId = "",
            [Description("JSON string for position, e.g., '{\"x\":0,\"y\":1,\"z\":0}'.")]
            string position = "",
            [Description("JSON string for rotation (quaternion or euler angles), e.g., '{\"x\":0,\"y\":90,\"z\":0}'.")]
            string rotation = "",
            [Description("JSON string for scale, e.g., '{\"x\":1,\"y\":1,\"z\":1}'.")]
            string scale = "",
            [Description("Whether to select the new instance in the editor after spawning.")]
            bool selectAfterSpawn = false)
        {
            var command = new UnityToolRequest
            {
                command = "spawn_prefab"
            };
            command.parameters["prefab_path"] = prefabPath;
            command.parameters["instance_name"] = instanceName;
            command.parameters["select_after_spawn"] = selectAfterSpawn;
            if (!string.IsNullOrEmpty(position))
                command.parameters["position"] = position;
            if (!string.IsNullOrEmpty(rotation))
                command.parameters["rotation"] = rotation;
            if (!string.IsNullOrEmpty(scale))
                command.parameters["scale"] = scale;
            if (!string.IsNullOrWhiteSpace(parentTarget) || !string.IsNullOrWhiteSpace(parentInstanceId))
                command.parameters["parent"] = new { target = parentTarget, instanceId = parentInstanceId };
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }
    }
}

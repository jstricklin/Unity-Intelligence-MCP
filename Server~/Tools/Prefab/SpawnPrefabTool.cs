using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Core.Attributes;

namespace UnityIntelligenceMCP.Tools.Prefab
{
    [McpServerToolType]
    public class SpawnPrefabTool
    {
        [McpServerTool(Name = "spawn_prefab"), Description("Spawns a prefab instance in the current scene.")]
        public async Task<string> SpawnPrefab(
            [Description("The asset path of the prefab to spawn.")] string prefabPath,
            [Description("Optional name for the new instance.")] string instanceName = null,
            [Description("Optional instance ID of the parent GameObject.")] string parentGameObjectId = null,
            [Description("JSON string for position, e.g., '{\"x\":0,\"y\":1,\"z\":0}'.")] string position = null,
            [Description("JSON string for rotation (quaternion or euler angles), e.g., '{\"x\":0,\"y\":90,\"z\":0}'.")] string rotation = null,
            [Description("JSON string for scale, e.g., '{\"x\":1,\"y\":1,\"z\":1}'.")] string scale = null,
            [Description("Whether to select the new instance in the editor after spawning.")] bool selectAfterSpawn = false)
        {
            var parameters = new JObject
            {
                ["prefab_path"] = prefabPath,
                ["instance_name"] = instanceName,
                ["parent_game_object_id"] = parentGameObjectId,
                ["select_after_spawn"] = selectAfterSpawn
            };

            if (!string.IsNullOrEmpty(position)) parameters["position"] = JObject.Parse(position);
            if (!string.IsNullOrEmpty(rotation)) parameters["rotation"] = JObject.Parse(rotation);
            if (!string.IsNullOrEmpty(scale)) parameters["scale"] = JObject.Parse(scale);

            var request = new JObject
            {
                ["tool"] = "spawn_prefab",
                ["parameters"] = parameters
            };
            return await EditorBridgeClientService.SendMessageToUnity(request.ToString());
        }
    }
}

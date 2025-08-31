using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Services;
using ModelContextProtocol.Server;

namespace UnityIntelligenceMCP.Tools.Prefab
{
    [McpServerToolType]
    public class CreatePrefabTool
    {
        [McpServerTool(Name = "create_prefab"), Description("Creates a new prefab from a GameObject in the scene.")]
        public async Task<string> CreatePrefab(
            [Description("The instance ID of the source GameObject.")] string sourceGameObjectId,
            [Description("The asset path where the new prefab will be saved.")] string savePath,
            [Description("If true, replaces the source GameObject with an instance of the new prefab.")] bool replaceOriginal = false)
        {
            var request = new JObject
            {
                ["tool"] = "create_prefab",
                ["parameters"] = new JObject
                {
                    ["source_game_object_id"] = sourceGameObjectId,
                    ["save_path"] = savePath,
                    ["replace_original"] = replaceOriginal
                }
            };
            return await EditorBridgeClientService.SendMessageToUnity(request.ToString());
        }
    }
}

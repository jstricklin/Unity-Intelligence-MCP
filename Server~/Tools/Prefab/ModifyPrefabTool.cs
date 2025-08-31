using System.ComponentModel;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Core.Services;
using UnityIntelligenceMCP.Core.Attributes;

namespace UnityIntelligenceMCP.Tools.Prefab
{
    [McpServerToolType]
    public class ModifyPrefabTool
    {
        [McpServerTool(Name = "modify_prefab"), Description("Applies a series of modifications to a prefab asset.")]
        public async Task<string> ModifyPrefab(
            [Description("The asset path of the prefab to modify.")] string prefabPath,
            [Description("A JSON string representing an array of modifications to apply.")] string modifications)
        {
            var request = new JObject
            {
                ["tool"] = "modify_prefab",
                ["parameters"] = new JObject
                {
                    ["prefab_path"] = prefabPath,
                    ["modifications"] = JArray.Parse(modifications)
                }
            };
            return await EditorBridgeClientService.SendMessageToUnity(request.ToString());
        }
    }
}

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
    public class ModifyPrefabTool
    {
        [McpServerTool(Name = "modify_prefab"), Description("Applies a series of modifications to a prefab asset.")]
        public async Task<string> ModifyPrefab(
            [Description("The asset path of the prefab to modify.")] string prefabPath,
            [Description("A JSON string representing an array of modifications to apply.")] string modifications)
        {
            // var request = new JObject
            // {
            //     ["command"] = "modify_prefab",
            //     ["parameters"] = new JObject
            //     {
            //         ["prefab_path"] = prefabPath,
            //         ["modifications"] = JArray.Parse(modifications)
            //     }
            // };
            var command = new UnityToolRequest
            {
                command = "modify_prefab"
            };
            command.parameters["prefab_path"] = prefabPath;
            command.parameters["modifications"] = modifications;
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
            // return await EditorBridgeClientService.SendMessageToUnity(request.ToString());
        }
    }
}

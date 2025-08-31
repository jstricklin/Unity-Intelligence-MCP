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
        [McpServerTool(Name = "modify_prefab"), Description("Applies a series of modifications to a prefab asset. Includes: update_transform, add_component, remove_component, rename_gameobject")]
        public async Task<string> ModifyPrefab(
            [Description("The asset path of the prefab to modify.")] string prefabPath,
            [Description("A JSON string representing an array of modifications to apply, ie. [{ 'operation':'update_transform', 'data': { 'target':'targetName', 'position':'1,2,3' }}]")] string modifications)
        {
            var command = new UnityToolRequest
            {
                command = "modify_prefab"
            };
            command.parameters["prefab_path"] = prefabPath;
            command.parameters["modifications"] = JArray.Parse(modifications);
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }
    }
}

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
    public class CreatePrefabTool
    {
        [McpServerTool(Name = "create_prefab"), Description("Creates a new prefab from a GameObject in the scene.")]
        public async Task<string> CreatePrefab(
            [Description("Prefab save path. ie, 'Assets/Prefabs/Cube.prefab'")] 
            string savePath,
            [Description("Name or path of the source GameObject.")]
            string target = "",
            [Description("Instance ID of the source GameObject.")]
            string instanceId = "",
            [Description("If true, replaces the source GameObject with an instance of the new prefab.")] 
            bool replaceOriginal = false)
        {
            var command = new UnityToolRequest
            {
                command = "create_prefab"
            };
            command.parameters["target"] = target;
            command.parameters["instanceId"] = instanceId;
            command.parameters["save_path"] = savePath;
            command.parameters["replace_original"] = replaceOriginal;
            return await EditorBridgeClientService.SendMessageToUnity(JsonSerializer.Serialize(command));
        }
    }
}

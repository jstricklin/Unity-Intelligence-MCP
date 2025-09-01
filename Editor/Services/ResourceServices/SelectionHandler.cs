using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityIntelligenceMCP.Editor.Models;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class SelectionHandler : IResourceHandler
    {
        public string ResourceURI => "unity://selection/current";

        public Task<ToolResponse> HandleRequest(JObject parameters)
        {
            var selectedGameObjects = Selection.gameObjects;

            if (selectedGameObjects == null || selectedGameObjects.Length == 0)
            {
                return Task.FromResult(ToolResponse.SuccessResponse("No GameObjects are currently selected.", new object[0]));
            }

            var selectionData = selectedGameObjects.Select(go => new
            {
                name = go.name,
                instanceId = go.GetInstanceID().ToString()
            }).ToList();

            return Task.FromResult(ToolResponse.SuccessResponse("Successfully retrieved selected GameObjects.", selectionData));
        }
    }
}

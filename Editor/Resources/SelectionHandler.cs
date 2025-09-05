using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Editor.Resources.Contracts;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class SelectionHandler : IResourceHandler
    {
        public string ResourceURI => "unity://selection/current";

        public Task<ResourceResponse> HandleRequest(JObject parameters)
        {
            var selectedGameObjects = Selection.gameObjects;

            if (selectedGameObjects == null || selectedGameObjects.Length == 0)
            {
                return Task.FromResult(ResourceResponse.SuccessResponse(ResourceURI, new object[0]));
            }

            var selectionData = selectedGameObjects.Select(go => new
            {
                name = go.name,
                instanceId = go.GetInstanceID().ToString()
            }).ToList();

            return Task.FromResult(ResourceResponse.SuccessResponse(ResourceURI, selectionData));
        }
    }
}

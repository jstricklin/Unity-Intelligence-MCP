using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityIntelligenceMCP.Editor.Models;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class AvailablePackagesHandler : IResourceHandler
    {
        public string ResourceURI => "packages/available";

        public Task<ToolResponse> HandleRequest(JObject parameters)
        {
            var tcs = new TaskCompletionSource<ToolResponse>();

            EditorApplication.delayCall += () =>
            {
                SearchRequest request = Client.SearchAll();
                void OnProgress(SearchRequest req)
                {
                    if (req.Status == StatusCode.Success)
                    {
                        var packages = req.Result.Select(p => new
                        {
                            name = p.name,
                            displayName = p.displayName,
                            version = p.version,
                            description = p.description
                        }).ToList();
                        tcs.TrySetResult(ToolResponse.SuccessResponse("Successfully retrieved available package list.", packages));
                        request.completed -= OnProgress;
                    }
                    else if (req.Status >= StatusCode.Failure)
                    {
                        tcs.TrySetResult(ToolResponse.ErrorResponse($"Failed to list available packages: {req.Error.message}"));
                        request.completed -= OnProgress;
                    }
                }
                request.completed += OnProgress;
            };

            return tcs.Task;
        }
    }
}

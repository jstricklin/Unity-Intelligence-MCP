using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityIntelligenceMCP.Editor.Models;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class InstalledPackagesHandler : IResourceHandler
    {
        public string ResourceURI => "packages/installed";

        public Task<ToolResponse> HandleRequest(JObject parameters)
        {
            var tcs = new TaskCompletionSource<ToolResponse>();

            EditorApplication.delayCall += () =>
            {
                ListRequest request = Client.List();
                void OnProgress(ListRequest req)
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
                        tcs.TrySetResult(ToolResponse.SuccessResponse("Successfully retrieved installed package list.", packages));
                        request.completed -= OnProgress;
                    }
                    else if (req.Status >= StatusCode.Failure)
                    {
                        tcs.TrySetResult(ToolResponse.ErrorResponse($"Failed to list installed packages: {req.Error.message}"));
                        request.completed -= OnProgress;
                    }
                }
                request.completed += OnProgress;
            };

            return tcs.Task;
        }
    }
}

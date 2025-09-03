using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Editor
{
    public class RemovePackageTool : EditorTool
    {
        public override string CommandName => "remove_package";

        public override Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            var packageName = parameters?["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return Task.FromResult(ToolResponse.ErrorResponse("Package 'name' parameter is required."));
            }

            var tcs = new TaskCompletionSource<ToolResponse>();

            EditorApplication.delayCall += () =>
            {
                RemoveRequest request = Client.Remove(packageName);
                void OnProgress(RemoveRequest req)
                {
                    if (req.Status == StatusCode.Success)
                    {
                        tcs.TrySetResult(ToolResponse.SuccessResponse($"Successfully removed package: {packageName}"));
                        request.completed -= OnProgress;
                    }
                    else if (req.Status >= StatusCode.Failure)
                    {
                        tcs.TrySetResult(ToolResponse.ErrorResponse($"Failed to remove package '{packageName}': {req.Error.message}"));
                        request.completed -= OnProgress;
                    }
                }
                request.completed += OnProgress;
            };

            return tcs.Task;
        }
    }
}

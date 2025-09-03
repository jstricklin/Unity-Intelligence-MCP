using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Editor
{
    public class AddPackageTool : EditorTool
    {
        public override string CommandName => "add_package";

        public override Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            var packageId = parameters?["identifier"]?.ToString();
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return Task.FromResult(ToolResponse.ErrorResponse("Package 'identifier' parameter is required."));
            }

            var tcs = new TaskCompletionSource<ToolResponse>();

            EditorApplication.delayCall += () =>
            {
                AddRequest request = Client.Add(packageId);
                void OnProgress(AddRequest req)
                {
                    if (req.Status == StatusCode.Success)
                    {
                        tcs.TrySetResult(ToolResponse.SuccessResponse($"Successfully added package: {req.Result.displayName}"));
                        request.completed -= OnProgress;
                    }
                    else if (req.Status >= StatusCode.Failure)
                    {
                        tcs.TrySetResult(ToolResponse.ErrorResponse($"Failed to add package '{packageId}': {req.Error.message}"));
                        request.completed -= OnProgress;
                    }
                }
                request.completed += OnProgress;
            };

            return tcs.Task;
        }
    }
}

using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Tools;
using UnityIntelligenceMCP.Utils;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class ProjectInfoHandler : IResourceHandler
    {
        public string ResourceURI => "unity://project/info";

        public ToolResponse HandleRequest(JObject parameters)
        {
            var projectInfo = new JObject
            {
                ["projectName"] = PlayerSettings.productName,
                ["unityVersion"] = Application.unityVersion,
                ["projectPath"] = Utilities.GetProjectPath(),
                ["buildTarget"] = EditorUserBuildSettings.activeBuildTarget.ToString(),
                ["applicationVersion"] = Application.version,
                ["isPlaying"] = EditorApplication.isPlaying
            };
            
            return ToolResponse.SuccessResponse("Project info retrieved", projectInfo);
        }
    }
}

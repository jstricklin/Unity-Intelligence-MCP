using System.IO;
using UnityEngine;
using UnityIntelligenceMCP.Unity;

namespace UnityIntelligenceMCP.Utils
{
    public static class Utilities
    {
        public static string GetProjectPath()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }
        public static string GetMcpServerPath()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{UnityIntelligenceMCPSettings.PackageName}");

            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                string serverPath = Path.Combine(packageInfo.resolvedPath, "Server~");

                // return CleanPathPrefix(serverPath);
                return serverPath;
            }
            else return "";
        }
    }
}
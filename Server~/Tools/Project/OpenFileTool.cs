using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Configuration;

namespace UnityIntelligenceMCP.Tools.Project
{
    [McpServerToolType]
    public class OpenFileTool
    {
        private readonly string _projectRoot = ConfigurationService.Instance.ProjectPath;

        [McpServerTool(Name = "open_file"), Description("Opens a specified file in the default system application (e.g., an IDE for script files).")]
        public Task<string> OpenFile(
            [Description("The project-relative path to the file to open (e.g., 'Assets/Scripts/MyScript.cs').")] string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult("Error: filePath parameter cannot be empty.");
            }

            var fullPath = Path.GetFullPath(Path.Combine(_projectRoot, filePath));

            // Security check to prevent path traversal
            if (!fullPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult("Error: Access denied. The specified path is outside the project directory.");
            }
            
            if (!File.Exists(fullPath))
            {
                return Task.FromResult($"Error: File not found at '{filePath}'.");
            }

            try
            {
                Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                return Task.FromResult($"Successfully requested to open file: {filePath}");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Error: Failed to open file. {ex.Message}");
            }
        }
    }
}

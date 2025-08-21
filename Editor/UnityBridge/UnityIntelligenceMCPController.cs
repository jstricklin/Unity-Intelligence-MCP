using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityIntelligenceMCP.Utils;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityIntelligenceMCP.Unity
{
    public class UnityIntelligenceMCPController
    {
        private readonly UnityIntelligenceMCPServer _server;
        private readonly UnityIntelligenceMCPSettings _settings;

        public UnityIntelligenceMCPController()
        {
            _server = UnityIntelligenceMCPServer.Instance;
            _settings = UnityIntelligenceMCPSettings.Instance;
        }

        public void Connect()
        {
            _server.Connect($"{_settings.ServerUrl}:{_settings.Port}/mcp-bridge");
        }

        public void Disconnect()
        {
            _server.Disconnect();
        }

        public void ChangeServerUrl(string newUrl)
        {
            if (string.IsNullOrWhiteSpace(newUrl) || string.IsNullOrEmpty(newUrl))
            {
                UnityEngine.Debug.LogError($"Please enter a valid value for a server url.");
                return;
            }

            if (newUrl != _settings.ServerUrl)
            {
                _settings.ServerUrl = newUrl;
                _settings.SaveSettings();
                if (_server.IsConnected)
                {
                    Disconnect();
                    Connect();
                }
            }
        }

        public void ChangePort(int newPort)
        {
            if (newPort < 1 || newPort > 65535)
            {
                UnityEngine.Debug.LogError($"{newPort} is an invalid port number. Please enter a number between 1 and 65535.");
                return;
            }

            if (newPort != _settings.Port)
            {
                _settings.Port = newPort;
                _settings.SaveSettings();
                if (_server.IsConnected)
                {
                    Disconnect();
                    Connect();
                }
            }
        }

        public void SendTestMessage()
        {
            _server.Send("{\"event\":\"test\", \"data\":\"Hello from Unity Editor\"}");
        }

        public void CopyMCPConfigToClipboard()
        {
            EditorGUIUtility.systemCopyBuffer = GetMCPConfigJson();
        }
        
        public string GetMCPConfigJson()
        {
            var config = new Dictionary<string, object>
            {
                ["servers"] = new Dictionary<string, object>
                {
                    ["unity-intelligence-mcp"] = new Dictionary<string, object> 
                    {
                        ["command"] = "dotnet",
                        ["args"] = new List<string> { "run" },
                        ["cwd"] = $"{Utilities.GetMcpServerPath()}",
                        ["env"] = new Dictionary<string, string>
                        {
                            ["MCP_SERVER_PORT"] = "8080"
                        }
                    }
                }
            };
            
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        public void ConfigureVSCode()
        {
            var projectRoot = Utilities.GetProjectPath();
            var vscodeDir = Path.Combine(projectRoot, ".vscode");
            var mcpJsonPath = Path.Combine(vscodeDir, "mcp.json");

            var jsonContent = GetMCPConfigJson();

            try
            {
                if (!Directory.Exists(vscodeDir))
                {
                    Directory.CreateDirectory(vscodeDir);
                }

                File.WriteAllText(mcpJsonPath, jsonContent);
                Debug.Log($"Successfully created VSCode configuration at: {mcpJsonPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create VSCode configuration file: {e.Message}");
            }
        }
    }
}

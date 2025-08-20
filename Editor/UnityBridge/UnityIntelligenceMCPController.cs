using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

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

        public void StartServer()
        {
            _server.StartServer(_settings.Port);
        }

        public void StopServer()
        {
            _server.StopServer();
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
                if (_server.IsListening)
                {
                    StopServer();
                    StartServer();
                }
            }
        }

        public void SendTestMessage()
        {
            _server.SendToAllClients("{\"event\":\"test\", \"data\":\"Hello from Unity Editor\"}");
        }

        public void ConfigureVSCode()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var vscodeDir = Path.Combine(projectRoot, ".vscode");
            var mcpJsonPath = Path.Combine(vscodeDir, "mcp.json");

            var config = new Dictionary<string, object>
            {
                ["mcp.servers"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "unity-mcp-server",
                        ["build"] = new Dictionary<string, object>
                        {
                            ["command"] = "dotnet",
                            ["args"] = new List<string> { "build" },
                            ["cwd"] = "${workspaceFolder}/UnityMCPServer"
                        },
                        ["run"] = new Dictionary<string, object>
                        {
                            ["command"] = "dotnet",
                            ["args"] = new List<string> { "run" },
                            ["cwd"] = "${workspaceFolder}/UnityMCPServer",
                            ["env"] = new Dictionary<string, string>
                            {
                                ["MCP_SERVER_PORT"] = "8080"
                            }
                        },
                        ["autoStart"] = true
                    }
                }
            };

            var jsonContent = JsonConvert.SerializeObject(config, Formatting.Indented);

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
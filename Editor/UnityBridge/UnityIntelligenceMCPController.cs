using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityIntelligenceMCP.Utils;
using UnityIntelligenceMCP.Unity.Services;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Editor.Services.ResourceServices;
using Newtonsoft.Json;
using UnityEditor;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace UnityIntelligenceMCP.Unity
{
    public class UnityIntelligenceMCPController
    {
        public static UnityIntelligenceMCPController Instance { get; private set; }
        private readonly UnityIntelligenceMCPServer _server;
        private readonly UnityIntelligenceMCPSettings _settings;
        private readonly VSCodeWorkspaceService _vsCodeWorkspaceService;
        private readonly IGameObjectService _gameObjectService;
        private readonly IComponentService _componentService;
        private readonly ToolService _toolService;
        public enum MCP_IDE
        {
            VSCode,
            RooCode
        };
        private string _currentKey = "servers";
        readonly Dictionary<MCP_IDE, string> JSONKeys = new Dictionary<MCP_IDE, string> {
            {MCP_IDE.VSCode, "servers"},
            {MCP_IDE.RooCode, "mcpServers"},
        };

        public UnityIntelligenceMCPController()
        {
            Instance = this;
            _server = UnityIntelligenceMCPServer.Instance;
            _settings = UnityIntelligenceMCPSettings.Instance;
            _vsCodeWorkspaceService = new VSCodeWorkspaceService();
            _gameObjectService = new GameObjectService();
            _componentService = new ComponentService();
            _toolService = new ToolService(_gameObjectService, _componentService);
        }

        public void StartServer()
        {
            _server.Start(_settings.Port);
        }

        public void StopServer()
        {
            _server.Stop();
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
                if (_server.IsListening)
                {
                    StopServer();
                    StartServer();
                }
            }
        }

        public void ChangeScriptsDir(string newScriptsDir)
        {
            _settings.ScriptsDir = newScriptsDir.Trim();
            _settings.SaveSettings();
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
            var envDict = new Dictionary<string, string>();
            envDict["SERVER_PORT"] = _settings.Port.ToString();
            
            if (!string.IsNullOrEmpty(_settings.ScriptsDir))
                envDict["SCRIPTS_DIR"] = _settings.ScriptsDir;
            // if (_settings.AnalyzeProjectCode)
            envDict["PROJECT_PATH"] = $"{Directory.GetParent(Application.dataPath).FullName}";
            // if (_settings.EmbeddUnityDocs)
            envDict["EDITOR_PATH"] = $"{System.IO.Path.GetDirectoryName(EditorApplication.applicationPath)}";

            var config = new Dictionary<string, object>
            {
                [_currentKey] = new Dictionary<string, object>
                {
                    ["unity-intelligence-mcp"] = new Dictionary<string, object>
                    {
                        ["command"] = "dotnet",
                        ["args"] = new List<string> { "run" },
                        ["cwd"] = $"{Utilities.GetMcpServerPath()}",
                        ["env"] = envDict
                    }
                }
            };
            
            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }
        public async void AddPackageCacheToWorkspace()
        {
            await _vsCodeWorkspaceService.GenerateWorkspaceAsync(Directory.GetParent(Application.dataPath).FullName);
        }

        public void ConfigureVSCode()
        {
            var projectRoot = Utilities.GetProjectPath();
            var vscodeDir = Path.Combine(projectRoot, ".vscode");
            _currentKey = JSONKeys[MCP_IDE.VSCode];
            var jsonContent = GetMCPConfigJson();
            Utilities.WriteFile(vscodeDir, "mcp.json", jsonContent);
        }

        public void ConfigureRooCode()
        {
            var projectRoot = Utilities.GetProjectPath();
            var rooCodeDir = Path.Combine(projectRoot, ".roo");
            _currentKey = JSONKeys[MCP_IDE.RooCode];
            var jsonContent = GetMCPConfigJson();
            Utilities.WriteFile(rooCodeDir, "mcp.json", jsonContent);
        }

        public async Task<ToolResponse> HandleResource(string resourceUri, JObject parameters)
        {
            return await ResourceService.HandleRequest(resourceUri, parameters);
        }
        public async Task<ToolResponse> ExecuteTool(string command, JObject parameters)
        {
            return await _toolService.Execute(command, parameters);
        }
    }
}

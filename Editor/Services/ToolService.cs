using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Contracts;
using UnityIntelligenceMCP.Tools.GameObjects;
using UnityIntelligenceMCP.Tools.Prefab;
using UnityIntelligenceMCP.Tools.Editor;

namespace UnityIntelligenceMCP.Editor.Services
{
    public class ToolService
    {
        private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
        private readonly IGameObjectService _gameObjectService;
        private readonly IComponentService _componentService;

        public ToolService(IGameObjectService gameObjectService, IComponentService componentService)
        {
            _gameObjectService = gameObjectService;
            _componentService = componentService;
            RegisterTools();
        }

        private void RegisterTools()
        {
            // Game Object Tools
            RegisterTool(new CreateGameObjectTool(_gameObjectService, _componentService));
            RegisterTool(new ModifyComponentTool(_gameObjectService, _componentService));
            RegisterTool(new RemoveComponentTool(_gameObjectService, _componentService));
            RegisterTool(new GetGameObjectInfoTool(_gameObjectService, _componentService));
            RegisterTool(new CreatePrimitiveTool(_gameObjectService));
            RegisterTool(new SelectGameObjectsTool(_gameObjectService));
            RegisterTool(new DeleteGameObjectTool(_gameObjectService));
            RegisterTool(new UpdateTransformTool(_gameObjectService));
            RegisterTool(new ModifyGameObjectTool(_gameObjectService));
            
            // Prefab tools
            RegisterTool(new SpawnPrefabTool(_gameObjectService));
            RegisterTool(new CreatePrefabTool(_gameObjectService));
            RegisterTool(new ModifyPrefabTool());

            // Editor Tools
            RegisterTool(new ExecuteMenuItemTool());
            RegisterTool(new AddPackageTool());
            RegisterTool(new RemovePackageTool());

            // Will add more tools later (analysis, docs, etc)
        }

        private void RegisterTool(ITool tool)
        {
            _tools[tool.CommandName] = tool;
        }

        public async Task<ToolResponse> Execute(string command, JObject parameters)
        {
            if (string.IsNullOrWhiteSpace(command))
                return ToolResponse.ErrorResponse("Command parameter is required");
                
            if (!_tools.TryGetValue(command, out ITool tool))
                return ToolResponse.ErrorResponse($"Unknown command: {command}");

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await tool.ExecuteAsync(parameters);
                stopwatch.Stop();
                return ToolResponse.FinalizeResponse(response, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Tool '{command}' failed: {ex.Message}\n{ex.StackTrace}");
                return ToolResponse.ErrorResponse($"{command} failed: {ex.Message}");
            }
        }
    }
}

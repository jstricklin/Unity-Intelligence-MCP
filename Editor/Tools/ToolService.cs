using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityEngine;
using UnityIntelligenceMCP.Tools.GameObjectTools;

namespace UnityIntelligenceMCP.Tools
{
    public class ToolService
    {
        private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
        private readonly IGameObjectService _gameObjectService;

        public ToolService(IGameObjectService gameObjectService)
        {
            _gameObjectService = gameObjectService;
            RegisterTools();
        }

        private void RegisterTools()
        {
            // Game Object Tools
            RegisterTool(new CreateGameObjectTool(_gameObjectService));
            RegisterTool(new CreatePrimitiveTool(_gameObjectService));
            RegisterTool(new FindGameObjectTool(_gameObjectService));
            RegisterTool(new UpdatePositionTool(_gameObjectService));
            RegisterTool(new UpdateScaleTool(_gameObjectService));
            RegisterTool(new UpdateRotationTool(_gameObjectService));
            RegisterTool(new DeleteGameObjectTool(_gameObjectService));
            
            // Will add more tools later (analysis, docs, etc)
        }

        private void RegisterTool(ITool tool)
        {
            _tools[tool.CommandName] = tool;
        }

        public async Task<ToolResponse> Execute(string command, JObject parameters)
        {
            if (string.IsNullOrWhiteSpace(command))
                return ToolResponse.Error("Command parameter is required");
                
            if (!_tools.TryGetValue(command, out ITool tool))
                return ToolResponse.Error($"Unknown command: {command}");

            try
            {
                return await tool.ExecuteAsync(parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Tool '{command}' failed: {ex.Message}\n{ex.StackTrace}");
                return ToolResponse.Error($"{command} failed: {ex.Message}");
            }
        }
    }
}

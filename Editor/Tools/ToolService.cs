using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.GameObjectTools;
using UnityIntelligenceMCP.Tools.EditorTools;
using UnityIntelligenceMCP.Unity.Services;

namespace UnityIntelligenceMCP.Tools
{
    public class ToolService
    {
        private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
        private readonly IGameObjectService _gameObjectService;
        private readonly IComponentService _componentService;

        public ToolService(IGameObjectService gameObjectService)
        {
            _gameObjectService = gameObjectService;
            _componentService = new ComponentService();
            RegisterTools();
        }

        private void RegisterTools()
        {
            // Game Object Tools
            RegisterTool(new CreateGameObjectTool(_gameObjectService));
            RegisterTool(new CreatePrimitiveTool(_gameObjectService));
            RegisterTool(new FindGameObjectTool(_gameObjectService));
            RegisterTool(new DeleteGameObjectTool(_gameObjectService));
            RegisterTool(new UpdateTransformTool(_gameObjectService));
            RegisterTool(new ModifyGameObjectTool(_gameObjectService));
            RegisterTool(new ModifyComponentTool(_gameObjectService, _componentService));
            
            // Editor Tools
            RegisterTool(new ExecuteMenuItemTool());

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
                return await tool.ExecuteAsync(parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Tool '{command}' failed: {ex.Message}\n{ex.StackTrace}");
                return ToolResponse.ErrorResponse($"{command} failed: {ex.Message}");
            }
        }
    }
}

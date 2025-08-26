using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Editor.Core;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public static class ResourceService
    {
        private static readonly Dictionary<string, IResourceHandler> _handlers = new();

        static ResourceService()
        {
            RegisterHandler(new ProjectInfoHandler());
            RegisterHandler(new SceneHierarchyHandler());
            // Add other handlers later
        }

        public static void RegisterHandler(IResourceHandler handler)
        {
            if (_handlers.ContainsKey(handler.ResourceURI))
            {
                Debug.LogWarning($"Handler for '{handler.ResourceURI}' already registered");
                return;
            }
            _handlers.Add(handler.ResourceURI, handler);
        }

        public static async Task<ToolResponse> HandleRequest(string resourceUri, JObject parameters = null)
        {
            try
            {
                if (!_handlers.TryGetValue(resourceUri, out var handler))
                {
                    return await Task.FromResult(ToolResponse.ErrorResponse($"Resource not supported: {resourceUri}"));
                }
                return await handler.HandleRequest(parameters);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Resource error: {ex.Message}\n{ex.StackTrace}");
                return await Task.FromResult(ToolResponse.ErrorResponse($"Internal error: {ex.Message}"));
            }
        }
    }
}

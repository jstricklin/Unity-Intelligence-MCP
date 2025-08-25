using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Tools;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Core;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public static class ResourceRouter
    {
        private static readonly Dictionary<string, IResourceHandler> _handlers = new();

        static ResourceRouter()
        {
            RegisterHandler(new ProjectInfoHandler());
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

        public static ToolResponse HandleRequest(string resourceUri, JObject parameters = null)
        {
            try
            {
                if (!_handlers.TryGetValue(resourceUri, out var handler))
                {
                    return ToolResponse.ErrorResponse($"Resource not supported: {resourceUri}");
                }

                // Execute on main thread (safe for Unity API access)
                return handler.HandleRequest(parameters);
                // return UnityThreadDispatcher.Execute(() => 
                //     handler.HandleRequest(parameters));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Resource error: {ex.Message}\n{ex.StackTrace}");
                return ToolResponse.ErrorResponse($"Internal error: {ex.Message}");
            }
        }
    }

    public interface IResourceHandler
    {
        string ResourceURI { get; }
        ToolResponse HandleRequest(JObject parameters);
    }
}

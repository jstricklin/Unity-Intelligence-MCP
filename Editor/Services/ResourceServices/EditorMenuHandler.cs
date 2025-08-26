using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using System.Linq;
using System;
using System.Reflection;
using UnityEditor;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class EditorMenuHandler : IResourceHandler
    {
        public string ResourceURI => "unity://editor/menuitems";

        public Task<ToolResponse> HandleRequest(JObject parameters)
        {
            var menuItems = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .SelectMany(method => Attribute.GetCustomAttributes(method, typeof(MenuItem)))
                .Cast<MenuItem>()
                .Select(menuItem => new
                {
                    path = menuItem.menuItem,
                    priority = menuItem.priority,
                    isValidation = menuItem.validate
                })
                .ToList();

            return Task.FromResult(ToolResponse.SuccessResponse("Successfully retrieved editor menu items.", menuItems));
        }
    }
}

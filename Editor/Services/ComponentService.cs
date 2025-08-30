using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;

namespace UnityIntelligenceMCP.Unity.Services
{
    public class ComponentService : IComponentService
    {
        public Type FindType(string name)
        {
            Type type = Type.GetType(name);
            if (type != null)
                return type;

            type = TryFindInUnityAssemblies(name);
            if (type != null)
                return type;

            type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name != null && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (type != null)
                return type;

            throw new InvalidOperationException($"Component type '{name}' not found.");
        }

        public bool RemoveComponent(GameObject target, string componentTypeName)
        {
            bool success = false;

            var componentType = FindType(componentTypeName);
            try 
            {
                var component = target.GetComponent(componentType);

                if (component != null)
                {
                    Undo.DestroyObjectImmediate(component);
                    success = true;
                }
            } 
            catch  
            {
                success = false;
            }
            return success;
        }

        public Component GetOrAddComponent(GameObject target, string componentTypeName)
        {

            var componentType = FindType(componentTypeName);
            var component = target.GetComponent(componentType);

            if (component == null)
            {
                component = Undo.AddComponent(target, componentType);
            }
            return component;
        }

        public void ApplyProperties(Component component, JObject properties)
        {
            foreach (var property in properties.Properties())
            {
                var propInfo = component.GetType().GetProperty(property.Name, BindingFlags.IgnoreCase |
BindingFlags.Public | BindingFlags.Instance);
                if (propInfo != null && propInfo.CanWrite)
                {
                    Undo.RecordObject(component, $"Update Property {property.Name} '{component.GetType().Name}'");
                    var value = ParseValue(property.Value, propInfo.PropertyType);
                    propInfo.SetValue(component, value);
                    continue;
                }

                var fieldInfo = component.GetType().GetField(property.Name, BindingFlags.IgnoreCase |
BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    Undo.RecordObject(component, $"Update Field {property.Name} '{component.GetType().Name}'");
                    var value = ParseValue(property.Value, fieldInfo.FieldType);
                    fieldInfo.SetValue(component, value);
                }

            }
        }

        private object ParseValue(JToken token, Type targetType)
        {
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, token.ToString(), true);
            }

            if (targetType == typeof(Vector2) && VectorParser.TryParseVector2(token, out var v2))
                return v2;

            if (targetType == typeof(Vector3) && VectorParser.TryParseVector3(token, out var v3))
                return v3;

            if (targetType == typeof(Vector4) && VectorParser.TryParseVector4(token, out var v4))
                return v4;

            if (targetType == typeof(Quaternion))
            {
                if (token is JObject jObj && VectorParser.TryParseRotation(jObj, out var q))
                    return q;
                if (VectorParser.TryParseVector4(token, out var v4q))
                    return new Quaternion(v4q.x, v4q.y, v4q.z, v4q.w);
            }

            if (token is JObject jObjColor && targetType == typeof(Color))
                return new Color(jObjColor["r"].Value<float>(), jObjColor["g"].Value<float>(), jObjColor["b"].Value<float>(), jObjColor.Value<float?>("a") ?? 1f);

            return token.ToObject(targetType);
        }

        private Type TryFindInUnityAssemblies(string name)
        {
            // Common Unity assembly names to check first
            // TODO resolve TMPro component support
            string[] unityAssemblyNames = {
                "UnityEngine",
                "UnityEngine.CoreModule", 
                "UnityEngine.PhysicsModule",
                "UnityEngine.UI",
                "UnityEngine.UIModule",
                "UnityEngine.UIElements",
                "Assembly-CSharp",
                "TMPro"
            };

            foreach (var assemblyName in unityAssemblyNames)
            {
                try
                {
                    Type type = Type.GetType($"{assemblyName}.{name}, {assemblyName}");
                    if (type != null)
                    {
                        // Debug.Log("Unity Assembly Found: " + type.FullName);
                        return type;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                    continue;
                }
            }
            
            return null;
        }
    }
}

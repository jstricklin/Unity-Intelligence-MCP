using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Unity.Services
{
    public class ComponentService : IComponentService
    {
        public Type FindType(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName != null && t.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public Component GetOrAddComponent(GameObject target, Type componentType)
        {
            var component = target.GetComponent(componentType);
            if (component == null)
            {
                component = target.AddComponent(componentType);
            }
            return component;
        }

        public void ApplyProperties(Component component, JObject properties)
        {
            foreach (var property in properties.Properties())
            {
                var propInfo = component.GetType().GetProperty(property.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propInfo != null && propInfo.CanWrite)
                {
                    var value = ParseValue(property.Value, propInfo.PropertyType);
                    propInfo.SetValue(component, value);
                    continue;
                }

                var fieldInfo = component.GetType().GetField(property.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo != null)
                {
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

            if (token.Type == JTokenType.Object)
            {
                if (targetType == typeof(Vector2))
                    return new Vector2(token["x"].Value<float>(), token["y"].Value<float>());
                
                if (targetType == typeof(Vector3))
                    return new Vector3(token["x"].Value<float>(), token["y"].Value<float>(), token["z"].Value<float>());

                if (targetType == typeof(Vector4) || targetType == typeof(Quaternion))
                {
                    var x = token["x"].Value<float>();
                    var y = token["y"].Value<float>();
                    var z = token["z"].Value<float>();
                    var w = token["w"].Value<float>();
                    return targetType == typeof(Quaternion) ? new Quaternion(x, y, z, w) : new Vector4(x, y, z, w);
                }

                if (targetType == typeof(Color))
                    return new Color(token["r"].Value<float>(), token["g"].Value<float>(), token["b"].Value<float>(), token.Value<float?>("a") ?? 1f);
            }
            
            return token.ToObject(targetType);
        }
    }
}

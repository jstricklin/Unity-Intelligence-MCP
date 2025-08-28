using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Tools;

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
                var propInfo = component.GetType().GetProperty(property.Name, BindingFlags.IgnoreCase |
BindingFlags.Public | BindingFlags.Instance);
                if (propInfo != null && propInfo.CanWrite)
                {
                    var value = ParseValue(property.Value, propInfo.PropertyType);
                    propInfo.SetValue(component, value);
                    continue;
                }

                var fieldInfo = component.GetType().GetField(property.Name, BindingFlags.IgnoreCase |
BindingFlags.Public | BindingFlags.Instance);
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
                return new Color(jObjColor["r"].Value<float>(), jObjColor["g"].Value<float>(),
jObjColor["b"].Value<float>(), jObjColor.Value<float?>("a") ?? 1f);

            return token.ToObject(targetType);
        }
    }
}

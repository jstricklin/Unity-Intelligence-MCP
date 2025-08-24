using UnityEngine;
using UnityEditor;
using System;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Unity.Services
{
    public class GameObjectService : IGameObjectService
    {
        public GameObject Create(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, $"Create '{name}'");
            Selection.activeObject = go;
            return go;
        }

        public GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            Undo.RegisterCreatedObjectUndo(go, $"Create '{name}' ({type})");
            Selection.activeObject = go;
            return go;
        }

        public GameObject Find(string value, string searchBy)
        {
            GameObject go = null;
            if (string.Equals(searchBy, "name", System.StringComparison.OrdinalIgnoreCase))
            {
                go = GameObject.Find(value);
            }
            else if (string.Equals(searchBy, "instanceId", System.StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value, out int instanceId))
                {
                    go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                }
            }
            
            if (go) Selection.activeObject = go;
            return go;
        }

        public void UpdatePosition(GameObject target, Vector3 newPosition)
        {
            if (!target) return;
            Undo.RecordObject(target.transform, $"Move '{target.name}'");
            target.transform.position = newPosition;
        }

        public void UpdateScale(GameObject target, Vector3 newScale)
        {
            if (!target) return;
            Undo.RecordObject(target.transform, $"Scale '{target.name}'");
            target.transform.localScale = newScale;
        }

        public void UpdateRotation(GameObject target, Quaternion newRotation)
        {
            if (!target) return;
            Undo.RecordObject(target.transform, $"Rotate '{target.name}'");
            target.transform.rotation = newRotation;
        }

        public void Delete(GameObject target)
        {
            if (!target) return;
            Undo.DestroyObjectImmediate(target);
        }
    }
}

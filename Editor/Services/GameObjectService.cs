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
            return PerformWithHistory(() => 
            {
                var go = new GameObject(name);
                go.transform.position = position;
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                
                // Select new object in hierarchy
                Selection.activeObject = go;
                
                return go;
            }, "Create GameObject");
        }

        public GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position)
        {
            return PerformWithHistory(() =>
            {
                var go = GameObject.CreatePrimitive(type);
                go.name = name;
                go.transform.position = position;
                Undo.RegisterCreatedObjectUndo(go, $"Create {name} ({type})");

                Selection.activeObject = go;

                return go;
            }, "Create Primitive");
        }

        public GameObject Find(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                Selection.activeObject = go;
            }
            return go;
        }

        public void UpdatePosition(GameObject target, Vector3 newPosition)
        {
            if (target == null) return;
            
            PerformWithHistory(() => 
            {
                Selection.activeObject = target;
                Undo.RecordObject(target.transform, "Position Update");
                target.transform.position = newPosition;
            }, "Update Position");
        }

        public void Delete(GameObject target)
        {
            if (target == null) return;
            
            PerformWithHistory(() => 
            {
                Undo.DestroyObjectImmediate(target);
            }, "Delete GameObject");
        }

        public void UndoLast()
        {
            Undo.PerformUndo();
        }

        private void PerformWithHistory(Action operation, string description)
        {
            Undo.IncrementCurrentGroup();
            operation();
            Undo.SetCurrentGroupName(description);
        }

        private T PerformWithHistory<T>(Func<T> operation, string description)
        {
            Undo.IncrementCurrentGroup();
            var result = operation();
            Undo.SetCurrentGroupName(description);
            return result;
        }
    }
}

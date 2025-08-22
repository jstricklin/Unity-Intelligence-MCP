using UnityEngine;

namespace UnityIntelligenceMCP.Unity.Services.Contracts
{
    public interface IGameObjectService
    {
        GameObject Create(string name, Vector3 position);
        GameObject Find(string name);
        void UpdatePosition(GameObject target, Vector3 newPosition);
        void Delete(GameObject target);
        void UndoLast();
        GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position);
    }
}

using UnityEngine;

namespace UnityIntelligenceMCP.Unity.Services.Contracts
{
    public interface IGameObjectService
    {
        GameObject Create(string name, Vector3 position, GameObject parent = null);
        GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position, GameObject parent = null);
        GameObject Find(string target, string instanceId);
        void UpdatePosition(GameObject target, Vector3 newPosition);
        void UpdateScale(GameObject target, Vector3 newScale);
        void UpdateRotation(GameObject target, Quaternion newRotation);
        void Delete(GameObject target);
        void UpdateParent(GameObject target, GameObject newParent);
        void ClearParent(GameObject target);
    }
}

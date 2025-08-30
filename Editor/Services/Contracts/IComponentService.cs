using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityIntelligenceMCP.Unity.Services.Contracts
{
    public interface IComponentService
    {
        Type FindType(string name);
        Component GetOrAddComponent(GameObject target, string componentTypeName);
        bool RemoveComponent(GameObject target, string componentTypeName);
        void ApplyProperties(Component component, JObject properties);
    }
}

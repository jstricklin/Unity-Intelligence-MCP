using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityIntelligenceMCP.Unity.Services.Contracts
{
    public interface IComponentService
    {
        Type FindType(string name);
        Component GetOrAddComponent(GameObject target, Type componentType);
        void ApplyProperties(Component component, JObject properties);
    }
}

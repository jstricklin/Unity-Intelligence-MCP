using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityIntelligenceMCP.Editor.Utils
{
    public static class JsonUtils
    {
        public static bool TryParseVector3(JToken token, out Vector3 vector)
        {
            vector = Vector3.zero;
            if (token == null || token.Type != JTokenType.Object)
            {
                return false;
            }

            var x = token["x"]?.Value<float>() ?? 0f;
            var y = token["y"]?.Value<float>() ?? 0f;
            var z = token["z"]?.Value<float>() ?? 0f;
            vector = new Vector3(x, y, z);
            return true;
        }
    }
}

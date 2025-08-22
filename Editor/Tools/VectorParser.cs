using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools
{
    public static class VectorParser
    {
        public static bool TryParsePosition(JObject obj, out Vector3 position)
        {
            position = Vector3.zero;
            try
            {
                if (obj == null) return false;
                position = new Vector3(
                    obj["x"]?.Value<float>() ?? 0,
                    obj["y"]?.Value<float>() ?? 0,
                    obj["z"]?.Value<float>() ?? 0
                );
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public static bool TryParseScale(JObject obj, out Vector3 scale)
        {
            scale = Vector3.one;
            try
            {
                if (obj == null) return false;
                scale = new Vector3(
                    obj["x"]?.Value<float>() ?? 1,
                    obj["y"]?.Value<float>() ?? 1,
                    obj["z"]?.Value<float>() ?? 1
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryParseRotation(JObject obj, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            try
            {
                if (obj == null) return false;
                
                // Support quaternion format
                if (obj["x"] != null && obj["y"] != null && 
                    obj["z"] != null && obj["w"] != null)
                {
                    rotation = new Quaternion(
                        obj["x"].Value<float>(),
                        obj["y"].Value<float>(),
                        obj["z"].Value<float>(),
                        obj["w"].Value<float>()
                    );
                    return true;
                }
                
                // Support euler angles format
                if (obj["x"] != null && obj["y"] != null && obj["z"] != null)
                {
                    rotation = Quaternion.Euler(
                        obj["x"].Value<float>(),
                        obj["y"].Value<float>(),
                        obj["z"].Value<float>()
                    );
                    return true;
                }
                
                // Support angle-axis format
                if (obj["angle"] != null && obj["axis"] != null)
                {
                    Vector3 axis;
                    if (TryParsePosition(obj["axis"] as JObject, out axis))
                    {
                        rotation = Quaternion.AngleAxis(
                            obj["angle"].Value<float>(),
                            axis
                        );
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

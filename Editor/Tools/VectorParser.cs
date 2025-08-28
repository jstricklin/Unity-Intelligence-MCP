using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools
{
    public static class VectorParser
    {
        public static bool TryParseVector2(JToken token, out Vector2 vector)
        {
            vector = Vector2.zero;
            try
            {
                if (token is JObject obj)
                {
                    vector = new Vector2(
                        obj["x"]?.Value<float>() ?? 0,
                        obj["y"]?.Value<float>() ?? 0
                    );
                    return true;
                }
                if (token is JValue str && str.ToString().Split(',') is string[] arr && arr.Length >= 2)
                {
                    vector = new Vector2(
                        float.Parse(arr[0]),
                        float.Parse(arr[1])
                    );
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryParseVector3(JToken token, out Vector3 vector) 
        {
            vector = Vector3.zero;
            try
            {
                if (token is JObject obj)
                {
                    vector = new Vector3(
                        obj["x"]?.Value<float>() ?? 0,
                        obj["y"]?.Value<float>() ?? 0,
                        obj["z"]?.Value<float>() ?? 0
                    );
                    return true;
                }
                if (token is JValue str && str.ToString().Split(',') is string[] arr && arr.Length >= 3)
                {
                    vector = new Vector3(
                        float.Parse(arr[0]),
                        float.Parse(arr[1]),
                        float.Parse(arr[2])
                    );
                    Debug.Log($"vector check: {vector}");
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryParseVector4(JToken token, out Vector4 vector)
        {
            vector = Vector4.zero;
            try
            {
                if (token is JObject obj)
                {
                    vector = new Vector4(
                        obj["x"]?.Value<float>() ?? 0,
                        obj["y"]?.Value<float>() ?? 0,
                        obj["z"]?.Value<float>() ?? 0,
                        obj["w"]?.Value<float>() ?? 0
                    );
                    return true;
                }
                if (token is JValue str && str.ToString().Split(',') is string[] arr && arr.Length >= 4)
                {
                    vector = new Vector4(
                        float.Parse(arr[0]),
                        float.Parse(arr[1]),
                        float.Parse(arr[2]),
                        float.Parse(arr[3])
                    );
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public static bool TryParsePosition(JToken token, out Vector3 vector)
        {
            return TryParseVector3(token, out vector);
        }

        public static bool TryParseScale(JToken token, out Vector3 scale)
        {
            scale = Vector3.one;
            try
            {
                if (token is JObject obj)
                {
                    scale = new Vector3(
                        obj["x"]?.Value<float>() ?? 1,
                        obj["y"]?.Value<float>() ?? 1,
                        obj["z"]?.Value<float>() ?? 1
                    );
                    return true;
                }
                if (token is JValue str && str.ToString().Split(',') is string[] arr && arr.Length >= 3)
                {
                    scale = new Vector3(
                        float.Parse(arr[0]),
                        float.Parse(arr[1]),
                        float.Parse(arr[2])
                    );
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryParseRotation(JToken token, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            try
            {
                if (token == null) return false;

                if (token is JObject obj)
                {
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
                    // if (obj["angle"] != null && obj["axis"] != null)
                    // {
                    //     Vector3 axis;
                    //     if (TryParseVector3(obj["axis"], out axis))
                    //     {
                    //         rotation = Quaternion.AngleAxis(
                    //             obj["angle"].Value<float>(),
                    //             axis
                    //         );
                    //         return true;
                    //     }
                    // }
                }
                if (token is JValue str)
                {
                    string[] arr = str.ToString().Split(',');
                    if (arr.Length >= 4)
                    {
                        rotation = new Quaternion(
                            float.Parse(arr[0]),
                            float.Parse(arr[1]),
                            float.Parse(arr[2]),
                            float.Parse(arr[3]) 
                        );
                        return true;
                    }
                    if (arr.Length == 3)
                    {
                        rotation = Quaternion.Euler(
                            float.Parse(arr[0]),
                            float.Parse(arr[1]),
                            float.Parse(arr[2])
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
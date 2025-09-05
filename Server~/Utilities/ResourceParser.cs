
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace UnityIntelligenceMCP.Utilities
{
    public static class ResourceParser
    {
        public static TextResourceContents ParseTextResourceContents(string jsonData)
        {

            using var doc = JsonDocument.Parse(jsonData);
            var root = doc.RootElement;

            string data = "";
            if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
            {
                data = root.GetProperty("data").GetRawText();
            }
            else
                data = root.TryGetProperty("error", out var msgEl) ? msgEl.GetRawText()! : "Unknown error from Unity Editor.";
            var uri = root.TryGetProperty("resource_uri", out var resourceUri) ? resourceUri.GetString()! : "Unknown Resource Uri.";
            return new TextResourceContents
            {
                Uri  = uri,
                Text = data,
                MimeType = "application/json"
            };
        }
    }
}
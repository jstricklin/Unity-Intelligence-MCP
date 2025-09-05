namespace UnityIntelligenceMCP.Models 
{
    [Serializable]
    class UnityResourceRequest 
    {
        public string type { get; set; } = "resource";
        public string command { get; set; } = "";
        public string resource_uri { get; set; } = "";
        public Dictionary<string,object> parameters { get; set; } = new Dictionary<string, object>();
    }
}
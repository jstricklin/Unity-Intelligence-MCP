namespace UnityIntelligenceMCP.Models 
{
    [Serializable]
    class UnityToolRequest 
    {
        public string command { get; set; } = "";
        public Dictionary<string,object> parameters { get; set; } = new Dictionary<string, object>();
    }
}
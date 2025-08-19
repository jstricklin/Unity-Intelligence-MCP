namespace UnityIntelligenceMCP.Models
{
    public class UnityMessageInfo
    {
        public string MessageName { get; }
        public MethodDetails Method { get; }
        public UnityMessageType Type { get; }
        public bool IsEmpty { get; }
        public bool HasPerformanceImplications { get; }

        public UnityMessageInfo(
            string messageName,
            MethodDetails method,
            UnityMessageType type,
            bool isEmpty,
            bool hasPerformanceImplications)
        {
            MessageName = messageName;
            Method = method;
            Type = type;
            IsEmpty = isEmpty;
            HasPerformanceImplications = hasPerformanceImplications;
        }
    }
}

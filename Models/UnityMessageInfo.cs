namespace UnityIntelligenceMCP.Models
{
    public record UnityMessageInfo(
        string MessageName,
        MethodDetails Method,
        UnityMessageType Type,
        bool IsEmpty,
        bool HasPerformanceImplications
    );

    public enum UnityMessageType
    {
        Awake,
        Start,
        Update,
        FixedUpdate,
        LateUpdate,
        OnEnable,
        OnDisable,
        OnDestroy,
        OnCollision,
        OnTrigger,
        OnGUI,
        OnRender,
        Other
    }
}

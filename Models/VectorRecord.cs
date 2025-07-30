using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models
{
    public record VectorRecord(
        string Id,
        float[] Vector,
        Dictionary<string, object> Metadata
    );
}

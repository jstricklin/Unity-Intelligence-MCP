using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public interface IEmbeddingService
    {
        Task<float[]> EmbedAsync(string text);
        Task<IEnumerable<float[]>> EmbedAsync(List<string> texts);
    }
}

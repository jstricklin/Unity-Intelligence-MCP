using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public interface IEmbeddingService
    {
        Task<float[]> EmbedAsync(string text);
    }
}

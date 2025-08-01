using System.Threading.Tasks;
using AllMiniLmL6V2Sharp;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class AllMiniLMEmbeddingService : IEmbeddingService
    {
        public Task<float[]> EmbedAsync(string text)
        {
            using var embedder = new AllMiniLmL6V2Embedder();
            var embedding = (float[])embedder.GenerateEmbedding(text);
            return Task.FromResult(embedding);
        }
    }
}

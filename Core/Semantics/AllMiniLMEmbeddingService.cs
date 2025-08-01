using System.Threading.Tasks;
using AllMiniLmL6V2Sharp;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class AllMiniLMEmbeddingService : IEmbeddingService
    {
        AllMiniLmL6V2Embedder _embedder = new AllMiniLmL6V2Embedder();
        public Task<float[]> EmbedAsync(string text)
        {
            var embedding = _embedder.GenerateEmbedding(text).ToArray();
            return Task.FromResult(embedding);
        }
        public Task<IEnumerable<float[]>> EmbedAsync(List<string> texts)
        {
            var embeddings = _embedder.GenerateEmbeddings(texts).Select(e => e.ToArray());
            return Task.FromResult(embeddings);
        }
        public void Dispose()
        {
            _embedder?.Dispose();
        }
    }
}

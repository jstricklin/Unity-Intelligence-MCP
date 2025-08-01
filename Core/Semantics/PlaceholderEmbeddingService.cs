using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class PlaceholderEmbeddingService : IEmbeddingService
    {
        private const int EmbeddingSize = 384;

        public Task<float[]> EmbedAsync(string text)
        {
            // For now, return a zero vector of the correct dimension.
            // In a real implementation, this would call an embedding model.
            var embedding = new float[EmbeddingSize];
            return Task.FromResult(embedding);
        }
    }
}

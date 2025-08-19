using System.Threading.Tasks;
using Microsoft.Extensions.AI;

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

        public Task<IEnumerable<float[]>> EmbedAsync(List<string> texts)
        {
            var embedding = new float[EmbeddingSize];
            IEnumerable<float[]> embeddings = new List<float[]>();
            foreach (var text in texts)
            {
                embeddings = embeddings.Append(embedding);
            }
            return Task.FromResult(embeddings);
        }
    }
}

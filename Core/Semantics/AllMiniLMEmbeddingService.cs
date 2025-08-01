using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AllMiniLmL6V2Sharp;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class AllMiniLMEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly AllMiniLmL6V2Embedder _embedder;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public AllMiniLMEmbeddingService()
        {
            _embedder = new AllMiniLmL6V2Embedder();
        }

        public async Task<float[]> EmbedAsync(string text)
        {
            await _lock.WaitAsync();
            try {
                var embedding = _embedder.GenerateEmbedding(text).ToArray();
                return await Task.FromResult(embedding);
            }
            finally {
                _lock.Release();
            }
        }

        public async Task<IEnumerable<float[]>> EmbedAsync(List<string> texts)
        {
            await _lock.WaitAsync();
            try {
                var embeddings = _embedder.GenerateEmbeddings(texts).Select(e => e.ToArray());
                return await Task.FromResult(embeddings);
            }
            finally {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            _embedder?.Dispose();
        }
    }
}

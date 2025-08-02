using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AllMiniLmL6V2Sharp;
using ModelContextProtocol.Protocol;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class AllMiniLMEmbeddingService : IEmbeddingService, IDisposable
    {
        private AllMiniLmL6V2Embedder _embedder;
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
            if (_embedder != null)
                Dispose();
            _embedder = new AllMiniLmL6V2Embedder();
            await _lock.WaitAsync();
            try
            {
                Console.Error.WriteLine($"[DEBUG] Generating embeddings...");
                var embeddings = _embedder.GenerateEmbeddings(texts).Select(e => e.ToArray());
                return await Task.FromResult(embeddings);
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("[ERROR] Embedding operation timed out - likely hung in native code");
                return Enumerable.Empty<float[]>();
                // throw;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[EMBEDDING ERROR] " + e.ToString());
                return Enumerable.Empty<float[]>();
            }
            finally
            {
                _lock.Release();
                Dispose();
            }
        }

        public void Dispose()
        {
            _embedder?.Dispose();
        }
    }
}

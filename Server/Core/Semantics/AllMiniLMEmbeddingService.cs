using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AllMiniLmL6V2Sharp;
using Microsoft.ML.Trainers;
using ModelContextProtocol.Protocol;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class AllMiniLMEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly ConcurrentBag<AllMiniLmL6V2Embedder> _embedderPool = new();
        private readonly SemaphoreSlim _poolSemaphore;
        private readonly int _maxEmbedders;
        private bool _disposed;

        public AllMiniLMEmbeddingService(int maxEmbedders = 4)
        {
            _maxEmbedders = Math.Max(1, maxEmbedders);
            _poolSemaphore = new SemaphoreSlim(_maxEmbedders, _maxEmbedders);
            
            // Pre-initialize embedders to avoid cold-start delays
            for (int i = 0; i < _maxEmbedders; i++)
            {
                _embedderPool.Add(CreateEmbedderInstance());
            }
        }

        public async Task<float[]> EmbedAsync(string text)
        {
            // For single items, still use batch API to leverage pool
            var results = await EmbedAsync(new List<string> { text });
            return results.First();
        }

        public async Task<IEnumerable<float[]>> EmbedAsync(List<string> texts)
        {
            await _poolSemaphore.WaitAsync();
            try
            {
                if (!_embedderPool.TryTake(out var embedder))
                {
                    embedder = CreateEmbedderInstance();
                }

                try
                {
                    return embedder.GenerateEmbeddings(texts).Select(e => e.ToArray()).ToList();
                }
                finally
                {
                    _embedderPool.Add(embedder);
                }
            }
            finally
            {
                _poolSemaphore.Release();
            }
        }

        private AllMiniLmL6V2Embedder CreateEmbedderInstance()
        {
            return new AllMiniLmL6V2Embedder();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var embedder in _embedderPool)
            {
                embedder?.Dispose();
            }
            _poolSemaphore?.Dispose();
        }
    }
}

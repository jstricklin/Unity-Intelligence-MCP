using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class DocumentationOrchestrationService
    {
        private readonly IDbWorkQueue _workQueue;
        private readonly IEmbeddingService _embeddingService;

        public DocumentationOrchestrationService(IDbWorkQueue workQueue, IEmbeddingService embeddingService)
        {
            _workQueue = workQueue;
            _embeddingService = embeddingService;
        }

        public async Task ProcessAndStoreSourceAsync(IDocumentationSource source)
        {
            var semanticRecord = await source.ToSemanticRecordAsync(_embeddingService);
            await _workQueue.EnqueueAsync(semanticRecord);
        }

        public bool TryCompleteQueue() => _workQueue.Writer.TryComplete();
    }
}

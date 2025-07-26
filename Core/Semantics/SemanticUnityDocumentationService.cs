using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class DocumentationOrchestrationService
    {
        private readonly IDocumentationRepository _repository;
        private readonly IEmbeddingService _embeddingService;

        public DocumentationOrchestrationService(IDocumentationRepository repository, IEmbeddingService embeddingService)
        {
            _repository = repository;
            _embeddingService = embeddingService;
        }

        public async Task ProcessAndStoreSourceAsync(IDocumentationSource source)
        {
            var universalRecord = await source.ToUniversalRecord(_embeddingService);
            await _repository.InsertDocumentAsync(universalRecord);
        }
    }
}

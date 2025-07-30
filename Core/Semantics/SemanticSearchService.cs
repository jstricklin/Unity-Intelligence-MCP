using System.Collections.Generic;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class SemanticSearchService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorRepository _vectorRepository;

        public SemanticSearchService(IEmbeddingService embeddingService, IVectorRepository vectorRepository)
        {
            _embeddingService = embeddingService;
            _vectorRepository = vectorRepository;
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query, int maxResults)
        {
            var queryVector = await _embeddingService.EmbedAsync(query);
            return await _vectorRepository.SearchAsync(queryVector, maxResults);
        }
    }
}

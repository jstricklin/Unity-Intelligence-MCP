
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public interface ISemanticSearchService
    {
        Task<IEnumerable<SemanticSearchResult>> SearchAsync(string query, int limit = 5, string sourceType = "scripting_api");
        public Task<List<DocumentGroup>> HybridSearchAsync(string query, int docLimit = 5, int chunksPerDoc = 3, double semanticWeight = 0.75, string sourceType = "scripting_api");
    }
}

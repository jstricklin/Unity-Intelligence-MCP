using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Core.Data
{
    public interface IDocumentationRepository
    {
        Task<int> InsertDocumentAsync(UniversalDocumentRecord record, CancellationToken cancellationToken = default);
        Task<IEnumerable<SearchResult>> SemanticSearchAsync(float[] embedding, int limit = 10, CancellationToken cancellationToken = default);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Core.Data
{
    public interface IDocumentationRepository
    {
        Task<int> InsertDocumentAsync(SemanticDocumentRecord record, CancellationToken cancellationToken = default);
        Task<IEnumerable<SearchResult>> SemanticSearchAsync(float[] embedding, int limit = 10, CancellationToken cancellationToken = default);
        Task<int> GetDocCountForVersionAsync(string unityVersion, CancellationToken cancellationToken = default);
        Task DeleteDocsByVersionAsync(string unityVersion, CancellationToken cancellationToken = default);
    }
}

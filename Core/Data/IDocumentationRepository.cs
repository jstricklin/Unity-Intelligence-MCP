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
        Task<IReadOnlyList<SemanticDocumentRecord>> InsertDocumentsInBulkAsync(IReadOnlyList<SemanticDocumentRecord> records, CancellationToken cancellationToken = default);
        Task<int> GetDocCountForVersionAsync(string unityVersion, CancellationToken cancellationToken = default);
        Task DeleteDocsByVersionAsync(string unityVersion, CancellationToken cancellationToken = default);
    }
}

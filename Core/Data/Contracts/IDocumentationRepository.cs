using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Database;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Core.Data.Contracts
{
    public interface IDocumentationRepository
    {
        Task<int> InsertDocumentAsync(SemanticDocumentRecord record, CancellationToken cancellationToken = default);
        Task<Dictionary<string, long>> InsertDocumentsInBulkAsync(IReadOnlyList<SemanticDocumentRecord> records, CancellationToken cancellationToken = default);
        Task InsertContentElementsInBulkAsync(IReadOnlyList<ContentElementRecord> elements, CancellationToken cancellationToken = default);
        Task<int> GetDocCountForVersionAsync(string unityVersion, CancellationToken cancellationToken = default);
        Task DeleteDocsByVersionAsync(string unityVersion, CancellationToken cancellationToken = default);
        Task InitializeDocumentTrackingAsync(string unityVersion, IEnumerable<FileStatus> fileStatuses);
        Task<Dictionary<string, FileStatus>> GetDocumentTrackingAsync(string unityVersion);
        Task MarkDocumentProcessingAsync(string filePath, string unityVersion);
        Task MarkDocumentProcessedAsync(string filePath, string unityVersion);
        Task MarkDocumentFailedAsync(string filePath, string unityVersion);
        Task RemoveDeprecatedDocumentsAsync(string unityVersion);
        Task ResetTrackingStateAsync(string unityVersion);
        Task RemoveOrphanedTrackingAsync(string unityVersion, IEnumerable<string> orphanedPaths);
    }
}

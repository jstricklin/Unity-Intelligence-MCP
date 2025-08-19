using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Models.Database;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Models
{
    public interface IDocumentationSource
    {
        string SourceType { get; }
        Task<SemanticDocumentRecord> ToSemanticRecordAsync(IEmbeddingService embeddingService);
    }
}

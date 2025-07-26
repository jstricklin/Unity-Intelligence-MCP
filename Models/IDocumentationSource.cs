using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Models
{
    public interface IDocumentationSource
    {
        string SourceType { get; }
        Task<UniversalDocumentRecord> ToUniversalRecord(IEmbeddingService embeddingService);
    }
}

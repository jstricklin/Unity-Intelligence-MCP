using System.Collections.Generic;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Data
{
    public interface IVectorRepository
    {
        Task InitializeChromaDbAsync();
        Task AddEmbeddingsAsync(IEnumerable<VectorRecord> records);
        Task<IEnumerable<SearchResult>> SearchAsync(ReadOnlyMemory<float> queryVector, int topK);
        Task DeleteByVersionAsync(string unityVersion);
    }
}

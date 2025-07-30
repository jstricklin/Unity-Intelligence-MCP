using System.Collections.Generic;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Data
{
    public interface IVectorRepository
    {
        Task InitializeAsync();
        Task AddEmbeddingsAsync(IEnumerable<VectorRecord> records);
        Task<IEnumerable<SearchResult>> SearchAsync(float[] queryVector, int topK);
        Task DeleteByVersionAsync(string unityVersion);
    }
}

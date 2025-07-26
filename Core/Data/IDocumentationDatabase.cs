using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data
{
    public interface IDocumentationDatabase
    {
        string GetConnectionString();
        Task InitializeDatabaseAsync();
    }
}

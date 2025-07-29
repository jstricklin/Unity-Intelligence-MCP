using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data
{
    public interface IApplicationDatabase
    {
        string GetConnectionString();
        Task InitializeDatabaseAsync();
    }
}

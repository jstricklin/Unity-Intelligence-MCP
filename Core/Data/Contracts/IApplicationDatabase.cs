using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data.Contracts
{
    public interface IApplicationDatabase
    {
        string GetConnectionString();
        Task InitializeDatabaseAsync(string unityVersion);
    }
}

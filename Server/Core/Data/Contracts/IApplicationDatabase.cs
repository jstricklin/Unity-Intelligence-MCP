using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data.Contracts
{
    public interface IApplicationDatabase
    {
        Task InitializeDatabaseAsync(string unityVersion);
    }
}

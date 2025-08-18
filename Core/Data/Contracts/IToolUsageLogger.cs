using System.Threading.Tasks;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Data.Contracts
{
    public interface IToolUsageLogger
    {
        Task LogAsync(ToolUsageLog log);
    }
}

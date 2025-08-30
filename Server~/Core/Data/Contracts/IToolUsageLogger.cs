using System.Threading.Tasks;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Data.Contracts
{
    public interface IToolUsageLogger
    {
        ToolUsageLog Parse(string toolData, ToolUsageLog? log = null);
        Task LogAsync(ToolUsageLog log);
    }
}

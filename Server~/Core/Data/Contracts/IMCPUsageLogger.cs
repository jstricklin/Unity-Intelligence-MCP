using System.Threading.Tasks;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Core.Data.Contracts
{
    public interface IMCPUsageLogger
    {
        MCPUsageLog Parse(string toolData, MCPUsageLog? log = null);
        Task LogAsync(MCPUsageLog log);
    }
}

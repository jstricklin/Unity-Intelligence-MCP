using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Services.Contracts
{
    public interface IVSCodeWorkspaceService
    {
        Task<string> GenerateWorkspaceAsync(string projectPath, string workspaceName = "project-workspace");
    }
}

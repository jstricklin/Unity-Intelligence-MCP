using UnityIntelligenceMCP.Core.Commands.Contracts;

namespace UnityIntelligenceMCP.Core.Commands.Contracts
{
    public interface ICommandService
    {
        public void RegisterCommand(IServerCommand handler);
        public Task<object> ExecuteCommand(string commandName, string? parameters = null);
    }
}

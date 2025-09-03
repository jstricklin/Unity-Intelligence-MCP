namespace UnityIntelligenceMCP.Core.Commands.Contracts
{
    public interface IServerCommand
    {
        public string CommandName { get; }
        public Task<object> ExecuteCommand(string? data = null);
    }
}

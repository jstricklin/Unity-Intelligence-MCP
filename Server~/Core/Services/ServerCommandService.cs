using Microsoft.AspNetCore.Hosting.Server;
using UnityIntelligenceMCP.Core.Commands;
using UnityIntelligenceMCP.Core.Commands.Contracts;

namespace UnityIntelligenceMCP.Core.Data.Contracts
{
    public class ServerCommandService : ICommandService
    {

        private readonly Dictionary<string, IServerCommand> _commands = new();
        private readonly ILogger<ServerCommandService> _logger;
        private readonly IDbWorkQueue _workQueue;
        public ServerCommandService(
            IDbWorkQueue workQueue,
            ILogger<ServerCommandService> logger)
        {
            _workQueue = workQueue;
            _logger = logger;
            
            RegisterCommand(new IngestConsoleLogsCommand(_workQueue));

            Console.Error.WriteLine("command service initialized");
        }

        public void RegisterCommand(IServerCommand command)
        {
            if (_commands.ContainsKey(command.CommandName))
            {
                _logger.LogWarning($"Command '{command.CommandName}' already registered");
                return;
            }
            _commands.Add(command.CommandName, command);
        }

        public async Task<object> ExecuteCommand(string commandName, string? parameters = null)
        {
            try
            {
                if (!_commands.TryGetValue(commandName, out var tool))
                {
                    // return await Task.FromResult(ToolResponse.ErrorResponse($"Resource not supported: {resourceUri}"));
                    return $"Command not supported: {commandName}";
                }
                // return tool?.ExecuteCommand(parameters)!;
                return await tool?.ExecuteCommand(parameters)!;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Command error: {ex.Message}\n{ex.StackTrace}");
                // return await Task.FromResult(ToolResponse.ErrorResponse($"Internal error: {ex.Message}"));
                return await Task.FromResult("Internal error: {ex.Message}");
            }
        }
    }
}

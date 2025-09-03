using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data.Contracts;

namespace UnityIntelligenceMCP.Core.Services
{
    public class ConsoleLogPruningService : BackgroundService
    {
        private readonly IConsoleLogRepository _logRepository;
        private readonly ILogger<ConsoleLogPruningService> _logger;
        private const int RetentionHours = 1; // Prune logs older than 1 hour

        public ConsoleLogPruningService(IConsoleLogRepository logRepository, ILogger<ConsoleLogPruningService> logger)
        {
            _logRepository = logRepository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Running console log pruning task.");
                
                try
                {
                    await _logRepository.PruneOldLogsAsync(RetentionHours);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during console log pruning.");
                }
                
                // Wait for 5 minutes before running again
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}

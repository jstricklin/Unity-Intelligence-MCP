using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models.Database;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Core.Data.Services
{
    public class QueuedDbWriterService : BackgroundService
    {
        private readonly IDbWorkQueue _workQueue;
        private readonly ILogger<QueuedDbWriterService> _logger;
        private readonly IDocumentationRepository _repository;
        private readonly Dictionary<Type, Func<IReadOnlyList<IDbWorkItem>, CancellationToken, Task>> _handlers;

        public QueuedDbWriterService(
            IDbWorkQueue workQueue, 
            ILogger<QueuedDbWriterService> logger, 
            IDocumentationRepository repository)
        {
            _workQueue = workQueue;
            _repository = repository;
            _logger = logger;

            // Map work item types to their specific bulk handling logic.
            _handlers = new Dictionary<Type, Func<IReadOnlyList<IDbWorkItem>, CancellationToken, Task>>
            {
                { typeof(SemanticDocumentRecord), HandleSemanticDocumentRecordsAsync }
            };
        }

        private Task HandleSemanticDocumentRecordsAsync(IReadOnlyList<IDbWorkItem> workItems, CancellationToken cancellationToken)
        {
            var records = workItems.Cast<SemanticDocumentRecord>().ToList();
            return _repository.InsertDocumentsInBulkAsync(records, cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && await _workQueue.Reader.WaitToReadAsync(stoppingToken))
            {
                var batch = new List<IDbWorkItem>();
                // Form a batch of up to 1000 items. Adjust size as needed.
                while (batch.Count < 1000 && _workQueue.Reader.TryRead(out var item))
                {
                    batch.Add(item);
                }

                if (batch.Count > 0)
                {
                    await ProcessBatch(batch, stoppingToken);
                }
            }
        }
        
        private async Task ProcessBatch(IReadOnlyList<IDbWorkItem> batch, CancellationToken stoppingToken)
        {
            var groupedItems = batch.GroupBy(item => item.GetType());

            foreach (var group in groupedItems)
            {
                if (_handlers.TryGetValue(group.Key, out var handler))
                {
                    try
                    {
                        await handler(group.ToList(), stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[ERROR] Failed to process DB work batch for type {group.Key.Name}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning($"[WARN] No handler registered for DB work item type: {group.Key.Name}");
                }
            }
        }
    }
}

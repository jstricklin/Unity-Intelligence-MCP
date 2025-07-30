using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Core.Data
{
    public class QueuedDbWriterService : BackgroundService
    {
        private readonly IDbWorkQueue _workQueue;
        private readonly IDocumentationRepository _repository;
        private readonly Dictionary<Type, Func<IDbWorkItem, CancellationToken, Task>> _handlers;

        public QueuedDbWriterService(IDbWorkQueue workQueue, IDocumentationRepository repository)
        {
            _workQueue = workQueue;
            _repository = repository;

            // Map work item types to their specific handling logic.
            _handlers = new Dictionary<Type, Func<IDbWorkItem, CancellationToken, Task>>
            {
                { typeof(SemanticDocumentRecord), HandleSemanticDocumentRecordAsync }
                // Add other handlers here for new DTO types.
            };
        }

        private Task HandleSemanticDocumentRecordAsync(IDbWorkItem workItem, CancellationToken cancellationToken)
        {
            var record = (SemanticDocumentRecord)workItem;
            return _repository.InsertDocumentAsync(record, cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Process items from the queue as they become available.
            await foreach (var workItem in _workQueue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    if (_handlers.TryGetValue(workItem.GetType(), out var handler))
                    {
                        await handler(workItem, stoppingToken);
                    }
                    else
                    {
                        Console.Error.WriteLine($"[WARN] No handler registered for DB work item type: {workItem.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Failed to process DB work item of type {workItem.GetType().Name}: {ex.Message}");
                }
            }
        }
    }
}

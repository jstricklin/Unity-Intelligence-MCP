using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Core.Data
{
    public class QueuedDbWriterService : BackgroundService
    {
        private readonly IDbWorkQueue _workQueue;
        private readonly IDocumentationRepository _repository;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorRepository _vectorRepository;
        private readonly Dictionary<Type, Func<IReadOnlyList<IDbWorkItem>, CancellationToken, Task>> _handlers;

        public QueuedDbWriterService(IDbWorkQueue workQueue, IDocumentationRepository repository, IEmbeddingService embeddingService, IVectorRepository vectorRepository)
        {
            _workQueue = workQueue;
            _repository = repository;
            _embeddingService = embeddingService;
            _vectorRepository = vectorRepository;

            // Map work item types to their specific bulk handling logic.
            _handlers = new Dictionary<Type, Func<IReadOnlyList<IDbWorkItem>, CancellationToken, Task>>
            {
                { typeof(SemanticDocumentRecord), HandleSemanticDocumentRecordsAsync }
            };
        }

        private async Task HandleSemanticDocumentRecordsAsync(IReadOnlyList<IDbWorkItem> workItems, CancellationToken cancellationToken)
        {
            var recordsToInsert = workItems.Cast<SemanticDocumentRecord>().ToList();
            
            // 1. Insert into DuckDB and get back the records with their new IDs
            var insertedRecords = await _repository.InsertDocumentsInBulkAsync(recordsToInsert, cancellationToken);

            // 2. Prepare records for ChromaDB
            var vectorRecords = new List<VectorRecord>();
            foreach (var record in insertedRecords)
            {
                foreach (var element in record.Elements)
                {
                    if (string.IsNullOrWhiteSpace(element.Content)) continue;

                    var embedding = await _embeddingService.EmbedAsync(element.Content);
                    var metadata = new Dictionary<string, object>
                    {
                        { "doc_key", record.DocKey },
                        { "element_type", element.ElementType },
                        { "title", record.Title },
                        { "class_name", record.DocType == "class" ? record.Title : string.Empty },
                        { "content", element.Content },
                        { "unity_version", record.UnityVersion ?? "unknown" }
                    };
                    vectorRecords.Add(new VectorRecord(element.Id.ToString(), embedding, metadata));
                }
            }
            
            // 3. Index in ChromaDB
            if(vectorRecords.Any())
            {
                await _vectorRepository.AddEmbeddingsAsync(vectorRecords);
                Console.Error.WriteLine($"[ChromaDB] Indexed {vectorRecords.Count} content elements.");
            }
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
                        Console.Error.WriteLine($"[ERROR] Failed to process DB work batch for type {group.Key.Name}: {ex.Message}");
                    }
                }
                else
                {
                    Console.Error.WriteLine($"[WARN] No handler registered for DB work item type: {group.Key.Name}");
                }
            }
        }
    }
}

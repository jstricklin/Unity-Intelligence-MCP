using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using UnityIntelligenceMCP.Core.Data;
using System.Threading;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Database;
using UnityIntelligenceMCP.Models.Documentation;
using UnityIntelligenceMCP.Utilities;

namespace UnityIntelligenceMCP.Core.Semantics
{
    public class DocumentationIndexingService
    {
        private readonly UnityInstallationService _unityInstallationService;
        private readonly IDocumentationRepository _repository;
        private readonly UnityDocumentationParser _parser;
        private readonly DocumentationOrchestrationService _orchestrationService;
        private readonly IDocumentChunker _chunker;
        private readonly IEmbeddingService _embeddingService;

        public DocumentationIndexingService(
            UnityInstallationService unityInstallationService,
            IDocumentationRepository repository,
            UnityDocumentationParser parser,
            DocumentationOrchestrationService orchestrationService,
            IDocumentChunker chunker,
            IEmbeddingService embeddingService)
        {
            _unityInstallationService = unityInstallationService;
            _repository = repository;
            _parser = parser;
            _orchestrationService = orchestrationService;
            _chunker = chunker;
            _embeddingService = embeddingService;
        }
        public async Task IndexDocumentationIfRequiredAsync(string projectPath, bool? forceReindex)
        {
            var unityVersion = _unityInstallationService.GetProjectVersion(projectPath);
            if (string.IsNullOrEmpty(unityVersion))
            {
                Console.Error.WriteLine("[WARN] Could not determine Unity version. Skipping documentation indexing.");
                return;
            }

            string docPath;
            try
            {
                docPath = _unityInstallationService.GetDocumentationPath(projectPath, "ScriptReference");
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.Error.WriteLine($"[ERROR] Could not start indexing. Documentation directory not found: {ex.Message}");
                return;
            }

            // Materialize file list immediately
            List<string> htmlFiles;
            try
            {
                htmlFiles = Directory.EnumerateFiles(docPath, "*.html", SearchOption.AllDirectories).ToList();
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.Error.WriteLine($"[ERROR] Documentation directory not found: {ex.Message}");
                return;
            }
            // Determine if indexing is needed based on file count
            bool shouldIndex = forceReindex == true || 
                await _repository.GetDocCountForVersionAsync(unityVersion) != htmlFiles.Count;
            
            // Only remove existing entries if forcing reindex
            if (shouldIndex && forceReindex == true)
            {
                await _repository.DeleteDocsByVersionAsync(unityVersion);
            }

            if (shouldIndex)
            {
                _ = Task.Run(async () => 
                {
                    try 
                    {
                        await ProcessDocumentationInBackground(unityVersion, htmlFiles);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[FATAL] Indexing failed: {ex}");
                    }
                });
                Console.Error.WriteLine("[INFO] Documentation indexing started in background");
            }
        }

        private async Task ProcessDocumentationInBackground(string unityVersion, List<string> htmlFiles)
        {
            Console.Error.WriteLine($"[PROCESS] Starting documentation for {unityVersion}");
            var sw = Stopwatch.StartNew();
            
            // Initialize tracking
            var fileStatuses = new List<FileStatus>();
            foreach (var file in htmlFiles)
            {
                fileStatuses.Add(new FileStatus {
                    FilePath = file,
                    ContentHash = await FileHasher.ComputeSHA256Async(file),
                    State = DocumentState.Pending
                });
            }
            await _repository.InitializeDocumentTrackingAsync(unityVersion, fileStatuses);
            
            // Get pending files
            var trackingData = await _repository.GetDocumentTrackingAsync(unityVersion);
            var pendingFiles = htmlFiles.Where(f => 
                !trackingData.ContainsKey(f) || 
                trackingData[f].State != DocumentState.Processed
            ).ToList();
            
            if (!pendingFiles.Any())
            {
                Console.Error.WriteLine("[INFO] No pending documents - indexing complete");
                return;
            }
            
            // Configure parallel processing
            const int FilesPerBatch = 6;  // Optimal for DuckDB performance
            const int MaxParallelism = Environment.ProcessorCount;
            var options = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism };
            int processedCount = 0;
            int totalFiles = pendingFiles.Count;

            await Parallel.ForEachAsync(
                BatchFiles(pendingFiles, FilesPerBatch),
                options,
                async (fileBatch, token) =>
                {
                    var batchRecords = new List<SemanticDocumentRecord>();
                    var processedInBatch = new List<string>();
                    
                    foreach (var filePath in fileBatch)
                    {
                        try
                        {
                            await _repository.MarkDocumentProcessingAsync(filePath, unityVersion);
                            
                            var parsedData = _parser.Parse(filePath);
                            parsedData.UnityVersion = unityVersion;
                            
                            var chunks = _chunker.ChunkDocument(parsedData);
                            var texts = new List<string> { parsedData.Description }
                                .Concat(chunks.Select(c => c.Text))
                                .ToList();
                            
                            // Get embeddings in one batch per document
                            var embeddings = await _embeddingService.EmbedAsync(texts);
                            
                            parsedData.Embedding = embeddings.ElementAt(0);
                            for (int i = 0; i < chunks.Count; i++)
                            {
                                chunks[i].Embedding = embeddings.ElementAt(i + 1);
                            }
                            
                            var source = new UnityDocumentationSource(parsedData, _chunker, chunks);
                            batchRecords.Add(await source.ToSemanticRecordAsync(_embeddingService));
                            processedInBatch.Add(filePath);
                            
                            // Update progress
                            var current = Interlocked.Increment(ref processedCount);
                            Console.Error.WriteLine($"[PROGRESS] {current}/{totalFiles} files processed");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[ERROR] {Path.GetFileName(filePath)}: {ex.Message}");
                            await _repository.MarkDocumentFailedAsync(filePath, unityVersion);
                        }
                    }
                    
                    // Batch insert to database
                    if (batchRecords.Count > 0)
                    {
                        await _repository.InsertDocumentsInBulkAsync(batchRecords);
                        
                        // Batch mark as processed
                        foreach (var filePath in processedInBatch)
                        {
                            await _repository.MarkDocumentProcessedAsync(filePath, unityVersion);
                        }
                    }
                });
            
            // Cleanup
            await _repository.RemoveDeprecatedDocumentsAsync(unityVersion);
            sw.Stop();
            Console.Error.WriteLine($"[COMPLETE] Indexing finished in {sw.Elapsed.TotalSeconds}s");
        }

        private IEnumerable<IEnumerable<TSource>> BatchFiles<TSource>(IEnumerable<TSource> source, int batchSize)
        {
            TSource[] bucket = null;
            var count = 0;
            
            foreach (var item in source)
            {
                bucket ??= new TSource[batchSize];
                bucket[count++] = item;
                
                if (count != batchSize) 
                    continue;
                
                yield return bucket;
                
                bucket = null;
                count = 0;
            }
            
            if (bucket != null && count > 0)
                yield return bucket.Take(count);
        }
    }
}

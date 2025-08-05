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
        private readonly IDuckDbConnectionFactory _connectionFactory;

        public DocumentationIndexingService(
            UnityInstallationService unityInstallationService,
            IDocumentationRepository repository,
            UnityDocumentationParser parser,
            DocumentationOrchestrationService orchestrationService,
            IDocumentChunker chunker,
            IEmbeddingService embeddingService,
            IDuckDbConnectionFactory connectionFactory)
        {
            _unityInstallationService = unityInstallationService;
            _repository = repository;
            _parser = parser;
            _orchestrationService = orchestrationService;
            _chunker = chunker;
            _embeddingService = embeddingService;
            _connectionFactory = connectionFactory;
        }

        public async Task<IndexingStatus> GetIndexingStatusAsync(string projectPath)
        {
            var unityVersion = _unityInstallationService.GetProjectVersion(projectPath);
            if (string.IsNullOrEmpty(unityVersion))
            {
                return new IndexingStatus { Status = "Error: Unity version not found.", UnityVersion = "Unknown" };
            }

            string docPath;
            try
            {
                docPath = _unityInstallationService.GetDocumentationPath(projectPath, "ScriptReference");
            }
            catch (DirectoryNotFoundException)
            {
                return new IndexingStatus { Status = "Error: Documentation directory not found.", UnityVersion = unityVersion };
            }

            var totalCount = Directory.EnumerateFiles(docPath, "*.html", SearchOption.AllDirectories).Count();

            if (totalCount == 0)
            {
                return new IndexingStatus { Status = "Complete", TotalCount = 0, ProcessedCount = 0, UnityVersion = unityVersion };
            }

            var trackingData = await _repository.GetDocumentTrackingAsync(unityVersion);
            var processedCount = trackingData.Values.Count(s => s.State == DocumentState.Processed);

            var status = "Not Started";
            if (processedCount == totalCount)
            {
                status = "Complete";
            }
            else if (processedCount > 0)
            {
                status = "In Progress";
            }

            return new IndexingStatus
            {
                UnityVersion = unityVersion,
                Status = status,
                ProcessedCount = processedCount,
                TotalCount = totalCount
            };
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
                await _repository.ResetTrackingStateAsync(unityVersion);
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
            // Add recovery before processing
            await _connectionFactory.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "CHECKPOINT";
                await cmd.ExecuteNonQueryAsync();
            });

            Console.Error.WriteLine($"[PROCESS] Starting documentation for {unityVersion}");
            var sw = Stopwatch.StartNew();
            
            // Load existing tracking data FIRST
            var trackingData = await _repository.GetDocumentTrackingAsync(unityVersion);
            
            // Initialize tracking with smart state detection
            var fileStatuses = new List<FileStatus>();
            var pathToHash = new Dictionary<string, string>();
            
            foreach (var file in htmlFiles)
            {
                var hash = await FileHasher.ComputeSHA256Async(file);
                pathToHash[file] = hash;
                
                var state = DocumentState.Pending;
                if (trackingData.TryGetValue(file, out var status))
                {
                    // Preserve processed files that haven't changed
                    if (status.State == DocumentState.Processed && status.ContentHash == hash)
                    {
                        state = DocumentState.Processed;
                    }
                    // Keep failed state for unchanged files
                    else if (status.State == DocumentState.Failed && status.ContentHash == hash)
                    {
                        state = DocumentState.Failed;
                    }
                }
                
                fileStatuses.Add(new FileStatus {
                    FilePath = file,
                    ContentHash = hash,
                    State = state
                });
            }
            
            // Cleanup orphaned tracking entries
            var orphans = trackingData.Keys.Except(htmlFiles).ToList();
            if (orphans.Any())
            {
                Console.Error.WriteLine($"[CLEANUP] Removing {orphans.Count} orphaned tracking entries");
                await _repository.RemoveOrphanedTrackingAsync(unityVersion, orphans);
            }
            
            // Initialize/update tracking
            await _repository.InitializeDocumentTrackingAsync(unityVersion, fileStatuses);
            
            // Get pending files (only those not successfully processed)
            var currentTracking = await _repository.GetDocumentTrackingAsync(unityVersion);
            var pendingFiles = htmlFiles.Where(f => 
                currentTracking[f].State != DocumentState.Processed
            ).ToList();
            
            if (!pendingFiles.Any())
            {
                Console.Error.WriteLine("[INFO] No pending documents - indexing complete");
                return;
            }
            
            // Configure parallel processing
            const int FilesPerBatch = 1024;  
            int MaxParallelism = Environment.ProcessorCount;
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
                            var record = await source.ToSemanticRecordAsync(_embeddingService);
                            
                            // ADD THESE LINES
                            record.SourceFilePath = filePath;
                            record.ContentHash = pathToHash[filePath];
                            
                            batchRecords.Add(record);
                            processedInBatch.Add(filePath);
                            
                            // Update progress
                            var current = Interlocked.Increment(ref processedCount);
                            // Console.Error.WriteLine($"[PROGRESS] {current}/{totalFiles} files processed");
                            if (((float)current / totalFiles) * 100 % 2 == 0)
                                Console.Error.WriteLine($"[PROGRESS] {(int)(((float)current/totalFiles) * 100)}% out of {totalFiles} total files prepared for insert.");
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
            
            sw.Stop();
            Console.Error.WriteLine($"[COMPLETE] Indexing finished in {TimeSpan.FromSeconds(sw.Elapsed.TotalSeconds).ToString(@"hh\:mm\:ss")}s");
        }

        private IEnumerable<IEnumerable<TSource>> BatchFiles<TSource>(IEnumerable<TSource> source, int batchSize)
        {
            TSource[]? bucket = null;
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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using UnityIntelligenceMCP.Core.Data;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Models;
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
            var filesOnDisk = htmlFiles.Count;
            
            bool shouldIndex = false;

            if (forceReindex == true)
            {
                Console.Error.WriteLine($"[INFO] Force re-indexing enabled. Deleting existing documentation for Unity version {unityVersion}...");
                await _repository.DeleteDocsByVersionAsync(unityVersion);
                shouldIndex = true;
            }
            else
            {
                var docsInDb = await _repository.GetDocCountForVersionAsync(unityVersion);

                if (filesOnDisk != docsInDb)
                {
                    Console.Error.WriteLine($"[INFO] Documentation mismatch detected (Disk: {filesOnDisk}, DB: {docsInDb}). Re-indexing required for version {unityVersion}.");
                    if (docsInDb > 0)
                    {
                        await _repository.DeleteDocsByVersionAsync(unityVersion);
                    }
                    shouldIndex = true;
                }
                else if (filesOnDisk == 0 && docsInDb == 0)
                {
                    Console.Error.WriteLine($"[WARN] No documentation files found at '{docPath}'. Cannot perform indexing.");
                }
                else
                {
                    Console.Error.WriteLine($"[INFO] Documentation for Unity version {unityVersion} is up to date ({docsInDb} documents). Skipping indexing.");
                }
            }

            if (!shouldIndex) return;
            
            // Start background processing
            _ = Task.Run(async () =>
            {
                try 
                {
                    await ProcessDocumentationInBackground(unityVersion, htmlFiles);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Background processing failed: {ex}");
                }
            });
            
            Console.Error.WriteLine("[INFO] Documentation indexing started in background");
        }

        private async Task ProcessDocumentationInBackground(string unityVersion, List<string> htmlFiles)
        {
            Console.Error.WriteLine($"[INFO] Starting documentation indexing process for Unity version {unityVersion}...");
            var stopwatch = Stopwatch.StartNew();
            
            // Phase 1: Parsing and collecting all texts...
            Console.Error.WriteLine("[INFO] Phase 1: Parsing and collecting all texts...");
            var parsedDocs = new List<UnityDocumentationData>();
            var allChunkLists = new List<List<DocumentChunk>>();
            var allTextsToEmbed = new List<string>();

            foreach (var filePath in htmlFiles)
            {
                try
                {
                    var parsedData = _parser.Parse(filePath);
                    parsedData.UnityVersion = unityVersion;
                    var chunks = _chunker.ChunkDocument(parsedData);

                    parsedDocs.Add(parsedData);
                    allChunkLists.Add(chunks);
                    allTextsToEmbed.Add(parsedData.Description);
                    allTextsToEmbed.AddRange(chunks.Select(c => c.Text));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Failed to parse {filePath}: {ex.Message}");
                }
            }
            
            // Phase 2: Generating embeddings...
            Console.Error.WriteLine($"[INFO] Phase 2: Generating embeddings for {parsedDocs.Count} documents and their chunks ({allTextsToEmbed.Count} total texts)...");
            var allEmbeddings = new List<float[]>(allTextsToEmbed.Count);
            if (allTextsToEmbed.Any())
            {
                int batchSize = 512;
                int batchNum = 0;
                var batchStopwatch = new Stopwatch();
                foreach (var batch in allTextsToEmbed.Chunk(batchSize))
                {
                    batchNum++;
                    Console.Error.WriteLine($"[INFO] Processing a batch {batchNum} of {batch.Length} embeddings...");
                    batchStopwatch.Restart();
                    var embeddingsInBatch = await _embeddingService.EmbedAsync(batch.ToList());
                    allEmbeddings.AddRange(embeddingsInBatch);
                    batchStopwatch.Stop();
                    Console.Error.WriteLine($"[INFO] Finished batch {batchNum} in {batchStopwatch.Elapsed.TotalSeconds:F2} seconds.");
                }
            }
            
            // Distribute embeddings...
            int embeddingIndex = 0;
            for (int i = 0; i < parsedDocs.Count; i++)
            {
                parsedDocs[i].Embedding = allEmbeddings[embeddingIndex++];
                foreach (var chunk in allChunkLists[i])
                {
                    chunk.Embedding = allEmbeddings[embeddingIndex++];
                }
            }
            
            // Phase 3: Enqueuing processed documents...
            Console.Error.WriteLine("[INFO] Phase 3: Enqueuing processed documents for database insertion...");
            for (int i = 0; i < parsedDocs.Count; i++)
            {
                try
                {
                    var source = new UnityDocumentationSource(parsedDocs[i], _chunker, allChunkLists[i]);
                    await _orchestrationService.ProcessAndStoreSourceAsync(source);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Failed to create and process source for {parsedDocs[i].FilePath}: {ex.Message}");
                }
            }
            
            // Signal queue completion
            if (_orchestrationService.TryCompleteQueue())
            {
                Console.Error.WriteLine("[INFO] All documentation has been enqueued for processing.");
            }

            stopwatch.Stop();
            Console.Error.WriteLine($"[INFO] Documentation parsing and enqueuing completed for Unity version {unityVersion} in {stopwatch.Elapsed.TotalSeconds:F2} seconds. Background processing will continue.");
        }
    }
}

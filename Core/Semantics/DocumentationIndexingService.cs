using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Data;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Models;
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

            bool shouldIndex = false;

            if (forceReindex == true)
            {
                Console.Error.WriteLine($"[INFO] Force re-indexing enabled. Deleting existing documentation for Unity version {unityVersion}...");
                await _repository.DeleteDocsByVersionAsync(unityVersion);
                shouldIndex = true;
            }
            else
            {

                var filesOnDisk = Directory.EnumerateFiles(docPath, "*.html", SearchOption.AllDirectories).Count();
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

            Console.Error.WriteLine($"[INFO] Starting documentation indexing process for Unity version {unityVersion}...");
            var stopwatch = Stopwatch.StartNew();
            var htmlFiles = Directory.EnumerateFiles(docPath, "*.html", SearchOption.AllDirectories);

            // Phase 1: Parse all files and collect their chunks
            var allChunksWithPaths = new List<(string FilePath, DocumentChunk Chunk)>();
            Console.Error.WriteLine("[INFO] Phase 1: Parsing and chunking all documents...");
            foreach (var filePath in htmlFiles)
            {
                try
                {
                    var parsedData = _parser.Parse(filePath);
                    parsedData.UnityVersion = unityVersion;
                    var chunks = _chunker.ChunkDocument(parsedData);
                    foreach (var chunk in chunks)
                    {
                        allChunksWithPaths.Add((filePath, chunk));
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Failed to parse and chunk {filePath}: {ex.Message}");
                }
            }

            // Phase 2: Batch embed all chunk texts in a single operation
            Console.Error.WriteLine($"[INFO] Phase 2: Generating embeddings for {allChunksWithPaths.Count} chunks...");
            var chunkTexts = allChunksWithPaths.Select(c => c.Chunk.Text).ToList();
            if (chunkTexts.Any())
            {
                var embeddings = (await _embeddingService.EmbedAsync(chunkTexts)).ToList();
                // Assign embeddings back to their respective chunks
                for (int i = 0; i < allChunksWithPaths.Count; i++)
                {
                    allChunksWithPaths[i].Chunk.Embedding = embeddings[i];
                }
            }

            // Phase 3: Group chunks by file and enqueue for database insertion
            Console.Error.WriteLine("[INFO] Phase 3: Enqueuing processed documents for database insertion...");
            var chunksGroupedByFile = allChunksWithPaths.GroupBy(c => c.FilePath, c => c.Chunk);

            foreach (var docGroup in chunksGroupedByFile)
            {
                try
                {
                    var parsedData = _parser.Parse(docGroup.Key);
                    parsedData.UnityVersion = unityVersion;

                    var source = new UnityDocumentationSource(parsedData, _chunker, docGroup.ToList());
                    await _orchestrationService.ProcessAndStoreSourceAsync(source);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Failed to create and process source for {docGroup.Key}: {ex.Message}");
                }
            }
            
            // Signal to the queue that no more items will be added.
            if (_orchestrationService.TryCompleteQueue())
            {
                Console.Error.WriteLine("[INFO] All documentation has been enqueued for processing.");
            }

            stopwatch.Stop();
            Console.Error.WriteLine($"[INFO] Documentation parsing and enqueuing completed for Unity version {unityVersion} in {stopwatch.Elapsed.TotalSeconds:F2} seconds. Background processing will continue.");
        }
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools
{
    [McpServerToolType]
    public class DocumentationTools
    {
        private readonly ISemanticSearchService _searchService;
        private readonly DocumentationIndexingService _indexingService;
        private readonly ConfigurationService _configurationService;

        public DocumentationTools(ISemanticSearchService searchService, DocumentationIndexingService indexer, ConfigurationService configurationService)
        {
            _searchService = searchService;
            _indexingService = indexer;
            _configurationService = configurationService;
        }

        [McpServerTool(Name = "get_asset_indexing_status"), Description("Get current status of documentation database indexing.")]
        public async Task<IndexingStatus> SearchDocumentation(
            CancellationToken cancellationToken = default)
        {
            return await _indexingService.GetIndexingStatusAsync();
        }

        [McpServerTool(Name = "semantic_docs_search"), Description("Finds relevant Unity Engine documentation using semantic search.")]
        public async Task<IEnumerable<SemanticSearchResult>> SearchDocumentation(
            [Description("Natural language query for Unity-related documentation")]
            string query,
            [Description("Maximum number of results (default: 5)")]
            int maxResults = 5,
            [Description("Document source: 'scripting_api', 'editor_manual', or 'tutorial'")]
            string source = "scripting_api",
            CancellationToken cancellationToken = default)
        {
            return await _searchService.SearchAsync(query, maxResults, source);
        }
        [McpServerTool(Name = "hybrid_semantic_docs_search"), Description("Default search tool combining semantic understanding with keyword matching.")]
        public async Task<List<DocumentGroup>> HybridSearchDocumentation(
            [Description("Natural language query for Unity-related documentation")]
            string query,
            [Description("Maximum number of results (default: 5)")]
            int maxResults = 5,
            [Description("Document source: 'scripting_api', 'editor_manual', or 'tutorial'")]
            string source = "scripting_api",
            CancellationToken cancellationToken = default)
        {
            return await _searchService.HybridSearchAsync(query, docLimit: maxResults, sourceType: source);
        }
    }
}

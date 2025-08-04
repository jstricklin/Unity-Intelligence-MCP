using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools
{
    [McpServerToolType]
    public class DocumentationTools
    {
        private readonly ISemanticSearchService _searchService;

        public DocumentationTools(ISemanticSearchService searchService)
        {
            _searchService = searchService;
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
    }
}

namespace UnityIntelligenceMCP.Models
{
    public class SemanticSearchResult
    {
        public long DocId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Source { get; set; }
        public string ContentSnippet { get; set; }
        public double RelevanceScore { get; set; }
        
        // public SemanticSearchResult() {}
        
        public SemanticSearchResult(long docId, string title, string url, string source, string contentSnippet, double relevanceScore)
        {
            DocId = docId;
            Title = title;
            Url = url;
            Source = source;
            ContentSnippet = contentSnippet;
            RelevanceScore = relevanceScore;
        }
    }
}

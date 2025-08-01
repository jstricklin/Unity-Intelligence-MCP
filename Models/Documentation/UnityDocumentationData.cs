using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models.Documentation
{
    public class UnityDocumentationData
    {
        public string? Namespace { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UnityVersion { get; set; } = string.Empty;
        public DocumentationLink? InheritsFrom { get; set; }
        public DocumentationLink? ImplementedIn { get; set; }
        public List<DocumentationLink> Properties { get; set; } = new();
        public List<DocumentationLink> PublicMethods { get; set; } = new();
        public List<DocumentationLink> StaticMethods { get; set; } = new();
        public List<DocumentationLink> Messages { get; set; } = new();
        public List<DocumentationLink> InheritedProperties { get; set; } = new();
        public List<DocumentationLink> InheritedPublicMethods { get; set; } = new();
        public List<DocumentationLink> InheritedStaticMethods { get; set; } = new();
        public List<DocumentationLink> InheritedOperators { get; set; } = new();
    }

    public class DocumentationLink
    {
        public string Title { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

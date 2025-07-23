namespace UnityCodeIntelligence.Models;
public class UnityDocumentationData
{
    // Core content
    public string HtmlContent { get; set; }
    public string PlainTextContent { get; set; } // Stripped HTML for search/indexing
    
    // Metadata
    public DocumentationMetadata Metadata { get; set; }
    
    // Navigation context
    public List<DocumentationLink> RelatedPages { get; set; }
    public List<DocumentationLink> SeeAlso { get; set; }
    public BreadcrumbPath NavigationPath { get; set; }
}

public class DocumentationMetadata
{
    public string Title { get; set; }
    public string Description { get; set; }
    public DocumentationType Type { get; set; } // Class, Method, Property, etc.
    public string UnityVersion { get; set; }
    public string Namespace { get; set; }
    public string Assembly { get; set; }
    public List<string> Tags { get; set; }
    public DateTime LastModified { get; set; }
    public string RelativePath { get; set; }
    public string CanonicalUrl { get; set; }
}

public class DocumentationLink
{
    public string Title { get; set; }
    public string RelativePath { get; set; }
    public string Description { get; set; }
}

public class BreadcrumbPath
{
    public List<DocumentationLink> Path { get; set; }
    public string CurrentPage { get; set; }
}

public enum DocumentationType
{
    Class,
    Interface,
    Struct,
    Enum,
    Method,
    Property,
    Field,
    Event,
    Namespace,
    Manual,
    Tutorial,
    ReleaseNotes
}

// Alternative simpler approach for initial implementation
public class SimpleUnityDocumentationData
{
    public string Content { get; set; }
    public string ContentType { get; set; } = "text/html";
    public string Title { get; set; }
    public string UnityVersion { get; set; }
    public string RelativePath { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
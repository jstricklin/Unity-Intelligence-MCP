using System.Security;
using ModelContextProtocol.Protocol;
using HtmlAgilityPack;
namespace UnityIntelligenceMCP.Models;
public class UnityDocumentationData
{
    private record ExtractionConfig(string Selector, Func<string, string> Transform);
    // This below should extract values to build our document data object
    private static readonly Dictionary<string, ExtractionConfig> ExtractionRules = new ()
    {
        ["Title"] = new ("//div[contains(@class, 'content')]/h1",
        text => text?.Trim()),
        ["Description"] = new ("//*[contains(@class, 'description') or contains(@class, 'summary')]",
        text => text?.Trim()),
        ["Namespace"] = new ("//*[contains(@class, 'namespace')]",
        text => text?.Trim()),
        // ["UnityVersion"] = new ("[data-unity-version], .unity-version",
        // text => text?.Trim()),
        ["MainContent"] = new ("//div[contains(@class, 'content')]//div[contains(@class, 'section-content')]",
        text => text?.Trim()),
    };

    // Parse Unity documentation structure TODO: Is there a way to parse docs async here?
    public UnityDocumentationData(string filePath)
    {
        string html = File.ReadAllText(filePath);
        this.FilePath = filePath;
        this.Title = ExtractByRule(html, "Title");
        this.Description = ExtractByRule(html, "Description");
        this.Namespace = ExtractByRule(html, "Namespace");
        this.MainContent = ExtractByRule(html, "MainContent");
        // this.CodeExamples = ExtractByRule(html, "Code Examples");
        // this.Parameters = ExtractByRule(html, "Parameters");
    }
    
    private string ExtractByRule(string html, string ruleName)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var config = ExtractionRules[ruleName];
        var node = doc.DocumentNode.SelectSingleNode(config.Selector);
        return config.Transform(node?.InnerText);
    }
    // Core content
    public string HtmlContent { get; set; }
    // public string PlainTextContent { get; set; } // Stripped HTML for search/indexing

    // Metadata
    // public DocumentationMetadata Metadata { get; set; }
    public DocumentationLink InheritsFrom { get; set; }
    public DocumentationLink ImplementedIn { get; set; }
    public string FilePath { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Namespace { get; set; }
    public string MainContent { get; set; }
    public string Type { get; set; }
    public string CodeExamples { get; set; }
    public string Parameters { get; set; }
}

// public class DocumentationMetadata
// {
//     public string Title { get; set; }
//     public string Description { get; set; }
//     // public DocumentationType Type { get; set; } // Class, Method, Property, etc.
//     public string UnityVersion { get; set; }
//     public string Namespace { get; set; }
//     public string Assembly { get; set; }
//     // public List<string> Tags { get; set; }
//     public DateTime LastModified { get; set; }
//     public string RelativePath { get; set; }
//     // public string CanonicalUrl { get; set; }
// }

public class DocumentationLink
{
    public string Title { get; set; }
    public string RelativePath { get; set; }
    public string Description { get; set; }
}

// public class BreadcrumbPath
// {
//     public List<DocumentationLink> Path { get; set; }
//     public string CurrentPage { get; set; }
// }

// public enum DocumentationType
// {
//     Class,
//     Interface,
//     Struct,
//     Enum,
//     Method,
//     Property,
//     Field,
//     Event,
//     Namespace,
//     Manual,
//     Tutorial,
//     ReleaseNotes
// }

// Alternative simpler approach for initial implementation
// public class UnityDocumentationData
// {
    // public string MainContent { get; set; }
    // public string FilePath { get; set; }
    // public string Description { get; set; }
    // public List<string> CodeExamples { get; set; }
    // public string Namespace { get; set; }
    // public string Title { get; set; }
    // public List<string> Parameters { get; set; }
    // public string UnityVersion { get; set; }
    // public string RelativePath { get; set; }
    // public Dictionary<string, object> Metadata { get; set; } = new();
// }

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityIntelligenceMCP.Models;
public class UnityDocumentationData
{
    #region Extraction Logic

    // XPaths for simple text elements
    private static readonly Dictionary<string, string> TextExtractionRules = new()
    {
        ["Title"] = "//div[contains(@class, 'content')]//h1",
    };

    // TODO update below to get class inheritance and implementation
    // XPaths to find the header of each section that contains a table of links
    private static readonly Dictionary<string, string> LinkSectionRules = new()
    {
        ["Properties"] = "//h3[text()='Properties']",
        ["PublicMethods"] = "//h3[text()='Public Methods']",
        ["StaticMethods"] = "//h3[text()='Static Methods']",
        ["Messages"] = "//h3[text()='Messages']",
        ["InheritedProperties"] = "//h3[text()='Inherited Members']/following::h3[text()='Properties'][1]",
        ["InheritedPublicMethods"] = "//h3[text()='Inherited Members']/following::h3[text()='Public Methods'][1]",
        ["InheritedStaticMethods"] = "//h3[text()='Inherited Members']/following::h3[text()='Static Methods'][1]",
        ["InheritedOperators"] = "//h3[text()='Inherited Members']/following::h3[text()='Operators'][1]",
    };

    public UnityDocumentationData(string filePath)
    {
        var html = File.ReadAllText(filePath);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var docNode = doc.DocumentNode;

        FilePath = filePath;

        // Extract single text values
        Title = docNode.SelectSingleNode(TextExtractionRules["Title"])?.InnerText.Trim() ?? string.Empty;
        Description = ExtractDescription(docNode);

        // Extract groups of links
        Properties = ExtractLinks(docNode, LinkSectionRules["Properties"]);
        PublicMethods = ExtractLinks(docNode, LinkSectionRules["PublicMethods"]);
        StaticMethods = ExtractLinks(docNode, LinkSectionRules["StaticMethods"]);
        Messages = ExtractLinks(docNode, LinkSectionRules["Messages"]);
        InheritedProperties = ExtractLinks(docNode, LinkSectionRules["InheritedProperties"]);
        InheritedPublicMethods = ExtractLinks(docNode, LinkSectionRules["InheritedPublicMethods"]);
        InheritedStaticMethods = ExtractLinks(docNode, LinkSectionRules["InheritedStaticMethods"]);
        InheritedOperators = ExtractLinks(docNode, LinkSectionRules["InheritedOperators"]);
    }

    private string ExtractDescription(HtmlNode docNode)
    {
        var descriptionHeader = docNode.SelectSingleNode("//h3[text()='Description']");
        if (descriptionHeader == null) return string.Empty;

        var sb = new StringBuilder();
        var currentNode = descriptionHeader.NextSibling;
        while (currentNode != null && currentNode.Name != "h3")
        {
            if (currentNode.NodeType == HtmlNodeType.Element && currentNode.Name == "p")
            {
                sb.AppendLine(currentNode.InnerText.Trim());
            }
            currentNode = currentNode.NextSibling;
        }
        return sb.ToString().Trim();
    }

    private List<DocumentationLink> ExtractLinks(HtmlNode docNode, string sectionHeaderXPath)
    {
        var links = new List<DocumentationLink>();
        var headerNode = docNode.SelectSingleNode(sectionHeaderXPath);
        if (headerNode == null) return links;

        var tableNode = headerNode.SelectSingleNode("following-sibling::table[1]");
        if (tableNode == null) return links;

        var rows = tableNode.SelectNodes(".//tr");
        if (rows == null) return links;

        foreach (var row in rows)
        {
            var linkNode = row.SelectSingleNode("./td[1]//a");
            var descriptionNode = row.SelectSingleNode("./td[2]");

            if (linkNode != null)
            {
                links.Add(new DocumentationLink
                {
                    Title = HtmlEntity.DeEntitize(linkNode.InnerText).Trim(),
                    RelativePath = linkNode.GetAttributeValue("href", string.Empty),
                    Description = descriptionNode != null ? HtmlEntity.DeEntitize(descriptionNode.InnerText).Trim() : string.Empty
                });
            }
        }
        return links;
    }

    #endregion

    #region DTO Properties

    public string FilePath { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    public List<DocumentationLink> Properties { get; set; } = new();
    public List<DocumentationLink> PublicMethods { get; set; } = new();
    public List<DocumentationLink> StaticMethods { get; set; } = new();
    public List<DocumentationLink> Messages { get; set; } = new();
    public List<DocumentationLink> InheritedProperties { get; set; } = new();
    public List<DocumentationLink> InheritedPublicMethods { get; set; } = new();
    public List<DocumentationLink> InheritedStaticMethods { get; set; } = new();
    public List<DocumentationLink> InheritedOperators { get; set; } = new();

    #endregion
}

public class DocumentationLink
{
    public string Title { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

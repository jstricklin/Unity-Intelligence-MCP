using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Utilities
{
    public class UnityDocumentationParser
    {
        private static readonly Dictionary<string, string> TextExtractionRules = new()
        {
            ["Title"] = "//div[contains(@class, 'content')]//h1",
            ["ConstructType"] = "//div[contains(@class, 'content')]//h1/following-sibling::p[1]",
        };

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

        public UnityDocumentationData Parse(string filePath)
        {
            var html = File.ReadAllText(filePath);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var docNode = doc.DocumentNode;

            var constructTypeText = docNode.SelectSingleNode(TextExtractionRules["ConstructType"])?.InnerText.Trim() ??
                                    string.Empty;
            var inIndex = constructTypeText.IndexOf(" in ");
            if (inIndex > 0)
            {
                constructTypeText = constructTypeText.Substring(0, inIndex);
            }

            var data = new UnityDocumentationData
            {
                FilePath = filePath,
                Title = docNode.SelectSingleNode(TextExtractionRules["Title"])?.InnerText.Trim() ?? string.Empty,
                ConstructType = constructTypeText,
                Description = ExtractDescription(docNode),
                InheritsFrom = ExtractLinkFollowingText(docNode, "Inherits from:"),
                ImplementedIn = ExtractLinkFollowingText(docNode, "Implemented in:"),
                Properties = ExtractLinks(docNode, LinkSectionRules["Properties"]),
                PublicMethods = ExtractLinks(docNode, LinkSectionRules["PublicMethods"]),
                StaticMethods = ExtractLinks(docNode, LinkSectionRules["StaticMethods"]),
                Messages = ExtractLinks(docNode, LinkSectionRules["Messages"]),
                InheritedProperties = ExtractLinks(docNode, LinkSectionRules["InheritedProperties"]),
                InheritedPublicMethods = ExtractLinks(docNode, LinkSectionRules["InheritedPublicMethods"]),
                InheritedStaticMethods = ExtractLinks(docNode, LinkSectionRules["InheritedStaticMethods"]),
                InheritedOperators = ExtractLinks(docNode, LinkSectionRules["InheritedOperators"]),
                ContentLinkGroups = ExtractContentLinkGroups(docNode)
            };

            return data;
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

        private List<LinkGroup> ExtractContentLinkGroups(HtmlNode docNode)
        {
            var linkGroups = new List<LinkGroup>();
            var descriptionHeader = docNode.SelectSingleNode("//h3[text()='Description']");
            if (descriptionHeader == null) return linkGroups;

            var paragraphNodes =
                docNode.SelectNodes("//h3[text()='Description']/following-sibling::p[count(preceding-sibling::h3)=1]");
            if (paragraphNodes == null) return linkGroups;

            foreach (var pNode in paragraphNodes)
            {
                var linkNodes = pNode.SelectNodes(".//a");
                if (linkNodes == null) continue;

                var linkGroup = new LinkGroup { Context = HtmlEntity.DeEntitize(pNode.InnerText).Trim() };

                foreach (var linkNode in linkNodes)
                {
                    var href = linkNode.GetAttributeValue("href", string.Empty);
                    if (string.IsNullOrEmpty(href) || href.StartsWith("#")) continue;

                    linkGroup.Links.Add(new DocumentationLink
                    {
                        Title = HtmlEntity.DeEntitize(linkNode.InnerText).Trim(), RelativePath = href
                    });
                }

                if (linkGroup.Links.Any())
                {
                    linkGroups.Add(linkGroup);
                }
            }
            return linkGroups;
        }

        private DocumentationLink? ExtractLinkFollowingText(HtmlNode docNode, string anchorText)
        {
            var textNode = docNode.SelectSingleNode($"//div[contains(@class, 'content')]//text()[contains(., '{anchorText}')]");
            if (textNode == null)
            {
                return null;
            }

            var node = textNode.NextSibling;
            while (node != null && node.NodeType != HtmlNodeType.Element)
            {
                node = node.NextSibling;
            }

            if (node is { Name: "a" })
            {
                return new DocumentationLink
                {
                    Title = HtmlEntity.DeEntitize(node.InnerText).Trim(),
                    RelativePath = node.GetAttributeValue("href", string.Empty)
                };
            }
            return null;
        }
    }
}

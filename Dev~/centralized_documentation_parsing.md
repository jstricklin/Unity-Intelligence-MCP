using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Utilities
{
    public class DocumentSection
    {
        public string Content { get; set; } = string.Empty;
        public HtmlNode OriginalNode { get; set; }
        public string SectionType { get; set; } = "text"; // "text", "table", "list", "code"
    }

    public class UnityDocumentationParser
    {
        public UnityDocumentationData Parse(string filePath)
        {
            var html = File.ReadAllText(filePath);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var docNode = doc.DocumentNode;

            // 1. CENTRALIZED PARSING - Parse document structure once
            var allSections = ExtractAllDocumentationSections(docNode);

            // 2. EXTRACT FROM SECTIONS - All extraction from centralized data
            var data = new UnityDocumentationData
            {
                FilePath = filePath,
                Title = ExtractTitleFromDOM(docNode), // Some things still need DOM
                ConstructType = ExtractConstructTypeFromDOM(docNode), // Some things still need DOM
                Description = GetSectionContent(allSections, "Description"),
                InheritsFrom = ExtractLinkFollowingText(docNode, "Inherits from:"), // Complex DOM extraction
                ImplementedIn = ExtractLinkFollowingText(docNode, "Implemented in:"), // Complex DOM extraction
                
                // ALL THESE NOW USE CENTRALIZED SECTIONS
                Properties = ExtractLinksFromSection(allSections, "Properties"),
                PublicMethods = ExtractLinksFromSection(allSections, "Public Methods"),
                StaticMethods = ExtractLinksFromSection(allSections, "Static Methods"),
                Messages = ExtractLinksFromSection(allSections, "Messages"),
                InheritedProperties = ExtractLinksFromSection(allSections, "Inherited Properties"),
                InheritedPublicMethods = ExtractLinksFromSection(allSections, "Inherited Public Methods"),
                InheritedStaticMethods = ExtractLinksFromSection(allSections, "Inherited Static Methods"),
                InheritedOperators = ExtractLinksFromSection(allSections, "Inherited Operators"),
                
                ContentLinkGroups = ExtractContentLinkGroupsFromSection(allSections, "Description"),
                AdditionalSections = allSections
                    .Where(kvp => !IsStandardSection(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Content)
            };
            
            // Handle overloads (still complex DOM traversal)
            var declarationHeaders = docNode.SelectNodes("//div[contains(@class, 'content')]//h2[text()='Declaration']");
            if (declarationHeaders != null && declarationHeaders.Any())
            {
                data.Overloads = ExtractOverloadsFromDOM(declarationHeaders);
            }
            else
            {
                // Extract examples from sections instead of DOM
                data.Examples = ExtractCodeExamplesFromSections(allSections);
            }

            return data;
        }

        private Dictionary<string, DocumentSection> ExtractAllDocumentationSections(HtmlNode docNode)
        {
            var sections = new Dictionary<string, DocumentSection>(StringComparer.OrdinalIgnoreCase);
            
            // Try different section patterns for Unity docs
            var sectionContainers = new[]
            {
                "//div[@class='subsection']",
                "//div[contains(@class, 'section')]",
                "//div[contains(@class, 'content')]//div[h3 or h2]"
            };

            HtmlNodeCollection sectionNodes = null;
            
            foreach (var selector in sectionContainers)
            {
                sectionNodes = docNode.SelectNodes(selector);
                if (sectionNodes != null && sectionNodes.Count > 0)
                    break;
            }

            if (sectionNodes == null)
            {
                return sections; // Return empty if no sections found
            }

            string currentSectionName = null;
            var currentContentBuilder = new StringBuilder();
            HtmlNode currentSectionNode = null;
            var orphanedContent = new StringBuilder();
            HtmlNode orphanedNode = null;

            foreach (var sectionNode in sectionNodes)
            {
                var header = sectionNode.SelectSingleNode(".//h3 | .//h2 | .//h4");
                var content = ExtractRawSectionContent(sectionNode);

                if (header != null)
                {
                    // Save previous section
                    SaveCurrentSection();
                    
                    // Handle orphaned content by combining with previous section
                    if (orphanedContent.Length > 0 && currentSectionName != null && sections.ContainsKey(currentSectionName))
                    {
                        sections[currentSectionName].Content += "\n" + orphanedContent.ToString().Trim();
                        orphanedContent.Clear();
                    }

                    // Start new section
                    currentSectionName = CleanHeaderText(header.InnerText);
                    currentContentBuilder.Clear();
                    currentSectionNode = sectionNode;
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        currentContentBuilder.AppendLine(content);
                    }
                }
                else
                {
                    // No header - add to current section or orphaned content
                    if (currentSectionName != null)
                    {
                        if (!string.IsNullOrEmpty(content))
                        {
                            currentContentBuilder.AppendLine(content);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(content))
                        {
                            orphanedContent.AppendLine(content);
                            orphanedNode = sectionNode;
                        }
                    }
                }
            }

            // Save final section
            SaveCurrentSection();

            // Handle remaining orphaned content
            if (orphanedContent.Length > 0)
            {
                var orphanedText = orphanedContent.ToString().Trim();
                if (sections.ContainsKey("Description"))
                {
                    sections["Description"].Content += "\n" + orphanedText;
                }
                else
                {
                    sections["Description"] = new DocumentSection
                    {
                        Content = orphanedText,
                        OriginalNode = orphanedNode,
                        SectionType = DetermineNodeType(orphanedNode)
                    };
                }
            }

            void SaveCurrentSection()
            {
                if (currentSectionName != null && (currentContentBuilder.Length > 0 || currentSectionNode != null))
                {
                    sections[currentSectionName] = new DocumentSection
                    {
                        Content = currentContentBuilder.ToString().Trim(),
                        OriginalNode = currentSectionNode,
                        SectionType = DetermineNodeType(currentSectionNode)
                    };
                }
            }

            return sections;
        }

        // NEW: Extract links from sections instead of DOM traversal
        private List<DocumentationLink> ExtractLinksFromSection(Dictionary<string, DocumentSection> allSections, string sectionName)
        {
            var links = new List<DocumentationLink>();
            
            if (!allSections.ContainsKey(sectionName))
                return links;

            var section = allSections[sectionName];
            
            // If we have the original node and it contains tables, extract from DOM
            if (section.OriginalNode != null && section.SectionType == "table")
            {
                return ExtractLinksFromTableNode(section.OriginalNode);
            }
            
            // Otherwise, try to extract from content text (fallback)
            return ExtractLinksFromContent(section.Content);
        }

        private List<DocumentationLink> ExtractLinksFromTableNode(HtmlNode sectionNode)
        {
            var links = new List<DocumentationLink>();
            
            // Look for table in this section or following siblings
            var tableNode = sectionNode.SelectSingleNode(".//table") ?? 
                           sectionNode.SelectSingleNode("following-sibling::table[1]");
            
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
                        Description = descriptionNode != null ? 
                            HtmlEntity.DeEntitize(descriptionNode.InnerText).Trim() : string.Empty
                    });
                }
            }
            return links;
        }

        // NEW: Extract content link groups from sections
        private List<LinkGroup> ExtractContentLinkGroupsFromSection(Dictionary<string, DocumentSection> allSections, string sectionName)
        {
            var linkGroups = new List<LinkGroup>();
            
            if (!allSections.ContainsKey(sectionName))
                return linkGroups;

            var section = allSections[sectionName];
            
            // If we have original node, use DOM extraction for accuracy
            if (section.OriginalNode != null)
            {
                var paragraphNodes = section.OriginalNode.SelectNodes(".//p");
                if (paragraphNodes != null)
                {
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
                                Title = HtmlEntity.DeEntitize(linkNode.InnerText).Trim(), 
                                RelativePath = href
                            });
                        }

                        if (linkGroup.Links.Any())
                        {
                            linkGroups.Add(linkGroup);
                        }
                    }
                }
            }
            
            return linkGroups;
        }

        // NEW: Extract code examples from sections
        private List<CodeExample> ExtractCodeExamplesFromSections(Dictionary<string, DocumentSection> allSections)
        {
            var examples = new List<CodeExample>();
            
            // Look through all sections for code examples
            foreach (var section in allSections.Values)
            {
                if (section.OriginalNode != null)
                {
                    var codeNodes = section.OriginalNode.SelectNodes(".//pre[contains(@class, 'codeExampleCS')]");
                    if (codeNodes != null)
                    {
                        examples.AddRange(ExtractCodeExamplesFromNodes(codeNodes));
                    }
                }
            }
            
            return examples;
        }

        // HELPER METHODS
        private string ExtractRawSectionContent(HtmlNode sectionNode)
        {
            var content = new StringBuilder();
            
            var paragraphs = sectionNode.SelectNodes(".//p");
            if (paragraphs != null)
            {
                foreach (var p in paragraphs)
                {
                    var text = HtmlEntity.DeEntitize(p.InnerText).Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        content.AppendLine(text);
                    }
                }
            }

            return content.ToString().Trim();
        }

        private string DetermineNodeType(HtmlNode node)
        {
            if (node == null) return "text";
            
            if (node.SelectSingleNode(".//table") != null) return "table";
            if (node.SelectSingleNode(".//ul | .//ol") != null) return "list";
            if (node.SelectSingleNode(".//pre | .//code") != null) return "code";
            
            return "text";
        }

        private string GetSectionContent(Dictionary<string, DocumentSection> sections, string sectionName)
        {
            return sections.ContainsKey(sectionName) ? sections[sectionName].Content : string.Empty;
        }

        private bool IsStandardSection(string sectionName)
        {
            var standardSections = new[] { "Description", "Properties", "Public Methods", "Static Methods", 
                                         "Messages", "Inherited Properties", "Inherited Public Methods", 
                                         "Inherited Static Methods", "Inherited Operators" };
            return standardSections.Contains(sectionName, StringComparer.OrdinalIgnoreCase);
        }

        private string CleanHeaderText(string headerText)
        {
            if (string.IsNullOrWhiteSpace(headerText)) return "Untitled";
            
            return headerText.Trim().Replace("\n", " ").Replace("\r", "").Replace("\t", " ").Trim();
        }

        // KEEP EXISTING METHODS FOR COMPLEX DOM EXTRACTION
        private string ExtractTitleFromDOM(HtmlNode docNode)
        {
            return docNode.SelectSingleNode("//div[contains(@class, 'content')]//h1")?.InnerText.Trim() ?? string.Empty;
        }

        private string ExtractConstructTypeFromDOM(HtmlNode docNode)
        {
            var constructTypeText = docNode.SelectSingleNode("//div[contains(@class, 'content')]//h1/following-sibling::p[1]")?.InnerText.Trim() ?? string.Empty;
            var inIndex = constructTypeText.IndexOf(" in ");
            if (inIndex > 0)
            {
                constructTypeText = constructTypeText.Substring(0, inIndex);
            }
            return constructTypeText;
        }

        // Fallback methods for when section extraction fails
        private List<DocumentationLink> ExtractLinksFromContent(string content)
        {
            // Simple fallback - could be enhanced with regex parsing
            return new List<DocumentationLink>();
        }

        private List<CodeExample> ExtractCodeExamplesFromNodes(HtmlNodeCollection codeNodes)
        {
            var examples = new List<CodeExample>();
            foreach (var codeNode in codeNodes)
            {
                var preNode = codeNode.ParentNode;
                var descriptionNode = preNode.PreviousSibling;
                while (descriptionNode != null && (descriptionNode.NodeType == HtmlNodeType.Text || descriptionNode.NodeType == HtmlNodeType.Comment))
                {
                    descriptionNode = descriptionNode.PreviousSibling;
                }

                var description = string.Empty;
                if (descriptionNode != null && descriptionNode.Name == "p")
                {
                    description = HtmlEntity.DeEntitize(descriptionNode.InnerText).Trim();
                }

                examples.Add(new CodeExample
                {
                    Description = description,
                    Code = HtmlEntity.DeEntitize(codeNode.InnerText).Trim(),
                    Language = "csharp"
                });
            }
            return examples;
        }

        private List<MethodOverload> ExtractOverloadsFromDOM(HtmlNodeCollection declarationHeaders)
        {
            // Keep your existing overload extraction logic here
            // This is complex enough that DOM traversal makes sense
            var overloads = new List<MethodOverload>();
            // ... existing logic
            return overloads;
        }

        // Keep your existing complex DOM methods unchanged
        private DocumentationLink? ExtractLinkFollowingText(HtmlNode docNode, string anchorText)
        {
            var textNode = docNode.SelectSingleNode($"//div[contains(@class, 'content')]//text()[contains(., '{anchorText}')]");
            if (textNode == null) return null;

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

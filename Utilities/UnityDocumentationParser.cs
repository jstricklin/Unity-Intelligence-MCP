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
    public class UnityDocumentationParser
    {
        private class DocumentSection
        {
            public string Content { get; set; } = string.Empty;
            public HtmlNode Node { get; set; }
            public string SectionType { get; set; } = "text";
        }

        public UnityDocumentationData Parse(string filePath)
        {
            var html = File.ReadAllText(filePath);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var docNode = doc.DocumentNode;

            var sections = ExtractSections(docNode);
            
            return new UnityDocumentationData()
            {
                FilePath = filePath,
                Title = ExtractTitle(docNode),
                ConstructType = ExtractConstructType(docNode),
                Description = GetSectionContent(sections, "Description"),
                InheritsFrom = ExtractLinkFollowingText(docNode, "Inherits from:"),
                ImplementedIn = ExtractLinkFollowingText(docNode, "Implemented in:"),
                Properties = ExtractLinksFromSection(sections, "Properties"),
                PublicMethods = ExtractLinksFromSection(sections, "Public Methods"),
                StaticMethods = ExtractLinksFromSection(sections, "Static Methods"),
                Messages = ExtractLinksFromSection(sections, "Messages"),
                InheritedProperties = ExtractLinksFromSection(sections, "Inherited Properties"),
                InheritedPublicMethods = ExtractLinksFromSection(sections, "Inherited Public Methods"),
                InheritedStaticMethods = ExtractLinksFromSection(sections, "Inherited Static Methods"),
                InheritedOperators = ExtractLinksFromSection(sections, "Inherited Operators"),
                ContentLinkGroups = ExtractContentLinkGroups(sections),
                Overloads = ExtractOverloads(docNode),
                Examples = ExtractCodeExamples(sections),
                AdditionalSections = GetAdditionalSections(sections)
            };
        }

        private Dictionary<string, DocumentSection> ExtractSections(HtmlNode docNode)
        {
            var sections = new Dictionary<string, DocumentSection>(StringComparer.OrdinalIgnoreCase);
            var sectionCursor = FindFirstSectionNode(docNode);

            if (sectionCursor == null) return sections;

            var currentSection = new StringBuilder();
            string currentKey = "Document";
            HtmlNode currentNode = sectionCursor;
            var orphanedContent = new StringBuilder();

            while (currentNode != null)
            {
                if (IsSectionHeader(currentNode))
                {
                    SaveCurrentSection();
                    currentKey = CleanHeaderText(currentNode.InnerText);
                    currentSection.Clear();
                }
                
                currentSection.AppendLine(ExtractNodeContent(currentNode));
                currentNode = currentNode.NextSibling;
            }
            SaveCurrentSection();
            HandleOrphanedContent();
            return sections;

            // Local helper functions
            void SaveCurrentSection()
            {
                if (currentSection.Length > 0)
                {
                    sections[currentKey] = new DocumentSection
                    {
                        Content = currentSection.ToString().Trim(),
                        Node = currentNode,
                        SectionType = DetermineNodeType(currentNode)
                    };
                }
            }

            void HandleOrphanedContent()
            {
                if (orphanedContent.Length > 0)
                {
                    sections["Document"] = new DocumentSection
                    {
                        Content = orphanedContent.ToString().Trim(),
                        SectionType = "text"
                    };
                }
            }
        }
        
        private HtmlNode FindFirstSectionNode(HtmlNode docNode)
        {
            string[] sectionSelectors = {
                "//div[@class='subsection']",
                "//div[contains(@class, 'section')]",
                "//div[contains(@class, 'content')]//div[h3 or h2]"
            };
            
            foreach (var selector in sectionSelectors)
            {
                var node = docNode.SelectSingleNode(selector);
                if (node != null) return node;
            }
            return docNode.SelectSingleNode("//body") ?? docNode;
        }

        private bool IsSectionHeader(HtmlNode node)
        {
            return node?.Name?.StartsWith("h", StringComparison.OrdinalIgnoreCase) == true &&
                   (node.Name.Length == 2 || node.Name.Length == 3) &&
                   char.IsDigit(node.Name[1]);
        }

        private string CleanHeaderText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "Untitled";
            return text.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim();
        }

        private string DetermineNodeType(HtmlNode node)
        {
            if (node == null) return "text";
            if (node.SelectSingleNode(".//table") != null) return "table";
            if (node.SelectSingleNode(".//ul|.//ol") != null) return "list";
            if (node.SelectSingleNode(".//pre|.//code") != null) return "code";
            return "text";
        }

        private string ExtractNodeContent(HtmlNode node)
        {
            return node?.InnerText?.Trim() ?? string.Empty;
        }

        private string GetSectionContent(Dictionary<string, DocumentSection> sections, string name)
        {
            return sections.TryGetValue(name, out var section) 
                ? section.Content 
                : string.Empty;
        }

        private List<DocumentationLink> ExtractLinksFromSection(
            Dictionary<string, DocumentSection> sections, string name)
        {
            if (!sections.TryGetValue(name, out var section) || section.Node == null)
                return new List<DocumentationLink>();

            return ExtractLinksFromTable(section.Node);
        }

        private List<DocumentationLink> ExtractLinksFromTable(HtmlNode sectionNode)
        {
            var table = sectionNode.SelectSingleNode(".//table") ??
                       sectionNode.SelectSingleNode("following-sibling::table");
            
            if (table == null) return new List<DocumentationLink>();

            var links = new List<DocumentationLink>();
            foreach (var row in table.SelectNodes(".//tr") ?? Enumerable.Empty<HtmlNode>())
            {
                var linkNode = row.SelectSingleNode(".//a");
                var descNode = row.SelectSingleNode("td[2]");
                
                if (linkNode != null)
                {
                    links.Add(new DocumentationLink
                    {
                        Title = HtmlEntity.DeEntitize(linkNode.InnerText).Trim(),
                        RelativePath = linkNode.GetAttributeValue("href", ""),
                        Description = descNode != null ? HtmlEntity.DeEntitize(descNode.InnerText).Trim() : ""
                    });
                }
            }
            return links;
        }

        private List<LinkGroup> ExtractContentLinkGroups(Dictionary<string, DocumentSection> sections)
        {
            var linkGroups = new List<LinkGroup>();
            if (!sections.TryGetValue("Description", out var section) || section.Node == null)
                return linkGroups;

            foreach (var paragraph in section.Node.SelectNodes(".//p") ?? Enumerable.Empty<HtmlNode>())
            {
                var links = paragraph.SelectNodes(".//a");
                if (links == null || links.Count == 0) continue;

                var group = new LinkGroup { Context = HtmlEntity.DeEntitize(paragraph.InnerText).Trim() };
                foreach (var link in links)
                {
                    var href = link.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(href) && !href.StartsWith("#"))
                    {
                        group.Links.Add(new DocumentationLink
                        {
                            Title = HtmlEntity.DeEntitize(link.InnerText).Trim(),
                            RelativePath = href
                        });
                    }
                }
                
                if (group.Links.Any())
                    linkGroups.Add(group);
            }
            return linkGroups;
        }

        private Dictionary<string, string> GetAdditionalSections(Dictionary<string, DocumentSection> sections)
        {
            var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Description", "Properties", "Public Methods", "Static Methods", 
                "Messages", "Inherited Properties", "Inherited Public Methods",
                "Inherited Static Methods", "Inherited Operators"
            };
            
            return sections
                .Where(kvp => !exclude.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Content);
        }

        private string ExtractTitle(HtmlNode docNode)
        {
            return docNode.SelectSingleNode("//div[contains(@class, 'content')]//h1")?
                .InnerText.Trim() ?? string.Empty;
        }

        private string ExtractConstructType(HtmlNode docNode)
        {
            var text = docNode.SelectSingleNode("//div[contains(@class, 'content')]//h1/following-sibling::p[1]")?
                .InnerText.Trim() ?? string.Empty;
            
            var idx = text.IndexOf(" in ");
            return idx > 0 ? text.Substring(0, idx) : text;
        }

        private DocumentationLink? ExtractLinkFollowingText(HtmlNode docNode, string text)
        {
            var textNode = docNode.SelectSingleNode($"//text()[contains(., '{text}')]");
            if (textNode == null) return null;

            var node = textNode.NextSibling;
            while (node != null && node.NodeType != HtmlNodeType.Element)
                node = node.NextSibling;

            return node?.Name == "a" ? new DocumentationLink
            {
                Title = HtmlEntity.DeEntitize(node.InnerText).Trim(),
                RelativePath = node.GetAttributeValue("href", "")
            } : null;
        }

        private List<MethodOverload> ExtractOverloads(HtmlNode docNode)
        {
            var overloads = new List<MethodOverload>();
            var declarations = docNode.SelectNodes("//div[contains(@class, 'content')]//h2[text()='Declaration']")
                                 ?? Enumerable.Empty<HtmlNode>();
            
            foreach (var header in declarations)
            {
                var methodNode = header.ParentNode;
                var signatureNode = header.SelectSingleNode("following-sibling::div[contains(@class, 'signature-CS')][1]");
                var signature = HtmlEntity.DeEntitize(signatureNode?.InnerText ?? "").Trim();

                overloads.Add(new MethodOverload
                {
                    Declaration = signature,
                    Parameters = ExtractParameters(methodNode),
                    Description = ExtractOverloadDescription(methodNode),
                    Examples = ExtractCodeExamples(methodNode)
                });
            }
            return overloads;
        }

        private List<CodeExample> ExtractCodeExamples(Dictionary<string, DocumentSection> sections)
        {
            var examples = new List<CodeExample>();
            foreach (var section in sections.Values)
            {
                if (section.Node == null) continue;
                
                var codeNodes = section.Node.SelectNodes(".//pre[contains(@class, 'codeExampleCS')]")
                                      ?? Enumerable.Empty<HtmlNode>();
                
                foreach (var code in codeNodes)
                {
                    var preNode = code.ParentNode;
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
                        Code = HtmlEntity.DeEntitize(code.InnerText).Trim(),
                        Language = "csharp"
                    });
                }
            }
            return examples;
        }
        
        private string ExtractOverloadDescription(HtmlNode node)
        {
            var descriptionHeader = node.SelectSingleNode(".//h3[text()='Description']");
            if (descriptionHeader == null) return string.Empty;

            var sb = new StringBuilder();
            var currentNode = descriptionHeader.NextSibling;
            while (currentNode != null && currentNode.Name != "h3" && currentNode.Name != "h2")
            {
                if (currentNode.NodeType == HtmlNodeType.Element && (currentNode.Name == "p" || (currentNode.Name == "div" && !currentNode.HasClass("subsection"))))
                {
                    sb.AppendLine(HtmlEntity.DeEntitize(currentNode.InnerText).Trim());
                }
                currentNode = currentNode.NextSibling;
            }
            return sb.ToString().Trim();
        }

        private List<ParameterInfo> ExtractParameters(HtmlNode node)
        {
            var parameters = new List<ParameterInfo>();
            var headerNode = node.SelectSingleNode(".//h3[text()='Parameters']");
            if (headerNode == null) return parameters;

            var tableNode = headerNode.SelectSingleNode("following-sibling::table[1]");
            if (tableNode == null) return parameters;

            var rows = tableNode.SelectNodes(".//tr");
            if (rows == null) return parameters;

            foreach (var row in rows)
            {
                var nameNode = row.SelectSingleNode("./td[1]");
                var descriptionNode = row.SelectSingleNode("./td[2]");

                if (nameNode != null && descriptionNode != null)
                {
                    parameters.Add(new ParameterInfo
                    {
                        Name = HtmlEntity.DeEntitize(nameNode.InnerText).Trim(),
                        Description = HtmlEntity.DeEntitize(descriptionNode.InnerText).Trim()
                    });
                }
            }
            return parameters;
        }

        private List<CodeExample> ExtractCodeExamples(HtmlNode node)
        {
            var examples = new List<CodeExample>();
            var codeNodes = node.SelectNodes(".//pre[contains(@class, 'codeExampleCS')]");

            if (codeNodes == null) return examples;

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

        public void DebugDumpSections(string filePath)
        {
            var html = File.ReadAllText(filePath);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var docNode = doc.DocumentNode;
            
            var sections = ExtractSections(docNode);
            Console.WriteLine($"Sections for: {Path.GetFileName(filePath)}");
            foreach (var (key, section) in sections)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n## {key} ({section.SectionType}) ##");
                Console.ResetColor();
                Console.WriteLine(section.Content.Length > 200 
                    ? section.Content.Substring(0, 200) + "..." 
                    : section.Content);
            }
        }
    }
}

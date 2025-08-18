using HtmlAgilityPack;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                ImplementedInterfaces = ExtractLinksFollowingText(docNode, "Implements interfaces:"),
                Properties = ExtractLinksFromSection(sections, "Properties"),
                PublicMethods = ExtractLinksFromSection(sections, "Public Methods"),
                StaticMethods = ExtractLinksFromSection(sections, "Static Methods"),
                Messages = ExtractLinksFromSection(sections, "Messages"),
                InheritedProperties = ExtractLinksFromSection(sections, "Inherited Properties"),
                InheritedPublicMethods = ExtractLinksFromSection(sections, "Inherited Public Methods"),
                InheritedStaticMethods = ExtractLinksFromSection(sections, "Inherited Static Methods"),
                InheritedOperators = ExtractLinksFromSection(sections, "Inherited Operators"),
                ContentLinkGroups = ExtractContentLinkGroups(sections),
                // FIXME: resolve overload parsing
                Overloads = ExtractOverloads(docNode),
                Examples = ExtractCodeExamples(sections),
                AdditionalSections = GetAdditionalSections(sections)
            };
        }

        private Dictionary<string, DocumentSection> ExtractSections(HtmlNode docNode)
        {
            var sections = new Dictionary<string, DocumentSection>(StringComparer.OrdinalIgnoreCase);
            var contentNode = FindFirstSectionNode(docNode);
            if (contentNode == null) return sections;

            var currentSectionNodes = new List<HtmlNode>();
            string currentKey = "Description"; 

            void SaveSection()
            {
                if (!currentSectionNodes.Any()) return;

                var container = HtmlNode.CreateNode("<div></div>");
                currentSectionNodes.ForEach(n => container.AppendChild(n.Clone()));
                var content = container.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(content)) return;

                if (sections.TryGetValue(currentKey, out var existingSection))
                {
                    existingSection.Content += "\n\n" + content;
                    currentSectionNodes.ForEach(n => existingSection.Node.AppendChild(n.Clone()));
                }
                else
                {
                    sections[currentKey] = new DocumentSection
                    {
                        Content = content,
                        Node = container,
                        SectionType = DetermineNodeType(container)
                    };
                }
            }

            foreach (var currentNode in contentNode.ChildNodes)
            {
                if (currentNode.NodeType is HtmlNodeType.Comment ||
                    (currentNode.NodeType is HtmlNodeType.Text && string.IsNullOrWhiteSpace(currentNode.InnerText)))
                {
                    continue;
                }
                
                if (currentNode.NodeType == HtmlNodeType.Element && currentNode.HasClass("mb20"))
                {
                    continue;
                }

                if (IsSectionHeader(currentNode))
                {
                    SaveSection();
                    currentSectionNodes.Clear();
                    var headerNode = currentNode.SelectSingleNode("./h3|./h2");
                    currentKey = CleanHeaderText(headerNode!.InnerText);
                }
                
                currentSectionNodes.Add(currentNode);
            }
            
            SaveSection();
            return sections;
        }
        
        private HtmlNode FindFirstSectionNode(HtmlNode docNode)
        {
            // This XPath is more reliable for finding the main content area across different doc pages.
            // We're looking for the <div class="section"> inside the main <div class="content">.
            return docNode.SelectSingleNode("//div[@class='content']/div[@class='section']");
        }

        private bool IsSectionHeader(HtmlNode node)
        {
            // A section header is defined as a div with class 'subsection' that contains a direct child h3 or h2 element.
            // This is the most consistent structural pattern across the example documents.
            if (node?.Name == "div" && node.HasClass("subsection"))
            {
                return node.SelectSingleNode("./h3|./h2") != null;
            }
            return false;
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
            var textNode = docNode.SelectSingleNode("//div[contains(@class, 'content')]//h1/following-sibling::p[1]");
            if (textNode != null)
            {
                var text = textNode.InnerText.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    // Using a dictionary is a great way to map to a canonical form.
                    // This makes parsing more robust than simple string manipulation.
                    var knownTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "class", "Class" },
                        { "struct", "Struct" },
                        { "enum", "Enum" },
                        { "enumeration", "Enumeration" },
                        { "interface", "Interface" }
                    };

                    var words = text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    var firstWord = words.FirstOrDefault();

                    if (firstWord != null && knownTypes.TryGetValue(firstWord, out var canonicalType))
                    {
                        return canonicalType;
                    }

                    // Fallback for constructs not in our map (e.g., "Attribute").
                    var idx = text.IndexOf(" in ");
                    return idx > 0 ? text.Substring(0, idx).Trim() : text;
                }
            }

            // Fallback for pages like properties or fields where the type is in the signature.
            var signatureNode = docNode.SelectSingleNode("//div[contains(@class, 'signature-CS')]");
            if (signatureNode != null)
            {
                // On method pages, the signature block contains an <h2>Declaration</h2> tag.
                // This reliably distinguishes them from property/field pages.
                if (signatureNode.SelectSingleNode(".//h2[text()='Declaration']") != null)
                {
                    return "Method";
                }

                // Member pages for properties or fields don't have the <h2> header.
                // Their signatures typically end with a semicolon or contain curly braces.
                var signatureText = HtmlEntity.DeEntitize(signatureNode.InnerText).Trim();
                if (signatureText.EndsWith(";") || signatureText.Contains("{"))
                {
                    return "Property";
                }
            }

            return string.Empty;
        }
        private List<DocumentationLink> ExtractLinksFollowingText(HtmlNode docNode, string text)
        {
            var links = new List<DocumentationLink>();
            var textNode = docNode.SelectSingleNode($"//text()[contains(., '{text}')]");
            if (textNode == null) return links;

            for (var node = textNode.NextSibling; node != null; node = node.NextSibling)
            {
                // If we hit another type of element that isn't a link, we've left the list.
                if (node.NodeType == HtmlNodeType.Element && node.Name != "a")
                {
                    break;
                }

                // We only care about <a> tags.
                if (node.Name == "a")
                {
                    links.Add(new DocumentationLink
                    {
                        Title = HtmlEntity.DeEntitize(node.InnerText).Trim(),
                        RelativePath = node.GetAttributeValue("href", "")
                    });
                }
            }
            return links;
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
                    while (descriptionNode != null && descriptionNode.NodeType != HtmlNodeType.Element)
                    {
                        descriptionNode = descriptionNode.PreviousSibling;
                    }

                    var description = string.Empty;
                    if (descriptionNode != null)
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
            foreach (var (key, section) in sections)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.ResetColor();
                Console.Error.WriteLine(section.Content.Length > 200 
                    ? section.Content.Substring(0, 200) + "..." 
                    : section.Content);
            }
        }
    }
}

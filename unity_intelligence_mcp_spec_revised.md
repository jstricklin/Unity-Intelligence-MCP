# Unity Code Intelligence MCP Server Architecture (C# + Microsoft.Unity.Analyzers)

## Overview
An extensible MCP server built in C# designed for enhanced Unity project understanding, leveraging Roslyn with Microsoft.Unity.Analyzers for deep semantic analysis, focusing on Unity-specific static code analysis, runtime context extraction, and semantic code intelligence.

## Core Architecture

### 1. Foundation Layer
```
MCP Server Core (.NET 8)
├── Protocol Handler (JSON-RPC over stdio/Named Pipes)
├── Tool Registry (Dynamic tool registration with reflection)
├── Resource Provider (Extensible resource system with async streams)
├── Plugin System (Assembly-based hot-loadable modules)
├── Configuration Manager (Project-specific settings with validation)
├── Roslyn Integration (Compilation services and semantic analysis)
└── Unity Analyzers Integration (Microsoft.Unity.Analyzers package)
```

### 2. Analysis Engine Architecture
```
Analysis Pipeline
├── Static Analyzers/
│   ├── RoslynCSharpAnalyzer (Full semantic analysis with Unity context)
│   ├── UnitySpecificAnalyzer (Microsoft.Unity.Analyzers integration)
│   ├── UnityAssetAnalyzer (Scene/Prefab YAML parser)
│   ├── AssemblyDependencyAnalyzer (Reflection-based)
│   └── PatternRecognitionAnalyzer (Unity patterns + ML)
├── Context Extractors/
│   ├── ComponentRelationshipExtractor
│   ├── UnityArchitecturePatternExtractor  
│   ├── UsageFrequencyExtractor
│   ├── UnityMessageExtractor (Start, Update, etc.)
│   └── SemanticContextExtractor
└── Intelligence Services/
    ├── SemanticSearchService (Vector embeddings)
    ├── UnityCodeSuggestionService (Unity-aware suggestions)
    ├── UnityDocumentationGenerator (Unity API integration)
    └── ArchitectureInsightService
```

## Component Specifications

### 1. Unity Analyzers Integration
```csharp
using Microsoft.Unity.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;

public class UnityAnalyzersService
{
    private readonly IReadOnlyList<DiagnosticAnalyzer> _unityAnalyzers;
    
    public UnityAnalyzersService()
    {
        // Load Microsoft.Unity.Analyzers analyzers
        _unityAnalyzers = new DiagnosticAnalyzer[]
        {
            new EmptyUnityMessageAnalyzer(),
            new InefficientMultidimensionalArrayUsageAnalyzer(), 
            new InefficientCameraMainUsageAnalyzer(),
            new UnityObjectNullComparisonAnalyzer(),
            new TypeInferenceIssueAnalyzer(),
            new UnityObjectNullPropagationAnalyzer(),
            new CameraNullComparisonAnalyzer(),
            new InefficientPropertyAccessAnalyzer(),
            new UnityMessageAnalyzer()
        };
    }
    
    public async Task<IEnumerable<UnityDiagnostic>> AnalyzeWithUnityRulesAsync(
        CSharpCompilation compilation, CancellationToken cancellationToken = default)
    {
        var diagnostics = new List<UnityDiagnostic>();
        
        foreach (var analyzer in _unityAnalyzers)
        {
            var compilationWithAnalyzers = compilation.WithAnalyzers(
                ImmutableArray.Create(analyzer), 
                cancellationToken: cancellationToken);
                
            var analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);
            
            diagnostics.AddRange(analyzerDiagnostics.Select(d => new UnityDiagnostic(
                d.Id,
                d.GetMessage(),
                d.Severity,
                d.Location.GetMappedLineSpan(),
                GetUnitySpecificContext(d)
            )));
        }
        
        return diagnostics;
    }
    
    private UnityDiagnosticContext GetUnitySpecificContext(Diagnostic diagnostic)
    {
        return diagnostic.Id switch
        {
            "UNT0001" => new UnityDiagnosticContext("EmptyUnityMessage", "Performance"),
            "UNT0002" => new UnityDiagnosticContext("InefficientTag", "Performance"), 
            "UNT0006" => new UnityDiagnosticContext("InefficientCameraMain", "Performance"),
            "UNT0014" => new UnityDiagnosticContext("UnityObjectNullComparison", "Best Practice"),
            _ => new UnityDiagnosticContext("General", "Code Quality")
        };
    }
}
```

### 2. Enhanced Plugin System Interface
```csharp
public interface IAnalysisPlugin
{
    string Name { get; }
    Version Version { get; }
    bool RequiresUnityAnalyzers { get; }
    Task InitializeAsync(string projectPath, PluginConfiguration config, 
                        UnityAnalyzersService? unityAnalyzers = null, 
                        CancellationToken cancellationToken = default);
    IEnumerable<ToolDefinition> GetTools();
    IEnumerable<ResourceDefinition> GetResources();
    Task<AnalysisResult> AnalyzeAsync(AnalysisContext context, CancellationToken cancellationToken = default);
    Task CleanupAsync();
}

public record ToolDefinition(
    string Name,
    string Description,
    JsonSchema InputSchema,
    Func<JsonElement, CancellationToken, Task<ToolResult>> Handler,
    bool IsUnitySpecific = false
);

public record ResourceDefinition(
    string Uri,
    string Name,
    string MimeType,
    Func<string, CancellationToken, Task<ResourceContent>> Provider,
    UnityResourceType? UnityType = null
);

public enum UnityResourceType
{
    Component,
    ScriptableObject,
    Scene,
    Prefab,
    Asset
}
```

### 3. Unity-Enhanced Analysis Types
```csharp
public record ProjectContext(
    string RootPath,
    UnityVersion UnityVersion,
    IReadOnlyList<AssemblyInfo> Assemblies,
    IReadOnlyList<SceneInfo> Scenes,
    IReadOnlyList<PrefabInfo> Prefabs,
    IReadOnlyList<ScriptInfo> Scripts,
    IReadOnlyList<AssetInfo> Assets,
    DependencyGraph Dependencies,
    IReadOnlyList<UnityDiagnostic> UnityDiagnostics
);

public record ScriptInfo(
    string Path,
    string ClassName,
    string? Namespace,
    string BaseType,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<FieldInfo> PublicFields,
    IReadOnlyList<MethodInfo> PublicMethods,
    IReadOnlyList<UsagePattern> UsagePatterns,
    IReadOnlyList<string> SemanticTags,
    UnityScriptAnalysis UnityAnalysis
)
{
    public ISymbol? Symbol { get; init; }
    public SyntaxTree? SyntaxTree { get; init; }
    public SemanticModel? SemanticModel { get; init; }
}

public record UnityScriptAnalysis(
    bool IsMonoBehaviour,
    bool IsScriptableObject,
    bool IsEditor,
    IReadOnlyList<UnityMessageInfo> UnityMessages,
    IReadOnlyList<SerializedFieldInfo> SerializedFields,
    IReadOnlyList<string> RequiredComponents,
    IReadOnlyList<UnityDiagnostic> SpecificDiagnostics,
    UnityPerformanceMetrics PerformanceMetrics
);

public record UnityMessageInfo(
    string MessageName,
    MethodInfo Method,
    UnityMessageType Type,
    bool IsEmpty,
    bool HasPerformanceImplications
);

public enum UnityMessageType
{
    Awake, Start, Update, FixedUpdate, LateUpdate,
    OnEnable, OnDisable, OnDestroy,
    OnCollision, OnTrigger,
    OnGUI, OnRender
}

public record UnityDiagnostic(
    string Id,
    string Message,
    DiagnosticSeverity Severity,
    FileLinePositionSpan Location,
    UnityDiagnosticContext Context
);

public record UnityDiagnosticContext(
    string Category,
    string Type,
    Dictionary<string, object>? AdditionalData = null
);
```

### 4. Enhanced Roslyn Integration with Unity Analyzers
```csharp
public class UnityRoslynAnalysisService
{
    private readonly UnityAnalyzersService _unityAnalyzers;
    private readonly CSharpCompilation _compilation;
    private readonly Dictionary<string, SemanticModel> _semanticModels;
    
    public UnityRoslynAnalysisService(UnityAnalyzersService unityAnalyzers)
    {
        _unityAnalyzers = unityAnalyzers;
    }
    
    public async Task<CSharpCompilation> CreateUnityCompilationAsync(string projectPath)
    {
        var syntaxTrees = await LoadSyntaxTreesAsync(projectPath);
        var references = GetUnityReferences();
        
        var compilation = CSharpCompilation.Create(
            "UnityProject",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        
        // Apply Unity-specific preprocessor symbols
        return compilation.WithOptions(compilation.Options.WithPreprocessorSymbols(
            "UNITY_2022_3_OR_NEWER", "UNITY_EDITOR", "UNITY_STANDALONE"));
    }
    
    public async Task<UnityComponentAnalysis> AnalyzeMonoBehaviourAsync(
        INamedTypeSymbol typeSymbol, CancellationToken cancellationToken = default)
    {
        var baseAnalysis = new ComponentAnalysis(
            BaseClasses: GetInheritanceChain(typeSymbol),
            SerializedFields: GetSerializableFields(typeSymbol),
            UnityMessages: FindUnityMessageMethods(typeSymbol),
            RequiredComponents: GetRequiredComponentAttributes(typeSymbol),
            Dependencies: AnalyzeTypeDependencies(typeSymbol)
        );
        
        // Get Unity-specific diagnostics for this type
        var unityDiagnostics = await _unityAnalyzers.AnalyzeWithUnityRulesAsync(
            _compilation, cancellationToken);
            
        var typeDiagnostics = unityDiagnostics
            .Where(d => IsRelevantToType(d, typeSymbol))
            .ToList();
            
        return new UnityComponentAnalysis(
            baseAnalysis,
            typeDiagnostics,
            AnalyzeUnityMessages(typeSymbol),
            CalculatePerformanceMetrics(typeSymbol, typeDiagnostics)
        );
    }
    
    private IReadOnlyList<UnityMessageInfo> AnalyzeUnityMessages(INamedTypeSymbol typeSymbol)
    {
        var unityMessages = new List<UnityMessageInfo>();
        var unityMessageNames = new HashSet<string>
        {
            "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
            "OnEnable", "OnDisable", "OnDestroy"
        };
        
        foreach (var member in typeSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (unityMessageNames.Contains(member.Name))
            {
                var messageType = Enum.Parse<UnityMessageType>(member.Name);
                var isEmpty = IsEmptyMethod(member);
                var hasPerformanceImplications = HasPerformanceImplications(member, messageType);
                
                unityMessages.Add(new UnityMessageInfo(
                    member.Name,
                    ConvertToMethodInfo(member),
                    messageType,
                    isEmpty,
                    hasPerformanceImplications
                ));
            }
        }
        
        return unityMessages;
    }
    
    private bool HasPerformanceImplications(IMethodSymbol method, UnityMessageType messageType)
    {
        // Check for common performance issues in Unity messages
        return messageType switch
        {
            UnityMessageType.Update => ContainsExpensiveOperations(method),
            UnityMessageType.FixedUpdate => ContainsExpensiveOperations(method),
            UnityMessageType.LateUpdate => ContainsExpensiveOperations(method),
            _ => false
        };
    }
}
```

## Implementation Phases

### Phase 1: Foundation (Core MCP + Unity-Aware Roslyn Analysis)
**Priority: High | Effort: Medium**

1. **Package Dependencies**
   ```xml
   <PackageReference Include="Microsoft.Unity.Analyzers" Version="1.17.0" />
   <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
   <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
   <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
   <PackageReference Include="System.Text.Json" Version="8.0.0" />
   ```

2. **MCP Server Core with Unity Integration**
   ```csharp
   // Program.cs
   var builder = Host.CreateApplicationBuilder(args);
   builder.Services.AddSingleton<IMcpServer, McpServer>();
   builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
   builder.Services.AddSingleton<IResourceProvider, ResourceProvider>();
   builder.Services.AddSingleton<IPluginManager, PluginManager>();
   builder.Services.AddSingleton<UnityAnalyzersService>();
   builder.Services.AddSingleton<UnityRoslynAnalysisService>();
   builder.Services.AddSingleton<UnityProjectAnalyzer>();
   
   var host = builder.Build();
   await host.RunAsync();
   ```

3. **Unity-Aware Project Analysis**
   ```csharp
   public class UnityProjectAnalyzer
   {
       private readonly UnityRoslynAnalysisService _roslynService;
       private readonly UnityAnalyzersService _unityAnalyzers;
       
       public async Task<ProjectContext> AnalyzeProjectAsync(string projectPath)
       {
           var compilation = await _roslynService.CreateUnityCompilationAsync(projectPath);
           var scripts = await AnalyzeScriptsAsync(compilation);
           var scenes = await AnalyzeScenesAsync(projectPath);
           var dependencies = BuildDependencyGraph(scripts, scenes);
           
           // Get Unity-specific diagnostics for the entire project
           var unityDiagnostics = await _unityAnalyzers.AnalyzeWithUnityRulesAsync(compilation);
           
           return new ProjectContext(projectPath, unityVersion, assemblies, 
                                   scenes, prefabs, scripts, assets, dependencies, unityDiagnostics);
       }
   }
   ```

4. **Unity-Specific Tool Set**
   ```csharp
   [Tool("analyze_unity_project")]
   public async Task<ToolResult> AnalyzeUnityProject(
       [FromJson] UnityProjectAnalysisRequest request)
   {
       var context = await _analyzer.AnalyzeProjectAsync(request.ProjectPath);
       return ToolResult.Success(context);
   }
   
   [Tool("find_unity_performance_issues")]
   public async Task<ToolResult> FindUnityPerformanceIssues(
       [FromJson] PerformanceAnalysisRequest request)
   {
       var issues = await _performanceAnalyzer
           .FindUnityPerformanceIssuesAsync(request.ProjectPath);
       return ToolResult.Success(issues);
   }
   
   [Tool("analyze_unity_messages")]
   public async Task<ToolResult> AnalyzeUnityMessages(
       [FromJson] UnityMessageRequest request)
   {
       var analysis = await _unityMessageAnalyzer
           .AnalyzeMessagesAsync(request.ScriptPath);
       return ToolResult.Success(analysis);
   }
   ```

5. **Unity-Specific Resources**
   ```csharp
   [Resource("unity://project-diagnostics")]
   public async Task<ResourceContent> GetUnityDiagnostics(string uri)
   {
       var diagnostics = await _unityAnalyzer.GetProjectDiagnosticsAsync();
       return new ResourceContent(JsonSerializer.Serialize(diagnostics), "application/json");
   }
   
   [Resource("unity://performance-report")]
   public async Task<ResourceContent> GetPerformanceReport(string uri)
   {
       var report = await _performanceAnalyzer.GenerateReportAsync();
       return new ResourceContent(JsonSerializer.Serialize(report), "application/json");
   }
   ```

### Phase 2: Enhanced Unity Intelligence (Unity Pattern Recognition)
**Priority: High | Effort: High**

1. **Unity-Specific Pattern Recognition**
   ```csharp
   public class UnityPatternAnalyzer : IAnalysisPlugin
   {
       public bool RequiresUnityAnalyzers => true;
       
       private readonly IReadOnlyList<IUnityPatternDetector> _detectors = new[]
       {
           new SingletonPatternDetector(),
           new ObjectPoolPatternDetector(),
           new ComponentPatternDetector(),
           new UnityEventPatternDetector(),
           new CoroutinePatternDetector(),
           new ScriptableObjectPatternDetector()
       };
       
       public async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context, 
                                                     CancellationToken cancellationToken = default)
       {
           var patterns = new List<DetectedUnityPattern>();
           
           foreach (var script in context.Scripts.Where(s => s.UnityAnalysis.IsMonoBehaviour))
           {
               foreach (var detector in _detectors)
               {
                   if (await detector.DetectAsync(script, cancellationToken))
                   {
                       patterns.Add(new DetectedUnityPattern(
                           detector.PatternName, 
                           script.Path, 
                           detector.Confidence,
                           detector.UnitySpecificMetrics
                       ));
                   }
               }
           }
           
           return new AnalysisResult { DetectedUnityPatterns = patterns };
       }
   }
   ```

2. **Unity Component Relationship Analysis**
   ```csharp
   public class UnityComponentRelationshipAnalyzer
   {
       public async Task<UnityComponentGraph> AnalyzeRelationshipsAsync(
           IEnumerable<ScriptInfo> scripts)
       {
           var graph = new UnityComponentGraph();
           
           foreach (var script in scripts.Where(s => s.UnityAnalysis.IsMonoBehaviour))
           {
               var relationships = await ExtractUnityRelationshipsAsync(script);
               graph.AddComponent(script.ClassName, relationships, script.UnityAnalysis);
           }
           
           return graph;
       }
       
       private async Task<IEnumerable<UnityComponentRelationship>> ExtractUnityRelationshipsAsync(
           ScriptInfo script)
       {
           var walker = new UnityComponentUsageWalker(script.SemanticModel);
           walker.Visit(script.SyntaxTree.GetRoot());
           
           return walker.Relationships.Select(r => new UnityComponentRelationship(
               r.SourceComponent,
               r.TargetComponent,
               r.RelationshipType,
               GetUnityRelationshipContext(r)
           ));
       }
   }
   ```

3. **Enhanced Unity Tools**
   ```csharp
   [Tool("find_unity_patterns")]
   public async Task<ToolResult> FindUnityPatterns(
       [FromJson] UnityPatternSearchRequest request)
   {
       var patterns = await _unityPatternAnalyzer.FindPatternsAsync(
           request.ProjectPath, request.PatternTypes);
       return ToolResult.Success(patterns);
   }
   
   [Tool("analyze_component_relationships")]
   public async Task<ToolResult> AnalyzeComponentRelationships(
       [FromJson] ComponentRelationshipRequest request)
   {
       var analysis = await _componentAnalyzer.AnalyzeAsync(request.ProjectPath);
       return ToolResult.Success(analysis);
   }
   ```

### Phase 3: Unity Semantic Intelligence (AI-Enhanced Understanding)
**Priority: Medium | Effort: High**

1. **Unity-Aware Semantic Search**
   ```csharp
   public class UnitySemanticSearchService
   {
       private readonly IEmbeddingService _embeddingService;
       private readonly IVectorDatabase _vectorDb;
       
       public async Task IndexUnityProjectAsync(ProjectContext context)
       {
           var tasks = context.Scripts.Select(async script =>
           {
               var searchableText = GenerateUnitySearchableText(script);
               var embedding = await _embeddingService.GenerateEmbeddingAsync(searchableText);
               var metadata = CreateUnityMetadata(script);
               
               await _vectorDb.StoreAsync(script.Path, embedding, metadata);
           });
           
           await Task.WhenAll(tasks);
       }
       
       private string GenerateUnitySearchableText(ScriptInfo script)
       {
           var text = new StringBuilder(script.GetSearchableText());
           
           // Add Unity-specific context
           if (script.UnityAnalysis.IsMonoBehaviour)
           {
               text.AppendLine($"MonoBehaviour Component");
               foreach (var message in script.UnityAnalysis.UnityMessages)
               {
                   text.AppendLine($"Unity Message: {message.MessageName}");
               }
           }
           
           if (script.UnityAnalysis.IsScriptableObject)
           {
               text.AppendLine($"ScriptableObject Asset");
           }
           
           foreach (var field in script.UnityAnalysis.SerializedFields)
           {
               text.AppendLine($"Serialized Field: {field.Name} Type: {field.Type}");
           }
           
           return text.ToString();
       }
       
       public async Task<IEnumerable<UnitySearchResult>> SearchByUnityIntentAsync(
           string query, UnitySearchFilter? filter = null, int maxResults = 10)
       {
           var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
           var results = await _vectorDb.SearchAsync(queryEmbedding, maxResults, filter);
           
           return results.Select(r => new UnitySearchResult(
               r.Path, r.Similarity, r.Metadata, 
               ExtractUnityContext(r.Metadata)
           ));
       }
   }
   ```

2. **Unity Documentation Generation**
   ```csharp
   public class UnityDocumentationGenerator
   {
       public async Task<UnityComponentDocumentation> GenerateComponentDocsAsync(
           ScriptInfo script)
       {
           var xmlDocs = ExtractXmlDocumentation(script.Symbol);
           var usageAnalysis = await AnalyzeUnityUsageAsync(script);
           var codeExamples = await GenerateUnityExamplesAsync(script);
           
           return new UnityComponentDocumentation(
               Name: script.ClassName,
               Type: GetUnityComponentType(script.UnityAnalysis),
               Summary: xmlDocs.Summary ?? await GenerateUnityAwareSummaryAsync(script),
               Usage: usageAnalysis,
               Examples: codeExamples,
               RelatedComponents: await FindRelatedUnityComponentsAsync(script),
               UnityMessages: script.UnityAnalysis.UnityMessages,
               SerializedFields: script.UnityAnalysis.SerializedFields,
               PerformanceConsiderations: GeneratePerformanceConsiderations(script.UnityAnalysis)
           );
       }
   }
   ```

3. **Advanced Unity Tools**
   ```csharp
   [Tool("search_unity_components")]
   public async Task<ToolResult> SearchUnityComponents(
       [FromJson] UnitySearchRequest request)
   {
       var results = await _unitySemanticSearch.SearchByUnityIntentAsync(
           request.Query, request.Filter, request.MaxResults);
       return ToolResult.Success(results);
   }
   
   [Tool("generate_unity_documentation")]
   public async Task<ToolResult> GenerateUnityDocumentation(
       [FromJson] UnityDocumentationRequest request)
   {
       var docs = await _unityDocGenerator.GenerateProjectDocsAsync(request.ProjectPath);
       return ToolResult.Success(docs);
   }
   ```

### Phase 4: Extensibility & Unity Ecosystem Integration
**Priority: Low | Effort: Medium**

1. **Unity Plugin System**
   ```csharp
   public class UnityPluginManager : IPluginManager
   {
       private readonly UnityAnalyzersService _unityAnalyzers;
       private readonly Dictionary<string, IAnalysisPlugin> _loadedPlugins = new();
       
       public async Task LoadUnityPluginAsync(string assemblyPath)
       {
           var assembly = Assembly.LoadFrom(assemblyPath);
           var pluginTypes = assembly.GetTypes()
               .Where(t => typeof(IAnalysisPlugin).IsAssignableFrom(t) && !t.IsAbstract);
               
           foreach (var pluginType in pluginTypes)
           {
               var plugin = (IAnalysisPlugin)Activator.CreateInstance(pluginType);
               
               // Pass Unity analyzers if required
               var unityAnalyzers = plugin.RequiresUnityAnalyzers ? _unityAnalyzers : null;
               await plugin.InitializeAsync(_projectPath, GetPluginConfig(plugin.Name), unityAnalyzers);
               
               _loadedPlugins[plugin.Name] = plugin;
           }
       }
   }
   ```

## Directory Structure

```
UnityCodeIntelligenceMCP/
├── src/
│   ├── UnityCodeIntelligence.Core/
│   │   ├── Server/
│   │   │   ├── McpServer.cs                   # Main MCP server
│   │   │   ├── ToolRegistry.cs                # Dynamic tool management  
│   │   │   ├── ResourceProvider.cs            # Resource management
│   │   │   └── PluginManager.cs               # Plugin lifecycle
│   │   ├── Analysis/
│   │   │   ├── UnityRoslynAnalysisService.cs  # Unity-aware Roslyn integration
│   │   │   ├── UnityAnalyzersService.cs       # Microsoft.Unity.Analyzers wrapper
│   │   │   ├── UnityProjectAnalyzer.cs        # Unity project analysis
│   │   │   ├── UnityComponentAnalyzer.cs      # Component-specific analysis
│   │   │   └── UnityDependencyGraphBuilder.cs # Unity dependency analysis
│   │   ├── Abstractions/
│   │   │   ├── IAnalysisPlugin.cs             # Plugin interface
│   │   │   ├── IUnityPatternDetector.cs       # Unity pattern detection
│   │   │   └── IUnitySemanticSearchService.cs # Unity search interface
│   │   └── Models/
│   │       ├── UnityProjectContext.cs         # Unity-aware data models
│   │       ├── UnityAnalysisResult.cs         # Unity analysis outputs
│   │       ├── UnityDiagnostic.cs             # Unity diagnostic information
│   │       └── UnityToolDefinitions.cs        # Unity tool schemas
│   ├── UnityCodeIntelligence.Plugins/
│   │   ├── UnityPatterns/
│   │   │   ├── UnityPatternDetectors.cs       # Unity-specific patterns
│   │   │   ├── ComponentPatternDetector.cs    # Component patterns
│   │   │   └── UnityPatternsPlugin.cs
│   │   ├── UnitySemanticSearch/
│   │   │   ├── UnityEmbeddingService.cs       # Unity-aware embeddings
│   │   │   ├── UnityVectorDatabase.cs         # Unity metadata storage
│   │   │   └── UnitySemanticSearchPlugin.cs
│   │   └── UnityDocumentation/
│   │       ├── UnityDocumentationGenerator.cs # Unity API docs
│   │       ├── UnityMarkdownRenderer.cs       # Unity-styled markdown
│   │       └── UnityDocumentationPlugin.cs
│   ├── UnityCodeIntelligence.Tools/
│   │   ├── UnityAnalysisTools.cs              # Unity-specific analysis tools
│   │   ├── UnitySearchTools.cs                # Unity search capabilities  
│   │   └── UnityDocumentationTools.cs         # Unity doc generation
│   └── UnityCodeIntelligence.Host/
│       ├── Program.cs                         # Application entry point
│       ├── ServiceCollectionExtensions.cs     # DI setup with Unity services
│       └── appsettings.json                   # Configuration
├── plugins/                                   # External plugins directory
├── config/
│   ├── default-unity-config.json              # Unity-specific default config
│   └── unity-schema.json                      # Unity configuration schema  
└── tests/
    ├── UnityCodeIntelligence.Core.Tests/      # Unit tests
    ├── UnityCodeIntelligence.Integration.Tests/ # Integration tests with Unity
    └── TestFixtures/                          # Test Unity projects
```

## Configuration System

### Unity Project Configuration
```json
{
  "unityProject": {
    "path": "/path/to/unity/project",
    "version": "2022.3.0f1",
    "assemblies": ["Assembly-CSharp", "Assembly-CSharp-Editor"],
    "targetFramework": "netstandard2.1"
  },
  "analysis": {
    "enableUnityAnalyzers": true,
    "enablePatternDetection": true,
    "enableSemanticSearch": false,
    "enableRoslynAnalysis": true,
    "cacheAnalysisResults": true,
    "watchFileChanges": true,
    "parallelAnalysis": true,
    "maxConcurrentAnalysis": 4,
    "unityDiagnosticLevel": "Info"
  },
  "unityAnalyzers": {
    "enablePerformanceAnalysis": true,
    "enableBestPracticeAnalysis": true,
    "enableCodeQualityAnalysis": true,
    "customRulesets": ["unity-performance.ruleset"],
    "excludedAnalyzers": [],
    "analyzerConfiguration": {
      "UNT0001": { "severity": "warning" },
      "UNT0002": { "severity": "info" },
      "UNT0006": { "severity": "error" }
    }
  },
  "plugins": {
    "enabled": ["UnityPatterns", "UnityDocumentationGenerator"],
    "disabled": ["UnitySemanticSearch"],
    "assemblyPaths": ["plugins/"],
          "configuration": {
        "UnityPatterns": {
          "detectCustomPatterns": true,
          "patternThreshold": 0.8,
          "enableMachineLearning": false,
          "unitySpecificPatterns": true
        },
        "UnityDocumentationGenerator": {
          "includeUnityAPIReferences": true,
          "generateComponentDiagrams": true,
          "includePerformanceMetrics": true
        }
      }
    },
    "server": {
      "transport": "stdio",
      "timeout": 30000,
      "enableLogging": true,
      "logLevel": "Information"
    },
    "roslyn": {
      "enableSemanticAnalysis": true,
      "loadUnityReferences": true,
      "analyzeGeneratedCode": false,
      "compilationTimeout": 60000,
      "unityPreprocessorSymbols": [
        "UNITY_2022_3_OR_NEWER",
        "UNITY_EDITOR",
        "UNITY_STANDALONE"
      ]
    }
  }
}
```

## Extension Points

### 1. Custom Unity Analyzers
```csharp
[UnityAnalysisPlugin("CustomUnityPerformanceAnalyzer", "1.0.0")]
public class CustomUnityPerformanceAnalyzer : IAnalysisPlugin
{
    private UnityAnalyzersService _unityAnalyzers;
    
    public bool RequiresUnityAnalyzers => true;
    
    public async Task InitializeAsync(string projectPath, PluginConfiguration config, 
                                    UnityAnalyzersService? unityAnalyzers = null,
                                    CancellationToken cancellationToken = default)
    {
        _unityAnalyzers = unityAnalyzers ?? throw new ArgumentNullException(nameof(unityAnalyzers));
        await base.InitializeAsync(projectPath, config, cancellationToken);
    }
    
    public async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context, 
                                                  CancellationToken cancellationToken = default)
    {
        var performanceIssues = new List<UnityPerformanceIssue>();
        
        foreach (var script in context.Scripts.Where(s => s.UnityAnalysis.IsMonoBehaviour))
        {
            // Use Microsoft.Unity.Analyzers for base analysis
            var unityDiagnostics = await _unityAnalyzers.AnalyzeWithUnityRulesAsync(
                context.Compilation, cancellationToken);
                
            // Add custom performance analysis
            var customIssues = await AnalyzeCustomPerformanceAsync(script, unityDiagnostics);
            performanceIssues.AddRange(customIssues);
        }
        
        return new AnalysisResult 
        { 
            UnityPerformanceIssues = performanceIssues,
            Suggestions = GenerateUnityOptimizationSuggestions(performanceIssues)
        };
    }
    
    private async Task<IEnumerable<UnityPerformanceIssue>> AnalyzeCustomPerformanceAsync(
        ScriptInfo script, IEnumerable<UnityDiagnostic> baseDiagnostics)
    {
        var issues = new List<UnityPerformanceIssue>();
        
        // Custom Unity performance analysis
        foreach (var message in script.UnityAnalysis.UnityMessages)
        {
            if (message.Type == UnityMessageType.Update && message.HasPerformanceImplications)
            {
                issues.Add(new UnityPerformanceIssue(
                    "Custom_Update_Performance",
                    $"Update method in {script.ClassName} may have performance implications",
                    UnityPerformanceImpact.High,
                    script.Path,
                    message.Method.LineNumber
                ));
            }
        }
        
        return issues;
    }
}
```

### 2. Unity-Specific Tools with Attributes
```csharp
public class UnitySpecificAnalysisTools
{
    [Tool("analyze_unity_performance")]
    [Description("Analyze Unity-specific performance patterns using Microsoft.Unity.Analyzers")]
    public async Task<ToolResult> AnalyzeUnityPerformance(
        [FromJson] UnityPerformanceAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var analysis = await _unityPerformanceAnalyzer.AnalyzeAsync(
            request.ProjectPath, request.AnalysisDepth, cancellationToken);
            
        // Enrich with Unity Analyzers diagnostics
        var unityDiagnostics = await _unityAnalyzers.AnalyzeWithUnityRulesAsync(
            analysis.Compilation, cancellationToken);
            
        return ToolResult.Success(new UnityPerformanceAnalysisResult(
            analysis, unityDiagnostics));
    }
    
    [Tool("validate_unity_best_practices")]
    [Description("Validate Unity best practices using built-in Unity analyzers")]
    public async Task<ToolResult> ValidateUnityBestPractices(
        [FromJson] UnityBestPracticesRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _unityBestPracticesValidator.ValidateAsync(
            request.ProjectPath, request.ValidationLevel, cancellationToken);
        return ToolResult.Success(validation);
    }
    
    [Tool("find_unity_message_issues")]
    [Description("Find issues with Unity message methods using Unity analyzers")]
    public async Task<ToolResult> FindUnityMessageIssues(
        [FromJson] UnityMessageAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var issues = await _unityMessageAnalyzer.FindIssuesAsync(
            request.ScriptPath, request.MessageTypes, cancellationToken);
        return ToolResult.Success(issues);
    }
}
```

### 3. Unity Resources with Streaming
```csharp
[Resource("unity://performance-insights")]
public async IAsyncEnumerable<ResourceChunk> GetUnityPerformanceInsights(
    string uri, 
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var projectPath = ExtractProjectPathFromUri(uri);
    
    await foreach (var insight in _unityPerformanceAnalyzer
        .StreamInsightsAsync(projectPath, cancellationToken))
    {
        // Enrich with Unity Analyzers data
        var unityContext = await _unityAnalyzers.GetContextAsync(insight, cancellationToken);
        var enrichedInsight = new UnityPerformanceInsight(insight, unityContext);
        
        yield return new ResourceChunk(JsonSerializer.Serialize(enrichedInsight));
    }
}

[Resource("unity://component-diagnostics/{componentName}")]
public async Task<ResourceContent> GetUnityComponentDiagnostics(string uri)
{
    var componentName = ExtractComponentNameFromUri(uri);
    var diagnostics = await _unityAnalyzers.GetComponentDiagnosticsAsync(componentName);
    
    return new ResourceContent(
        JsonSerializer.Serialize(diagnostics), 
        "application/json",
        UnityResourceType.Component
    );
}
```

## Implementation Considerations

### Unity Analyzers Integration
```csharp
public class UnityAnalyzersIntegrationService
{
    private readonly ILogger<UnityAnalyzersIntegrationService> _logger;
    
    public async Task<UnityAnalysisConfiguration> ConfigureAnalyzersAsync(
        string projectPath, UnityAnalyzersConfig config)
    {
        // Load Unity project settings
        var unityVersion = await DetectUnityVersionAsync(projectPath);
        var targetFramework = await DetectTargetFrameworkAsync(projectPath);
        
        // Configure analyzers based on Unity version
        var analyzerConfig = new UnityAnalysisConfiguration
        {
            UnityVersion = unityVersion,
            TargetFramework = targetFramework,
            EnabledAnalyzers = GetEnabledAnalyzersForVersion(unityVersion),
            AnalyzerSettings = config.AnalyzerConfiguration
        };
        
        _logger.LogInformation(
            "Configured Unity Analyzers for Unity {UnityVersion} targeting {TargetFramework}",
            unityVersion, targetFramework);
            
        return analyzerConfig;
    }
    
    private IReadOnlyList<string> GetEnabledAnalyzersForVersion(UnityVersion version)
    {
        return version.Major switch
        {
            2022 => new[] { "UNT0001", "UNT0002", "UNT0006", "UNT0014", "UNT0018" },
            2023 => new[] { "UNT0001", "UNT0002", "UNT0006", "UNT0014", "UNT0018", "UNT0019" },
            _ => new[] { "UNT0001", "UNT0002", "UNT0006" }
        };
    }
}
```

### Performance Optimization with Unity Focus
```csharp
public class UnityOptimizedAnalysisService : IDisposable
{
    private readonly SemaphoreSlim _analysisSemaphore;
    private readonly MemoryCache _unityAnalysisCache;
    private readonly UnityAnalyzersService _unityAnalyzers;
    
    public async Task<UnityAnalysisResult> AnalyzeWithUnityOptimizationsAsync(
        AnalysisContext context, CancellationToken cancellationToken = default)
    {
        await _analysisSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Check cache for Unity-specific analysis results
            var cacheKey = GenerateUnityCacheKey(context);
            if (_unityAnalysisCache.TryGetValue(cacheKey, out UnityAnalysisResult cached))
            {
                return cached;
            }
            
            // Perform Unity-aware analysis
            var result = await PerformUnityAnalysisAsync(context, cancellationToken);
            
            // Cache with Unity-specific invalidation rules
            _unityAnalysisCache.Set(cacheKey, result, GetUnityCacheOptions());
            
            return result;
        }
        finally
        {
            _analysisSemaphore.Release();
        }
    }
    
    private async Task<UnityAnalysisResult> PerformUnityAnalysisAsync(
        AnalysisContext context, CancellationToken cancellationToken)
    {
        // Parallel analysis of Unity-specific concerns
        var analysisTask = PerformCoreAnalysisAsync(context, cancellationToken);
        var unityDiagnosticsTask = _unityAnalyzers.AnalyzeWithUnityRulesAsync(
            context.Compilation, cancellationToken);
        var unityPatternTask = AnalyzeUnityPatternsAsync(context, cancellationToken);
        
        await Task.WhenAll(analysisTask, unityDiagnosticsTask, unityPatternTask);
        
        return new UnityAnalysisResult(
            await analysisTask,
            await unityDiagnosticsTask,
            await unityPatternTask
        );
    }
}
```

### Error Handling & Unity-Specific Resilience
```csharp
public class UnityResilientAnalysisService
{
    private readonly ILogger<UnityResilientAnalysisService> _logger;
    private readonly CircuitBreakerPolicy _circuitBreaker;
    private readonly UnityAnalyzersService _unityAnalyzers;
    
    public async Task<UnityAnalysisResult> AnalyzeWithUnityResilienceAsync(
        AnalysisContext context, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            try
            {
                return await _coreAnalyzer.AnalyzeAsync(context, cancellationToken);
            }
            catch (UnityCompilationException ex)
            {
                _logger.LogWarning(ex, "Unity compilation errors found, providing partial analysis");
                
                // Fallback to Unity Analyzers only
                var unityDiagnostics = await _unityAnalyzers.AnalyzeWithUnityRulesAsync(
                    context.Compilation, cancellationToken);
                    
                return new UnityAnalysisResult(
                    PartialAnalysisResult: true,
                    UnityDiagnostics: unityDiagnostics,
                    ErrorMessage: ex.Message
                );
            }
            catch (UnityAssetParsingException ex)
            {
                _logger.LogWarning(ex, "Unity asset parsing failed, skipping asset analysis");
                
                // Continue with script analysis only
                return await _scriptOnlyAnalyzer.AnalyzeAsync(context, cancellationToken);
            }
        });
    }
}
```

## Success Metrics

### Technical Metrics
- **Unity Analysis Performance**: < 10 seconds for typical Unity projects (leveraging Microsoft.Unity.Analyzers efficiency)
- **Memory Efficiency**: < 250MB for large Unity projects (1000+ scripts) with Unity Analyzers integration
- **Unity Compilation Success**: > 98% successful Unity compilation rate with proper Unity references
- **Unity Diagnostic Accuracy**: > 95% accuracy in Unity-specific issue detection using Microsoft analyzers

### Unity-Specific Code Quality Metrics
- **Unity Best Practices Compliance**: > 90% compliance with Unity coding standards
- **Unity Performance Issue Detection**: > 95% detection rate of common Unity performance issues
- **Unity Pattern Recognition Precision**: > 92% precision in Unity-specific pattern detection
- **Unity API Documentation Coverage**: > 85% of Unity components documented with API references

### Integration Success Metrics
- **Microsoft.Unity.Analyzers Integration**: 100% of Unity analyzer rules properly integrated
- **Unity Version Compatibility**: Support for Unity 2022.3+ with appropriate analyzer configurations
- **Plugin Extensibility**: < 2 seconds plugin loading time with Unity analyzer dependencies
- **Unity Project Compatibility**: > 95% successful analysis of real-world Unity projects

## Key Advantages of Unity Analyzers Integration

### 1. **Authoritative Unity Analysis**
- Leverages Microsoft's official Unity analysis rules
- Stays current with Unity best practices automatically
- Provides Unity-specific diagnostic codes (UNT0001, etc.)

### 2. **Enhanced Performance Detection**
- Built-in detection of common Unity performance anti-patterns
- Automatic identification of inefficient Unity API usage
- Specialized analysis for Unity message methods

### 3. **Deep Unity Context Understanding**
- Recognizes Unity-specific types and patterns
- Understands Unity serialization semantics
- Provides Unity lifecycle method analysis

### 4. **Professional Development Integration**
- Consistent with Unity's official tooling
- Integrates with Unity's development workflow
- Provides diagnostics familiar to Unity developers

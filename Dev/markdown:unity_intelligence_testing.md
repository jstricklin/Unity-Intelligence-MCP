# Unity Code Intelligence MCP Server - Testing Strategy

## 1. Phase 1 Testing (Core Functionality)

### Service Layer Tests
```csharp
// Basic project analysis
[Fact]
public async Task AnalyzeProjectAsync_ValidProject_ReturnsContext()
{
    // Arrange
    var projectPath = "TestProjects/BasicProject";
    var analyzer = CreateProjectAnalyzer();
    
    // Act
    var context = await analyzer.AnalyzeProjectAsync(projectPath);
    
    // Assert
    Assert.NotNull(context);
    Assert.NotEmpty(context.Scripts);
    Assert.Equal(3, context.Scripts.Count);
}

// Error handling for invalid paths
[Fact]
public async Task AnalyzeProjectAsync_InvalidPath_ThrowsMeaningfulException()
{
    var analyzer = CreateProjectAnalyzer();
    var exception = await Assert.ThrowsAsync<AnalysisException>(
        () => analyzer.AnalyzeProjectAsync("invalid/path")
    );
    Assert.Contains("Project not found", exception.Message);
}
```

### Tool Endpoint Verification
```csharp
[Fact]
public async Task AnalyzeUnityProjectTool_ReturnsCompleteContext()
{
    // Arrange
    var tool = _serviceProvider.GetRequiredService<AnalysisTools>();
    
    // Act
    var result = await tool.AnalyzeUnityProject("TestProjects/AdvancedProject");
    
    // Assert
    Assert.NotNull(result.Scenes);
    Assert.NotNull(result.Prefabs);
    Assert.NotEmpty(result.Scripts);
}
```

### Roslyn Integration Checks
```csharp
[Fact]
public void RoslynAnalysis_UsingDirectives_ResolveUnityReferences()
{
    var code = "using UnityEngine; public class TestBehaviour : MonoBehaviour {}";
    var tree = CSharpSyntaxTree.ParseText(code);
    var compilation = _roslynService.CreateUnityCompilation("TestProject", new[] { tree });
    
    var model = compilation.GetSemanticModel(tree);
    var symbol = model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes()
        .OfType<ClassDeclarationSyntax>().First());
    
    Assert.Equal("MonoBehaviour", symbol.BaseType.Name);
}
```

## 2. Phase 2 Testing (Pattern Recognition)

### Pattern Detection Unit Tests
```csharp
// Singleton detection
[Fact]
public void SingletonDetector_ManagerClass_Identified()
{
    var script = new ScriptInfo("GameManager.cs", "GameManager");
    var detector = new SingletonPatternDetector();
    
    var result = detector.DetectAsync(script).Result;
    
    Assert.True(result);
}

// Coroutine detection
[Fact]
public void CoroutineDetector_IEnumeratorMethod_ReturnsTrue()
{
    var script = new ScriptInfo("Spawner.cs", "Spawner")
        .WithMethod("public IEnumerator SpawnEnemies()");
    
    var result = new CoroutinePatternDetector().DetectAsync(script).Result;
    Assert.True(result);
}
```

### Relationship Analysis Tests
```csharp
[Fact]
public void RelationshipAnalyzer_RequireComponentAttribute_IdentifiesDependency()
{
    var script = new ScriptInfo("Player.cs", "Player")
        .WithAttribute("[RequireComponent(typeof(Rigidbody))]");
    
    var relationships = _relationshipAnalyzer.Analyze(new List<ScriptInfo> { script });
    
    Assert.Contains(
        new ComponentRelationship("Rigidbody", "RequireComponent"),
        relationships.Nodes["Player"]
    );
}
```

### End-to-End Integration
```csharp
[Fact]
public async Task FullAnalysis_NewPatterns_AppearInContext()
{
    var context = await _projectAnalyzer.AnalyzeProjectAsync("TestProjects/PatternDemo");
    
    Assert.Contains(context.DetectedPatterns, 
        p => p.PatternName == "ObjectPool");
    Assert.True(context.ComponentRelationships.Nodes.ContainsKey("PoolManager"));
}
```

## 3. Performance Benchmarks

### Analysis Time Targets
| Project Size | Scripts | Max Acceptable Time |
|--------------|---------|---------------------|
| Small        | 50      | 3s                 |
| Medium       | 500     | 15s                |
| Large        | 2000    | 45s                |

### Memory Usage Limits
| Test Case | Max Memory | Test Method |
|-----------|------------|-------------|
| Basic project | 100MB | `GC.GetTotalMemory(true)` |
| Complex pattern matching | 300MB | Performance Profiler |
| Large project analysis | 500MB | MemoryDiagnoser |

## 4. Test Framework

### Recommended Structure
```
Tests/
├── Unit/
│   ├── Analysis/
│   │   ├── PatternDetectorTests.cs
│   │   └── RelationshipAnalyzerTests.cs
│   └── Services/
│       └── RoslynAnalysisTests.cs
├── Integration/
│   ├── ProjectAnalysisTests.cs
│   └── ToolEndpointTests.cs
└── Performance/
    ├── AnalysisBenchmarks.cs
    └── MemoryProfilingTests.cs
```

### Sample Project Structure
**TestProjects/BasicPatternDemo**:
- `GameManager.cs` (Singleton)
- `ObjectPool.cs` (Object Pool)
- `PlayerController.cs` (Uses Coroutines)
- `EnemySpawner.cs` (State Machine)

### Execution Commands
```bash
# Run all unit tests
dotnet test --filter "Category=Unit"

# Run integration tests
dotnet test --filter "Category=Integration"

# Benchmark tests
dotnet test -c Release --filter "Category=Performance"
```

## 5. Manual Validation Checklist
1. Pattern Accuracy Verification
   - [ ] Verify Singleton detection in manager classes
   - [ ] Check Object Pool identification in pooling systems
   - [ ] Confirm Coroutine detection in spawner scripts

2. Relationship Mapping
   - [ ] Validate RequireComponent relationships
   - [ ] Check GetComponent usage tracking
   - [ ] Verify message-based dependencies

3. Tool Responses
   - [ ] Test `analyze_unity_project` endpoint
   - [ ] Verify `find_unity_patterns` output
   - [ ] Check `analyze_component_relationships` response

4. Real Project Testing
   - [ ] Unity Microgame templates
   - [ ] Third-party Unity assets
   - [ ] Internal project repositories

## 6. Success Metrics
| Key Area | Metric | Target Value |
|----------|--------|--------------|
| Pattern Detection | Accuracy | >95% |
| Relationship Analysis | Coverage | >90% |  
| Project Loading | Time for 100 scripts | <5s |
| Memory Usage | Peak working set | <500MB |
| Reliability | Exception rate | <1% |

> "Quality is never an accident; it is always the result of intelligent effort." - John Ruskin

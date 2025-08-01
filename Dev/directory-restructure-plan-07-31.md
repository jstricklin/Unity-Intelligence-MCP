# Data/Models Directory Restructure Plan

## Core/Data Structure
```
Core/
└── Data/
    ├── Contracts/
    │   ├── IApplicationDatabase.cs
    │   ├── IDbWorkQueue.cs
    │   ├── IDbWorkItem.cs
    │   ├── IDocumentationRepository.cs
    │   └── IDuckDbConnectionFactory.cs
    │
    ├── Infrastructure/
    │   ├── DuckDbApplicationDatabase.cs
    │   ├── DocumentationRepository.cs
    │   ├── DuckDbConnectionFactory.cs
    │   └── DbWorkQueue.cs
    │
    └── Services/
        └── QueuedDbWriterService.cs
```

## Models Structure
```
Models/
├── Database/
│   ├── ContentElement.cs
│   ├── DocMetadata.cs
│   └── SemanticDocumentRecord.cs
│
├── Analysis/
│   ├── DependencyGraph.cs
│   └── ScriptInfo.cs
│
├── Documentation/
│   └── UnityDocumentationData.cs
│
├── Resources/
│   └── ResourceResult.cs
```

## Namespace Updates Needed:
1. `UnityIntelligenceMCP.Models.Documentation` → `UnityIntelligenceMCP.Models.Database`
   - For: ContentElement.cs, DocMetadata.cs, SemanticDocumentRecord.cs
   
2. `UnityIntelligenceMCP.Core.Data` → `UnityIntelligenceMCP.Core.Data.Infrastructure`
   - For: DocumentationRepository.cs
   - And: DuckDbApplicationDatabase.cs

3. `UnityIntelligenceMCP.Core.Data.Services` (new)
   - For: QueuedDbWriterService.cs
   
4. `UnityIntelligenceMCP.Models` → `UnityIntelligenceMCP.Models.Analysis`
   - For: ScriptInfo.cs, DependencyGraph.cs

5. `UnityIntelligenceMCP.Models.Resources` (new)
   - For: ResourceResult.cs

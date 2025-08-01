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

mkdir -p Core/Data/Contracts Core/Data/Infrastructure Core/Data/Services;
mkdir -p Models/Database Models/Analysis Models/Documentation Models/Resources;
mv Core/Data/IApplicationDatabase.cs Core/Data/Contracts/IApplicationDatabase.cs;
mv Core/Data/IDbWorkQueue.cs Core/Data/Contracts/IDbWorkQueue.cs;
mv Core/Data/IDbWorkItem.cs Core/Data/Contracts/IDbWorkItem.cs;
mv Core/Data/IDocumentationRepository.cs Core/Data/Contracts/IDocumentationRepository.cs;
mv Core/Data/DuckDbApplicationDatabase.cs Core/Data/Infrastructure/DuckDbApplicationDatabase.cs;
mv Core/Data/DocumentationRepository.cs Core/Data/Infrastructure/DocumentationRepository.cs;
mv Core/Data/DbWorkQueue.cs Core/Data/Infrastructure/DbWorkQueue.cs;
mv Core/Data/QueuedDbWriterService.cs Core/Data/Services/QueuedDbWriterService.cs;
mv Models/Documentation/ContentElement.cs Models/Database/ContentElement.cs;
mv Models/Documentation/DocMetadata.cs Models/Database/DocMetadata.cs;
mv Models/Documentation/SemanticDocumentRecord.cs Models/Database/SemanticDocumentRecord.cs;
mv Models/DependencyGraph.cs Models/Analysis/DependencyGraph.cs;
mv Models/ScriptInfo.cs Models/Analysis/ScriptInfo.cs;
mv Models/UnityDocumentationData.cs Models/Documentation/UnityDocumentationData.cs;
mv Models/ResourceResult.cs Models/Resources/ResourceResult.cs;
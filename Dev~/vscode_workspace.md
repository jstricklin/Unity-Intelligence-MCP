import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';

interface VSCodeWorkspace {
  folders: Array<{
    name?: string;
    path: string;
  }>;
  settings?: Record<string, any>;
  extensions?: {
    recommendations?: string[];
  };
}

class WorkspaceIntegrationServer {
  private server: Server;
  
  constructor() {
    this.server = new Server(
      {
        name: 'workspace-integration-server',
        version: '1.0.0',
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );
    
    this.setupToolHandlers();
  }
  
  private setupToolHandlers() {
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      return {
        tools: [
          {
            name: 'setup_vscode_workspace',
            description: 'Automatically configure VSCode workspace with project dependencies and cache folders',
            inputSchema: {
              type: 'object',
              properties: {
                projectPath: {
                  type: 'string',
                  description: 'Absolute path to the project root',
                },
                additionalFolders: {
                  type: 'array',
                  items: { type: 'string' },
                  description: 'Additional folders to include in workspace (like cache directories)',
                  default: [],
                },
                workspaceName: {
                  type: 'string',
                  description: 'Name for the workspace file',
                  default: 'project-workspace',
                },
              },
              required: ['projectPath'],
            },
          },
          {
            name: 'detect_project_dependencies',
            description: 'Detect and return project dependency folders (like Unity PackageCache, node_modules, etc.)',
            inputSchema: {
              type: 'object',
              properties: {
                projectPath: {
                  type: 'string',
                  description: 'Absolute path to the project root',
                },
                projectType: {
                  type: 'string',
                  enum: ['unity', 'nodejs', 'dotnet', 'auto'],
                  description: 'Type of project to detect dependencies for',
                  default: 'auto',
                },
              },
              required: ['projectPath'],
            },
          },
        ],
      };
    });
    
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;
      
      switch (name) {
        case 'setup_vscode_workspace':
          return await this.setupVSCodeWorkspace(args as any);
        case 'detect_project_dependencies':
          return await this.detectProjectDependencies(args as any);
        default:
          throw new Error(`Unknown tool: ${name}`);
      }
    });
  }
  
  private async detectProjectDependencies(args: {
    projectPath: string;
    projectType?: string;
  }) {
    const { projectPath, projectType = 'auto' } = args;
    const dependencyFolders: Array<{ path: string; type: string; name: string }> = [];
    
    try {
      // Detect project type if auto
      let detectedType = projectType;
      if (projectType === 'auto') {
        detectedType = await this.detectProjectType(projectPath);
      }
      
      // Unity project detection
      if (detectedType === 'unity') {
        const unityFolders = await this.detectUnityDependencies(projectPath);
        dependencyFolders.push(...unityFolders);
      }
      
      // Node.js project detection
      if (detectedType === 'nodejs') {
        const nodeFolders = await this.detectNodeDependencies(projectPath);
        dependencyFolders.push(...nodeFolders);
      }
      
      // .NET project detection
      if (detectedType === 'dotnet') {
        const dotnetFolders = await this.detectDotNetDependencies(projectPath);
        dependencyFolders.push(...dotnetFolders);
      }
      
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify({
              projectType: detectedType,
              dependencyFolders,
              summary: `Found ${dependencyFolders.length} dependency folders for ${detectedType} project`
            }, null, 2)
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: 'text',
            text: `Error detecting dependencies: ${error.message}`
          }
        ],
        isError: true
      };
    }
  }
  
  private async detectProjectType(projectPath: string): Promise<string> {
    const files = await fs.promises.readdir(projectPath).catch(() => []);
    
    // Unity project markers
    if (files.some(f => f === 'Assets' || f === 'ProjectSettings')) {
      return 'unity';
    }
    
    // Node.js project markers
    if (files.some(f => f === 'package.json')) {
      return 'nodejs';
    }
    
    // .NET project markers
    if (files.some(f => f.endsWith('.sln') || f.endsWith('.csproj'))) {
      return 'dotnet';
    }
    
    return 'unknown';
  }
  
  private async detectUnityDependencies(projectPath: string): Promise<Array<{ path: string; type: string; name: string }>> {
    const folders = [];
    
    // Unity PackageCache (equivalent to PackedCache in the original question)
    const packageCachePath = path.join(projectPath, 'Library', 'PackageCache');
    if (await this.pathExists(packageCachePath)) {
      folders.push({
        path: packageCachePath,
        type: 'unity-packages',
        name: 'Unity Packages Cache'
      });
    }
    
    // Unity's global package cache
    const globalCacheBase = path.join(os.homedir(), 'AppData', 'Local', 'Unity', 'cache', 'packages');
    if (await this.pathExists(globalCacheBase)) {
      folders.push({
        path: globalCacheBase,
        type: 'unity-global-cache',
        name: 'Unity Global Package Cache'
      });
    }
    
    // Packages folder (for local packages)
    const packagesPath = path.join(projectPath, 'Packages');
    if (await this.pathExists(packagesPath)) {
      folders.push({
        path: packagesPath,
        type: 'unity-local-packages',
        name: 'Unity Local Packages'
      });
    }
    
    return folders;
  }
  
  private async detectNodeDependencies(projectPath: string): Promise<Array<{ path: string; type: string; name: string }>> {
    const folders = [];
    
    const nodeModulesPath = path.join(projectPath, 'node_modules');
    if (await this.pathExists(nodeModulesPath)) {
      folders.push({
        path: nodeModulesPath,
        type: 'node-modules',
        name: 'Node Modules'
      });
    }
    
    return folders;
  }
  
  private async detectDotNetDependencies(projectPath: string): Promise<Array<{ path: string; type: string; name: string }>> {
    const folders = [];
    
    // NuGet global packages folder
    const nugetPath = path.join(os.homedir(), '.nuget', 'packages');
    if (await this.pathExists(nugetPath)) {
      folders.push({
        path: nugetPath,
        type: 'nuget-packages',
        name: 'NuGet Packages'
      });
    }
    
    return folders;
  }
  
  private async setupVSCodeWorkspace(args: {
    projectPath: string;
    additionalFolders?: string[];
    workspaceName?: string;
  }) {
    const { projectPath, additionalFolders = [], workspaceName = 'project-workspace' } = args;
    
    try {
      // Detect dependencies automatically
      const dependencies = await this.detectProjectDependencies({ projectPath, projectType: 'auto' });
      const dependencyData = JSON.parse(dependencies.content[0].text);
      
      // Create workspace configuration
      const workspace: VSCodeWorkspace = {
        folders: [
          {
            name: "Project Root",
            path: projectPath
          }
        ],
        settings: {
          "search.exclude": {},
          "files.exclude": {},
          "typescript.preferences.includePackageJsonAutoImports": "on"
        },
        extensions: {
          recommendations: []
        }
      };
      
      // Add detected dependency folders
      for (const folder of dependencyData.dependencyFolders) {
        workspace.folders.push({
          name: folder.name,
          path: folder.path
        });
        
        // Add search exclusions for large cache folders to improve performance
        if (folder.type.includes('cache') || folder.type === 'node-modules') {
          workspace.settings["search.exclude"][`**/${path.basename(folder.path)}/**`] = true;
        }
      }
      
      // Add additional folders
      for (const folder of additionalFolders) {
        if (await this.pathExists(folder)) {
          workspace.folders.push({
            name: path.basename(folder),
            path: folder
          });
        }
      }
      
      // Add project-specific recommendations
      if (dependencyData.projectType === 'unity') {
        workspace.extensions.recommendations.push(
          'ms-dotnettools.csharp',
          'kleber-swf.unity-code-snippets',
          'tobiah.unity-tools'
        );
      } else if (dependencyData.projectType === 'nodejs') {
        workspace.extensions.recommendations.push(
          'ms-vscode.vscode-typescript-next',
          'bradlc.vscode-tailwindcss'
        );
      }
      
      // Write workspace file
      const workspaceFile = path.join(projectPath, `${workspaceName}.code-workspace`);
      await fs.promises.writeFile(workspaceFile, JSON.stringify(workspace, null, 2));
      
      return {
        content: [
          {
            type: 'text',
            text: `✅ VSCode workspace created successfully!
            
📁 Workspace file: ${workspaceFile}
🔍 Project type: ${dependencyData.projectType}
📦 Folders included: ${workspace.folders.length}
🛠️ Extensions recommended: ${workspace.extensions.recommendations.length}

The workspace includes:
${workspace.folders.map(f => `  • ${f.name}: ${f.path}`).join('\n')}

To use:
1. Open VSCode
2. File → Open Workspace from File
3. Select: ${workspaceFile}

This will give your IDE and AI assistants better context about your project dependencies!`
          }
        ]
      };
    } catch (error) {
      return {
        content: [
          {
            type: 'text',
            text: `❌ Error setting up workspace: ${error.message}`
          }
        ],
        isError: true
      };
    }
  }
  
  private async pathExists(path: string): Promise<boolean> {
    try {
      await fs.promises.access(path);
      return true;
    } catch {
      return false;
    }
  }
  
  async run() {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('Workspace Integration MCP Server running on stdio');
  }
}

const server = new WorkspaceIntegrationServer();
server.run().catch(console.error);

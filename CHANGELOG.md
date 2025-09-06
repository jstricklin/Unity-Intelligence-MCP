# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
## [0.8.0] 2025 - 08 - 22
### Added

- Add Editor project resources: 
    - Available Editor Menu Items
    - Scene Hierarchy
    - Project Info
    - List Prefabs
    - Get Package Info
    - Get Console Logs
    - List Installed Packages
    - List Available Packages

- Add new Unity Editor Tools:
    - Create Prefab
    - Spawn Prefab
    - Execute Menu Item
    - Create GameObject
    - Create Primitive
    - Delete GameObject
    - Find GameObject
    - Fetch GameObject Info
    - Modify GameObject
    - Modify/Add Component
    - Remove Component
    - Update Transform
    - Add Package
    - Remove Package
    - Open Scene
    - Close Scene
    - Open File

- Add Editor WebSocket server AutoStart and automatic restart on unintended server drop
- Add new proper tool and resource usage logging in DuckDb

## Changed

- Update Unity Editor Window settings and layout to support new AutoRun feature


## [0.2.0] 2025 - 08 - 22
### Added

- Add Unity Package Cache (Used during Roslyn Compilation Analysis and in VSCode Workspace)
- Add SCRIPT_DIR environ variable and Editor Window Server Setting to configure project script directory for analysis

### Fixed

- Resolve package scripts returned in project script analysis (SCRIPT_DIR var)

### Deprecated

- SearchScope tool parameters deprecated

## [0.1.0] - 2025 - 08 - 21
### Added

- Working basic MCP Websocket integration to act as bridge for Unity Editor interactions
- Added Unity Editor Websocket client connection to MCP Server WS Host
- Added mcp.json configuration to enable IDE integration with MCP Server

### Changed

- MCP Server configuration from appsettings.json now overridden by values input in Unity Editor Server Window
- appsettings.json format updated to flatten structure



## [0.0.1] - 2025 - 08 - 20
### Added

- Start Changelog!
- Working basic MCP Server with core functionality
- Working basic RAG funcionality added with Unity3D script reference processing
- All MiniLM L6 V2 local embedding model properly integrated
- DuckDB integrated to serve as application and VectorDB
- Initial tools to analyze and gather code and Unity documentation built
- Basic Unity Editor Window built
- Editor Websocket Server built - connections pending
- Unity Package prepared

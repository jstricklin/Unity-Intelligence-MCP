# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added

- Add Unity Package Cache to VSCode workspace (Roslyn Static Analysis)
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

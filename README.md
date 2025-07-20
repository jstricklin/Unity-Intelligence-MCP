# Unity Code Intelligence MCP Server

This document provides instructions on how to build, run, and interact with the Unity Code Intelligence server.

## Prerequisites

Before you begin, ensure you have the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or a newer version installed.

## Building and Running

The following instructions apply to both Windows and macOS.

### 1. Build the Server

From the root directory of the repository, run the following command to build the project and restore dependencies:

```bash
dotnet build
```

### 2. Run the Server

To start the server, execute the following command from the root directory:

```bash
dotnet run
```

The server will now be running and ready to accept requests from an MCP client.

## Interacting with the Server using mcp-cli

You can use the `mcp-cli` tool to interact with the running server.

### 1. Install mcp-cli

If you don't have it installed, run the following command:

```bash
dotnet tool install --global mcp-cli --prerelease
```

### 2. List Available Tools

To see the tools provided by the server, use the `list-tools` command. The `--cmd` argument tells `mcp-cli` how to start our server.

```bash
mcp-cli --cmd "dotnet run" list-tools
```

### 3. Call a Tool

To analyze a Unity project, use the `call-tool` command. You must provide the absolute path to your Unity project.

```bash
# Replace "/path/to/your/unity/project" with an actual path
mcp-cli --cmd "dotnet run" call-tool analyze_unity_project '{"projectPath": "/path/to/your/unity/project"}'
```

### 4. List Available Resources

To see the available data resources, use the `list-resources` command.

```bash
mcp-cli --cmd "dotnet run" list-resources
```

### 5. Get a Resource

To retrieve a resource, such as project-wide diagnostics, use the `get-resource` command. The project path must be URL-encoded within the resource URI.

```bash
# Replace "/path/to/your/unity/project" with a URL-encoded path.
# For example, '/' becomes '%2F'.
mcp-cli --cmd "dotnet run" get-resource "unity://project-diagnostics/%2Fpath%2Fto%2Fyour%2Funity%2Fproject"
```

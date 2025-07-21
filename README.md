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

## Interacting with the Server

You can interact with the server using the MCP Inspector, which provides a web-based interface.

```bash
# Navigate to your MCP server project directory
# (The directory containing Unity_Intelligence_MCP.csproj)
cd path/to/Unity_Intelligence_MCP

# Install and run the MCP Inspector
npx @modelcontextprotocol/inspector dotnet run
```

Follow the instructions in your terminal. You will be prompted to open a URL in your web browser to access the inspector.

From the web interface, you can select a tool like `analyze_unity_project`. The inspector will then display a form where you must provide the required arguments, such as the `projectPath`. Enter the full, absolute path to your Unity project and execute the tool.

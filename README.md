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

## Configuration

The server is configured using the `appsettings.json` file, which must be placed in the project's root directory. This file allows you to specify paths required for the analysis tools to function correctly.

### `UnityAnalysisSettings`

All settings are located within the `UnityAnalysisSettings` object in `appsettings.json`.

-   **`ProjectPath`** (Required): The absolute path to the root of the Unity project you intend to analyze.
    -   *Example*: `"C:\\Users\\YourUser\\Documents\\MyUnityProject"`

-   **`InstallRoot`** (Optional): The path to the directory containing your Unity Editor version folders. This is typically the `Editor` folder inside your Unity Hub installation directory. This setting is used with the project's detected version to automatically locate the correct Unity Editor.
    -   *Example*: `"C:\\Program Files\\Unity\\Hub\\Editor"`

-   **`EditorPath`** (Optional): A direct and explicit path to a specific Unity Editor installation folder. If provided, this path takes priority over the `InstallRoot` setting. This is useful for development or if your editor is in a non-standard location.
    -   *Example*: `"C:\\UnityEditors\\2022.3.15f1"`

To function, the analyzer must be able to locate the Unity Editor installation. You must configure **either** `InstallRoot` (so the editor can be found automatically) **or** provide a direct `EditorPath`.

### Sample appsettings.json:
```
{
  "UnityAnalysisSettings": {
    "InstallRoot": "/Applications/Unity/Hub/Editor/",
    "EditorPath": "",
    "ProjectPath": "C:\\Path\\To\\Your\\UnityProject"
  }
}
```

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

From the web interface, you can select a tool like `analyze_unity_project`. The tool will use the `ProjectPath` configured in your `appsettings.json` to run the analysis.

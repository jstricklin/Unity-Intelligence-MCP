# Unity Code Intelligence Server

This document provides instructions on how to build and run the Unity Code Intelligence server.

## Prerequisites

Before you begin, ensure you have the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or a newer version installed.

## Building and Running

The following instructions apply to both Windows and macOS, as the `dotnet` command-line interface (CLI) is cross-platform.

### 1. Open a Terminal

-   **On Windows:** Open Command Prompt, PowerShell, or Windows Terminal.
-   **On macOS:** Open the Terminal application.

Navigate to the root directory of this repository after cloning it.

### 2. Build the Server

Run the following command to build the solution. This will restore all dependencies and compile the projects.

```bash
dotnet build
```

### 3. Run the Server

To start the server, execute the following command from the root of the repository:

```bash
dotnet run --project src/UnityCodeIntelligence.Host
```

The server will now be running and ready to accept connections.

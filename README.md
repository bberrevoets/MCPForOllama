# MCP For Ollama

An MCP (Model Context Protocol) server that exposes tools callable by Ollama models over the local network. Built with .NET 10 and designed to integrate with OpenWebUI via Streamable HTTP transport.

## Features

- **Streamable HTTP transport** — network-accessible MCP server, no stdio bridge needed
- **OpenWebUI integration** — connects directly via Admin Panel > Settings > External Tools
- **Auto-discovery** — new tools are picked up automatically at startup
- **Health endpoint** — quick reachability check at `/health`

### Available Tools

| Tool | Description |
|------|-------------|
| `GenerateRandomNumber` | Generates a random integer between min and max (inclusive). Defaults to 1–100. |

## Tech Stack

- **.NET 10** / C#
- **ModelContextProtocol.AspNetCore** v0.9.0-preview.1
- **xUnit v3** for testing

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.103 or later)

### Build & Run

```bash
# Build
dotnet build

# Run the server (listens on http://0.0.0.0:5000)
dotnet run --project src/MCPForOllama.Server

# Run tests
dotnet test
```

### Verify

```bash
# Health check
curl http://localhost:5000/health
# Returns: {"status":"healthy","service":"MCPForOllama"}
```

### Connect from OpenWebUI

1. Open **Admin Panel > Settings > External Tools**
2. Click **Add (+)**
3. Type: **MCP Streamable HTTP**
4. URL: `http://<server-ip>:5000/mcp`

### Firewall (Ubuntu)

If deploying on Ubuntu with ufw active:

```bash
sudo ufw allow 5000/tcp
```

## Adding New Tools

Create a new class in `src/MCPForOllama.Server/Tools/`:

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MCPForOllama.Server.Tools;

[McpServerToolType]
public static class MyNewTool
{
    [McpServerTool, Description("Describe what this tool does.")]
    public static string DoSomething(
        [Description("Describe the parameter.")] string input)
    {
        return $"Result: {input}";
    }
}
```

No changes to `Program.cs` required — tools are auto-discovered at startup.

## Project Structure

```
MCPForOllama/
├── MCPForOllama.slnx
├── global.json
├── src/
│   └── MCPForOllama.Server/
│       ├── MCPForOllama.Server.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Properties/launchSettings.json
│       └── Tools/
│           └── RandomNumberTool.cs
└── tests/
    └── MCPForOllama.Server.Tests/
        ├── MCPForOllama.Server.Tests.csproj
        └── Tools/
            └── RandomNumberToolTests.cs
```

## Author

- **Company:** Berrevoets Systems
- **Author:** Bert Berrevoets
- **Contact:** bert@berrevoets.net

## License

This project is for personal/educational use.

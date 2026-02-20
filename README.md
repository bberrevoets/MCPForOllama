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

> **Docker note:** If OpenWebUI runs in Docker, use `host.docker.internal` instead of `localhost`.

1. Open **Admin Panel > Settings > External Tools**
2. Click **+** to add a new connection
3. Fill in:
   - **Connection Type:** MCP Streamable HTTP
   - **URL:** `http://host.docker.internal:5000/mcp` (Docker) or `http://localhost:5000/mcp` (native)
   - **Auth:** None
   - **ID:** `mcpforollama`
   - **Name:** `MCPForOllama`
   - **Access:** All users
4. Click **Save**, then click the **refresh icon** next to the URL to verify the connection

For detailed step-by-step testing instructions, see [docs/LOCAL-TESTING.md](docs/LOCAL-TESTING.md).

### Firewall

If deploying on Ubuntu with ufw active:

```bash
sudo ufw allow 5000/tcp
```

On Windows, if the firewall blocks the connection:

```bash
netsh advfirewall firewall add rule name="MCPForOllama" dir=in action=allow protocol=TCP localport=5000
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
├── docs/
│   └── LOCAL-TESTING.md
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

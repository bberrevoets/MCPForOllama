# MCP For Ollama

An MCP (Model Context Protocol) server that exposes tools callable by Ollama models over the local network. Built with .NET 10 and designed to integrate with OpenWebUI via Streamable HTTP transport.

## Features

- **Streamable HTTP transport** — network-accessible MCP server, no stdio bridge needed
- **OpenWebUI integration** — connects directly via Admin Panel > Settings > External Tools
- **Dependency injection** — tools are instance classes with constructor-injected services
- **Structured logging** — Serilog with Console, File, and Seq sinks
- **Health endpoint** — quick reachability check at `/health`

### Available Tools

| Tool | Description |
|------|-------------|
| `generate_random_number` | Generates a random number between min and max (inclusive). Defaults to 1-100. |
| `get_temperatures` | Gets current temperature and humidity readings from all Netatmo weather stations and modules. Requires one-time OAuth2 setup. |

## Tech Stack

- **.NET 10** / C#
- **ModelContextProtocol.AspNetCore** v0.9.0-preview.1
- **Serilog** (Console, File, Seq)
- **xUnit v3** for testing

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.103 or later)
- [Seq](https://datalust.co/seq) (optional, for centralized log viewing)
- [Netatmo developer app](https://dev.netatmo.com/apps) (for the Netatmo Weather tool)

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
5. In the model's **Advanced Parameters**, set **Function Calling** to `Native`

> **Recommended models:** `qwen2.5`, `qwen3`, `mistral-nemo` — these have reliable tool calling support. `llama3.1` may hallucinate tool results instead of invoking them.

> **Important:** After restarting the server or changing tools, always **start a new chat**. Old chats cache stale tool definitions and may not call updated tools correctly.

For detailed step-by-step testing instructions, see [docs/LOCAL-TESTING.md](docs/LOCAL-TESTING.md).

## Netatmo Weather Setup

The `get_temperatures` tool reads temperature and humidity from your Netatmo weather stations. It requires a one-time OAuth2 setup.

### 1. Create a Netatmo App

1. Go to [dev.netatmo.com/apps](https://dev.netatmo.com/apps) and log in
2. Create a new app
3. Set the redirect URI to `http://localhost:5000/netatmo/callback`
4. Note your **Client ID** and **Client Secret**

### 2. Store Credentials

```bash
dotnet user-secrets set "Netatmo:ClientId" "YOUR_CLIENT_ID" --project src/MCPForOllama.Server
dotnet user-secrets set "Netatmo:ClientSecret" "YOUR_CLIENT_SECRET" --project src/MCPForOllama.Server
```

### 3. Authorize

1. Start the server: `dotnet run --project src/MCPForOllama.Server`
2. Open `http://localhost:5000/netatmo/auth` in your browser
3. Log in to Netatmo and grant access
4. You'll see a success message — tokens are stored in `netatmo-tokens.json`

Tokens refresh automatically. You only need to repeat this if the refresh token expires (typically after extended inactivity).

## Logging

The server uses [Serilog](https://serilog.net/) for structured logging with three sinks:

| Sink | Description |
|------|-------------|
| **Console** | Structured output with timestamp, level, service name, and source context |
| **File** | Daily rolling logs in `logs/mcpforollama-YYYYMMDD.log` with 7-day retention |
| **Seq** | Sends structured events to a [Seq](https://datalust.co/seq) server at `http://localhost:5341` |

All logging configuration is in `appsettings.json`. The Seq API key (if needed) is stored via .NET user secrets:

```bash
dotnet user-secrets set "Serilog:WriteTo:2:Args:apiKey" "YOUR_SEQ_API_KEY" --project src/MCPForOllama.Server
```

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
public class MyNewTool(ILogger<MyNewTool> logger)
{
    [McpServerTool, Description("Describe what this tool does.")]
    public string DoSomething(
        [Description("Describe the parameter.")] string input)
    {
        logger.LogInformation("DoSomething invoked with input={Input}", input);
        return $"Result: {input}";
    }
}
```

**Important:** Always return `string` from tool methods — OpenWebUI expects string results from MCP tools.

Then register it in `Program.cs`:

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<RandomNumberTool>()
    .WithTools<MyNewTool>();
```

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
│       ├── Models/
│       │   ├── NetatmoSettings.cs
│       │   ├── NetatmoTokens.cs
│       │   └── NetatmoStationData.cs
│       ├── Services/
│       │   ├── INetatmoTokenStore.cs
│       │   ├── FileNetatmoTokenStore.cs
│       │   ├── INetatmoApiService.cs
│       │   └── NetatmoApiService.cs
│       └── Tools/
│           ├── RandomNumberTool.cs
│           └── NetatmoWeatherTool.cs
└── tests/
    └── MCPForOllama.Server.Tests/
        ├── MCPForOllama.Server.Tests.csproj
        ├── Services/
        │   ├── FileNetatmoTokenStoreTests.cs
        │   └── NetatmoApiServiceTests.cs
        └── Tools/
            ├── RandomNumberToolTests.cs
            └── NetatmoWeatherToolTests.cs
```

## Author

- **Company:** Berrevoets Systems
- **Author:** Bert Berrevoets
- **Contact:** bert@berrevoets.net

## License

This project is for personal/educational use.

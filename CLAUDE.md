# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MCP (Model Context Protocol) server that exposes tools callable by Ollama models running on the local network. Built with .NET 10 and C# using Visual Studio 2026. Uses Streamable HTTP transport so that OpenWebUI can connect directly.

## Goals

- Create an MCP server providing tools for local Ollama models
- First tool: random number generator
- Additional tools to be added over time

## Tech Stack

- **Runtime:** .NET 10
- **Language:** C#
- **IDE:** Visual Studio 2026
- **MCP SDK:** ModelContextProtocol.AspNetCore v0.9.0-preview.1
- **Logging:** Serilog (Console, File, Seq sinks) — configured via `appsettings.json` + user secrets
- **Test Framework:** xUnit v3
- **Transport:** Streamable HTTP (network-accessible)

## Solution Structure

```
MCPForOllama/
├── MCPForOllama.slnx                       (solution file)
├── docs/                                   (documentation)
│   └── LOCAL-TESTING.md
├── src/
│   └── MCPForOllama.Server/                (ASP.NET Core MCP server)
│       ├── Program.cs                      (host setup, Serilog, MCP + OAuth endpoints)
│       ├── Models/                         (configuration and API response models)
│       │   ├── NetatmoSettings.cs
│       │   ├── NetatmoTokens.cs
│       │   └── NetatmoStationData.cs
│       ├── Services/                       (API clients and token management)
│       │   ├── INetatmoTokenStore.cs
│       │   ├── FileNetatmoTokenStore.cs
│       │   ├── INetatmoApiService.cs
│       │   └── NetatmoApiService.cs
│       └── Tools/                          (MCP tool classes, registered via DI)
│           ├── RandomNumberTool.cs
│           └── NetatmoWeatherTool.cs
└── tests/
    └── MCPForOllama.Server.Tests/          (xUnit v3 tests)
        ├── Services/
        │   ├── FileNetatmoTokenStoreTests.cs
        │   └── NetatmoApiServiceTests.cs
        └── Tools/
            ├── RandomNumberToolTests.cs
            └── NetatmoWeatherToolTests.cs
```

## Build & Run

```bash
dotnet build
dotnet run --project src/MCPForOllama.Server
dotnet test
```

## Key Endpoints

- `http://0.0.0.0:5000/mcp` — MCP Streamable HTTP endpoint (for OpenWebUI)
- `http://0.0.0.0:5000/health` — Health check
- `http://0.0.0.0:5000/netatmo/auth` — Starts Netatmo OAuth2 flow (one-time setup)
- `http://0.0.0.0:5000/netatmo/callback` — OAuth2 callback (receives authorization code)

## Logging

Structured logging via Serilog with three sinks:

- **Console** — structured output with service name and source context
- **File** — daily rolling logs in `logs/mcpforollama-YYYYMMDD.log`, 7-day retention
- **Seq** — sends events to `http://localhost:5341` (API key stored in user secrets)

Configuration lives in `appsettings.json`. Seq API key is stored via .NET user secrets:

```bash
dotnet user-secrets set "Serilog:WriteTo:2:Args:apiKey" "YOUR_SEQ_API_KEY" --project src/MCPForOllama.Server
```

## Netatmo Integration

The Netatmo Weather tool requires OAuth2 authentication with the Netatmo API.

### Setup

1. Register an app at [dev.netatmo.com](https://dev.netatmo.com/apps)
2. Set the redirect URI to `http://localhost:5000/netatmo/callback`
3. Store credentials via user secrets:

```bash
dotnet user-secrets set "Netatmo:ClientId" "YOUR_CLIENT_ID" --project src/MCPForOllama.Server
dotnet user-secrets set "Netatmo:ClientSecret" "YOUR_CLIENT_SECRET" --project src/MCPForOllama.Server
```

4. Start the server and navigate to `http://localhost:5000/netatmo/auth` in your browser
5. Authorize the app — tokens are stored in `netatmo-tokens.json` and refresh automatically

## OpenWebUI Integration

Tested and verified with OpenWebUI running in Docker. Use `host.docker.internal` instead of `localhost` when connecting from a Docker container. See `docs/LOCAL-TESTING.md` for full setup steps.

## Adding New Tools

1. Create a new class in `src/MCPForOllama.Server/Tools/`
2. Mark the class with `[McpServerToolType]`
3. Use a primary constructor to inject `ILogger<YourTool>` (and any other dependencies)
4. Mark public methods with `[McpServerTool]`
5. Add `[Description]` attributes to methods and parameters
6. **Return `string`** — OpenWebUI expects string results from MCP tools
7. Register the tool in `Program.cs` with `.WithTools<YourTool>()`

## OpenWebUI Model Configuration

- Set **Function Calling** to `Native` in the model's Advanced Parameters
- Use models with good tool support: `qwen2.5`, `qwen3`, `mistral-nemo`
- After server restart or tool changes, **start a new chat** — old chats cache stale tool definitions

## Author

- **Company:** Berrevoets Systems
- **Author:** Bert Berrevoets
- **Contact:** <bert@berrevoets.net>

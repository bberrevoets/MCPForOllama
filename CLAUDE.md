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
│       ├── Program.cs                      (host setup, Serilog, MCP + health endpoint)
│       └── Tools/                          (MCP tool classes, auto-discovered)
│           └── RandomNumberTool.cs
└── tests/
    └── MCPForOllama.Server.Tests/          (xUnit v3 tests)
        └── Tools/
            └── RandomNumberToolTests.cs
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

## Logging

Structured logging via Serilog with three sinks:

- **Console** — structured output with service name and source context
- **File** — daily rolling logs in `logs/mcpforollama-YYYYMMDD.log`, 7-day retention
- **Seq** — sends events to `http://localhost:5341` (API key stored in user secrets)

Configuration lives in `appsettings.json`. Seq API key is stored via .NET user secrets:

```bash
dotnet user-secrets set "Serilog:WriteTo:2:Args:apiKey" "YOUR_SEQ_API_KEY" --project src/MCPForOllama.Server
```

## OpenWebUI Integration

Tested and verified with OpenWebUI running in Docker. Use `host.docker.internal` instead of `localhost` when connecting from a Docker container. See `docs/LOCAL-TESTING.md` for full setup steps.

## Adding New Tools

1. Create a new class in `src/MCPForOllama.Server/Tools/`
2. Mark the class with `[McpServerToolType]`
3. Mark public static methods with `[McpServerTool]`
4. Add `[Description]` attributes to methods and parameters
5. Add structured logging using a property: `private static ILogger Logger => Log.ForContext(typeof(YourTool));`
6. The tool is auto-discovered at startup — no changes to `Program.cs` needed

## Author

- **Company:** Berrevoets Systems
- **Author:** Bert Berrevoets
- **Contact:** <bert@berrevoets.net>

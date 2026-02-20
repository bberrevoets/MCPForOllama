# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [0.1.0] - 2026-02-20

### Added

- MCP server with Streamable HTTP transport (`ModelContextProtocol.AspNetCore` v0.9.0-preview.1)
- `GenerateRandomNumber` tool — generates a random integer between min and max (inclusive)
- Health check endpoint at `/health`
- Kestrel configured to listen on `http://0.0.0.0:5000`
- xUnit v3 test project with 9 tests for RandomNumberTool
- .gitignore for .NET projects
- global.json pinning .NET SDK to 10.0.103
- Solution file (`MCPForOllama.slnx`)
- Local testing guide (`docs/LOCAL-TESTING.md`)
- Verified working with OpenWebUI (Docker) using `host.docker.internal`

## [0.0.1] - 2026-02-20

### Added

- Initial project scaffolding (CLAUDE.md, README.md)
- Repository setup with `main` and `FirstGo` branches

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

### Added
- Netatmo Historical Data tool (`get_historical_data`) — retrieves historical temperature and humidity for a specific module/room over a configurable time period (up to 30 days)
  - Automatic module name resolution (case-insensitive) with helpful error messages listing available modules
  - Auto-scale selection based on time range (30min/1hour/3hours/1day)
  - Formatted plain-text table output with min/max/avg summary
- `GetMeasureAsync` method on `INetatmoApiService` / `NetatmoApiService` for Netatmo `getmeasure` API
- `NetatmoMeasureData.cs` response model (`NetatmoMeasureApiResponse`, `NetatmoMeasureBody`)
- Device and module ID (`_id`) properties on `NetatmoDevice` and `NetatmoModule` models
- 25 new tests for historical data tool, measure API service, scale selection, and module resolution
- Netatmo Weather tool (`get_temperatures`) — reads temperature and humidity from all Netatmo weather stations and modules
- Netatmo OAuth2 authentication flow with `/netatmo/auth` and `/netatmo/callback` endpoints
- Token persistence via JSON file with automatic refresh
- `NetatmoApiService` with typed HttpClient for Netatmo API communication
- `FileNetatmoTokenStore` for thread-safe token storage with atomic writes
- Configuration models (`NetatmoSettings`, `NetatmoTokens`, `NetatmoStationData`)
- 15 new tests for Netatmo tool, API service, and token store
- Structured logging with Serilog (Console, File, and Seq sinks)
- Bootstrap logger for capturing startup errors
- Automatic HTTP request logging via `UseSerilogRequestLogging()`
- Structured log messages in `RandomNumberTool` (invocation, validation, result)
- Daily rolling file logs in `logs/` with 7-day retention
- Seq integration for centralized log viewing
- User secrets support for storing Seq API key

### Changed
- Tools are now instance classes with constructor-injected `ILogger<T>` via DI (instead of static classes)
- Tool registration uses explicit `.WithTools<T>()` instead of `.WithToolsFromAssembly()`
- `GenerateRandomNumber` returns `string` instead of `int` for OpenWebUI compatibility

### Removed
- `appsettings.Development.json` — all config now in `appsettings.json` + user secrets

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

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MCP (Model Context Protocol) server that exposes tools callable by Ollama models running locally. Built with .NET 10 and C# using Visual Studio 2026.

## Goals

- Create an MCP server providing tools for local Ollama models
- First tool: random number generator
- Additional tools to be added over time

## Tech Stack

- **Runtime:** .NET 10
- **Language:** C#
- **IDE:** Visual Studio 2026

## Build & Run (once project scaffolding exists)

```bash
dotnet build
dotnet run
dotnet test
```

## Author

- **Company:** Berrevoets Systems
- **Author:** Bert Berrevoets
- **Contact:** <bert@berrevoets.net>

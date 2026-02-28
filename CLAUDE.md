# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build entire solution
dotnet build FitCycle.sln

# Run the API (HTTP: localhost:5294, HTTPS: localhost:7103)
dotnet run --project src/FitCycle.Api

# Run the MAUI app (Windows)
dotnet build src/FitCycle.App -f net8.0-windows10.0.19041.0

# Run all tests
dotnet test FitCycle.sln

# Run a single test project
dotnet test tests/FitCycle.Core.Tests
dotnet test tests/FitCycle.App.Tests

# Run a specific test by name
dotnet test tests/FitCycle.Core.Tests --filter "FullyQualifiedName~TestMethodName"
```

## Architecture

This is a .NET 8.0 solution with a layered architecture and a cross-platform MAUI client.

**Projects:**
- **FitCycle.Core** (`src/FitCycle.Core`) — Domain models and business logic. Uses FluentValidation.
- **FitCycle.Infrastructure** (`src/FitCycle.Infrastructure`) — Data access and external service integrations.
- **FitCycle.Api** (`src/FitCycle.Api`) — ASP.NET Core minimal API backend. Swagger UI enabled in development at `/`. Endpoints: `/weatherforecast`, `/health`, `/version`.
- **FitCycle.App** (`src/FitCycle.App`) — .NET MAUI cross-platform app (Android, iOS, macOS, Windows). XAML-based UI with Shell navigation.

**Test projects** (`tests/`) use xUnit with coverlet for code coverage.

## Key Patterns

- **Minimal APIs** in `Program.cs` — no controllers, endpoints defined inline.
- **Platform-specific code** lives under `src/FitCycle.App/Platforms/{Android,iOS,MacCatalyst,Windows,Tizen}`.
- **API base URL** differs by platform: Android emulator uses `http://10.0.2.2:5294`, others use `http://localhost:5294`. Configured in `MauiProgram.cs`.
- **Services** (e.g., `WeatherService`) are registered via DI in `MauiProgram.cs` and injected into pages.
- **Styles** use XAML resource dictionaries in `Resources/Styles/` with light/dark theme support. Primary color: `#512BD4`.

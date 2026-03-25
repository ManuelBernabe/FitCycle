# CLAUDE.md

## Build & Run

```bash
dotnet build FitCycle.sln                    # Build all
dotnet run --project src/FitCycle.Api         # Run API (HTTP:5294, HTTPS:7103)
dotnet test FitCycle.sln                     # Run all tests
dotnet test tests/FitCycle.Core.Tests --filter "FullyQualifiedName~TestName"  # Single test
```

## Architecture

.NET 8.0 solution, layered architecture:

- **FitCycle.Core** — Domain models, FluentValidation
- **FitCycle.Infrastructure** — EF Core (SQLite), services, repositories
- **FitCycle.Api** — ASP.NET Core minimal API + vanilla JS SPA frontend (`wwwroot/`)
- **FitCycle.App** — .NET MAUI cross-platform app (not actively used)

Tests: xUnit + coverlet (`tests/`)

## Workflow

- Push to `develop` first, wait for user approval, then merge to `main` for production (Railway)
- Always check if user manual (`tutorial.js`) needs updating when adding features
- C# and frontend conventions are in `.claude/rules/`
- Use `/cache-bump` after modifying frontend files
- Use `/database-migration MigrationName` for schema changes
- Use `/deploy` to commit and push

# CLAUDE.md

## Build Commands

From the Build directory (`src/Build/`):
```bash
cd src/Build
dotnet run -- Build     # Builds all projects for all frameworks
dotnet run -- Test      # Runs all tests for all frameworks
dotnet run -- Pack      # Creates NuGet packages
```

## Publishing to NuGet

Releases are published by the "Publish to NuGet" GitHub Actions workflow
(`.github/workflows/publish.yml`) using NuGet Trusted Publishing (OIDC) — there is no
stored API key, and the local Build project's `Publish` target is not the release path.
Trigger it manually after bumping versions: Actions tab → Publish to NuGet → Run workflow,
or `gh workflow run "Publish to NuGet"`.

Note: the generated `AdventureWorks*.fs` test schemas embed the CLI version in their
headers, so any `<Version>` bump requires `dotnet run -- Regen` (with the docker test
databases running) or CI's regen guard will fail.

For specific framework testing:
```bash
dotnet run -- TestNet8  # Test on .NET 8.0
dotnet run -- TestNet9  # Test on .NET 9.0
```

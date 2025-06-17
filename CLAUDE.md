# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

From the Build directory (`src/Build/`):
```bash
cd src/Build
dotnet run -- Build     # Builds all projects for all frameworks
dotnet run -- Test      # Runs all tests for all frameworks
dotnet run -- Pack      # Creates NuGet packages
dotnet run -- Publish   # Publishes to NuGet (requires SQLHYDRA_NUGET_KEY env var)
```

For specific framework testing:
```bash
dotnet run -- TestNet8  # Test on .NET 8.0
dotnet run -- TestNet9  # Test on .NET 9.0
```

## High-Level Architecture

SqlHydra consists of two main NuGet packages:

### SqlHydra.Cli
- **Purpose**: Code generation tool that creates F# types from database schemas
- **Supported Databases**: SQL Server, PostgreSQL, Oracle, SQLite, MySQL
- **Key Features**:
  - Generates F# record types from database tables
  - Creates strongly-typed `HydraReader` for efficient data reading
  - Supports nullable columns as F# Options
  - Handles database-specific types (e.g., Postgres enums, arrays)

### SqlHydra.Query
- **Purpose**: Type-safe LINQ-style query builder using F# computation expressions
- **Dependencies**: Uses SqlKata internally for SQL generation
- **Query Types**: `selectTask`, `selectAsync`, `insertTask`, `updateTask`, `deleteTask`
- **Key Design**: Computation expressions provide compile-time type safety over SqlKata's runtime query building

## Project Structure

```
src/
├── SqlHydra.Cli/       # CLI tool with provider-specific implementations
│   ├── SqlServer/      # SQL Server schema provider
│   ├── Npgsql/         # PostgreSQL schema provider
│   ├── Oracle/         # Oracle schema provider
│   ├── Sqlite/         # SQLite schema provider
│   └── MySql/          # MySQL schema provider
├── SqlHydra.Query/     # Query builder library
├── SqlHydra.Domain/    # Shared domain types
├── Tests/              # Comprehensive test suite
└── Build/              # FAKE build automation
```

## Development Workflow

1. **Configuration**: Uses TOML files for code generation configuration
2. **Multi-targeting**: Targets .NET 8.0 and .NET 9.0 (Query also targets netstandard2.0)
3. **Testing**: Comprehensive tests using NUnit and Verify for snapshot testing
4. **Dev Environment**: Supports VS Code Remote Containers with Docker Compose for test databases

## Key Design Decisions

- **Type Safety First**: All query operations are strongly typed at compile time
- **Computation Expressions**: F# CEs provide intuitive query syntax while maintaining type safety
- **Generated Code Philosophy**: Generated files are marked as `Visible="false"` to reduce project clutter
- **SqlKata Integration**: Leverages SqlKata's cross-database SQL generation capabilities
- **Conditional Query Building**: v3.0 added support for conditional where/orderBy clauses using `&&` and `^^` operators
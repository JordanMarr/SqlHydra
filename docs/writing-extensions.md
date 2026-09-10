# Writing SqlHydra Extensions

SqlHydra can be extended in three ways without changing the tool: a whole new database
provider, a type mapping for a column the built-in mappings do not cover, and a column the
database catalog never reports. Each is a plain interface from `SqlHydra.Domain` that you
implement in your own project and register in the TOML `[extensions]` section.

## Creating a Custom Database Provider

SqlHydra supports 5 built-in database providers (SQL Server, PostgreSQL, SQLite, MySQL, Oracle), but you can add support for any database by implementing the `ISqlHydraDbProvider` interface from `SqlHydra.Domain`.

### Implementing the Provider

Create a library project that references `SqlHydra.Domain` and implements `ISqlHydraDbProvider`:

```fsharp
open SqlHydra.Domain

type DuckDbProvider() =
    interface ISqlHydraDbProvider with
        member _.Id = "duckdb"
        member _.Name = "SqlHydra.DuckDB"
        member _.Type = Custom "DuckDb"
        member _.DefaultReaderType = "System.Data.Common.DbDataReader"
        member _.DefaultProvider = "DuckDB.NET.Data"
        member _.SqlEmitter = "MyApp.DuckDbEmitter()"
        member _.ProviderConnectionType = "DuckDB.NET.Data.DuckDBConnection"
        member _.GetSchema(cfg, isLegacy, extensions) =
            // Query database metadata and return a Schema
            // with Tables, Columns, and type mappings
            ...
```

The `GetSchema` method is the core of your provider -- it connects to the database using `cfg.ConnectionString`, reads schema metadata (tables, columns, types), applies any `IExtendTypeMapping` extensions, and returns a `Schema` record that SqlHydra uses to generate F# types.

The `SqlEmitter` property should be the fully-qualified constructor expression for your `ISqlEmitter` implementation (used in the generated `QueryContextFactory`).

### Running with a Custom Provider

Add your provider project as a `ProjectReference` (or publish it as a NuGet package and add a `PackageReference`), build your project, then run:

```bash
dotnet sqlhydra custom SqlHydra.Query.DuckDB --toml-file sqlhydra-duckdb.toml
```

SqlHydra will load the named assembly from the project's build output and discover the `ISqlHydraDbProvider` implementation automatically.

## Overriding Database Type Mappings

SqlHydra supports type mapping extensions via the `IExtendTypeMapping` interface in `SqlHydra.Domain`. This lets you add custom database-to-CLR type mappings that SqlHydra doesn't handle out of the box.

### Implementing a Type Mapping Extension

Add a class implementing `IExtendTypeMapping` in your project (or in a separate library):

```fsharp
open SqlHydra.Domain

type MyCustomMapping() =
    interface IExtendTypeMapping with
        member _.Extend(baseTryFind) =
            fun (ctx: TypeMappingContext) ->
                match ctx.Column.ProviderTypeName.ToLower() with
                | "vector" ->
                    Some {
                        TypeMapping.ColumnTypeAlias = "vector"
                        TypeMapping.ClrType = "Pgvector.Vector"
                        TypeMapping.DbType = System.Data.DbType.Object
                        // No NpgsqlDbType for vector -- Pgvector.Npgsql infers it from the value.
                        TypeMapping.ProviderDbType = None
                    }
                | _ -> baseTryFind ctx
```

Your extension wraps the built-in type mapping function, giving you a chance to handle custom types before falling back to the default behavior.

### Registering the Extension

Type mapping extensions must be explicitly registered in your TOML configuration. The name should match your project name, `PackageReference`, or `ProjectReference`:

```toml
[extensions]
type_mappings = ["MyProject"]
```

This gives you control over which providers use which extensions. For example, if you only want a custom mapping applied to SQLite, add it to `sqlhydra-sqlite.toml` but not to `sqlhydra-mssql.toml`.

> **Note:** Make sure your project is built before running `sqlhydra` so the extension assembly can be found.

### The TypeMappingContext

Your extension receives a `TypeMappingContext` with full schema metadata for the column being mapped:

```fsharp
type TypeMappingContext =
    {
        Table: TableSchema   // Table catalog, schema, name, type, and all columns
        Column: ColumnSchema  // Column name, type, nullability, precision, scale, etc.
    }
```

This lets you make mapping decisions based on the table name, column name, schema, or any other metadata -- not just the provider type name.

### NuGet Extension Packages

Type mapping extensions can also be published as NuGet packages. Add it as a `PackageReference` in your project and register it in your TOML configuration:

```toml
[extensions]
type_mappings = ["SqlHydra.Query.Pgvector"]
```

SqlHydra will resolve the assembly from your project's build output and load any `IExtendTypeMapping` implementations it finds.

[**SqlHydra.Query.Pgvector**](https://github.com/michaelglass/SqlHydra.Query.Pgvector) is a worked example of such a package: it maps the PostgreSQL `vector` column type to `Pgvector.Vector` and adds pgvector distance operators (`<=>`, `<->`, `<#>`) for `SqlHydra.Query`.

### Multiple Extensions

Multiple extensions compose in order -- each wraps the previous one. An extension should call `baseTryFind ctx` for any types it doesn't handle, allowing the next extension (or the built-in mappings) to take over.

## Contributing Columns the Catalog Does Not List

`IExtendTypeMapping` retypes a column that was *discovered*. Some columns are never discovered:
a PostgreSQL system column such as `xmin` is not in `information_schema`, so no type mapping is
ever consulted for it and it cannot appear in the generated record at all.

`IContributeColumns` fills that gap. It runs once over the finished schema -- after discovery and
type mapping, before emission -- and returns the columns to append to a table:

```fsharp
open SqlHydra.Domain

type XminColumn() =
    interface IContributeColumns with
        member _.Contribute(baseFn) =
            fun (ctx: ColumnContributionContext) ->
                let contributed = baseFn ctx

                // A system column exists on base tables, on PostgreSQL only.
                if ctx.Provider = ProviderType.Npgsql && ctx.Table.Type = TableType.Table then
                    contributed @ [
                        {
                            Column.Name = "xmin"
                            Column.TypeMapping =
                                {
                                    ClrType = "uint"
                                    DbType = System.Data.DbType.UInt32
                                    // Npgsql has no default mapping for uint32; without this,
                                    // binding the column as a parameter throws client-side.
                                    ProviderDbType = Some "Xid"
                                    ColumnTypeAlias = "xid"
                                }
                            Column.IsNullable = false
                            Column.IsPK = false
                            Column.Doc =
                                [ "PostgreSQL's row version: the id of the transaction that"
                                  "inserted this row version." ]
                        }
                    ]
                else
                    contributed
```

Register it exactly like a type-mapping extension, in the TOML `[extensions]` section.

A contributed column is an ordinary one from there on: its `ProviderDbType` becomes a
`[<ProviderDbType(...)>]` attribute and `IExtendNaming` renames it like any other. Contributing a
name the table already has raises, rather than shadowing the discovered column.

### Documenting a Contributed Column

`Column.Doc` is emitted as `///` lines above the generated field. A caution that lives only in an
extension's README reaches whoever configured the extension and nobody else; on the field it
reaches whoever reaches for the column.

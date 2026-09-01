module SqlHydra.SchemaTemplate

open Domain

let backticks = Fantomas.FCS.Syntax.PrettyNaming.NormalizeIdentifierBackticks
let newLine = "\n"

let versionModule (cfg: Config) (v: Version.InformationalVersion) = $"""
module Version =
    let cli = System.Version({v.Version.Major}, {v.Version.Minor}, {v.Version.Build})
    let ns = "%s{cfg.Namespace}"
    SqlHydra.Query.VersionCheck.assertIsCompatible cli ns
"""

let mkEnum db schema enum = stringBuffer {
    let enumType =
        db.Enums
        |> List.find (fun e -> e.Schema = schema && e.Name = enum)

    let labels =
        enumType.Labels
        |> List.sortBy _.SortOrder

    $"type {backticks enumType.Name} ="
    indent {
        for label in labels do
            $"| {backticks label.Name} = {label.SortOrder}"
    }
}

/// Emits an additive helper that registers all generated PostgreSQL enum types with an
/// Npgsql.NpgsqlDataSourceBuilder. Emits output only for the Npgsql provider when the schema
/// contains at least one enum; otherwise emits nothing.
let mkEnumRegistration (cfg: Config) (provider: ISqlHydraDbProvider) (db: Schema) = stringBuffer {
    if provider.Type = ProviderType.Npgsql && not db.Enums.IsEmpty then
        let enums =
            db.Enums
            |> List.sortBy (fun e -> e.Schema, e.Name)

        let mapEnumLines =
            enums
            |> List.map (fun e ->
                let typeArg = $"{backticks e.Schema}.{backticks e.Name}"
                let pgName = if e.Schema = "public" then e.Name else $"{e.Schema}.{e.Name}"
                $"        builder.MapEnum<{typeArg}>(\"{pgName}\") |> ignore")
            |> String.concat newLine

        $"""
[<RequireQualifiedAccess>]
module Enums =
    /// Registers all generated PostgreSQL enum types with the data source builder.
    let register (builder: Npgsql.NpgsqlDataSourceBuilder) : Npgsql.NpgsqlDataSourceBuilder =
{mapEnumLines}
        builder
    """
}

/// Where a record sits in a `type ... and ...` group.
type RecordDeclaration =
    | OpensGroup
    | ContinuesGroup

/// The CLR type of a column, with array type names normalized:
/// "byte[]", "string[]", "int[]", "int []", "int array" → "byte []", etc.
let clrBaseType (col: Column) =
    if col.TypeMapping.ClrType.EndsWith "[]" || col.TypeMapping.ClrType.EndsWith "array" then
        let baseTypeNm = col.TypeMapping.ClrType.Split([| "[]"; " []"; " array" |], System.StringSplitOptions.RemoveEmptyEntries) |> Array.head
        $"{baseTypeNm} []"
    else
        col.TypeMapping.ClrType

/// `[<ProviderDbType(...)>]` for a column, when the schema declared one and the config wants them.
let providerDbTypeAttribute (cfg: Config) (col: Column) =
    match col.TypeMapping.ProviderDbType with
    | Some providerDbType when cfg.ProviderDbTypeAttributes ->
        Some $"[<ProviderDbType(\"{providerDbType}\")>]"
    | _ ->
        None

let mkTable cfg db (table: Table) schema tableName columnName = stringBuffer {
    let tableType =
        db.Tables
        |> List.find (fun t -> t.Schema = schema && t.Name = table.Name)

    let columnPropertyType (col: Column) =
        let baseType = clrBaseType col

        if col.IsNullable then
            match cfg.NullablePropertyType with
            | NullablePropertyType.Option ->
                $"Option<{baseType}>"
            | NullablePropertyType.Nullable ->
                if col.TypeMapping.IsValueType()
                then $"System.Nullable<{baseType}>"
                else baseType
        else
            baseType

    let providerDbTypeAttribute = providerDbTypeAttribute cfg

    let rawFieldName (col: Column) = columnName { NamingContext.Table = table; Column = Some col }
    let fieldName (col: Column) = backticks (rawFieldName col)

    /// `IWriteColumns.WriteColumns` for `columns`: what the query layer binds instead of reflecting.
    let writeColumnsMember (columns: Column list) = stringBuffer {
        "member this.WriteColumns ="
        indent {
            "["
            indent {
                for col in columns do
                    let providerDbType =
                        match col.TypeMapping.ProviderDbType with
                        | Some providerDbType when cfg.ProviderDbTypeAttributes -> $"Some \"{providerDbType}\""
                        | _ -> "None"
                    $"{{ WriteColumn.Name = \"{rawFieldName col}\"; Value = box this.{fieldName col}; ProviderDbType = {providerDbType} }}"
            }
            "]"
        }
    }

    /// One record of a `type ... and ...` group: the read record opens it and the write record
    /// continues it, so each may name the other.
    let mkRecord (declaration: RecordDeclaration) (typeName: string) (columns: Column list) (members: string option) = stringBuffer {
        match declaration, cfg.IsCLIMutable with
        | OpensGroup, true ->
            "[<CLIMutable>]"
            $"type {backticks typeName} ="
        | OpensGroup, false -> $"type {backticks typeName} ="
        | ContinuesGroup, true -> $"and [<CLIMutable>] {backticks typeName} ="
        | ContinuesGroup, false -> $"and {backticks typeName} ="
        indent {
            "{"
            indent {
                for col in columns do
                    // Before the attributes, so the doc comment binds to the field.
                    for line in col.Doc do
                        $"/// {line}"
                    match providerDbTypeAttribute col with
                    | Some attribute -> attribute
                    | None -> ()
                    $"""{if cfg.IsMutableProperties then "mutable " else ""}{fieldName col}: {columnPropertyType col}"""
            }
            "}"
            members
        }
    }

    let tblName = tableName { NamingContext.Table = table; Column = None }
    let writeName = $"{tblName}_write"

    let writableColumns, readOnlyColumns =
        tableType.Columns |> List.partition (fun col -> not col.IsReadOnly)

    // Only a table with something to hide needs a write record, and a record cannot be empty.
    let hasWriteRecord = not readOnlyColumns.IsEmpty && not writableColumns.IsEmpty

    let readMembers = stringBuffer {
        if hasWriteRecord then
            $"interface IHasWrite<{backticks writeName}> with"
            indent {
                "/// This row's writable columns, as `writeEntity` takes them: `toWrite row`."
                $"member this.ToWrite() : {backticks writeName} ="
                indent {
                    "{"
                    indent {
                        for col in writableColumns do
                            $"{fieldName col} = this.{fieldName col}"
                    }
                    "}"
                }
            }
        "interface IWriteColumns with"
        indent { writeColumnsMember writableColumns }
    }

    mkRecord OpensGroup tblName tableType.Columns (Some readMembers)

    if hasWriteRecord then
        ""
        $"/// The columns of `{tblName}` a caller may write; the database owns the rest."
        let writeMembers = stringBuffer {
            $"interface IWriteOf<{backticks tblName}> with"
            indent { writeColumnsMember writableColumns }
        }
        mkRecord ContinuesGroup writeName writableColumns (Some writeMembers)
}

/// Emits the per-schema `LeftJoined` module: for each table, a left-view record — the same
/// record with every column in its nullable form — implementing `ILeftViewOf<base>`, and (with
/// table_declarations) a `leftTable` token for use as a `leftJoin` source.
/// `ToOption()` is emitted separately in `mkLeftViewExtensions`, after the schema modules close.
/// Only generated for `NullablePropertyType.Option` configs.
let mkLeftJoinedViews (cfg: Config) (tables: Table list) tableName columnName = stringBuffer {
    "/// Left-views for `leftJoin`: each table record with every column in its nullable form,"
    "/// so an unmatched row reads as None per column instead of one Option around the record."
    "module LeftJoined ="
    indent {
        for table in tables do
            let rawFieldName (col: Column) = columnName { NamingContext.Table = table; Column = Some col }
            let fieldName (col: Column) = backticks (rawFieldName col)
            let rawTblName = tableName { NamingContext.Table = table; Column = None }
            let tblName = backticks rawTblName
            // The base record, aliased before the view of the same name shadows it. The enclosing
            // schema module cannot be referenced by its qualified path from within itself.
            let baseAlias = backticks $"{rawTblName} (base)"

            $"type private {baseAlias} = {tblName}"
            ""
            // NoEquality/NoComparison: the views are transient query artifacts users never
            // construct; comparisons go through columns — and it halves their compile cost.
            if cfg.IsCLIMutable
            then "[<CLIMutable; NoEquality; NoComparison>]"
            else "[<NoEquality; NoComparison>]"
            $"type {tblName} ="
            indent {
                "{"
                indent {
                    for col in table.Columns do
                        match providerDbTypeAttribute cfg col with
                        | Some attribute -> attribute
                        | None -> ()
                        // An already-nullable column stays a single Option: a NULL from a
                        // missed join is indistinguishable from a stored NULL, as in SQL.
                        $"""{if cfg.IsMutableProperties then "mutable " else ""}{fieldName col}: Option<{clrBaseType col}>"""
                }
                "}"
                $"interface ILeftViewOf<{baseAlias}>"
            }
            ""

            if cfg.TableDeclarations then
                $"let {tblName} = leftTable<{baseAlias}, {tblName}>"
                ""
    }
}

/// Emits `[<AutoOpen>] module LeftViewExtensions`: one `ToOption()` extension member per left-view,
/// declared after the schema modules close. Inside `LeftJoined` the view shadows its base record's
/// name (and F# forbids referencing the enclosing module by qualified path from within itself), so
/// an inline `ToOption` could only name the base through a `(base)` alias — out here both types are
/// addressable by their real paths and the signature reads `Schema.Table option`, as users expect.
/// AutoOpen activates when the generated namespace is opened, which query code already requires.
let mkLeftViewExtensions (tablesBySchema: (string * Table list) list) tableName columnName = stringBuffer {
    // The match witness may be any NOT NULL column: a missed join makes every column None; a
    // matched row makes every NOT NULL column Some. Prefer the PK. A table whose columns are all
    // nullable gets no ToOption: a row of all NULLs is indistinguishable from a missed join, in
    // SQL itself.
    let witnessOf (table: Table) =
        table.Columns
        |> List.tryFind (fun col -> col.IsPK && not col.IsNullable)
        |> Option.orElseWith (fun () -> table.Columns |> List.tryFind (fun col -> not col.IsNullable))

    let eligible =
        tablesBySchema
        |> List.collect (fun (schema, tables) ->
            tables |> List.choose (fun table -> witnessOf table |> Option.map (fun w -> schema, table, w)))

    if not eligible.IsEmpty then
        "[<AutoOpen>]"
        "module LeftViewExtensions ="
        indent {
            for (schema, table, w) in eligible do
                let rawFieldName (col: Column) = columnName { NamingContext.Table = table; Column = Some col }
                let fieldName (col: Column) = backticks (rawFieldName col)
                let tblName = backticks (tableName { NamingContext.Table = table; Column = None })
                let baseType = $"{backticks schema}.{tblName}"
                let viewType = $"{backticks schema}.LeftJoined.{tblName}"

                $"type {viewType} with"
                indent {
                    "/// Recovers the whole-record option after materialization (pure .NET, not SQL):"
                    "/// Some when the left join matched, None when it did not."
                    $"member this.ToOption() : {baseType} option ="
                    indent {
                        $"match this.{fieldName w} with"
                        "| Some value ->"
                        indent {
                            $"let record : {baseType} ="
                            indent {
                                "{"
                                indent {
                                    for col in table.Columns do
                                        if col.Name = w.Name then $"{fieldName col} = value"
                                        elif col.IsNullable then $"{fieldName col} = this.{fieldName col}"
                                        else $"{fieldName col} = this.{fieldName col}.Value"
                                }
                                "}"
                            }
                            "Some record"
                        }
                        "| None -> None"
                    }
                }
                ""
        }
}

let generate (cfg: Config) (provider: ISqlHydraDbProvider) (db: Schema) (version: Version.InformationalVersion) (namingExtensions: IExtendNaming list) = stringBuffer {
    let tableName =
        let baseFn (ctx: NamingContext) = ctx.Table.Name
        namingExtensions |> List.fold (fun acc ext -> ext.ExtendTableName acc) baseFn

    let columnName =
        let baseFn (ctx: NamingContext) = ctx.Column.Value.Name
        namingExtensions |> List.fold (fun acc ext -> ext.ExtendColumnName acc) baseFn

    let filteredTables =
        db.Tables
        |> List.sortBy (fun tbl -> tbl.Schema, tbl.Name)

    let schemas =
        let enumSchemas = db.Enums |> List.map (fun e -> e.Schema)
        let tableSchemas = filteredTables |> List.map (fun t -> t.Schema)
        enumSchemas @ tableSchemas |> List.distinct

    $$"""
// This code was generated by `{{provider.Name}}` -- v%%s{{version.InformationalVersion}}.
namespace {{cfg.Namespace}}
    """

    "open SqlHydra"
    "open SqlHydra.Query"

    versionModule cfg version

    for schema in schemas do
        $"module {backticks schema} ="

        let enums =
            db.Enums
            |> List.filter (fun e -> e.Schema = schema)
            |> List.map _.Name

        indent {
            for enum in enums do
                mkEnum db schema enum
                newLine
        }

        let tables =
            filteredTables
            |> List.filter (fun t -> t.Schema = schema)

        indent {
            for table in tables do
                mkTable cfg db table schema tableName columnName
                newLine

                if cfg.TableDeclarations then
                    let tblName = tableName { NamingContext.Table = table; Column = None }
                    $"let {backticks tblName} = table<{backticks tblName}>"
                    newLine

            // Left-views are Option-shaped by design; Nullable-typed configs skip them.
            if cfg.LeftJoinedViews && cfg.NullablePropertyType = NullablePropertyType.Option && not tables.IsEmpty then
                mkLeftJoinedViews cfg tables tableName columnName
                newLine
        }

    // The left-views' ToOption() lives outside the schema modules: see mkLeftViewExtensions.
    if cfg.LeftJoinedViews && cfg.NullablePropertyType = NullablePropertyType.Option then
        let tablesBySchema =
            schemas
            |> List.map (fun schema -> schema, filteredTables |> List.filter (fun t -> t.Schema = schema))
            |> List.filter (fun (_, tables) -> not tables.IsEmpty)

        mkLeftViewExtensions tablesBySchema tableName columnName
        newLine

    // If the user configures ProviderDbTypeAttributes, we know they are using SqlHydra.Query.
    if cfg.ProviderDbTypeAttributes then
        // Emit an additive enum-registration helper (Npgsql only, when enums exist).
        // Placed after the per-schema modules so the generated enum types are in scope,
        // and before the factory so it can register enums on the data source it builds.
        mkEnumRegistration cfg provider db

        let emitter = provider.SqlEmitter
        let connectionType = provider.ProviderConnectionType

        if provider.Type = ProviderType.Npgsql then
            // When enums were generated, the factory builds its data source through
            // Enums.register so the generated enum types work without any manual MapEnum calls.
            let createDataSource =
                if db.Enums.IsEmpty then
                    "Npgsql.NpgsqlDataSource.Create(connectionString)"
                else
                    "(Npgsql.NpgsqlDataSourceBuilder(connectionString) |> Enums.register).Build()"

            $"""
type QueryContextFactory =
    {{
        OpenContext: unit -> QueryContext
        OpenContextAsync: unit -> System.Threading.Tasks.Task<QueryContext>
        /// Disposes the NpgsqlDataSource when the factory created it from a connection string; a no-op when the caller supplied their own.
        Dispose: unit -> unit
    }}
    interface System.IDisposable with
        member this.Dispose() = this.Dispose()
    interface IQueryContextFactory with
        member this.OpenContextAsync() = this.OpenContextAsync()
    static member Create(connectionString: string, ?sqlLogger) =
        // The factory creates this data source, so it owns and disposes it.
        let dataSource = {createDataSource}
        QueryContextFactory.CreateInternal(dataSource, (fun () -> dataSource.Dispose()), ?sqlLogger = sqlLogger)
    static member Create(dataSource: Npgsql.NpgsqlDataSource, ?sqlLogger) =
        // The caller supplied this data source, so the caller owns its lifetime.
        QueryContextFactory.CreateInternal(dataSource, ignore, ?sqlLogger = sqlLogger)
    static member private CreateInternal(dataSource: Npgsql.NpgsqlDataSource, dispose: unit -> unit, ?sqlLogger) =
        let emitter = {emitter}

        let createConn () : System.Data.Common.DbConnection =
            dataSource.OpenConnection()

        let openContext () =
            let conn = createConn ()
            let ctx = new QueryContext(conn, emitter)
            sqlLogger |> Option.iter (fun logger -> ctx.Logger <- logger)
            ctx

        let openContextAsync () =
            task {{
                let! conn = dataSource.OpenConnectionAsync()
                let ctx = new QueryContext(conn, emitter)
                sqlLogger |> Option.iter (fun logger -> ctx.Logger <- logger)
                return ctx
            }}

        {{
            OpenContext = openContext
            OpenContextAsync = openContextAsync
            Dispose = dispose
        }}
    """
        else
            $"""
type QueryContextFactory =
    {{
        OpenContext: unit -> QueryContext
        OpenContextAsync: unit -> System.Threading.Tasks.Task<QueryContext>
    }}
    // This provider holds no factory-level resources; each connection is owned and disposed by its QueryContext.
    member _.Dispose() = ()
    interface System.IDisposable with
        member this.Dispose() = this.Dispose()
    interface IQueryContextFactory with
        member this.OpenContextAsync() = this.OpenContextAsync()
    static member Create(connectionString: string, ?sqlLogger) =
        let emitter = {emitter}

        let createConn () : System.Data.Common.DbConnection =
            new {connectionType}(connectionString)

        let openContext () =
            let conn = createConn ()
            conn.Open()
            let ctx = new QueryContext(conn, emitter)
            sqlLogger |> Option.iter (fun logger -> ctx.Logger <- logger)
            ctx

        let openContextAsync () =
            task {{
                let conn = createConn ()
                do! conn.OpenAsync()
                let ctx = new QueryContext(conn, emitter)
                sqlLogger |> Option.iter (fun logger -> ctx.Logger <- logger)
                return ctx
            }}

        {{
            OpenContext = openContext
            OpenContextAsync = openContextAsync
        }}
    """

}

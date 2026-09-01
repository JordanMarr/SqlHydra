module UnitTests.ContributeColumns

open System
open NUnit.Framework
open Swensen.Unquote
open SqlHydra
open SqlHydra.Domain

// ---------------------------------------------------------------------------------------
// Fixtures: a discovered schema, and extensions that contribute to it.
// ---------------------------------------------------------------------------------------

let private mapping clrType dbType providerDbType alias =
    {
        TypeMapping.ClrType = clrType
        TypeMapping.DbType = dbType
        TypeMapping.ProviderDbType = providerDbType
        TypeMapping.ColumnTypeAlias = alias
    }

let private discovered name =
    {
        Column.Name = name
        Column.TypeMapping = mapping "int" Data.DbType.Int32 None "int4"
        Column.IsNullable = false
        Column.IsPK = name = "id"
        Column.IsReadOnly = false
        Column.Doc = []
    }

let private mkTable schema name tableType columns =
    {
        Table.Catalog = ""
        Table.Schema = schema
        Table.Name = name
        Table.Type = tableType
        Table.Columns = columns
        Table.TotalColumns = List.length columns
    }

let private usersTable = mkTable "public" "users" TableType.Table [ discovered "id"; discovered "age" ]
let private activeUsersView = mkTable "public" "active_users" TableType.View [ discovered "id" ]

let private schema: Schema =
    {
        Tables = [ usersTable; activeUsersView ]
        Enums = []
    }

/// The column PgSystemColumns needs and cannot currently produce: it is not in
/// `information_schema`, so no type mapping is ever consulted for it, and `uint32` with no
/// `NpgsqlDbType` throws client-side on a compare-and-swap.
let private xminColumn =
    {
        Column.Name = "xmin"
        Column.TypeMapping = mapping "uint" Data.DbType.UInt32 (Some "Xid") "xid"
        Column.IsNullable = false
        Column.IsPK = false
        Column.IsReadOnly = false
        Column.Doc =
            [ "The id of the transaction that inserted this row version — PostgreSQL's row version."
              "It changes on every write to the row." ]
    }

/// Contributes `xmin` to PostgreSQL base tables only — a view has no system columns.
type XminContribution() =
    interface IContributeColumns with
        member _.Contribute(baseFn) =
            fun (ctx: ColumnContributionContext) ->
                let contributed = baseFn ctx

                if ctx.Provider = ProviderType.Npgsql && ctx.Table.Type = TableType.Table then
                    contributed @ [ xminColumn ]
                else
                    contributed

/// A second extension, to pin down composition order and that each sees the running list.
type CtidContribution() =
    interface IContributeColumns with
        member _.Contribute(baseFn) =
            fun ctx ->
                baseFn ctx
                @ [ { xminColumn with
                        Name = "ctid"
                        TypeMapping = mapping "NpgsqlTypes.NpgsqlTid" Data.DbType.Object (Some "Tid") "tid" } ]

let private apply extensions = Extensions.contributeColumns extensions ProviderType.Npgsql schema

let private columnNames tableName (s: Schema) =
    s.Tables |> List.find (fun t -> t.Name = tableName) |> _.Columns |> List.map _.Name

// ---------------------------------------------------------------------------------------
// The seam itself
// ---------------------------------------------------------------------------------------

[<Test>]
let ``Contributes a column the provider could not discover`` () =
    let result = apply [ XminContribution() ]

    test <@ columnNames "users" result = [ "id"; "age"; "xmin" ] @>

[<Test>]
let ``Contributed column keeps the type mapping the extension gave it`` () =
    let result = apply [ XminContribution() ]
    let xmin = result.Tables |> List.find (fun t -> t.Name = "users") |> _.Columns |> List.last

    test <@ xmin.TypeMapping.ClrType = "uint" @>
    test <@ xmin.TypeMapping.ProviderDbType = Some "Xid" @>

[<Test>]
let ``Context carries the table, so an extension can skip a view`` () =
    let result = apply [ XminContribution() ]

    // A view has no system columns; the extension decided that, not the seam.
    test <@ columnNames "active_users" result = [ "id" ] @>

[<Test>]
let ``Context carries the provider, so a column contributes only where it exists`` () =
    let result = Extensions.contributeColumns [ XminContribution() ] ProviderType.Sqlite schema

    test <@ columnNames "users" result = [ "id"; "age" ] @>

[<Test>]
let ``Extensions compose in registration order, each wrapping the last`` () =
    let result = apply [ XminContribution(); CtidContribution() ]

    test <@ columnNames "users" result = [ "id"; "age"; "xmin"; "ctid" ] @>

[<Test>]
let ``No extensions leaves the schema untouched`` () =
    test <@ Extensions.contributeColumns [] ProviderType.Npgsql schema = schema @>

[<Test>]
let ``Contributing a discovered column's name raises rather than shadowing it`` () =
    let collide =
        { new IContributeColumns with
            member _.Contribute(baseFn) = fun ctx -> baseFn ctx @ [ discovered "age" ] }

    let ex = Assert.Throws<Exception>(fun () -> apply [ collide ] |> ignore)

    test <@ ex.Message.Contains "age" @>
    test <@ ex.Message.Contains "public.users" @>

[<Test>]
let ``Two extensions contributing the same name raises`` () =
    let ex = Assert.Throws<Exception>(fun () -> apply [ XminContribution(); XminContribution() ] |> ignore)

    test <@ ex.Message.Contains "xmin" @>
    test <@ ex.Message.Contains "public.users" @>

// ---------------------------------------------------------------------------------------
// What the contributed column becomes in the generated file
// ---------------------------------------------------------------------------------------

let private cfg: Config =
    {
        LeftJoinedViews = false
        ConnectionString = ""
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        IsMutableProperties = false
        NullablePropertyType = NullablePropertyType.Option
        ProviderDbTypeAttributes = true
        TableDeclarations = false
        Readers = None
        Filters = Filters.Empty
        TypeMappingExtensions = []
    }

let private version: Version.InformationalVersion =
    {
        InformationalVersion = "0.0.0"
        Version = Version(0, 0, 0)
        PreReleaseSuffix = None
    }

let private generate namingExts s =
    SchemaTemplate.generate cfg SqlHydra.Npgsql.Provider.instance s version namingExts

[<Test>]
let ``Generated field carries the provider db type the extension asked for`` () =
    let code = apply [ XminContribution() ] |> generate []

    // Mandatory, not decoration: Npgsql has no default mapping for uint32, so a parameter
    // without it throws client-side.
    test <@ code.Contains "[<ProviderDbType(\"Xid\")>]" @>
    test <@ code.Contains "xmin: uint" @>

/// The body of a generated record, so an assertion about a field is not answered by a doc
/// comment somewhere else in the file.
let private recordBody (typeName: string) (code: string) =
    let fromType = code.Substring(code.IndexOf $"type {typeName} =")
    fromType.Substring(0, fromType.IndexOf "}")

[<Test>]
let ``A column with an empty Doc emits no comment`` () =
    let body = generate [] schema |> recordBody "users"

    test <@ not (body.Contains "///") @>

[<Test>]
let ``A contributed column carries the doc comment the extension gave it`` () =
    let body = apply [ XminContribution() ] |> generate [] |> recordBody "users"

    // The caution belongs on the field, not only in the extension's README: whoever reaches
    // for the column is reading the generated type, not the extension's docs.
    test <@ body.Contains "/// It changes on every write to the row." @>

[<Test>]
let ``A naming extension renames a contributed column like any other`` () =
    let upperCase =
        { new IExtendNaming with
            member _.ExtendTableName(baseFn) = baseFn
            member _.ExtendColumnName(baseFn) = fun ctx -> (baseFn ctx).ToUpper() }

    let code = apply [ XminContribution() ] |> generate [ upperCase ]

    test <@ code.Contains "XMIN: uint" @>
    test <@ not (code.Contains "xmin: uint") @>

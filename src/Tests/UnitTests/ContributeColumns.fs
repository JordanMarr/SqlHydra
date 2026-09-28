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

/// A system column: absent from `information_schema`, so no provider discovers it.
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

/// An extension appending `cols` to every table it is offered.
let private contributing cols =
    { new IContributeColumns with
        member _.Contribute(baseFn) = fun ctx -> baseFn ctx @ cols }

/// `xmin`, on PostgreSQL base tables only: a view has no system columns.
let private xmin =
    { new IContributeColumns with
        member _.Contribute(baseFn) =
            fun ctx ->
                let contributed = baseFn ctx

                if ctx.Provider = ProviderType.Npgsql && ctx.Table.Type = TableType.Table
                then contributed @ [ ContributedColumn.ReadOnly xminColumn ]
                else contributed }

let private apply extensions = Extensions.contributeColumns extensions ProviderType.Npgsql schema

let private columnsOf tableName (s: Schema) =
    s.Tables |> List.find (fun t -> t.Name = tableName) |> _.Columns

let private columnNames tableName = columnsOf tableName >> List.map _.Name

let private raises extensions =
    Assert.Throws<Exception>(fun () -> apply extensions |> ignore).Message

// ---------------------------------------------------------------------------------------
// The seam itself
// ---------------------------------------------------------------------------------------

[<Test>]
let ``Contributes a column the provider could not discover, with the extension's type mapping`` () =
    let result = apply [ xmin ]

    test <@ columnNames "users" result = [ "id"; "age"; "xmin" ] @>
    test <@ (columnsOf "users" result |> List.last).TypeMapping = xminColumn.TypeMapping @>

[<Test>]
let ``Context carries the table, so an extension can skip a view`` () =
    test <@ columnNames "active_users" (apply [ xmin ]) = [ "id" ] @>

[<Test>]
let ``Context carries the provider, so a column contributes only where it exists`` () =
    let result = Extensions.contributeColumns [ xmin ] ProviderType.Sqlite schema

    test <@ columnNames "users" result = [ "id"; "age" ] @>

[<Test>]
let ``Extensions compose in registration order, each wrapping the last`` () =
    let ctid = contributing [ ContributedColumn.ReadOnly { xminColumn with Name = "ctid" } ]

    test <@ columnNames "users" (apply [ xmin; ctid ]) = [ "id"; "age"; "xmin"; "ctid" ] @>

[<Test>]
let ``The ContributedColumn case decides IsReadOnly, not the wrapped column`` () =
    let result =
        apply
            [ contributing
                  [ ContributedColumn.ReadOnly { xminColumn with IsReadOnly = false }
                    ContributedColumn.Writable { xminColumn with Name = "rowid"; IsReadOnly = true } ] ]

    // Discovered columns keep the flag their provider gave them.
    test <@ columnsOf "users" result |> List.map (fun c -> c.Name, c.IsReadOnly) = [ "id", false; "age", false; "xmin", true; "rowid", false ] @>

[<Test>]
let ``No extensions leaves the schema untouched`` () =
    test <@ Extensions.contributeColumns [] ProviderType.Npgsql schema = schema @>

// `Age` covers SQL Server and MySQL, where it is the `age` column and two fields bound to it
// would compile.
[<TestCase "age">]
[<TestCase "Age">]
let ``Contributing a discovered column's name, in any case, raises rather than shadowing it`` (name: string) =
    let message = raises [ contributing [ ContributedColumn.ReadOnly { xminColumn with Name = name } ] ]

    test <@ message.Contains $"'{name}'" && message.Contains "public.users" @>

[<Test>]
let ``Two extensions contributing the same name raises`` () =
    let message = raises [ xmin; xmin ]

    test <@ message.Contains "'xmin'" && message.Contains "public.users" @>

// ---------------------------------------------------------------------------------------
// What the contributed column becomes in the generated file
// ---------------------------------------------------------------------------------------

let private generate namingExts s =
    SchemaTemplate.generate testConfig SqlHydra.Npgsql.Provider.instance s testVersion namingExts

/// The body of the record declared as `{declaration} =`. The last match, since a read record
/// names its write record in `ToWrite() : {table}_write =` before the write record's declaration.
let private recordBody (declaration: string) (code: string) =
    let fromType = code.Substring(code.LastIndexOf $"{declaration} =")
    fromType.Substring(0, fromType.IndexOf "}")

[<Test>]
let ``Generated field carries the provider db type the extension asked for`` () =
    let body = apply [ xmin ] |> generate [] |> recordBody "type users"

    // Npgsql has no default mapping for uint32, so a parameter without it throws client-side.
    test <@ body.Contains "[<ProviderDbType(\"Xid\")>]" && body.Contains "xmin: uint" @>

[<Test>]
let ``A column with an empty Doc emits no comment`` () =
    test <@ not ((generate [] schema |> recordBody "type users").Contains "///") @>

[<Test>]
let ``A contributed column carries the doc comment the extension gave it`` () =
    let body = apply [ xmin ] |> generate [] |> recordBody "type users"

    test <@ body.Contains "/// It changes on every write to the row." @>

[<Test>]
let ``A doc entry with a line break is emitted as separate comment lines`` () =
    let multiLine = contributing [ ContributedColumn.ReadOnly { xminColumn with Doc = [ "first\nsecond\r\nthird" ] } ]
    let lines = apply [ multiLine ] |> generate [] |> recordBody "type users" |> _.Split('\n') |> Array.map _.Trim()

    test <@ [ "/// first"; "/// second"; "/// third" ] |> List.forall (fun l -> Array.contains l lines) @>
    test <@ not (lines |> Array.exists (fun l -> l = "second" || l = "third")) @>

[<Test>]
let ``A naming extension renames a contributed column like any other`` () =
    let upperCase =
        { new IExtendNaming with
            member _.ExtendTableName(baseFn) = baseFn
            member _.ExtendColumnName(baseFn) = fun ctx -> (baseFn ctx).ToUpper() }

    let code = apply [ xmin ] |> generate [ upperCase ]

    test <@ code.Contains "XMIN: uint" && not (code.Contains "xmin: uint") @>

[<Test>]
let ``Only a Writable contribution lands on the write record`` () =
    let rowid = contributing [ ContributedColumn.Writable { xminColumn with Name = "rowid"; Doc = [] } ]
    let body = apply [ xmin; rowid ] |> generate [] |> recordBody "users_write"

    test <@ body.Contains "rowid: uint" && not (body.Contains "xmin") @>

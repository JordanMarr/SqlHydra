module Sqlite.``Schema Provider Tests``

open System
open System.IO
open Microsoft.Data.Sqlite
open NUnit.Framework
open Swensen.Unquote
open SqlHydra.Domain
open SqlHydra.Sqlite

let private withTempSqliteDb (ddl: string) (assertion: string -> unit) =
    let dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db")
    let connectionString = $"Data Source={dbPath}"

    try
        use conn = new SqliteConnection(connectionString)
        conn.Open()

        use cmd = conn.CreateCommand()
        cmd.CommandText <- ddl
        cmd.ExecuteNonQuery() |> ignore

        assertion connectionString
    finally
        SqliteConnection.ClearAllPools()
        if File.Exists(dbPath) then
            File.Delete(dbPath)

let private mkConfig connectionString restrictions =
    {
        ConnectionString = connectionString
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        IsMutableProperties = false
        NullablePropertyType = NullablePropertyType.Option
        ProviderDbTypeAttributes = true
        TableDeclarations = true
        LeftJoinedViews = false
        Readers = Some { ReadersConfig.ReaderType = "System.Data.Common.DbDataReader" }
        Filters = { Filters.Empty with Restrictions = restrictions }
        TypeMappingExtensions = []
    }

let private getTable (tableName: string) (schema: Schema) =
    schema.Tables |> List.find (fun tbl -> tbl.Name = tableName)

let private getColumn (columnName: string) (table: Table) =
    table.Columns |> List.find (fun col -> col.Name = columnName)

[<Test>]
let ``Table restriction by name limits schema tables`` () =
    withTempSqliteDb
        """
        CREATE TABLE t_customers (id INTEGER PRIMARY KEY, name TEXT);
        CREATE TABLE t_orders (id INTEGER PRIMARY KEY, status TEXT);
        """
        (fun connectionString ->
            let restrictions = Map [ "Tables", [| null; null; "t_orders" |] ]
            let cfg = mkConfig connectionString restrictions
            let schema = SqliteSchemaProvider.getSchema(cfg, false, [])

            schema.Tables |> List.map _.Name =! [ "t_orders" ])

[<Test>]
let ``Table type restriction VIEW includes only views`` () =
    withTempSqliteDb
        """
        CREATE TABLE t_orders (id INTEGER PRIMARY KEY, status TEXT);
        CREATE VIEW v_orders AS SELECT id, status FROM t_orders;
        """
        (fun connectionString ->
            let restrictions = Map [ "Tables", [| null; null; null; "VIEW" |] ]
            let cfg = mkConfig connectionString restrictions
            let schema = SqliteSchemaProvider.getSchema(cfg, false, [])

            schema.Tables |> List.map (fun t -> t.Name, t.Type) =! [ "v_orders", TableType.View ])

[<Test>]
let ``Column restriction by table and column narrows discovered columns`` () =
    withTempSqliteDb
        """
        CREATE TABLE t_orders (id INTEGER PRIMARY KEY, status TEXT, amount INTEGER);
        """
        (fun connectionString ->
            let restrictions = Map [ "Columns", [| null; null; "t_orders"; "status" |] ]
            let cfg = mkConfig connectionString restrictions
            let schema = SqliteSchemaProvider.getSchema(cfg, false, [])
            let orders = getTable "t_orders" schema

            orders.Columns |> List.map _.Name =! [ "status" ])

[<Test>]
let ``INTEGER PRIMARY KEY is inferred as non-nullable`` () =
    withTempSqliteDb
        """
        CREATE TABLE t_integer_pk (id INTEGER PRIMARY KEY, payload TEXT);
        """
        (fun connectionString ->
            let cfg = mkConfig connectionString Map.empty
            let schema = SqliteSchemaProvider.getSchema(cfg, false, [])
            let table = getTable "t_integer_pk" schema
            let idCol = getColumn "id" table

            idCol.IsNullable =! false)

[<Test>]
let ``TEXT PRIMARY KEY remains nullable in non-strict rowid tables`` () =
    withTempSqliteDb
        """
        CREATE TABLE t_text_pk (id TEXT PRIMARY KEY, payload TEXT);
        """
        (fun connectionString ->
            let cfg = mkConfig connectionString Map.empty
            let schema = SqliteSchemaProvider.getSchema(cfg, false, [])
            let table = getTable "t_text_pk" schema
            let idCol = getColumn "id" table

            idCol.IsNullable =! true)

[<Test>]
let ``TEXT PRIMARY KEY is non-nullable in WITHOUT ROWID tables`` () =
    withTempSqliteDb
        """
        CREATE TABLE t_text_pk_without_rowid (id TEXT PRIMARY KEY, payload TEXT) WITHOUT ROWID;
        """
        (fun connectionString ->
            let cfg = mkConfig connectionString Map.empty
            let schema = SqliteSchemaProvider.getSchema(cfg, false, [])
            let table = getTable "t_text_pk_without_rowid" schema
            let idCol = getColumn "id" table

            idCol.IsNullable =! false)

[<Test>]
let ``TEXT PRIMARY KEY is non-nullable in STRICT tables`` () =
    withTempSqliteDb
        """
        CREATE TABLE t_text_pk_strict (id TEXT PRIMARY KEY, payload TEXT) STRICT;
        """
        (fun connectionString ->
            let cfg = mkConfig connectionString Map.empty
            let schema = SqliteSchemaProvider.getSchema(cfg, false, [])
            let table = getTable "t_text_pk_strict" schema
            let idCol = getColumn "id" table

            idCol.IsNullable =! false)



[<Test>]
let ``INTEGER IDENTITY with arguments maps to int64`` () =
    withTempSqliteDb
        """
        CREATE TABLE t_identity_with_args (id INTEGER IDENTITY (1, 1), payload TEXT);
        """
        (fun connectionString ->
            let cfg = mkConfig connectionString Map.empty
            let schema = SqliteSchemaProvider.getSchema(cfg, false, [])
            let table = getTable "t_identity_with_args" schema
            let idCol = getColumn "id" table

            idCol.TypeMapping.ClrType =! "int64")

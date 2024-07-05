module SqlHydra.MySql.MySqlSchemaProvider

open System.Data
open MySql.Data
open SqlHydra.Domain
open SqlHydra

let getSchema (cfg: Config) : Schema =
    use conn = new MySqlClient.MySqlConnection(cfg.ConnectionString)
    conn.Open()

    let sTables = conn.GetSchema("Tables", cfg.Filters.TryGetRestrictionsByKey("Tables"))
    let sColumns = conn.GetSchema("Columns", cfg.Filters.TryGetRestrictionsByKey("Columns"))

    let pks =
        let sql =
            """
            SELECT
                tc.table_schema,
                tc.constraint_name,
                tc.table_name,
                kcu.column_name
            FROM
                information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            WHERE 
                tc.constraint_type = 'PRIMARY KEY';
            """

        use cmd = new MySqlClient.MySqlCommand(sql, conn)
        use rdr = cmd.ExecuteReader()
        [
            while rdr.Read() do
                rdr.["TABLE_SCHEMA"] :?> string,
                rdr.["TABLE_NAME"] :?> string,
                rdr.["COLUMN_NAME"] :?> string
        ]
        |> Set.ofList

    let allColumns =
        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun col ->
            {|
                TableCatalog = col["TABLE_CATALOG"] :?> string
                TableSchema = col["TABLE_SCHEMA"] :?> string
                TableName = col["TABLE_NAME"] :?> string
                ColumnName = col["COLUMN_NAME"] :?> string
                ProviderTypeName = col["DATA_TYPE"] :?> string
                OrdinalPosition = col["ORDINAL_POSITION"] :?> int
                IsNullable =
                    match col["IS_NULLABLE"] :?> string with
                    | "YES" -> true
                    | _ -> false
                IsPK =
                    match col["COLUMN_KEY"] :?> string with
                    | "PRI" -> true
                    | _ -> false
            |}
        )
        |> Seq.sortBy (fun column -> column.OrdinalPosition)

    let tables =
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl ->
            {|
                Catalog = tbl["TABLE_CATALOG"] :?> string
                Schema = tbl["TABLE_SCHEMA"] :?> string
                Name  = tbl["TABLE_NAME"] :?> string
                Type = tbl["TABLE_TYPE"] :?> string
            |}
        )
        |> Seq.filter (fun tbl -> tbl.Type <> "SYSTEM_TABLE")
        |> SchemaFilters.filterTables cfg.Filters
        |> Seq.choose (fun tbl ->
            let tableColumns =
                allColumns
                |> Seq.filter (fun col ->
                    col.TableCatalog = tbl.Catalog &&
                    col.TableSchema = tbl.Schema &&
                    col.TableName = tbl.Name
                )

            let supportedColumns = 
                tableColumns
                |> Seq.choose (fun col -> 
                    MySqlDataTypes.tryFindTypeMapping(col.ProviderTypeName)
                    |> Option.map (fun typeMapping -> 
                        { 
                            Column.Name = col.ColumnName
                            Column.IsNullable = col.IsNullable
                            Column.TypeMapping = typeMapping
                            Column.IsPK = pks.Contains(col.TableSchema, col.TableName, col.ColumnName)
                        }
                    )
                )
                |> Seq.toList

            let filteredColumns =
                supportedColumns
                |> SchemaFilters.filterColumns cfg.Filters tbl.Schema tbl.Name
                |> Seq.toList

            if filteredColumns |> Seq.isEmpty then
                None
            else
                Some {
                    Table.Catalog = tbl.Catalog
                    Table.Schema = tbl.Schema
                    Table.Name =  tbl.Name
                    Table.Type = if tbl.Type = "table" then TableType.Table else TableType.View
                    Table.Columns = filteredColumns
                    Table.TotalColumns = tableColumns |> Seq.length
                }
        )
        |> Seq.toList

    {
        Tables = tables
        Enums = []
        PrimitiveTypeReaders = MySqlDataTypes.primitiveTypeReaders
    }

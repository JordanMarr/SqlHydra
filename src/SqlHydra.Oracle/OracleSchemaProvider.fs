module SqlHydra.Oracle.OracleSchemaProvider

open System.Data
open SqlHydra.Domain
open SqlHydra
open Oracle.ManagedDataAccess.Client

let getSchema (cfg: Config) : Schema =
    use conn = new OracleConnection(cfg.ConnectionString)
    conn.Open()
    let sTables = conn.GetSchema("Tables")
    let sColumns = conn.GetSchema("Columns")
    let sViews = conn.GetSchema("Views")

    let pks = 
        let sql =
            """
            SELECT
                tc.table_schema, 
                tc.constraint_name, 
                tc.table_name, 
                kcu.column_name, 
                ccu.table_schema AS foreign_table_schema,
                ccu.table_name AS foreign_table_name,
                ccu.column_name AS foreign_column_name 
            FROM 
                information_schema.table_constraints AS tc 
            JOIN information_schema.key_column_usage AS kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage AS ccu
                ON ccu.constraint_name = tc.constraint_name
                AND ccu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'PRIMARY KEY';
            """

        use cmd = new OracleCommand(sql, conn)
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
                TableCatalog = col.["TABLE_CATALOG"] :?> string
                TableSchema = col.["TABLE_SCHEMA"] :?> string
                TableName = col.["TABLE_NAME"] :?> string
                ColumnName = col.["COLUMN_NAME"] :?> string
                ProviderTypeName = col.["DATA_TYPE"] :?> string
                OrdinalPosition = col.["ORDINAL_POSITION"] :?> int
                IsNullable = 
                    match col.["IS_NULLABLE"] :?> string with 
                    | "YES" -> true
                    | _ -> false
            |}
        )
        |> Seq.sortBy (fun column -> column.OrdinalPosition)

    let views = 
        sViews.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl -> 
            {| 
                TableCatalog = tbl.["TABLE_CATALOG"] :?> string
                TableSchema = tbl.["TABLE_SCHEMA"] :?> string
                TableName  = tbl.["TABLE_NAME"] :?> string
                TableType = "view"
            |}
        )
        
    let tables = 
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl -> 
            {| 
                TableCatalog = tbl.["TABLE_CATALOG"] :?> string
                TableSchema = tbl.["TABLE_SCHEMA"] :?> string
                TableName  = tbl.["TABLE_NAME"] :?> string
                TableType = tbl.["TABLE_TYPE"] :?> string 
            |}
        )
        |> Seq.filter (fun tbl -> tbl.TableType <> "SYSTEM_TABLE")
        |> Seq.append views
        |> Seq.map (fun tbl -> 
            let tableColumns = 
                allColumns
                |> Seq.filter (fun col -> 
                    col.TableCatalog = tbl.TableCatalog && 
                    col.TableSchema = tbl.TableSchema &&
                    col.TableName = tbl.TableName
                )

            let supportedColumns = 
                tableColumns
                |> Seq.choose (fun col -> 
                    OracleDataTypes.tryFindTypeMapping(col.ProviderTypeName)
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
                |> SchemaFilters.filterColumns cfg.Filters tbl.TableSchema tbl.TableName

            { 
                Table.Catalog = tbl.TableCatalog
                Table.Schema = tbl.TableSchema
                Table.Name =  tbl.TableName
                Table.Type = if tbl.TableType = "table" then TableType.Table else TableType.View
                Table.Columns = filteredColumns
                Table.TotalColumns = tableColumns |> Seq.length
            }
        )
        |> Seq.toList

    { 
        Tables = tables
        PrimitiveTypeReaders = OracleDataTypes.primitiveTypeReaders
    }
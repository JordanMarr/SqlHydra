module SqlHydra.SqlServer.SqlServerSchemaProvider

open System.Data
open Microsoft.Data.SqlClient
open SqlHydra.Domain

let getSchema (cfg: Config) : Schema = 
    use conn = new SqlConnection(cfg.ConnectionString)
    conn.Open()
    
    let pks = 
        let sql = 
            """
            select s.name as TABLE_SCHEMA, t.name as TABLE_NAME, tc.name as COLUMN_NAME, ic.key_ordinal as KEY_ORDER
            from sys.schemas s 
            inner join sys.tables t   on s.schema_id=t.schema_id
            inner join sys.indexes i  on t.object_id=i.object_id
            inner join sys.index_columns ic on i.object_id=ic.object_id and i.index_id=ic.index_id
            inner join sys.columns tc on ic.object_id=tc.object_id and ic.column_id=tc.column_id
            where i.is_primary_key=1 
            order by t.name, ic.key_ordinal
            """
        use cmd = new SqlCommand(sql, conn)
        use rdr = cmd.ExecuteReader()
        [
            while rdr.Read() do
                rdr.["TABLE_SCHEMA"] :?> string,
                rdr.["TABLE_NAME"] :?> string,
                rdr.["COLUMN_NAME"] :?> string
        ]
        |> Set.ofList

    let allColumns = 
        let sColumns = conn.GetSchema("Columns")

        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun col -> 
            {| 
                TableCatalog = col.["TABLE_CATALOG"] :?> string
                TableSchema = col.["TABLE_SCHEMA"] :?> string
                TableName = col.["TABLE_NAME"] :?> string
                ColumnName = col.["COLUMN_NAME"] :?> string
                ProviderTypeName = col.["DATA_TYPE"] :?> string
                IsNullable = 
                    match col.["IS_NULLABLE"] :?> string with 
                    | "YES" -> true
                    | _ -> false
            |}
        )

    let tables = 
        let sTables = conn.GetSchema("Tables")

        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl -> 
            let tableCatalog = tbl.["TABLE_CATALOG"] :?> string
            let tableSchema = tbl.["TABLE_SCHEMA"] :?> string
            let tableName  = tbl.["TABLE_NAME"] :?> string
            let tableType = tbl.["TABLE_TYPE"] :?> string

            let tableColumns = 
                allColumns
                |> Seq.filter (fun col -> 
                    col.TableCatalog = tableCatalog && 
                    col.TableSchema = tableSchema &&
                    col.TableName = tableName
                )

            let supportedColumns = 
                tableColumns
                |> Seq.choose (fun col -> 
                    SqlServerDataTypes.tryFindTypeMapping(col.ProviderTypeName)
                    |> Option.map (fun typeMapping -> 
                        {
                            Column.Name = col.ColumnName
                            Column.DbColumnType = None
                            Column.IsNullable = col.IsNullable
                            Column.TypeMapping = typeMapping
                            Column.IsPK = pks.Contains(col.TableSchema, col.TableName, col.ColumnName)
                        }
                    )
                )
                |> Seq.toList

            { 
                Table.Catalog = tableCatalog
                Table.Schema = tableSchema
                Table.Name =  tableName
                Table.Type = if tableType = "BASE TABLE" then TableType.Table else TableType.View
                Table.Columns = supportedColumns
                Table.TotalColumns = tableColumns |> Seq.length
            }
        )
        |> Seq.toList

    { 
        Tables = tables 
        PrimitiveTypeReaders = SqlServerDataTypes.primitiveTypeReaders
    }

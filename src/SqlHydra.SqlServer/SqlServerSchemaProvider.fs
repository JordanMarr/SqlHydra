module SqlHydra.SqlServer.SqlServerSchemaProvider

open System.Data
open Microsoft.Data.SqlClient
open SqlHydra.Schema

let getSchema (cfg: Config) : Schema = 
    use conn = new SqlConnection(cfg.ConnectionString)
    conn.Open()
    let sTables = conn.GetSchema("Tables")
    let sColumns = conn.GetSchema("Columns")

    let allColumns = 
        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun col -> 
            {| TableCatalog = col.["TABLE_CATALOG"] :?> string
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
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl -> 
            let tableCatalog = tbl.["TABLE_CATALOG"] :?> string
            let tableSchema = tbl.["TABLE_SCHEMA"] :?> string
            let tableName  = tbl.["TABLE_NAME"] :?> string
            let tableType = tbl.["TABLE_TYPE"] :?> string

            let columns = 
                allColumns
                |> Seq.filter (fun col -> 
                    col.TableCatalog = tableCatalog && 
                    col.TableSchema = tableSchema &&
                    col.TableName = tableName
                )
                |> Seq.map (fun col -> 
                    { Column.Name = col.ColumnName
                      Column.IsNullable = col.IsNullable
                      Column.TypeMapping = SqlServerDataTypes.findTypeMapping(col.ProviderTypeName)
                    }
                )
                |> Seq.toArray

            { Table.Catalog = tableCatalog
              Table.Schema = tableSchema
              Table.Name =  tableName
              Table.Type = if tableType = "BASE TABLE" then TableType.Table else TableType.View
              Table.Columns = columns
            }
        )
        |> Seq.toArray

    { Tables = tables }

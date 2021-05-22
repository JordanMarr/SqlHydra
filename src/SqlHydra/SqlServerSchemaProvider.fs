module SqlHydra.SqlServerSchemaProvider

open System.Data
open Microsoft.Data.SqlClient
open SqlHydra.Schema

let getSchema (connectionString: string) : Schema = 
    use conn = new SqlConnection(connectionString)
    conn.Open()
    let sTables = conn.GetSchema("Tables")
    let sColumns = conn.GetSchema("Columns")

    let tables = 
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun r -> 
            { Table.Catalog = r.["TABLE_CATALOG"] :?> string
              Table.Schema = r.["TABLE_SCHEMA"] :?> string
              Table.Name = r.["TABLE_NAME"] :?> string
              Table.Type = r.["TABLE_TYPE"] :?> string }
        )
        |> Seq.toArray

    let columns = 
        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun r -> 
            { Column.TableCatalog = r.["TABLE_CATALOG"] :?> string
              Column.TableSchema = r.["TABLE_SCHEMA"] :?> string
              Column.TableName = r.["TABLE_NAME"] :?> string
              Column.ColumnName = r.["COLUMN_NAME"] :?> string
              Column.DataType = r.["DATA_TYPE"] :?> string
              Column.IsNullable = 
                match r.["IS_NULLABLE"] :?> string with 
                | "YES" -> true
                | _ -> false
            }
        )
        |> Seq.toArray

    { Tables = tables; Columns = columns }

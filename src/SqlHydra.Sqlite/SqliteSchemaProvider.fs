module SqlHydra.Sqlite.SqliteSchemaProvider

open System.Data
open System.Data.SQLite
open SqlHydra.Schema

let dbNullOpt<'T> (o: obj) : 'T option =
    match o with
    | :? System.DBNull -> None
    | _ -> o :?> 'T |> Some

let getSchema (connectionString: string) : Schema = 
    use conn = new SQLiteConnection(connectionString)
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
               DataType = col.["DATA_TYPE"] :?> string
               IsNullable = col.["IS_NULLABLE"] :?> bool
            |}
        )

    let tables = 
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl -> 
            {| TableCatalog = tbl.["TABLE_CATALOG"] :?> string
               TableSchema = tbl.["TABLE_SCHEMA"] |> dbNullOpt<string> |> Option.defaultValue "sqlite_default_schema"
               TableName  = tbl.["TABLE_NAME"] :?> string
               TableType = tbl.["TABLE_TYPE"] :?> string |}
        )
        |> Seq.filter (fun tbl -> tbl.TableType <> "SYSTEM_TABLE")
        |> Seq.map (fun tbl -> 
            let columns = 
                allColumns
                |> Seq.filter (fun col -> 
                    col.TableCatalog = tbl.TableCatalog && 
                    col.TableSchema = tbl.TableSchema &&
                    col.TableName = tbl.TableName
                )
                |> Seq.map (fun col -> 
                    { Column.Name = col.ColumnName
                      Column.IsNullable = col.IsNullable
                      Column.DataType = col.DataType
                      Column.ClrType = 
                        SqliteDataTypes.tryFindClrType col.DataType
                        |> Option.defaultValue "obj"
                    }
                )
                |> Seq.toArray

            { Table.Catalog = tbl.TableCatalog
              Table.Schema = tbl.TableSchema
              Table.Name =  tbl.TableName
              Table.Type = if tbl.TableType = "table" then TableType.Table else TableType.View
              Table.Columns = columns
            }
        )
        |> Seq.toArray

    { Tables = tables }

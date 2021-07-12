module SqlHydra.Sqlite.SqliteSchemaProvider

open System.Data
open System.Data.SQLite
open SqlHydra.Schema

let dbNullOpt<'T> (o: obj) : 'T option =
    match o with
    | :? System.DBNull -> None
    | _ -> o :?> 'T |> Some

let getSchema (cfg: Config) : Schema = 
    use conn = new SQLiteConnection(cfg.ConnectionString)
    conn.Open()
    let sTables = conn.GetSchema("Tables")
    let sColumns = conn.GetSchema("Columns")

    // SQLite only supports one schema per file.
    // We will override to be main; otherwise, all columns will have "sqlite_default_schema"
    let defaultSchema = "main"

    let allColumns = 
        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun col -> 
            {| TableCatalog = col.["TABLE_CATALOG"] :?> string
               TableSchema = defaultSchema // col.["TABLE_SCHEMA"] :?> string
               TableName = col.["TABLE_NAME"] :?> string
               ColumnName = col.["COLUMN_NAME"] :?> string
               ProviderTypeName = col.["DATA_TYPE"] :?> string
               IsNullable = col.["IS_NULLABLE"] :?> bool
            |}
        )

    let tables = 
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl -> 
            {| TableCatalog = tbl.["TABLE_CATALOG"] :?> string
               TableSchema = tbl.["TABLE_SCHEMA"] |> dbNullOpt<string> |> Option.defaultValue defaultSchema
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
                      Column.TypeMapping = SqliteDataTypes.findTypeMapping(col.ProviderTypeName)
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

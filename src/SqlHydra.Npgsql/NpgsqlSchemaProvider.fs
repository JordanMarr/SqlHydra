module SqlHydra.Npgsql.NpgsqlSchemaProvider

open System.Data
open Npgsql
open SqlHydra.Domain

let getSchema (cfg: Config) : Schema =
    use conn = new Npgsql.NpgsqlConnection(cfg.ConnectionString)
    conn.Open()
    let sTables = conn.GetSchema("Tables")
    let sColumns = conn.GetSchema("Columns")

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
                IsNullable = 
                    match col.["IS_NULLABLE"] :?> string with 
                    | "YES" -> true
                    | _ -> false
                IsPK = false
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
        |> Seq.map (fun tbl -> 
            let columns = 
                allColumns
                |> Seq.filter (fun col -> 
                    col.TableCatalog = tbl.TableCatalog && 
                    col.TableSchema = tbl.TableSchema &&
                    col.TableName = tbl.TableName
                )
                |> Seq.map (fun col -> 
                    { 
                        Column.Name = col.ColumnName
                        Column.IsNullable = col.IsNullable
                        Column.TypeMapping = //SqliteDataTypes.findTypeMapping(col.ProviderTypeName)
                                {
                                    TypeMapping.ColumnTypeAlias = ""
                                    TypeMapping.ClrType = ""
                                    TypeMapping.DbType = DbType.String
                                    TypeMapping.ReaderMethod = None
                                }
                        Column.IsPK = col.IsPK
                    }
                )
                |> Seq.toList

            { 
                Table.Catalog = tbl.TableCatalog
                Table.Schema = tbl.TableSchema
                Table.Name =  tbl.TableName
                Table.Type = if tbl.TableType = "table" then TableType.Table else TableType.View
                Table.Columns = columns
            }
        )
        |> Seq.toList

    { 
        Tables = tables
        PrimitiveTypeReaders = seq []
    }
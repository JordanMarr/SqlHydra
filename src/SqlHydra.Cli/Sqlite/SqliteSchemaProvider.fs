module SqlHydra.Sqlite.SqliteSchemaProvider

open System.Data
open System.Data.SQLite
open SqlHydra.Domain
open SqlHydra

let dbNullOpt<'T> (o: obj) : 'T option =
    match o with
    | :? System.DBNull -> None
    | _ -> o :?> 'T |> Some

let getSchema (cfg: Config) : Schema = 
    use conn = new SQLiteConnection(cfg.ConnectionString)
    conn.Open()
    let sTables = conn.GetSchema("Tables", cfg.Filters.TryGetRestrictionsByKey("Tables"))
    let sColumns = conn.GetSchema("Columns", cfg.Filters.TryGetRestrictionsByKey("Columns"))

    // SQLite only supports one schema per file.
    // We will override to be main; otherwise, all columns will have "sqlite_default_schema"
    let defaultSchema = "main"

    let allColumns = 
        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun col -> 
            {| 
                TableCatalog = col.["TABLE_CATALOG"] :?> string
                TableSchema = defaultSchema // col.["TABLE_SCHEMA"] :?> string
                TableName = col.["TABLE_NAME"] :?> string
                ColumnName = col.["COLUMN_NAME"] :?> string
                ProviderTypeName = col.["DATA_TYPE"] :?> string
                OrdinalPosition = col.["ORDINAL_POSITION"] :?> int
                IsNullable = col.["IS_NULLABLE"] :?> bool
                IsPK = col.["PRIMARY_KEY"] :?> bool
            |}
        )
        |> Seq.sortBy (fun column -> column.OrdinalPosition)

    let tables = 
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl -> 
            {| 
                Catalog = tbl.["TABLE_CATALOG"] :?> string
                Schema = tbl.["TABLE_SCHEMA"] |> dbNullOpt<string> |> Option.defaultValue defaultSchema
                Name  = tbl.["TABLE_NAME"] :?> string
                Type = tbl.["TABLE_TYPE"] :?> string 
            |}
        )
        |> Seq.filter (fun tbl -> tbl.Type <> "SYSTEM_TABLE")
        |> SchemaFilters.filterTables cfg.Filters
        |> Seq.map (fun tbl -> 
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
                    SqliteDataTypes.tryFindTypeMapping(col.ProviderTypeName)
                    |> Option.map (fun typeMapping -> 
                        { 
                            Column.Name = col.ColumnName
                            Column.IsNullable = col.IsNullable
                            Column.TypeMapping = typeMapping
                            Column.IsPK = col.IsPK
                        }
                    )
                )
                |> Seq.toList

            let filteredColumns = 
                supportedColumns
                |> SchemaFilters.filterColumns cfg.Filters tbl.Schema tbl.Name
                |> Seq.toList

            { 
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
        PrimitiveTypeReaders = SqliteDataTypes.primitiveTypeReaders
    }

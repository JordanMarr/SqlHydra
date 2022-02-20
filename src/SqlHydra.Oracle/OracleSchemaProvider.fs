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
    let sPrimaryKeys = conn.GetSchema("PrimaryKeys")

    let pks = 
        sPrimaryKeys.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun pk -> 
            pk.["OWNER"] :?> string,
            pk.["TABLE_NAME"] :?> string,
            pk.["GENERATED"] :?> string
        )
        |> Set.ofSeq

    let allColumns = 
        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun col -> 
            {| 
                TableCatalog = col.["OWNER"] :?> string
                TableSchema = col.["OWNER"] :?> string
                TableName = col.["TABLE_NAME"] :?> string
                ColumnName = col.["COLUMN_NAME"] :?> string
                ProviderTypeName = col.["DATATYPE"] :?> string
                //OrdinalPosition = col.["ORDINAL_POSITION"] :?> int
                IsNullable = 
                    match col.["NULLABLE"] :?> string with 
                    | "YES" -> true
                    | _ -> false
            |}
        )
        //|> Seq.sortBy (fun column -> column.OrdinalPosition)
        |> Seq.sortBy (fun column -> column.ColumnName)
        |> Seq.toList

    let views = 
        sViews.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun view -> 
            {| 
                TableCatalog = view.["OWNER"] :?> string
                TableSchema = view.["OWNER"] :?> string
                TableName  = view.["VIEW_NAME"] :?> string
                TableType = "view"
            |}
        )
        |> Seq.toList
        
    let tables = 
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl -> 
            {| 
                TableCatalog = tbl.["OWNER"] :?> string
                TableSchema = tbl.["OWNER"] :?> string
                TableName  = tbl.["TABLE_NAME"] :?> string
                TableType = tbl.["TYPE"] :?> string 
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
        |> Seq.filter (fun t -> 
            // Exclude Oracle system tables
            ["SYS"; "MDSYS"; "OLAPSYS"; "WMSYS"; "CTXSYS"; "XDB"; "GSMADMIN_INTERNAL"; "ORDSYS"; "ORDDATA"; "LBACSYS"; "SYSTEM"] 
            |> List.contains t.Schema
            |> not
        )
        |> Seq.toList

    { 
        Tables = tables
        PrimitiveTypeReaders = OracleDataTypes.primitiveTypeReaders
    }
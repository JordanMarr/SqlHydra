module SqlHydra.Oracle.OracleSchemaProvider

open System.Data
open Oracle.ManagedDataAccess.Client
open SqlHydra.Domain
open SqlHydra

let getSchema (cfg: Config) (isLegacy: bool) : Schema =
    use conn = new OracleConnection(cfg.ConnectionString)
    conn.Open()
    let sTables = conn.GetSchema("Tables", cfg.Filters.TryGetRestrictionsByKey("Tables"))
    let sColumns = conn.GetSchema("Columns", cfg.Filters.TryGetRestrictionsByKey("Columns"))
    let sViews = conn.GetSchema("Views", cfg.Filters.TryGetRestrictionsByKey("Views"))

    let systemOwners = 
        ["SYS"; "MDSYS"; "OLAPSYS"; "WMSYS"; "CTXSYS"; "XDB"; "GSMADMIN_INTERNAL"; "ORDSYS"; "ORDDATA"; "LBACSYS"; "SYSTEM"] 
        |> Set.ofList

    let pks = 
        let sql =
            """
            SELECT cols.table_name, cols.column_name, cols.position, cons.status, cons.owner
            FROM all_constraints cons, all_cons_columns cols
            WHERE cons.constraint_type = 'P'
            AND cons.constraint_name = cols.constraint_name
            AND cons.owner = cols.owner
            AND cons.owner NOT IN ('SYS','SYSTEM','DBSNMP','CTXSYS','OJVMSYS','DVSYS','GSMADMIN_INTERNAL','ORDDATA','MDSYS','OLAPSYS','LBACSYS','XDB','WMSYS','ORDSYS')
            ORDER BY cols.table_name, cols.position
            """

        use cmd = new OracleCommand(sql, conn)
        use rdr = cmd.ExecuteReader()
        [
            while rdr.Read() do
                rdr.["OWNER"] :?> string,
                rdr.["TABLE_NAME"] :?> string,
                rdr.["COLUMN_NAME"] :?> string
        ]
        |> Set.ofList

    let columns = 
        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.filter (fun col -> not (systemOwners.Contains(col.["OWNER"] :?> string)))
        |> Seq.map (fun col -> 
            {| 
                TableCatalog = col.["OWNER"] :?> string
                TableSchema = col.["OWNER"] :?> string
                TableName = col.["TABLE_NAME"] :?> string
                ColumnName = col.["COLUMN_NAME"] :?> string
                ProviderTypeName = col.["DATATYPE"] :?> string
                //OrdinalPosition = col.["ORDINAL_POSITION"] :?> int
                Precision = 
                    match col.["PRECISION"] with
                    | :? decimal as precision -> Some (int precision)
                    | _ -> None
                Scale = 
                    match col.["SCALE"] with
                    | :? decimal as scale -> Some (int scale)
                    | _ -> None
                IsNullable = col.["NULLABLE"] :?> string = "Y"
            |}
        )
        //|> Seq.sortBy (fun column -> column.OrdinalPosition)
        |> Seq.sortBy (fun column -> column.ColumnName)
        |> Seq.toList

    let views = 
        sViews.Rows
        |> Seq.cast<DataRow>
        |> Seq.filter (fun view -> not (systemOwners.Contains(view.["OWNER"] :?> string)))
        |> Seq.map (fun view -> 
            {| 
                Catalog = view.["OWNER"] :?> string
                Schema = view.["OWNER"] :?> string
                Name  = view.["VIEW_NAME"] :?> string
                Type = "view"
            |}
        )
        |> Seq.toList
        
    let tables = 
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl -> 
            {| 
                Catalog = tbl.["OWNER"] :?> string
                Schema = tbl.["OWNER"] :?> string
                Name  = tbl.["TABLE_NAME"] :?> string
                Type = tbl.["TYPE"] :?> string // [ "view"; "User"; "System" ]
            |}
        )
        |> Seq.filter (fun tbl -> System.String.Compare(tbl.Type, "System", true) <> 0) // Exclude system
        |> Seq.append views
        |> SchemaFilters.filterTables cfg.Filters
        |> Seq.choose (fun tbl -> 
            let tableColumns = 
                columns
                |> Seq.filter (fun col -> 
                    col.TableCatalog = tbl.Catalog && 
                    col.TableSchema = tbl.Schema &&
                    col.TableName = tbl.Name
                )                
                
            let supportedColumns = 
                tableColumns                
                |> Seq.choose (fun col -> 
                    OracleDataTypes.tryFindTypeMapping (col.ProviderTypeName, col.Precision, col.Scale)
                    |> Option.map (fun typeMapping ->
                        { 
                            Column.Name = col.ColumnName
                            Column.IsNullable = col.IsNullable
                            Column.TypeMapping = typeMapping
                            Column.IsPK = pks.Contains(col.TableSchema, col.TableName, col.ColumnName)
                        }
                    )
                )

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
                    Table.Type = if System.String.Compare(tbl.Type, "view", true) = 0 then TableType.View else TableType.Table
                    Table.Columns = filteredColumns
                    Table.TotalColumns = tableColumns |> Seq.length
                }
        )
        |> Seq.filter (fun t -> not (systemOwners.Contains t.Schema)) // Exclude Oracle system tables
        |> Seq.toList

    { 
        Tables = tables
        Enums = []
        PrimitiveTypeReaders = OracleDataTypes.primitiveTypeReaders
    }
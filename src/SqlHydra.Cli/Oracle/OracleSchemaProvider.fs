module SqlHydra.Oracle.OracleSchemaProvider

open System.Data
open Oracle.ManagedDataAccess.Client
open SqlHydra.Domain
open SqlHydra

let getColumnSchema (conn: OracleConnection) = 
    use cmd = conn.CreateCommand()
    cmd.CommandText <- """
        SELECT OWNER, TABLE_NAME, COLUMN_NAME, DATA_TYPE AS DATATYPE, DATA_PRECISION AS PRECISION, DATA_SCALE AS SCALE, NULLABLE 
        FROM ALL_TAB_COLUMNS 
        WHERE OWNER NOT IN ('SYS', 'SYSTEM') 
        ORDER BY OWNER, TABLE_NAME, COLUMN_ID 
        """    
    let adapter = new OracleDataAdapter(cmd)
    let dataTable = new DataTable()
    adapter.Fill(dataTable) |> ignore
    dataTable

let getSchema (cfg: Config, isLegacy: bool, extensions: IExtendTypeMapping list) : Schema =
    use conn = new OracleConnection(cfg.ConnectionString)
    conn.Open()
    let sTables = conn.GetSchema("Tables", cfg.Filters.TryGetRestrictionsByKey("Tables"))
    let sColumns = getColumnSchema conn
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

    let allColumns =
        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.filter (fun col -> not (systemOwners.Contains(col.["OWNER"] :?> string)))
        |> Seq.map (fun col ->
            let owner = col.["OWNER"] :?> string
            let table = col.["TABLE_NAME"] :?> string
            let name = col.["COLUMN_NAME"] :?> string
            {
                ColumnSchema.Catalog = owner
                ColumnSchema.Schema = owner
                ColumnSchema.Table = table
                ColumnSchema.Name = name
                ColumnSchema.ProviderTypeName = col.["DATATYPE"] :?> string
                ColumnSchema.Ordinal = 0
                ColumnSchema.IsNullable = col.["NULLABLE"] :?> string = "Y"
                ColumnSchema.Precision =
                    match col.["PRECISION"] with
                    | :? decimal as precision -> Some (int precision)
                    | _ -> None
                ColumnSchema.Scale =
                    match col.["SCALE"] with
                    | :? decimal as scale -> Some (int scale)
                    | _ -> None
                ColumnSchema.IsPrimaryKey = pks.Contains(owner, table, name)
                ColumnSchema.IsComputed = false
                ColumnSchema.DefaultValue = None
            }
        )
        |> Seq.sortBy (fun col -> col.Name)
        |> Seq.toList

    let columnsByTable =
        allColumns
        |> List.groupBy (fun col -> col.Catalog, col.Schema, col.Table)
        |> Map.ofList

    let tableSchemas =
        let views =
            sViews.Rows
            |> Seq.cast<DataRow>
            |> Seq.filter (fun view -> not (systemOwners.Contains(view.["OWNER"] :?> string)))
            |> Seq.map (fun view ->
                let owner = view.["OWNER"] :?> string
                let name = view.["VIEW_NAME"] :?> string
                {
                    TableSchema.Catalog = owner
                    TableSchema.Schema = owner
                    TableSchema.Name = name
                    TableSchema.Type = TableType.View
                    TableSchema.Columns =
                        columnsByTable
                        |> Map.tryFind (owner, owner, name)
                        |> Option.defaultValue []
                }
            )
            |> Seq.toList

        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.filter (fun tbl -> System.String.Compare(tbl.["TYPE"] :?> string, "System", true) <> 0)
        |> Seq.map (fun tbl ->
            let owner = tbl.["OWNER"] :?> string
            let name = tbl.["TABLE_NAME"] :?> string
            {
                TableSchema.Catalog = owner
                TableSchema.Schema = owner
                TableSchema.Name = name
                TableSchema.Type = TableType.Table
                TableSchema.Columns =
                    columnsByTable
                    |> Map.tryFind (owner, owner, name)
                    |> Option.defaultValue []
            }
        )
        |> Seq.toList
        |> fun tables -> tables @ views
        |> SchemaFilters.filterTables cfg.Filters
        |> Seq.toList

    let tryFindTypeMapping =
        let baseTryFind = OracleDataTypes.tryFindTypeMapping
        extensions |> List.fold (fun acc (ext: IExtendTypeMapping) -> ext.Extend(acc)) baseTryFind

    let tables =
        tableSchemas
        |> List.choose (fun tableSchema ->
            let supportedColumns =
                tableSchema.Columns
                |> List.choose (fun col ->
                    let ctx = { TypeMappingContext.Table = tableSchema; TypeMappingContext.Column = col }
                    tryFindTypeMapping ctx
                    |> Option.map (fun typeMapping ->
                        {
                            Column.Name = col.Name
                            Column.IsNullable = col.IsNullable
                            Column.TypeMapping = typeMapping
                            Column.IsPK = col.IsPrimaryKey
                            Column.IsReadOnly = false
                            Column.Doc = []
                        }
                    )
                )

            let filteredColumns =
                supportedColumns
                |> SchemaFilters.filterColumns cfg.Filters tableSchema.Schema tableSchema.Name
                |> Seq.toList

            if filteredColumns |> Seq.isEmpty then
                None
            else
                Some {
                    Table.Catalog = tableSchema.Catalog
                    Table.Schema = tableSchema.Schema
                    Table.Name = tableSchema.Name
                    Table.Type = tableSchema.Type
                    Table.Columns = filteredColumns
                    Table.TotalColumns = tableSchema.Columns |> List.length
                }
        )
        |> List.filter (fun t -> not (systemOwners.Contains t.Schema))
        |> Seq.toList

    {
        Tables = tables
        Enums = []
    }
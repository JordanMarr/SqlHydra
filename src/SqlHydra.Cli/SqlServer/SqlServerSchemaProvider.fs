module SqlHydra.SqlServer.SqlServerSchemaProvider

open System.Data
open Microsoft.Data.SqlClient
open SqlHydra.Domain
open SqlHydra

let getSchema (cfg: Config, isLegacy: bool, extensions: IExtendTypeMapping list) : Schema =
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
        let sColumns = conn.GetSchema("Columns", cfg.Filters.TryGetRestrictionsByKey("Columns"))

        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun col ->
            let schema = col.["TABLE_SCHEMA"] :?> string
            let table = col.["TABLE_NAME"] :?> string
            let name = col.["COLUMN_NAME"] :?> string
            {
                ColumnSchema.Catalog = col.["TABLE_CATALOG"] :?> string
                ColumnSchema.Schema = schema
                ColumnSchema.Table = table
                ColumnSchema.Name = name
                ColumnSchema.ProviderTypeName = col.["DATA_TYPE"] :?> string
                ColumnSchema.Ordinal = col.["ORDINAL_POSITION"] :?> int
                ColumnSchema.IsNullable =
                    match col.["IS_NULLABLE"] :?> string with
                    | "YES" -> true
                    | _ -> false
                ColumnSchema.Precision = None
                ColumnSchema.Scale = None
                ColumnSchema.IsPrimaryKey = pks.Contains(schema, table, name)
                ColumnSchema.IsComputed = false
                ColumnSchema.DefaultValue = None
            }
        )
        |> Seq.sortBy (fun col -> col.Ordinal)
        |> Seq.toList

    let columnsByTable =
        allColumns
        |> List.groupBy (fun col -> col.Catalog, col.Schema, col.Table)
        |> Map.ofList

    let tableSchemas =
        let sTables = conn.GetSchema("Tables", cfg.Filters.TryGetRestrictionsByKey("Tables"))

        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl ->
            let catalog = tbl.["TABLE_CATALOG"] :?> string
            let schema = tbl.["TABLE_SCHEMA"] :?> string
            let name = tbl.["TABLE_NAME"] :?> string
            let tableType = tbl.["TABLE_TYPE"] :?> string
            {
                TableSchema.Catalog = catalog
                TableSchema.Schema = schema
                TableSchema.Name = name
                TableSchema.Type = if tableType = "BASE TABLE" then TableType.Table else TableType.View
                TableSchema.Columns =
                    columnsByTable
                    |> Map.tryFind (catalog, schema, name)
                    |> Option.defaultValue []
            }
        )
        |> SchemaFilters.filterTables cfg.Filters
        |> Seq.toList

    let tryFindTypeMapping =
        let baseTryFind = SqlServerDataTypes.tryFindTypeMapping isLegacy
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

    {
        Tables = tables
        Enums = []
    }

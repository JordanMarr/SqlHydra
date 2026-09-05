module SqlHydra.MySql.MySqlSchemaProvider

open System.Data
open MySql.Data
open SqlHydra.Domain
open SqlHydra

let getSchema (cfg: Config, isLegacy: bool, extensions: IExtendTypeMapping list) : Schema =
    use conn = new MySqlClient.MySqlConnection(cfg.ConnectionString)
    conn.Open()

    let sTables = conn.GetSchema("Tables", cfg.Filters.TryGetRestrictionsByKey("Tables"))
    let sColumns = conn.GetSchema("Columns", cfg.Filters.TryGetRestrictionsByKey("Columns"))

    let pks =
        let sql =
            """
            SELECT
                tc.table_schema,
                tc.constraint_name,
                tc.table_name,
                kcu.column_name
            FROM
                information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            WHERE
                tc.constraint_type = 'PRIMARY KEY';
            """

        use cmd = new MySqlClient.MySqlCommand(sql, conn)
        use rdr = cmd.ExecuteReader()
        [
            while rdr.Read() do
                rdr.["TABLE_SCHEMA"] :?> string,
                rdr.["TABLE_NAME"] :?> string,
                rdr.["COLUMN_NAME"] :?> string
        ]
        |> Set.ofList

    let allColumns =
        sColumns.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun col ->
            let schema = col["TABLE_SCHEMA"] :?> string
            let table = col["TABLE_NAME"] :?> string
            let name = col["COLUMN_NAME"] :?> string
            {
                ColumnSchema.Catalog = col["TABLE_CATALOG"] :?> string
                ColumnSchema.Schema = schema
                ColumnSchema.Table = table
                ColumnSchema.Name = name
                ColumnSchema.ProviderTypeName = col["DATA_TYPE"] :?> string
                ColumnSchema.Ordinal = col["ORDINAL_POSITION"] :?> uint64 |> int
                ColumnSchema.IsNullable =
                    match col["IS_NULLABLE"] :?> string with
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
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.filter (fun tbl -> tbl["TABLE_TYPE"] :?> string <> "SYSTEM_TABLE")
        |> Seq.map (fun tbl ->
            let catalog = tbl["TABLE_CATALOG"] :?> string
            let schema = tbl["TABLE_SCHEMA"] :?> string
            let name = tbl["TABLE_NAME"] :?> string
            let tableType = tbl["TABLE_TYPE"] :?> string
            {
                TableSchema.Catalog = catalog
                TableSchema.Schema = schema
                TableSchema.Name = name
                TableSchema.Type = if tableType = "table" then TableType.Table else TableType.View
                TableSchema.Columns =
                    columnsByTable
                    |> Map.tryFind (catalog, schema, name)
                    |> Option.defaultValue []
            }
        )
        |> SchemaFilters.filterTables cfg.Filters
        |> Seq.toList

    let tryFindTypeMapping =
        let baseTryFind = MySqlDataTypes.tryFindTypeMapping isLegacy
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

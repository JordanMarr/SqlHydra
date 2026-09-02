module SqlHydra.Npgsql.NpgsqlSchemaProvider

open System.Data
open SqlHydra.Domain
open SqlHydra

/// A column's generation flags, named as `pg_attribute` names them.
/// https://www.postgresql.org/docs/current/catalog-pg-attribute.html
type PgAttribute =
    { AttGenerated: string
      AttIdentity: string }

/// True for a column PostgreSQL writes itself: generated ('s' stored, 'v' virtual) or
/// identity ALWAYS ('a'). Identity BY DEFAULT ('d') is writable and must not match.
/// `AttGenerated` is compared, not matched, so a kind added in a later PostgreSQL is caught.
let isDatabaseGenerated (att: PgAttribute) =
    att.AttGenerated <> "" || att.AttIdentity = "a"

/// One column, named. A tuple key here would let the schema and table be probed in the
/// wrong order — which compiles, finds nothing, and marks nothing read-only.
type ColumnRef =
    { Schema: string
      Table: string
      Column: string }

let getSchema (cfg: Config, isLegacy: bool, extensions: IExtendTypeMapping list) : Schema =
    use conn = new Npgsql.NpgsqlConnection(cfg.ConnectionString)
    conn.Open()
    // NOTE: GetSchema will fail if a Postgres enum doesn't exists in a custom schema but not in public schema.
    // Error: "type {enum name} does not exist"
    // This is a Postgres issue, not a SqlHydra issue.
    let sTables = conn.GetSchema("Tables", cfg.Filters.TryGetRestrictionsByKey("Tables"))
    let sColumns = conn.GetSchema("Columns", cfg.Filters.TryGetRestrictionsByKey("Columns"))
    let sViews = conn.GetSchema("Views", cfg.Filters.TryGetRestrictionsByKey("Views"))

    // MaterializedViews requires Npgsql v8 or greater (which requires net8 or greater).
#if NET8_0_OR_GREATER
    let sMaterializedViews = conn.GetSchema("MaterializedViews", cfg.Filters.TryGetRestrictionsByKey("MaterializedViews"))
#else
    let sMaterializedViews = new DataTable()
#endif

    let pks =
        let sql =
            """
            SELECT
                tc.table_schema,
                tc.constraint_name,
                tc.table_name,
                kcu.column_name,
                ccu.table_schema AS foreign_table_schema,
                ccu.table_name AS foreign_table_name,
                ccu.column_name AS foreign_column_name
            FROM
                information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage AS ccu
                ON ccu.constraint_name = tc.constraint_name
                AND ccu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'PRIMARY KEY';
            """

        use cmd = new Npgsql.NpgsqlCommand(sql, conn)
        use rdr = cmd.ExecuteReader()
        [
            while rdr.Read() do
                rdr.["TABLE_SCHEMA"] :?> string,
                rdr.["TABLE_NAME"] :?> string,
                rdr.["COLUMN_NAME"] :?> string
        ]
        |> Set.ofList

    let generatedColumns =
        let sql =
            """
            SELECT
                pg_namespace.nspname AS table_schema,
                pg_class.relname AS table_name,
                pg_attribute.attname AS column_name,
                pg_attribute.attgenerated::text AS attgenerated,
                pg_attribute.attidentity::text AS attidentity
            FROM pg_attribute
            INNER JOIN pg_class ON pg_class.oid = pg_attribute.attrelid
            INNER JOIN pg_namespace ON pg_namespace.oid = pg_class.relnamespace
            WHERE
                -- ordinary (r) and partitioned (p) tables; nothing else can be written
                pg_class.relkind in ('r', 'p') AND
                pg_attribute.attnum >= 1 AND
                NOT pg_attribute.attisdropped AND
                pg_namespace.nspname not in ('pg_catalog', 'information_schema');
            """

        use cmd = new Npgsql.NpgsqlCommand(sql, conn)
        use rdr = cmd.ExecuteReader()
        [
            while rdr.Read() do
                let att =
                    { AttGenerated = rdr["attgenerated"] :?> string
                      AttIdentity = rdr["attidentity"] :?> string }
                if isDatabaseGenerated att then
                    { Schema = rdr["table_schema"] :?> string
                      Table = rdr["table_name"] :?> string
                      Column = rdr["column_name"] :?> string }
        ]
        |> Set.ofList

    let isReadOnly (col: ColumnSchema) =
        generatedColumns.Contains { Schema = col.Schema; Table = col.Table; Column = col.Name }

    let enums =
        let sql =
            """
            SELECT n.nspname as Schema, t.typname as Enum, e.enumlabel as Label, e.enumsortorder as LabelOrder
            FROM pg_enum e
            JOIN pg_type t ON e.enumtypid = t.oid
            LEFT JOIN   pg_catalog.pg_namespace n ON n.oid = t.typnamespace
            WHERE (t.typrelid = 0 OR (SELECT c.relkind = 'c' FROM pg_catalog.pg_class c WHERE c.oid = t.typrelid)) and typtype = 'e'
                AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_type el WHERE el.oid = t.typelem AND el.typarray = t.oid)
                AND n.nspname NOT IN ('pg_catalog', 'information_schema');
            """

        use cmd = new Npgsql.NpgsqlCommand(sql, conn)
        use rdr = cmd.ExecuteReader()

        [
            while rdr.Read() do
                {|
                    Schema = rdr["Schema"] :?> string
                    Enum = rdr["Enum"] :?> string
                    Label = rdr["Label"] :?> string
                    LabelOrder = rdr["LabelOrder"] :?> single
                |}
        ]
        |> List.groupBy (fun r -> r.Schema, r.Enum)
        |> List.map (fun (_, grp) ->
            let h = grp |> List.head
            {
                Schema = h.Schema
                Name = h.Enum
                Labels = grp |> List.map (fun r -> { Name = r.Label; SortOrder = System.Convert.ToInt32(r.LabelOrder) })
            }
        )

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
                ColumnSchema.Ordinal = col["ORDINAL_POSITION"] :?> int
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

    let views =
        sViews.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl ->
            {|
                Catalog = tbl["TABLE_CATALOG"] :?> string
                Schema = tbl["TABLE_SCHEMA"] :?> string
                Name  = tbl["TABLE_NAME"] :?> string
                Type = "view"
            |}
        )

    let materializedViews =
        sMaterializedViews.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl ->
            {|
                Catalog = tbl["TABLE_CATALOG"] :?> string
                Schema = tbl["TABLE_SCHEMA"] :?> string
                Name  = tbl["TABLE_NAME"] :?> string
                Type = "materialized view"
            |}
        )

    let materializedViewColumns =
        let sql =
            """
            SELECT
                pg_namespace.nspname AS table_schema,
                pg_class.relname AS table_name,
                pg_class.relkind,
                pg_attribute.attname AS column_name,
                pg_attribute.attnum AS ordinal_position,
                pg_type.typname AS data_type,
                pg_attribute.attnotnull AS not_null
            FROM pg_class
            INNER JOIN pg_namespace on (pg_class.relnamespace = pg_namespace.oid)
            INNER JOIN pg_attribute on (pg_class.oid = pg_attribute.attrelid)
            INNER JOIN pg_type on (pg_attribute.atttypid = pg_type.oid)
            WHERE
                -- get ordinary tables (r), views (v), and materialized views (m)
                relkind in ('r', 'v', 'm') AND
                -- filter out any "weird" columns
                pg_attribute.attnum >= 1 AND
                -- filter out internal schemas
                pg_namespace.nspname not in ('pg_catalog', 'information_schema')
            ORDER BY
                table_schema,
                table_name,
                ordinal_position

            """

        use cmd = new Npgsql.NpgsqlCommand(sql, conn)
        use rdr = cmd.ExecuteReader()
        [
            while rdr.Read() do
                let schema = rdr["TABLE_SCHEMA"] :?> string
                let table = rdr["TABLE_NAME"] :?> string
                let name = rdr["COLUMN_NAME"] :?> string
                {
                    ColumnSchema.Catalog = ""
                    ColumnSchema.Schema = schema
                    ColumnSchema.Table = table
                    ColumnSchema.Name = name
                    ColumnSchema.ProviderTypeName = rdr["DATA_TYPE"] :?> string
                    ColumnSchema.Ordinal = rdr["ORDINAL_POSITION"] :?> int16 |> int
                    ColumnSchema.IsNullable = rdr["not_null"] :?> bool |> not
                    ColumnSchema.Precision = None
                    ColumnSchema.Scale = None
                    ColumnSchema.IsPrimaryKey = pks.Contains(schema, table, name)
                    ColumnSchema.IsComputed = false
                    ColumnSchema.DefaultValue = None
                }
        ]
        |> Seq.sortBy (fun col -> col.Ordinal)
        |> Seq.groupBy (fun col -> col.Schema, col.Table)
        |> Map.ofSeq

    let columnsByTable =
        allColumns
        |> List.groupBy (fun col -> col.Catalog, col.Schema, col.Table)
        |> Map.ofList

    let tryFindTypeMapping =
        let baseTryFind = NpgsqlDataTypes.tryFindTypeMapping isLegacy
        extensions |> List.fold (fun acc (ext: IExtendTypeMapping) -> ext.Extend(acc)) baseTryFind

    let matViews =
        materializedViews
        |> Seq.choose (fun tbl ->
            let matViewCols =
                match materializedViewColumns.TryFind(tbl.Schema, tbl.Name) with
                | Some cols -> cols |> Seq.toList
                | None -> []

            let tableSchema =
                {
                    TableSchema.Catalog = tbl.Catalog
                    TableSchema.Schema = tbl.Schema
                    TableSchema.Name = tbl.Name
                    TableSchema.Type = TableType.View
                    TableSchema.Columns = matViewCols
                }

            let mappedColumns =
                matViewCols
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

            if mappedColumns.Length > 0 then
                Some {
                    Table.Catalog = tbl.Catalog
                    Table.Schema = tbl.Schema
                    Table.Name =  tbl.Name
                    Table.Type = TableType.View
                    Table.Columns = mappedColumns
                    Table.TotalColumns = matViewCols |> List.length
                }
            else None
        )
        |> Seq.toList

    let tables =
        sTables.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl ->
            {|
                Catalog = tbl["TABLE_CATALOG"] :?> string
                Schema = tbl["TABLE_SCHEMA"] :?> string
                Name  = tbl["TABLE_NAME"] :?> string
                Type = tbl["TABLE_TYPE"] :?> string
            |}
        )
        |> Seq.filter (fun tbl -> tbl.Type <> "SYSTEM_TABLE")
        |> Seq.append views
        |> SchemaFilters.filterTables cfg.Filters
        |> Seq.choose (fun tbl ->
            let tableCols =
                columnsByTable
                |> Map.tryFind (tbl.Catalog, tbl.Schema, tbl.Name)
                |> Option.defaultValue []

            let tableSchema =
                {
                    TableSchema.Catalog = tbl.Catalog
                    TableSchema.Schema = tbl.Schema
                    TableSchema.Name = tbl.Name
                    TableSchema.Type = if tbl.Type = "table" then TableType.Table else TableType.View
                    TableSchema.Columns = tableCols
                }

            let mappedColumns =
                tableCols
                |> List.choose (fun col ->
                    let ctx = { TypeMappingContext.Table = tableSchema; TypeMappingContext.Column = col }
                    tryFindTypeMapping ctx
                    |> Option.map (fun typeMapping ->
                        {
                            Column.Name = col.Name
                            Column.IsNullable = col.IsNullable
                            Column.TypeMapping = typeMapping
                            Column.IsPK = col.IsPrimaryKey
                            Column.IsReadOnly = isReadOnly col
                        }
                    )
                )

            let enumColumns =
                tableCols
                |> List.choose (fun col ->
                    let fullyQualified = enums |> List.tryFind (fun e -> col.ProviderTypeName = $"{e.Schema}.{e.Name}")
                    let unqualified = enums |> List.tryFind (fun e -> col.ProviderTypeName = e.Name)

                    // The same enum can exist in different schemas.
                    // So ideally, col.ProviderTypeName has a fully qualified enum type (schema.enumName).
                    // If no qualified enum is found, then just use the first unqualified enum.
                    fullyQualified
                    |> Option.orElse unqualified
                    |> Option.map (fun enum -> col, enum)
                )
                |> List.map (fun (col, enum) ->
                    {
                        Column.Name = col.Name
                        Column.IsNullable = col.IsNullable
                        Column.TypeMapping =
                            {
                                TypeMapping.ColumnTypeAlias = col.ProviderTypeName
                                TypeMapping.ClrType =                       // Enum type (will be generated)
                                    if col.Schema <> enum.Schema
                                    then $"{enum.Schema}.{enum.Name}"       // Enum lives in a different schema/module
                                    else enum.Name                          // Enum lives in this module
                                TypeMapping.DbType = DbType.Object
                                TypeMapping.ProviderDbType = None
                            }
                        Column.IsPK = col.IsPrimaryKey
                        Column.IsReadOnly = isReadOnly col
                    }
                )

            let supportedColumns = mappedColumns @ enumColumns

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
                    Table.Type = if tbl.Type = "table" then TableType.Table else TableType.View
                    Table.Columns = filteredColumns
                    Table.TotalColumns = tableCols |> List.length
                }
        )
        |> Seq.toList

    {
        Tables = tables @ matViews
        Enums = enums
    }

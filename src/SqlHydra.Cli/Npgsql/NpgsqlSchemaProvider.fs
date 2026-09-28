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

/// True for an ordinary base table, false for a view. Npgsql reports a table's type as
/// "BASE TABLE"; the view rows carry our own "view" / "materialized view" labels.
let isBaseTableType tableType =
    tableType <> "view" && tableType <> "materialized view"

let getSchema (cfg: Config, isLegacy: bool, extensions: IExtendTypeMapping list) : Schema =
    use conn = new Npgsql.NpgsqlConnection(cfg.ConnectionString)
    conn.Open()
    // NOTE: GetSchema will fail if a Postgres enum doesn't exists in a custom schema but not in public schema.
    // Error: "type {enum name} does not exist"
    // This is a Postgres issue, not a SqlHydra issue.
    let sTables = conn.GetSchema("Tables", cfg.Filters.TryGetRestrictionsByKey("Tables"))
    let sViews = conn.GetSchema("Views", cfg.Filters.TryGetRestrictionsByKey("Views"))

    // MaterializedViews requires Npgsql v8 or greater (which requires net8 or greater).
#if NET8_0_OR_GREATER
    let sMaterializedViews = conn.GetSchema("MaterializedViews", cfg.Filters.TryGetRestrictionsByKey("MaterializedViews"))
#else
    let sMaterializedViews = new DataTable()
#endif

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

    let relations (table: DataTable) (typeOf: DataRow -> string) =
        table.Rows
        |> Seq.cast<DataRow>
        |> Seq.map (fun tbl ->
            {|
                Catalog = tbl["TABLE_CATALOG"] :?> string
                Schema = tbl["TABLE_SCHEMA"] :?> string
                Name  = tbl["TABLE_NAME"] :?> string
                Type = typeOf tbl
            |}
        )

    /// Every relation the filters keep, in one pass so the filter summary is reported once.
    let includedRelations =
        relations sTables (fun tbl -> tbl["TABLE_TYPE"] :?> string)
        |> Seq.filter (fun tbl -> tbl.Type <> "SYSTEM_TABLE")
        |> Seq.append (relations sViews (fun _ -> "view"))
        |> Seq.append (relations sMaterializedViews (fun _ -> "materialized view"))
        |> SchemaFilters.filterTables cfg.Filters
        |> Seq.toList

    /// Every relation's columns, from `pg_catalog`: `information_schema.columns`, which
    /// `GetSchema("Columns")` reads, omits materialized views. The type is spelled as Npgsql's
    /// `GetSchema` spells it, `format_type` of the base type (a domain resolves to what it
    /// wraps), so the type mappings see the same names for a table, a view or a matview.
    let columns =
        let sql =
            """
            SELECT
                current_database() AS table_catalog,
                n.nspname AS table_schema,
                c.relname AS table_name,
                a.attname AS column_name,
                a.attnum AS ordinal_position,
                format_type(COALESCE(NULLIF(t.typbasetype, 0), a.atttypid), NULL) AS data_type,
                a.attnotnull OR (t.typtype = 'd' AND t.typnotnull) AS not_null,
                EXISTS (
                    SELECT 1 FROM pg_index i
                    WHERE i.indrelid = c.oid AND i.indisprimary AND a.attnum = ANY(i.indkey)
                ) AS is_pk,
                -- only a table's own columns can be generated; a foreign table's are not ours to write
                CASE WHEN c.relkind IN ('r', 'p') THEN a.attgenerated::text ELSE '' END AS attgenerated,
                CASE WHEN c.relkind IN ('r', 'p') THEN a.attidentity::text ELSE '' END AS attidentity
            FROM pg_attribute a
            INNER JOIN pg_class c ON c.oid = a.attrelid
            INNER JOIN pg_namespace n ON n.oid = c.relnamespace
            INNER JOIN pg_type t ON t.oid = a.atttypid
            WHERE
                -- tables, views, materialized views, foreign and partitioned tables
                c.relkind IN ('r', 'v', 'm', 'f', 'p') AND
                a.attnum >= 1 AND
                NOT a.attisdropped AND
                n.nspname NOT IN ('pg_catalog', 'information_schema') AND
                has_column_privilege(c.oid, a.attnum, 'SELECT, INSERT, UPDATE, REFERENCES')
            """

        // `GetSchema`'s positional restrictions: catalog, schema, table, column; an empty one matches all.
        let restrictions = cfg.Filters.TryGetRestrictionsByKey "Columns"
        let restricted (fields: string list) =
            fields |> List.indexed |> List.forall (fun (i, field) ->
                i >= restrictions.Length || System.String.IsNullOrEmpty restrictions[i] || restrictions[i] = field)

        use cmd = new Npgsql.NpgsqlCommand(sql, conn)
        use rdr = cmd.ExecuteReader()
        [
            while rdr.Read() do
                let catalog = rdr["table_catalog"] :?> string
                let schema = rdr["table_schema"] :?> string
                let table = rdr["table_name"] :?> string
                let name = rdr["column_name"] :?> string
                if restricted [ catalog; schema; table; name ] then
                    {
                        ColumnSchema.Catalog = catalog
                        ColumnSchema.Schema = schema
                        ColumnSchema.Table = table
                        ColumnSchema.Name = name
                        ColumnSchema.ProviderTypeName = rdr["data_type"] :?> string
                        ColumnSchema.Ordinal = rdr["ordinal_position"] :?> int16 |> int
                        ColumnSchema.IsNullable = rdr["not_null"] :?> bool |> not
                        ColumnSchema.Precision = None
                        ColumnSchema.Scale = None
                        ColumnSchema.IsPrimaryKey = rdr["is_pk"] :?> bool
                        ColumnSchema.IsComputed =
                            isDatabaseGenerated
                                { AttGenerated = rdr["attgenerated"] :?> string
                                  AttIdentity = rdr["attidentity"] :?> string }
                        ColumnSchema.DefaultValue = None
                    }
        ]
        |> List.sortBy _.Ordinal
        |> List.groupBy (fun col -> col.Schema, col.Table)
        |> Map.ofList

    let tryFindTypeMapping =
        let baseTryFind = NpgsqlDataTypes.tryFindTypeMapping isLegacy
        extensions |> List.fold (fun acc (ext: IExtendTypeMapping) -> ext.Extend(acc)) baseTryFind

    let tables =
        includedRelations
        |> Seq.choose (fun tbl ->
            let tableCols = columns |> Map.tryFind (tbl.Schema, tbl.Name) |> Option.defaultValue []

            let tableType = if isBaseTableType tbl.Type then TableType.Table else TableType.View

            let tableSchema =
                {
                    TableSchema.Catalog = tbl.Catalog
                    TableSchema.Schema = tbl.Schema
                    TableSchema.Name = tbl.Name
                    TableSchema.Type = tableType
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
                            Column.IsReadOnly = col.IsComputed
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
                        Column.IsReadOnly = col.IsComputed
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
                    Table.Type = tableType
                    Table.Columns = filteredColumns
                    Table.TotalColumns = tableCols |> List.length
                }
        )
        |> Seq.toList

    {
        Tables = tables
        Enums = enums
    }

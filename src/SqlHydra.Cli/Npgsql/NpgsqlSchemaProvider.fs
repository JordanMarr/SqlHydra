﻿module SqlHydra.Npgsql.NpgsqlSchemaProvider

open System.Data
open SqlHydra.Domain
open SqlHydra

let getSchema (cfg: Config) : Schema =
    use conn = new Npgsql.NpgsqlConnection(cfg.ConnectionString)
    conn.Open()
    // NOTE: GetSchema will fail if a Postgres enum doesn't exists in a custom schema but not in public schema.
    // Error: "type {enum name} does not exist"
    // This is a Postgres issue, not a SqlHydra issue.
    let sTables = conn.GetSchema("Tables", cfg.Filters.TryGetRestrictionsByKey("Tables"))
    let sColumns = conn.GetSchema("Columns", cfg.Filters.TryGetRestrictionsByKey("Columns"))
    let sViews = conn.GetSchema("Views", cfg.Filters.TryGetRestrictionsByKey("Views"))
    let sMaterializedViews = conn.GetSchema("MaterializedViews", cfg.Filters.TryGetRestrictionsByKey("MaterializedViews"))
    
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
            {| 
                TableCatalog = col["TABLE_CATALOG"] :?> string
                TableSchema = col["TABLE_SCHEMA"] :?> string
                TableName = col["TABLE_NAME"] :?> string
                ColumnName = col["COLUMN_NAME"] :?> string
                ProviderTypeName = col["DATA_TYPE"] :?> string
                OrdinalPosition = col["ORDINAL_POSITION"] :?> int
                IsNullable = 
                    match col["IS_NULLABLE"] :?> string with 
                    | "YES" -> true
                    | _ -> false
            |}
        )
        |> Seq.sortBy (fun column -> column.OrdinalPosition)

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
        |> Seq.append materializedViews
        |> SchemaFilters.filterTables cfg.Filters
        |> Seq.choose (fun tbl -> 
            let tableColumns = 
                allColumns
                |> Seq.filter (fun col -> 
                    col.TableCatalog = tbl.Catalog && 
                    col.TableSchema = tbl.Schema &&
                    col.TableName = tbl.Name
                )

            let mappedColumns = 
                tableColumns
                |> Seq.choose (fun col -> 
                    NpgsqlDataTypes.tryFindTypeMapping(col.ProviderTypeName)
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

            let enumColumns = 
                tableColumns
                |> Seq.choose (fun col -> 
                    let fullyQualified = enums |> List.tryFind (fun e -> col.ProviderTypeName = $"{e.Schema}.{e.Name}")
                    let unqualified = enums |> List.tryFind (fun e -> col.ProviderTypeName = e.Name)

                    // The same enum can exist in different schemas.
                    // So ideally, col.ProviderTypeName has a fully qualified enum type (schema.enumName).
                    // If no qualified enum is found, then just use the first unqualified enum.
                    fullyQualified 
                    |> Option.orElse unqualified
                    |> Option.map (fun enum -> col, enum)
                )
                |> Seq.map (fun (col, enum) ->
                    {
                        Column.Name = col.ColumnName
                        Column.IsNullable = col.IsNullable
                        Column.TypeMapping = 
                            { 
                                TypeMapping.ColumnTypeAlias = col.ProviderTypeName
                                TypeMapping.ClrType =                       // Enum type (will be generated)
                                    if col.TableSchema <> enum.Schema
                                    then $"{enum.Schema}.{enum.Name}"       // Enum lives in a different schema/module
                                    else enum.Name                          // Enum lives in this module
                                TypeMapping.DbType = DbType.Object
                                TypeMapping.ReaderMethod = "GetFieldValue"  // Requires registration with Npgsql via `MapEnum`
                                TypeMapping.ProviderDbType = None
                            }
                        Column.IsPK = pks.Contains(col.TableSchema, col.TableName, col.ColumnName)
                    }
                )
                |> Seq.toList

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
                    Table.TotalColumns = tableColumns |> Seq.length
                }
        )
        |> Seq.toList

    { 
        Tables = tables
        Enums = enums
        PrimitiveTypeReaders = NpgsqlDataTypes.primitiveTypeReaders
    }
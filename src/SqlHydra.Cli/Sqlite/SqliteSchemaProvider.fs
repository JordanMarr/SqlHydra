module SqlHydra.Sqlite.SqliteSchemaProvider

open Microsoft.Data.Sqlite
open SqlHydra.Domain
open SqlHydra

[<Literal>]
let private MainSchema = "main"

type private SchemaRow = {
    TableName: string
    TableType: string
    Column: ColumnSchema
}

type private TableRestrictions = {
    Catalog: string option
    Schema: string option
    Name: string option
    TableType: string option
}

type private ColumnRestrictions = {
    Catalog: string option
    Schema: string option
    Table: string option
    Name: string option
}

/// Determines whether a column should be treated as nullable.
/// From SQLite docs: PRIMARY KEY columns are implicitly NOT NULL only for INTEGER PRIMARY KEY,
/// WITHOUT ROWID, and STRICT tables. Other PK types may allow NULL due to a legacy SQLite bug.
/// https://www.sqlite.org/lang_createtable.html#the_primary_key
let private isColumnNullable (notnull: int) (pk: int) (dataType: string) (withoutRowId: int) (strict: int) =
    let isPkImplicitlyNotNull =
        pk > 0
        && (dataType.Equals("INTEGER", System.StringComparison.OrdinalIgnoreCase)
            || withoutRowId <> 0
            || strict <> 0)

    notnull = 0 && not isPkImplicitlyNotNull

let private matchesTableTypeRestriction (restriction: string) (tableType: string) =
    let normalize (value: string) = value.Trim().ToUpperInvariant()
    let restrictionType = normalize restriction
    let actualTableType = normalize tableType

    match restrictionType with
    | "TABLE"
    | "BASE TABLE" -> actualTableType = "TABLE" || actualTableType = "VIRTUAL"
    | "VIEW" -> actualTableType = "VIEW"
    | "SYSTEM_TABLE" -> false
    | _ -> actualTableType = restrictionType

let private restrictionAt index (values: string array) =
    match values |> Array.tryItem index with
    | Some value when not (isNull value) ->
        let trimmed = value.Trim()
        if System.String.IsNullOrWhiteSpace(trimmed) then None else Some trimmed
    | _ -> None

let private parseTableRestrictions (values: string array) =
    {
        Catalog = restrictionAt 0 values
        Schema = restrictionAt 1 values
        Name = restrictionAt 2 values
        TableType = restrictionAt 3 values
    }

let private parseColumnRestrictions (values: string array) =
    {
        Catalog = restrictionAt 0 values
        Schema = restrictionAt 1 values
        Table = restrictionAt 2 values
        Name = restrictionAt 3 values
    }

let private matchesRestriction (restriction: string option) (value: string) =
    restriction
    |> Option.forall _.Equals(value, System.StringComparison.OrdinalIgnoreCase)

let private matchesTableRestrictions (restriction: TableRestrictions) (row: SchemaRow) =
    matchesRestriction restriction.Catalog MainSchema
    && matchesRestriction restriction.Schema MainSchema
    && matchesRestriction restriction.Name row.TableName
    && (restriction.TableType
        |> Option.forall (fun expected -> matchesTableTypeRestriction expected row.TableType))

let private matchesColumnRestrictions (restriction: ColumnRestrictions) (row: SchemaRow) =
    matchesRestriction restriction.Catalog MainSchema
    && matchesRestriction restriction.Schema MainSchema
    && matchesRestriction restriction.Table row.TableName
    && matchesRestriction restriction.Name row.Column.Name

let private readAllSchemaRows (conn: SqliteConnection) =
    use cmd = conn.CreateCommand()

    cmd.CommandText <-
        """
        SELECT
            t.name      AS TABLE_NAME,
            t.type      AS TABLE_TYPE,
            t.wr        AS WITHOUT_ROWID,
            t.strict    AS IS_STRICT,
            p.name      AS COLUMN_NAME,
            p.type      AS DATA_TYPE,
            p.cid       AS ORDINAL_POSITION,
            p."notnull" AS NOT_NULL,
            p.pk        AS PRIMARY_KEY
        FROM pragma_table_list() t
        JOIN pragma_table_info(t.name) p
        WHERE t.type IN ('table', 'view', 'virtual')
          AND t.name NOT LIKE 'sqlite_%'
        ORDER BY TABLE_NAME, ORDINAL_POSITION"""

    use reader = cmd.ExecuteReader()
    let tableName = reader.GetOrdinal("TABLE_NAME")
    let tableType = reader.GetOrdinal("TABLE_TYPE")
    let withoutRowId = reader.GetOrdinal("WITHOUT_ROWID")
    let isStrict = reader.GetOrdinal("IS_STRICT")
    let columnName = reader.GetOrdinal("COLUMN_NAME")
    let dataType = reader.GetOrdinal("DATA_TYPE")
    let ordinalPos = reader.GetOrdinal("ORDINAL_POSITION")
    let notNull = reader.GetOrdinal("NOT_NULL")
    let primaryKey = reader.GetOrdinal("PRIMARY_KEY")

    [ while reader.Read() do
          let colDataType = if reader.IsDBNull(dataType) then "" else reader.GetString(dataType)
          yield
              {
                  TableName = reader.GetString(tableName)
                  TableType = reader.GetString(tableType)
                  Column = {
                      ColumnSchema.Catalog = MainSchema
                      Schema = MainSchema
                      Table = reader.GetString(tableName)
                      Name = reader.GetString(columnName)
                      ProviderTypeName = colDataType
                      Ordinal = reader.GetInt32(ordinalPos)
                      IsNullable = isColumnNullable (reader.GetInt32(notNull)) (reader.GetInt32(primaryKey)) colDataType (reader.GetInt32(withoutRowId)) (reader.GetInt32(isStrict))
                      IsPrimaryKey = reader.GetInt32(primaryKey) > 0
                      Precision = None
                      Scale = None
                      IsComputed = false
                      DefaultValue = None
                  }
              } ]

/// Reads tables and columns from the database using PRAGMA functions.
/// Microsoft.Data.Sqlite doesn't support GetSchema(...) like System.Data.SQLite did:
/// https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/metadata#schema-metadata
let private readSchema (conn: SqliteConnection) (tableRestrictions: string array) (columnRestrictions: string array) =
    let allRows = readAllSchemaRows conn

    let parsedTableRestrictions = parseTableRestrictions tableRestrictions
    let parsedColumnRestrictions = parseColumnRestrictions columnRestrictions

    let rows =
        allRows
        |> List.filter (fun row ->
            matchesTableRestrictions parsedTableRestrictions row
            && matchesColumnRestrictions parsedColumnRestrictions row
        )

    let tables =
        rows
        |> List.map (fun row -> row.TableName, row.TableType)
        |> List.distinctBy fst
    let columns =
        rows |> List.map _.Column
    tables, columns

let getSchema (cfg: Config, isLegacy: bool, extensions: IExtendTypeMapping list) : Schema =
    use conn = new SqliteConnection(cfg.ConnectionString)
    conn.Open()

    let tableRestrictions = cfg.Filters.TryGetRestrictionsByKey("Tables")
    let columnRestrictions = cfg.Filters.TryGetRestrictionsByKey("Columns")
    let sTables, sColumns = readSchema conn tableRestrictions columnRestrictions

    let columnsByTable =
        sColumns
        |> List.sortBy _.Ordinal
        |> Seq.groupBy _.Table
        |> Map.ofSeq

    let tryFindTypeMapping =
        let baseTryFind = SqliteDataTypes.tryFindTypeMapping isLegacy
        extensions |> List.fold (fun acc (ext: IExtendTypeMapping) -> ext.Extend(acc)) baseTryFind

    let tableSchemas =
        sTables
        |> List.map (fun (name, tableType) ->
            let cols =
                columnsByTable
                |> Map.tryFind name
                |> Option.map Seq.toList
                |> Option.defaultValue []
            {
                TableSchema.Catalog = MainSchema
                Schema = MainSchema
                Name = name
                Type = if tableType.Equals("view", System.StringComparison.OrdinalIgnoreCase) then TableType.View else TableType.Table
                Columns = cols
            }
        )
        |> SchemaFilters.filterTables cfg.Filters

    let tables =
        tableSchemas
        |> Seq.choose (fun tableSchema ->
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
        |> Seq.toList

    {
        Tables = tables
        Enums = []
    }

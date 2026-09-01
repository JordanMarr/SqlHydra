module SqlHydra.Domain

open System.Data

let private valueTypes = 
    Set [ "bool"; "int"; "int64"; "int16"; "byte"; "decimal"; "double"; "System.Single"
          "System.DateTimeOffset"; "System.DateTime"; "System.DateOnly"; "System.TimeOnly"
          "System.Guid"; ]

let isValueType (typeName: string) = 
    valueTypes.Contains typeName

/// Represents the mapping between a database column type and the generated F# type.
type TypeMapping =
    {
        /// The fully-qualified CLR type name to use in generated code.
        /// Example: "string", "System.DateTimeOffset", "MyApp.Types.EmailAddress".
        ClrType: string

        /// The ADO.NET DbType used when binding parameters for this column.
        /// This is provider-agnostic and applies to all database providers.
        DbType: DbType

        /// The provider-specific database type used for parameter binding.
        ///
        /// When set, SqlHydra.Query will assign this value to the provider's
        /// parameter type enum (e.g., SqlDbType, NpgsqlDbType, OracleDbType).
        ///
        /// Use this when the provider requires a specific enum value for correct
        /// parameter binding (e.g., "Jsonb", "Vector", "UniqueIdentifier").
        ///
        /// Leave as None for providers that do not have provider-specific
        /// parameter types (e.g., SQLite), or when the default provider mapping
        /// is sufficient.
        ProviderDbType: string option

        /// The original database column type name (e.g., "varchar", "jsonb").
        /// This is used for code generation and for extension authors to
        /// preserve or override the provider's type alias.
        ColumnTypeAlias: string
    }
    member this.IsValueType() = 
        isValueType this.ClrType

type Column = 
    {
        Name: string
        TypeMapping: TypeMapping
        IsNullable: bool
        IsPK: bool

        /// True when the database owns the value and rejects a statement that names the
        /// column: a generated column, or `GENERATED ALWAYS AS IDENTITY`.
        IsReadOnly: bool
        /// Doc-comment lines emitted above the generated field, one `///` line each.
        ///
        /// A caution that lives only in an extension's README reaches whoever configured
        /// the extension and nobody else. This puts it on the field, where the person
        /// reaching for the column is looking.
        Doc: string list
    }

type TableType = 
    | Table = 0
    | View = 1

type Table = 
    {
        Catalog: string
        Schema: string
        Name: string
        Type: TableType
        Columns: Column list
        TotalColumns: int
    }

type EnumLabel = 
    {
        Name: string
        SortOrder: int
    }

type Enum = 
    {
        Name: string
        Schema: string
        Labels: EnumLabel list
    }

type Schema =
    {
        Tables: Table list

        /// Support for Postgres enums
        Enums: Enum list
    }

type ReadersConfig = 
    {
        /// A fully qualified reader type. Ex: "Microsoft.Data.SqlClient.SqlDataReader"
        ReaderType: string
    }

type Filters = 
    {
        /// Glob patterns to include "{schema}/{table}.{column}"
        Includes: string list
        /// Glob patterns to exclude "{schema}/{table}.{column}"
        Excludes: string list        
        /// Restrictions applied to GetSchema() calls. Ex: Map [ "Tables", [| "dbo" |]; "Views", [||]; "Columns", [||] ]
        Restrictions: Map<string, string array>
    }
    static member Empty = { Includes = []; Excludes = []; Restrictions = Map.empty }
    member this.TryGetRestrictionsByKey (key: string) = 
        this.Restrictions.TryFind key 
        |> Option.defaultValue [||]        

type Config = 
    {
        /// General: Db conneciton string
        ConnectionString: string

        /// General: path to the generated .fs output file
        OutputFile: string

        /// General: namespace for the generated .fs output file
        Namespace: string

        /// General: if true, makes generated table records CLIMutable
        IsCLIMutable: bool

        /// General: if true, makes generated table record properties mutable
        IsMutableProperties: bool

        /// General: determines whether to use F# Option or System.Nullable for nullable columns.
        NullablePropertyType: NullablePropertyType
        
        /// SqlHydra.Query Integration: generates support for creating Db specific parameter types
        ProviderDbTypeAttributes: bool
        
        /// SqlHydra.Query Integration: creates a SqlHydra.Query table declaration for each table
        TableDeclarations: bool

        /// SqlHydra.Query Integration: generates a `LeftJoined` module per schema with a
        /// left-view record per table (every column nullable), for `leftJoin` sources.
        LeftJoinedViews: bool

        /// Readers: provides a Db provider specific IDataReader type (for access to Db-specific features)
        Readers: ReadersConfig option
        
        /// Filters: optional filters for schemas, tables and columns
        Filters: Filters

        /// Extensions: type mapping extension assembly names
        TypeMappingExtensions: string list
    }

and [<RequireQualifiedAccess>] 
    NullablePropertyType = 
    | Option
    | Nullable

type ProviderType =
    | SqlServer
    | Npgsql
    | Sqlite
    | MySql
    | Oracle
    | Custom of string

type ColumnSchema =
    {
        Catalog: string
        Schema: string
        Table: string
        Name: string
        ProviderTypeName: string
        IsNullable: bool
        Ordinal: int
        Precision: int option
        Scale: int option
        IsPrimaryKey: bool
        IsComputed: bool
        DefaultValue: string option
    }

type TableSchema =
    {
        Catalog: string
        Schema: string
        Name: string
        Type: TableType
        Columns: ColumnSchema list
    }

type ISqlHydraExtension = interface end

type TypeMappingContext =
    {
        Table: TableSchema
        Column: ColumnSchema
    }

type IExtendTypeMapping =
    inherit ISqlHydraExtension
    abstract member Extend: baseTryFind: (TypeMappingContext -> TypeMapping option) -> (TypeMappingContext -> TypeMapping option)

type NamingContext =
    {
        Table: Table
        Column: Column option
    }

type IExtendNaming =
    inherit ISqlHydraExtension
    abstract member ExtendTableName: baseFn: (NamingContext -> string) -> (NamingContext -> string)
    abstract member ExtendColumnName: baseFn: (NamingContext -> string) -> (NamingContext -> string)

/// What an extension is handed when it is offered a table to contribute columns to.
type ColumnContributionContext =
    {
        /// The table as the provider discovered it: type mappings resolved (extensions
        /// included) and column filters already applied. `Type` distinguishes a base table
        /// from a view, and `Columns` is what was found, so an extension can decide per
        /// table rather than blanketing the schema.
        Table: Table

        /// Which provider discovered it. A column that only exists on one database engine
        /// is contributed only when this matches.
        Provider: ProviderType
    }

/// Contributes columns that the provider's discovery cannot see.
///
/// A schema provider learns its columns from the catalog, so a column the catalog does not
/// list -- a PostgreSQL system column such as `xmin`, a pseudo-column, a column projected by
/// a companion library -- can never be typed, named or emitted, because no later stage is
/// ever asked about it. This runs once over the finished schema, after discovery and type
/// mapping and before emission, and returns the columns to append to a table.
///
/// A contributed `Column` is an ordinary one from there on: its `TypeMapping.ProviderDbType`
/// becomes a `[<ProviderDbType(...)>]` attribute, `IExtendNaming` renames it like any other,
/// and whatever marks a column as one the database owns applies to it too.
///
/// Contributing a name the table already has is an error rather than an override: an
/// extension that silently shadows a discovered column produces a file that compiles and is
/// wrong.
type IContributeColumns =
    inherit ISqlHydraExtension
    abstract member Contribute: baseFn: (ColumnContributionContext -> Column list) -> (ColumnContributionContext -> Column list)

type ISqlHydraDbProvider =
    abstract member Id: string
    abstract member Name: string
    abstract member Type: ProviderType
    abstract member DefaultReaderType: string
    abstract member DefaultProvider: string
    abstract member SqlEmitter: string
    abstract member ProviderConnectionType: string
    abstract member GetSchema: cfg: Config * isLegacy: bool * extensions: IExtendTypeMapping list -> Schema
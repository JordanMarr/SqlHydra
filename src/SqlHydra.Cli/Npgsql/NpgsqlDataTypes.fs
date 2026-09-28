module SqlHydra.Npgsql.NpgsqlDataTypes

open System.Data
open NpgsqlTypes
open SqlHydra.Domain

/// A list of supported column type mappings
let supportedTypeMappings isLegacy = // https://www.npgsql.org/doc/types/basic.html
    [//  ColumnTypeAlias                ClrType                                          DbType                  ProviderDbType                          ArrayBaseType
        "boolean",                      "bool",                                          DbType.Boolean,         Some (nameof NpgsqlDbType.Boolean),     Some NpgsqlDbType.Boolean
        "smallint",                     "int16",                                         DbType.Int16,           Some (nameof NpgsqlDbType.Smallint),    Some NpgsqlDbType.Smallint
        "integer",                      "int",                                           DbType.Int32,           Some (nameof NpgsqlDbType.Integer),     Some NpgsqlDbType.Integer
        "bigint",                       "int64",                                         DbType.Int64,           Some (nameof NpgsqlDbType.Bigint),      Some NpgsqlDbType.Bigint
        "real",                         "double",                                        DbType.Double,          Some (nameof NpgsqlDbType.Real),        Some NpgsqlDbType.Real
        "double precision",             "double",                                        DbType.Double,          Some (nameof NpgsqlDbType.Double),      Some NpgsqlDbType.Double
        "numeric",                      "decimal",                                       DbType.Decimal,         Some (nameof NpgsqlDbType.Numeric),     Some NpgsqlDbType.Numeric
        "money",                        "decimal",                                       DbType.Decimal,         Some (nameof NpgsqlDbType.Money),       Some NpgsqlDbType.Money
        "text",                         "string",                                        DbType.String,          Some (nameof NpgsqlDbType.Text),        Some NpgsqlDbType.Text
        "character varying",            "string",                                        DbType.String,          Some (nameof NpgsqlDbType.Varchar),     None
        "character",                    "string",                                        DbType.String,          Some (nameof NpgsqlDbType.Char),        None
        "citext",                       "string",                                        DbType.String,          Some (nameof NpgsqlDbType.Citext),      None
        "json",                         "string",                                        DbType.String,          Some (nameof NpgsqlDbType.Json),        None
        "jsonb",                        "string",                                        DbType.String,          Some (nameof NpgsqlDbType.Jsonb),       None
        "xml",                          "string",                                        DbType.String,          Some (nameof NpgsqlDbType.Xml),         None
        // skipped unsupported types
        "bit(1)",                       "bool",                                          DbType.Boolean,         Some (nameof NpgsqlDbType.Bit),         Some NpgsqlDbType.Bit
        // skipped unsupported types
        "uuid",                         "System.Guid",                                   DbType.Guid,            Some (nameof NpgsqlDbType.Uuid),        Some NpgsqlDbType.Uuid
        "cidr",                         "System.Net.IPNetwork",                          DbType.Object,          Some (nameof NpgsqlDbType.Cidr),        Some NpgsqlDbType.Cidr
        "inet",                         "System.Net.IPAddress",                          DbType.Object,          Some (nameof NpgsqlDbType.Inet),        Some NpgsqlDbType.Inet
        "macaddr",                      "System.Net.NetworkInformation.PhysicalAddress", DbType.Object,          Some (nameof NpgsqlDbType.MacAddr),     Some NpgsqlDbType.MacAddr
        "macaddr8",                     "System.Net.NetworkInformation.PhysicalAddress", DbType.Object,          Some (nameof NpgsqlDbType.MacAddr8),    Some NpgsqlDbType.MacAddr8
        // skipped unsupported types
        "interval",                     "System.TimeSpan",                               DbType.Time,            Some (nameof NpgsqlDbType.Interval),    Some NpgsqlDbType.Interval

        if isLegacy then
         "date",                        "System.DateTime",                               DbType.DateTime,        Some (nameof NpgsqlDbType.Date),        Some NpgsqlDbType.Date
         "time without time zone",      "System.TimeSpan",                               DbType.Time,            Some (nameof NpgsqlDbType.Time),        Some NpgsqlDbType.Time
        else
         "date",                        "System.DateOnly",                               DbType.DateTime,        Some (nameof NpgsqlDbType.Date),        Some NpgsqlDbType.Date
         "time without time zone",      "System.TimeOnly",                               DbType.Time,            Some (nameof NpgsqlDbType.Time),        Some NpgsqlDbType.Time

        "timestamp with time zone",     "System.DateTime",                               DbType.DateTime,        Some (nameof NpgsqlDbType.TimestampTz), Some NpgsqlDbType.TimestampTz
        "timestamp without time zone",  "System.DateTime",                               DbType.DateTime,        Some (nameof NpgsqlDbType.Timestamp),   Some NpgsqlDbType.Timestamp
        "time with time zone",          "System.DateTimeOffset",                         DbType.DateTimeOffset,  Some (nameof NpgsqlDbType.TimeTz),      Some NpgsqlDbType.TimeTz
        "bytea",                        "byte[]",                                        DbType.Binary,          Some (nameof NpgsqlDbType.Bytea),       None
        // skipped unsupported types
        "name",                         "string",                                        DbType.String,          Some (nameof NpgsqlDbType.Name),        None
        "(internal) char",              "char",                                          DbType.String,          Some (nameof NpgsqlDbType.InternalChar), Some NpgsqlDbType.InternalChar
        // skipped unsupported types
    ]
    /// Programmatically add array mappings (where ArrayType is Some)
    |> List.collect (fun (columnTypeAlias, clrType, dbType, providerDbType, arrayBaseType) ->
        [
            columnTypeAlias, clrType, dbType, providerDbType
            if arrayBaseType.IsSome then
                let npgsqlDbType = arrayBaseType.Value
                yield!
                    [ for suffix in [ "[]"; " []"; " array" ] do
                        let columnArrayTypeAlias = $"{columnTypeAlias}{suffix}"                         // ex: "text" becomes: "text[]", "text []", "text array"
                        let clrArrayType = $"{clrType}[]"                                               // ex: "string" becomes: "string[]"
                        let providerDbArrayType = Some $"{npgsqlDbType},{nameof NpgsqlDbType.Array}"    // ex: "Text,Array" (which can be parsed by Enum.Parse when setting parameter NpgsqlDbType property)
                        columnArrayTypeAlias, clrArrayType, dbType, providerDbArrayType ]
        ]
    )

let typeMappingsByName isLegacy =
    supportedTypeMappings isLegacy
    |> List.map (fun (columnTypeAlias, clrType, dbType, providerDbType) ->
        columnTypeAlias,
        {
            TypeMapping.ColumnTypeAlias = columnTypeAlias
            TypeMapping.ClrType = clrType
            TypeMapping.DbType = dbType
            TypeMapping.ProviderDbType = providerDbType
        }
    )
    |> Map.ofList

let tryFindTypeMapping isLegacy =
    let map = typeMappingsByName isLegacy
    fun (ctx: TypeMappingContext) ->
        map.TryFind (ctx.Column.ProviderTypeName.ToLower().Trim())

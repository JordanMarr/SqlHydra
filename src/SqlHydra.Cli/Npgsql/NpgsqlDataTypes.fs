module SqlHydra.Npgsql.NpgsqlDataTypes

open System.Data
open NpgsqlTypes
open SqlHydra.Domain

let private r : Npgsql.NpgsqlDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings =
    [// ColumnTypeAlias                 ClrType                 DbType              ProviderDbType                      ReaderMethod            ArrayBaseType
        "boolean",                      "bool",                 DbType.Boolean,     None,                               nameof r.GetBoolean,    Some NpgsqlDbType.Boolean
        "smallint",                     "int16",                DbType.Int16,       None,                               nameof r.GetInt16,      Some NpgsqlDbType.Smallint
        "integer",                      "int",                  DbType.Int32,       None,                               nameof r.GetInt32,      Some NpgsqlDbType.Integer
        "bigint",                       "int64",                DbType.Int64,       None,                               nameof r.GetInt64,      Some NpgsqlDbType.Bigint
        "real",                         "double",               DbType.Double,      None,                               nameof r.GetDouble,     Some NpgsqlDbType.Real
        "double precision",             "double",               DbType.Double,      None,                               nameof r.GetDouble,     Some NpgsqlDbType.Double
        "numeric",                      "decimal",              DbType.Decimal,     None,                               nameof r.GetDecimal,    Some NpgsqlDbType.Numeric
        "money",                        "decimal",              DbType.Decimal,     None,                               nameof r.GetDecimal,    Some NpgsqlDbType.Money
        "text",                         "string",               DbType.String,      None,                               nameof r.GetString,     Some NpgsqlDbType.Text
        "character varying",            "string",               DbType.String,      None,                               nameof r.GetString,     None
        "character",                    "string",               DbType.String,      None,                               nameof r.GetString,     None
        "citext",                       "string",               DbType.String,      None,                               nameof r.GetString,     None
        "json",                         "string",               DbType.String,      Some (nameof NpgsqlDbType.Json),    nameof r.GetString,     None
        "jsonb",                        "string",               DbType.String,      Some (nameof NpgsqlDbType.Jsonb),   nameof r.GetString,     None
        "xml",                          "string",               DbType.String,      None,                               nameof r.GetString,     None
        // skipped unsupported types
        "bit(1)",                       "bool",                 DbType.Boolean,     None,                               nameof r.GetBoolean,    Some NpgsqlDbType.Bit
        // skipped unsupported types
        "uuid",                         "System.Guid",          DbType.Guid,        None,                               nameof r.GetGuid,       Some NpgsqlDbType.Uuid
        // skipped unsupported types
        "interval",                     "System.TimeSpan",      DbType.Time,        None,                               nameof r.GetTimeSpan,   Some NpgsqlDbType.Interval
        "date",                         "System.DateOnly",      DbType.DateTime,    None,                               "GetDateOnly",          Some NpgsqlDbType.Date
        "time without time zone",       "System.TimeOnly",      DbType.Time,        None,                               "GetTimeOnly",          Some NpgsqlDbType.Time
        "timestamp with time zone",     "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime,   Some NpgsqlDbType.TimestampTz 
        "timestamp without time zone",  "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime,   Some NpgsqlDbType.Timestamp
        "time with time zone",          "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime,   Some NpgsqlDbType.TimeTz
        "bytea",                        "byte[]",               DbType.Binary,      None,                               nameof r.GetValue,      None
        // skipped unsupported types
        "name",                         "string",               DbType.String,      None,                               nameof r.GetString,     None
        "(internal) char",              "char",                 DbType.String,      None,                               nameof r.GetChar,       Some NpgsqlDbType.Char
        // skipped unsupported types
    ]
    /// Programmically add array mappings (where ArrayType is Some)
    |> List.collect (fun (columnTypeAlias, clrType, dbType, providerDbType, readerMethod, arrayBaseType) ->
        [
            columnTypeAlias, clrType, dbType, providerDbType, readerMethod
            if arrayBaseType.IsSome then
                let npgsqlDbType = arrayBaseType.Value
                yield!
                    [ for suffix in [ "[]"; " []"; " array" ] do
                        let columnArrayTypeAlias = $"{columnTypeAlias}{suffix}"                         // ex: "text" becomes: "text[]", "text []", "text array"
                        let clrArrayType = $"{clrType}[]"                                               // ex: "string" becomes: "string[]"
                        let providerDbArrayType = Some $"{npgsqlDbType},{nameof NpgsqlDbType.Array}"    // ex: "Text,Array" (which can be parsed by Enum.Parse when setting parameter NpgsqlDbType property)
                        let readerMethodArray = nameof r.GetFieldValue
                        columnArrayTypeAlias, clrArrayType, dbType, providerDbArrayType, readerMethodArray ]
        ]
    )

let typeMappingsByName =
    supportedTypeMappings
    
    |> List.map (fun (columnTypeAlias, clrType, dbType, providerDbType, readerMethod) ->
        columnTypeAlias,
        { 
            TypeMapping.ColumnTypeAlias = columnTypeAlias
            TypeMapping.ClrType = clrType
            TypeMapping.DbType = dbType
            TypeMapping.ProviderDbType = providerDbType
            TypeMapping.ReaderMethod = readerMethod
        }
    )
    |> Map.ofList

let tryFindTypeMapping (providerTypeName: string) = 
    typeMappingsByName.TryFind (providerTypeName.ToLower().Trim())

let primitiveTypeReaders = 
    supportedTypeMappings
    |> List.map(fun (_, clrType, _, _, readerMethod) ->
        { PrimitiveTypeReader.ClrType = clrType; PrimitiveTypeReader.ReaderMethod = readerMethod }
    )
    |> List.distinctBy (fun ptr -> ptr.ClrType)

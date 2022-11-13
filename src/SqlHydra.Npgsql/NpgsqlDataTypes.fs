module SqlHydra.Npgsql.NpgsqlDataTypes

open System.Data
open NpgsqlTypes
open SqlHydra.Domain

let private r : Npgsql.NpgsqlDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings =
    [// ColumnTypeAlias                 ClrType                 DbType              ProviderDbType                      ReaderMethod
        "boolean",                      "bool",                 DbType.Boolean,     None,                               nameof r.GetBoolean
        "smallint",                     "int16",                DbType.Int16,       None,                               nameof r.GetInt16
        "integer",                      "int",                  DbType.Int32,       None,                               nameof r.GetInt32
        "bigint",                       "int64",                DbType.Int64,       None,                               nameof r.GetInt64
        "real",                         "double",               DbType.Double,      None,                               nameof r.GetDouble
        "double precision",             "double",               DbType.Double,      None,                               nameof r.GetDouble
        "numeric",                      "decimal",              DbType.Decimal,     None,                               nameof r.GetDecimal
        "money",                        "decimal",              DbType.Decimal,     None,                               nameof r.GetDecimal
        "text",                         "string",               DbType.String,      None,                               nameof r.GetString
        "character varying",            "string",               DbType.String,      None,                               nameof r.GetString
        "character",                    "string",               DbType.String,      None,                               nameof r.GetString
        "citext",                       "string",               DbType.String,      None,                               nameof r.GetString
        "json",                         "string",               DbType.String,      Some (nameof NpgsqlDbType.Json),    nameof r.GetString
        "jsonb",                        "string",               DbType.String,      Some (nameof NpgsqlDbType.Jsonb),   nameof r.GetString
        "xml",                          "string",               DbType.String,      None,                               nameof r.GetString
        // skipped unsupported types
        "bit(1)",                       "bool",                 DbType.Boolean,     None,                               nameof r.GetBoolean
        // skipped unsupported types
        "uuid",                         "System.Guid",          DbType.Guid,        None,                               nameof r.GetGuid
        // skipped unsupported types
        "interval",                     "System.TimeSpan",      DbType.Time,        None,                               nameof r.GetTimeSpan
#if NET5_0
        "date",                         "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime 
        "time without time zone",       "System.TimeSpan",      DbType.Time,        None,                               nameof r.GetTimeSpan 
#endif
#if NET6_0_OR_GREATER
        "date",                         "System.DateOnly",      DbType.DateTime,    None,                               "GetDateOnly"
        "time without time zone",       "System.TimeOnly",      DbType.Time,        None,                               "GetTimeOnly"
#endif
        "timestamp with time zone",     "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime 
        "timestamp without time zone",  "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime 
        "time with time zone",          "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime 
        "bytea",                        "byte[]",               DbType.Binary,      None,                               nameof r.GetValue 
        // skipped unsupported types
        "name",                         "string",               DbType.String,      None,                               nameof r.GetString
        "(internal) char",              "char",                 DbType.String,      None,                               nameof r.GetChar
        // skipped unsupported types

        // "Text,Array" can be parsed by Enum.Parse.
        let textArray = $"{nameof NpgsqlDbType.Text},{nameof NpgsqlDbType.Array}"
        "text[]",                       "string[]",             DbType.String,      Some textArray,                     nameof r.GetValue
        
        let integerArray = $"{nameof NpgsqlDbType.Integer},{nameof NpgsqlDbType.Array}"
        "integer[]",                    "int[]",                DbType.Int32,       Some integerArray,                  nameof r.GetValue
    ]

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

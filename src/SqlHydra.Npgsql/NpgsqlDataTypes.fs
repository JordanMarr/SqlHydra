module SqlHydra.Npgsql.NpgsqlDataTypes

open System.Data
open NpgsqlTypes
open SqlHydra.Domain

let private r : Npgsql.NpgsqlDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings =
    [ 
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
        "date",                         "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime 
        "interval",                     "System.TimeSpan",      DbType.Time,        None,                               nameof r.GetTimeSpan
        "timestamp without time zone",  "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime 
        "timestamp with time zone",     "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime 
        "time without time zone",       "System.TimeSpan",      DbType.Time,        None,                               nameof r.GetTimeSpan 
        "time with time zone",          "System.DateTime",      DbType.DateTime,    None,                               nameof r.GetDateTime 
        "bytea",                        "byte[]",               DbType.Binary,      None,                               nameof r.GetValue 
        // skipped unsupported types
        "name",                         "string",               DbType.String,      None,                               nameof r.GetString
        "(internal) char",              "char",                 DbType.String,      None,                               nameof r.GetChar
        // skipped unsupported types
    ]

let typeMappingsByName =
    supportedTypeMappings
    |> List.map (fun (columnTypeAlias, clrType, dbType, providerDbType, readerMethod) ->
        columnTypeAlias,
        { 
            TypeMapping.ColumnTypeAlias = columnTypeAlias
            TypeMapping.ClrType = clrType
            TypeMapping.DbType = dbType
            TypeMapping.ReaderMethod = readerMethod
            TypeMapping.ProviderDbType = providerDbType
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

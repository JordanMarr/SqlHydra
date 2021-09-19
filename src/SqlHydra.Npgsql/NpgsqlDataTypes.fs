module SqlHydra.Npgsql.NpgsqlDataTypes

open System.Data
open SqlHydra.Domain

let private r : Npgsql.NpgsqlDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings =
    [ 
        "boolean",                      "bool",                 DbType.Boolean,         nameof r.GetBoolean
        "smallint",                     "int16",                DbType.Int16,           nameof r.GetInt16
        "integer",                      "int",                  DbType.Int32,           nameof r.GetInt32
        "bigint",                       "int64",                DbType.Int64,           nameof r.GetInt64
        "real",                         "double",               DbType.Double,          nameof r.GetDouble
        "double precision",             "double",               DbType.Double,          nameof r.GetDouble
        "numeric",                      "decimal",              DbType.Decimal,         nameof r.GetDecimal
        "money",                        "decimal",              DbType.Decimal,         nameof r.GetDecimal
        "text",                         "string",               DbType.String,          nameof r.GetString
        "character varying",            "string",               DbType.String,          nameof r.GetString
        "character",                    "string",               DbType.String,          nameof r.GetString
        "citext",                       "string",               DbType.String,          nameof r.GetString
        "json",                         "string",               DbType.String,          nameof r.GetString
        "jsonb",                        "string",               DbType.String,          nameof r.GetString
        "xml",                          "string",               DbType.String,          nameof r.GetString
        // skipped unsupported types
        "bit(1)",                       "bool",                 DbType.Boolean,         nameof r.GetBoolean
        // skipped unsupported types
        "uuid",                         "System.Guid",          DbType.Guid,            nameof r.GetGuid
        // skipped unsupported types
        "date",                         "System.DateTime",      DbType.DateTime,        nameof r.GetDateTime 
        "interval",                     "System.TimeSpan",      DbType.Time,            nameof r.GetTimeSpan
        "timestamp without time zone",  "System.DateTime",      DbType.DateTime,        nameof r.GetDateTime 
        "timestamp with time zone",     "System.DateTime",      DbType.DateTime,        nameof r.GetDateTime 
        "time without time zone",       "System.TimeSpan",      DbType.Time,            nameof r.GetTimeSpan 
        "time with time zone",          "System.DateTime",      DbType.DateTime,        nameof r.GetDateTime 
        "bytea",                        "byte[]",               DbType.Binary,          nameof r.GetValue 
        // skipped unsupported types
        "name",                         "string",               DbType.String,          nameof r.GetString
        "(internal) char",              "char",                 DbType.String,          nameof r.GetChar
        // skipped unsupported types
    ]

let typeMappingsByName =
    supportedTypeMappings
    |> List.map (fun (columnTypeAlias, clrType, dbType, readerMethod) ->
        columnTypeAlias,
        { 
            TypeMapping.ColumnTypeAlias = columnTypeAlias
            TypeMapping.ClrType = clrType
            TypeMapping.DbType = dbType
            TypeMapping.ReaderMethod = readerMethod
        }
    )
    |> Map.ofList

let tryFindTypeMapping (providerTypeName: string) = 
    typeMappingsByName.TryFind (providerTypeName.ToLower().Trim())

let primitiveTypeReaders = 
    supportedTypeMappings
    |> List.map(fun (_, clrType, _, readerMethod) ->
        { PrimitiveTypeReader.ClrType = clrType; PrimitiveTypeReader.ReaderMethod = readerMethod }
    )
    |> List.distinctBy (fun ptr -> ptr.ClrType)

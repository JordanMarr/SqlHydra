module SqlHydra.Npgsql.NpgsqlDataTypes

open System.Data
open NpgsqlTypes
open SqlHydra.Domain

let private r : Npgsql.NpgsqlDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings =    

    (* https://www.npgsql.org/doc/types/basic.html
     * ProviderDbTypes add attributes to the generated record fields that are used by the SqlHydra.Query to specify a parameter's NpgsqlDbType type.
     *)

    [// ColumnTypeAlias                 ClrType                 DbType              ProviderDbTypes                 ReaderMethod   
        "boolean",                      "bool",                 DbType.Boolean,     [],                             nameof r.GetBoolean
        "smallint",                     "int16",                DbType.Int16,       [],                             nameof r.GetInt16
        "integer",                      "int",                  DbType.Int32,       [],                             nameof r.GetInt32
        "bigint",                       "int64",                DbType.Int64,       [],                             nameof r.GetInt64
        "real",                         "double",               DbType.Double,      [],                             nameof r.GetDouble
        "double precision",             "double",               DbType.Double,      [],                             nameof r.GetDouble
        "numeric",                      "decimal",              DbType.Decimal,     [],                             nameof r.GetDecimal
        "money",                        "decimal",              DbType.Decimal,     [],                             nameof r.GetDecimal
        "text",                         "string",               DbType.String,      [],                             nameof r.GetString
        "character varying",            "string",               DbType.String,      [],                             nameof r.GetString
        "character",                    "string",               DbType.String,      [],                             nameof r.GetString
        "citext",                       "string",               DbType.String,      [],                             nameof r.GetString
        "json",                         "string",               DbType.String,      [nameof NpgsqlDbType.Json],     nameof r.GetString
        "jsonb",                        "string",               DbType.String,      [nameof NpgsqlDbType.Jsonb],    nameof r.GetString
        "xml",                          "string",               DbType.String,      [],                             nameof r.GetString
        // skipped unsupported types
        "bit(1)",                       "bool",                 DbType.Boolean,     [],                             nameof r.GetBoolean
        // skipped unsupported types
        "uuid",                         "System.Guid",          DbType.Guid,        [],                             nameof r.GetGuid
        // skipped unsupported types
        "interval",                     "System.TimeSpan",      DbType.Time,        [],                             nameof r.GetTimeSpan
#if NET5_0
        "date",                         "System.DateTime",      DbType.DateTime,    [],                             nameof r.GetDateTime 
        "time without time zone",       "System.TimeSpan",      DbType.Time,        [],                             nameof r.GetTimeSpan 
#endif
#if NET6_0_OR_GREATER
        "date",                         "System.DateOnly",      DbType.DateTime,    [],                             "GetDateOnly"
        "time without time zone",       "System.TimeOnly",      DbType.Time,        [],                             "GetTimeOnly"
#endif
        "timestamp with time zone",     "System.DateTime",      DbType.DateTime,    [],                             nameof r.GetDateTime 
        "timestamp without time zone",  "System.DateTime",      DbType.DateTime,    [],                             nameof r.GetDateTime 
        "time with time zone",          "System.DateTime",      DbType.DateTime,    [],                             nameof r.GetDateTime 
        "bytea",                        "byte[]",               DbType.Binary,      [],                             nameof r.GetValue 
        // skipped unsupported types
        "name",                         "string",               DbType.String,      [],                             nameof r.GetString
        "(internal) char",              "char",                 DbType.String,      [],                             nameof r.GetChar
        // skipped unsupported types

        // SqlHydra.Query will need to set parameter NpgsqlDbType = NpgsqlDbType.Text ||| NpgsqlDbType.Array
        let textArray = [nameof NpgsqlDbType.Text; nameof NpgsqlDbType.Array]
        "text[]",                       "string[]",             DbType.Object,      textArray,                      nameof r.GetValue
    ]

let typeMappingsByName =
    supportedTypeMappings
    |> List.map (fun (columnTypeAlias, clrType, dbType, providerDbTypes, readerMethod) ->
        columnTypeAlias,
        { 
            TypeMapping.ColumnTypeAlias = columnTypeAlias
            TypeMapping.ClrType = clrType
            TypeMapping.DbType = dbType
            TypeMapping.ProviderDbTypes = providerDbTypes
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

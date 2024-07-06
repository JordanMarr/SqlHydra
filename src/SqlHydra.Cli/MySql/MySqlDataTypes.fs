module SqlHydra.MySql.MySqlDataTypes

open System.Data
open MySql.Data.MySqlClient
open SqlHydra.Domain

let private r : System.Data.Common.DbDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings = // https://dev.mysql.com/doc/refman/9.0/en/data-types.html
    [// ColumnTypeAlias                 ClrType                 DbType                  ProviderDbType
        "bit",                          "int16",                DbType.Int16,           Some (nameof MySqlDbType.Bit),         nameof r.GetInt16
        "tinyint",                      "int16",                DbType.Int16,           Some (nameof MySqlDbType.Int16),       nameof r.GetInt16
        "bool",                         "int16",                DbType.Boolean,         Some (nameof MySqlDbType.Int16),       nameof r.GetBoolean
        "boolean",                      "int16",                DbType.Boolean,         Some (nameof MySqlDbType.Int16),       nameof r.GetBoolean
        "smallint",                     "int16",                DbType.Int16,           Some (nameof MySqlDbType.Int16),       nameof r.GetInt16
        "mediumint",                    "int",                  DbType.Int32,           Some (nameof MySqlDbType.Int24),       nameof r.GetInt32
        "int",                          "int",                  DbType.Int32,           Some (nameof MySqlDbType.Int32),       nameof r.GetInt32
        "integer",                      "int",                  DbType.Int32,           Some (nameof MySqlDbType.Int32),       nameof r.GetInt32
        "bigint",                       "int64",                DbType.Int64,           Some (nameof MySqlDbType.Int64),       nameof r.GetInt64
        "decimal",                      "decimal",              DbType.Decimal,         Some (nameof MySqlDbType.Decimal),     nameof r.GetDecimal
        "dec",                          "decimal",              DbType.Decimal,         Some (nameof MySqlDbType.Decimal),     nameof r.GetDecimal
        "float",                        "float",                DbType.Single,          Some (nameof MySqlDbType.Float),       nameof r.GetFloat
        "double",                       "double",               DbType.Double,          Some (nameof MySqlDbType.Double),      nameof r.GetDouble
        "double precision",             "double",               DbType.Double,          Some (nameof MySqlDbType.Double),      nameof r.GetDouble
        "char",                         "string",               DbType.String,          Some (nameof MySqlDbType.String),      nameof r.GetString
        "varchar",                      "string",               DbType.String,          Some (nameof MySqlDbType.VarChar),     nameof r.GetString
        "nvarchar",                     "string",               DbType.String,          Some (nameof MySqlDbType.VarChar),     nameof r.GetString
        "binary",                       "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.Binary),      nameof r.GetFieldValue
        "char BYTE",                    "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.Binary),      nameof r.GetFieldValue
        "varbinary",                    "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.VarBinary),   nameof r.GetFieldValue
        "tinyblob",                     "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.TinyBlob),    nameof r.GetFieldValue
        "blob",                         "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.Blob),        nameof r.GetFieldValue
        "mediumblob",                   "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.MediumBlob),  nameof r.GetFieldValue
        "longblob",                     "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.LongBlob),    nameof r.GetFieldValue
        "tinytext",                     "string",               DbType.String,          Some (nameof MySqlDbType.TinyText),    nameof r.GetString
        "text",                         "string",               DbType.String,          Some (nameof MySqlDbType.Text),        nameof r.GetString
        "mediumtext",                   "string",               DbType.String,          Some (nameof MySqlDbType.MediumText),  nameof r.GetString
        "longtext",                     "string",               DbType.String,          Some (nameof MySqlDbType.LongText),    nameof r.GetString
        "enum",                         "string",               DbType.String,          Some (nameof MySqlDbType.Enum),        nameof r.GetString
        "set",                          "string",               DbType.String,          Some (nameof MySqlDbType.Set),         nameof r.GetString
        "json",                         "string",               DbType.String,          Some (nameof MySqlDbType.JSON),        nameof r.GetString
        "date",                         "System.DateOnly",      DbType.DateTime,        Some (nameof MySqlDbType.Date),        "GetDateOnly"
        "time",                         "System.TimeOnly",      DbType.Time,            Some (nameof MySqlDbType.Time),        "GetTimeOnly"
        "datetime",                     "System.DateTime",      DbType.DateTime,        Some (nameof MySqlDbType.DateTime),    nameof r.GetDateTime
        "timestamp",                    "System.DateTime",      DbType.DateTime,        Some (nameof MySqlDbType.Timestamp),   nameof r.GetDateTime
        "year",                         "int16",                DbType.Int16,           Some (nameof MySqlDbType.Year),        nameof r.GetInt16
        // skipped unsupported
        "bool",                         "bool",                 DbType.Boolean,         Some (nameof MySqlDbType.Int16),       nameof r.GetBoolean
        "boolean",                      "bool",                 DbType.Boolean,         Some (nameof MySqlDbType.Int16),       nameof r.GetBoolean
        "float4",                       "float",                DbType.Single,          Some (nameof MySqlDbType.Float),       nameof r.GetFloat
        "float8",                       "double",               DbType.Double,          Some (nameof MySqlDbType.Double),      nameof r.GetDouble
        "numeric",                      "decimal",              DbType.Decimal,         Some (nameof MySqlDbType.Decimal),     nameof r.GetDecimal
        "long",                         "string",               DbType.String,          Some (nameof MySqlDbType.MediumText),  nameof r.GetString
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

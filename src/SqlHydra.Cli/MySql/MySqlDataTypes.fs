module SqlHydra.MySql.MySqlDataTypes

open System.Data
open MySql.Data.MySqlClient
open SqlHydra.Domain

let private r : System.Data.Common.DbDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings = // https://dev.mysql.com/doc/refman/9.0/en/data-types.html
    [// ColumnTypeAlias                 ClrType                 DbType                  ProviderDbType                         
        "BIT",                          "int16",                DbType.Int16,           Some (nameof MySqlDbType.Bit),         nameof r.GetInt16
        "TINYINT",                      "int16",                DbType.Int16,           Some (nameof MySqlDbType.Int16),       nameof r.GetInt16
        "TINYINT UNSIGNED",             "uint16",               DbType.UInt16,          Some (nameof MySqlDbType.UInt16),      nameof r.GetFieldValue<uint16>
        "BOOL",                         "int16",                DbType.Boolean,         Some (nameof MySqlDbType.Int16),       nameof r.GetBoolean
        "BOOLEAN",                      "int16",                DbType.Boolean,         Some (nameof MySqlDbType.Int16),       nameof r.GetBoolean
        "SMALLINT",                     "int16",                DbType.Int16,           Some (nameof MySqlDbType.Int16),       nameof r.GetInt16
        "SMALLINT UNSIGNED",            "uint16",               DbType.UInt16,          Some (nameof MySqlDbType.UInt16),      nameof r.GetFieldValue<uint16>
        "MEDIUMINT",                    "int24",                DbType.Int32,           Some (nameof MySqlDbType.Int24),       nameof r.GetInt32
        "MEDIUMINT UNSIGNED",           "uint24",               DbType.UInt32,          Some (nameof MySqlDbType.UInt24),      nameof r.GetFieldValue<uint32>
        "INT",                          "int",                  DbType.Int32,           Some (nameof MySqlDbType.Int32),       nameof r.GetInt32
        "INT UNSIGNED",                 "int",                  DbType.UInt32,          Some (nameof MySqlDbType.UInt32),      nameof r.GetFieldValue<uint32>
        "INTEGER",                      "int",                  DbType.Int32,           Some (nameof MySqlDbType.Int32),       nameof r.GetInt32
        "INTEGER UNSIGNED",             "int",                  DbType.UInt32,          Some (nameof MySqlDbType.UInt32),      nameof r.GetFieldValue<uint32>
        "BIGINT",                       "int64",                DbType.Int64,           Some (nameof MySqlDbType.Int64),       nameof r.GetInt64
        "BIGINT UNSIGNED",              "int64",                DbType.UInt64,          Some (nameof MySqlDbType.UInt64),      nameof r.GetFieldValue<uint64>
        "SERIAL",                       "int64",                DbType.UInt64,          Some (nameof MySqlDbType.UInt64),      nameof r.GetFieldValue<uint64>
        "DECIMAL",                      "decimal",              DbType.Decimal,         Some (nameof MySqlDbType.Decimal),     nameof r.GetDecimal
        "DEC",                          "decimal",              DbType.Decimal,         Some (nameof MySqlDbType.Decimal),     nameof r.GetDecimal
        "FLOAT",                        "float",                DbType.Single,          Some (nameof MySqlDbType.Float),       nameof r.GetFloat
        "DOUBLE",                       "double",               DbType.Double,          Some (nameof MySqlDbType.Double),      nameof r.GetDouble
        "DOUBLE PRECISION",             "double",               DbType.Double,          Some (nameof MySqlDbType.Double),      nameof r.GetDouble
        "CHAR",                         "string",               DbType.String,          Some (nameof MySqlDbType.String),      nameof r.GetString
        "VARCHAR",                      "string",               DbType.String,          Some (nameof MySqlDbType.VarChar),     nameof r.GetString
        "NVARCHAR",                     "string",               DbType.String,          Some (nameof MySqlDbType.VarChar),     nameof r.GetString
        "BINARY",                       "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.Binary),      nameof r.GetFieldValue
        "CHAR BYTE",                    "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.Binary),      nameof r.GetFieldValue
        "VARBINARY",                    "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.VarBinary),   nameof r.GetFieldValue
        "TINYBLOB",                     "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.TinyBlob),    nameof r.GetFieldValue
        "BLOB",                         "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.Blob),        nameof r.GetFieldValue
        "MEDIUMBLOB",                   "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.MediumBlob),  nameof r.GetFieldValue
        "LONGBLOB",                     "byte[]",               DbType.Binary,          Some (nameof MySqlDbType.LongBlob),    nameof r.GetFieldValue
        "TINYTEXT",                     "string",               DbType.String,          Some (nameof MySqlDbType.TinyText),    nameof r.GetString
        "TEXT",                         "string",               DbType.String,          Some (nameof MySqlDbType.Text),        nameof r.GetString
        "MEDIUMTEXT",                   "string",               DbType.String,          Some (nameof MySqlDbType.MediumText),  nameof r.GetString
        "LONGTEXT",                     "string",               DbType.String,          Some (nameof MySqlDbType.LongText),    nameof r.GetString
        "ENUM",                         "string",               DbType.String,          Some (nameof MySqlDbType.Enum),        nameof r.GetString
        "SET",                          "string",               DbType.String,          Some (nameof MySqlDbType.Set),         nameof r.GetString
        "JSON",                         "string",               DbType.String,          Some (nameof MySqlDbType.JSON),        nameof r.GetString
        "DATE",                         "System.DateOnly",      DbType.DateTime,        Some (nameof MySqlDbType.Date),        "GetDateOnly"
        "TIME",                         "System.TimeOnly",      DbType.Time,            Some (nameof MySqlDbType.Time),        "GetTimeOnly"
        "DATETIME",                     "System.DateTime",      DbType.DateTime,        Some (nameof MySqlDbType.DateTime),    nameof r.GetDateTime
        "TIMESTAMP",                    "System.DateTime",      DbType.DateTime,        Some (nameof MySqlDbType.Timestamp),   nameof r.GetDateTime
        "YEAR",                         "int16",                DbType.Int16,           Some (nameof MySqlDbType.Year),        nameof r.GetInt16
        // skipped unsupported 
        "BOOL",                         "bool",                 DbType.Boolean,         Some (nameof MySqlDbType.Int16),       nameof r.GetBoolean
        "BOOLEAN",                      "bool",                 DbType.Boolean,         Some (nameof MySqlDbType.Int16),       nameof r.GetBoolean
        "FLOAT4",                       "float",                DbType.Single,          Some (nameof MySqlDbType.Float),       nameof r.GetFloat
        "FLOAT8",                       "double",               DbType.Double,          Some (nameof MySqlDbType.Double),      nameof r.GetDouble
        "NUMERIC",                      "decimal",              DbType.Decimal,         Some (nameof MySqlDbType.Decimal),     nameof r.GetDecimal
        "LONG",                         "string",               DbType.String,          Some (nameof MySqlDbType.MediumText),  nameof r.GetString
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

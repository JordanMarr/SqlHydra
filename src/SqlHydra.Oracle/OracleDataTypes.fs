module SqlHydra.Oracle.OracleDataTypes

open System.Data
open SqlHydra.Domain

let private r : Oracle.ManagedDataAccess.Client.OracleDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings =
    [   // https://docs.oracle.com/cd/B19306_01/win.102/b14306/appendixa.htm
        "PLS_INTEGER",                      "int",                          DbType.Int32,               nameof r.GetInt32
        "LONG",                             "int64",                        DbType.Int64,               nameof r.GetInt64
        "NUMBER",                           "decimal",                      DbType.Decimal,             nameof r.GetDecimal
        "FLOAT",                            "double",                       DbType.Double,              nameof r.GetDouble
        "BINARY_DOUBLE",                    "double",                       DbType.Double,              nameof r.GetDouble
        "BINARY_FLOAT",                     "System.Single",                DbType.Single,              nameof r.GetFieldValue
        "REAL",                             "System.Single",                DbType.Single,              nameof r.GetFieldValue
        "ROWID",                            "string",                       DbType.String,              nameof r.GetString
        "UROWID",                           "string",                       DbType.String,              nameof r.GetString
        "VARCHAR",                          "string",                       DbType.String,              nameof r.GetString
        "VARCHAR2",                         "string",                       DbType.String,              nameof r.GetString
        "NVARCHAR",                         "string",                       DbType.String,              nameof r.GetString
        "NVARCHAR2",                        "string",                       DbType.String,              nameof r.GetString
        "CHAR",                             "string",                       DbType.String,              nameof r.GetString
        "XMLType",                          "string",                       DbType.String,              nameof r.GetString
        "NCHAR",                            "string",                       DbType.StringFixedLength,   nameof r.GetString
        "TEXT",                             "string",                       DbType.String,              nameof r.GetString
        "NTEXT",                            "string",                       DbType.String,              nameof r.GetString
        "DATE",                             "System.DateTime",              DbType.Date,                nameof r.GetDateTime
        "TIMESTAMP",                        "System.DateTime",              DbType.Date,                nameof r.GetDateTime
        "TIMESTAMP WITH LOCAL TIME ZONE",   "System.DateTime",              DbType.Date,                nameof r.GetDateTime
        "TIMESTAMP WITH TIME ZONE",         "System.DateTime",              DbType.Date,                nameof r.GetDateTime
        "INTERVAL DAY TO SECOND",           "System.TimeSpan",              DbType.Time,                nameof r.GetTimeSpan
        "BFILE",                            "byte[]",                       DbType.Binary,              nameof r.GetValue
        "BLOB",                             "byte[]",                       DbType.Binary,              nameof r.GetValue
        "LONG RAW",                         "byte[]",                       DbType.Binary,              nameof r.GetValue
        "RAW",                              "byte[]",                       DbType.Binary,              nameof r.GetValue
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
            TypeMapping.ProviderDbType = None
        }
    )
    |> Map.ofList
        
let tryFindTypeMapping (providerTypeName: string) =
    typeMappingsByName.TryFind (providerTypeName.ToUpper())
        
let primitiveTypeReaders = 
    supportedTypeMappings
    |> List.map (fun (_, clrType, _, readerMethod) ->
        { PrimitiveTypeReader.ClrType = clrType; PrimitiveTypeReader.ReaderMethod = readerMethod }
    )
    |> List.distinctBy (fun ptr -> ptr.ClrType)

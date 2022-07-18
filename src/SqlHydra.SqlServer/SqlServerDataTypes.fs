module SqlHydra.SqlServer.SqlServerDataTypes

open System.Data
open SqlHydra.Domain

let private r : Microsoft.Data.SqlClient.SqlDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings =
    [   // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
        "UNIQUEIDENTIFIER",     "System.Guid",                              DbType.Guid,                nameof r.GetGuid
        "BIT",                  "bool",                                     DbType.Boolean,             nameof r.GetBoolean
        "INT",                  "int",                                      DbType.Int32,               nameof r.GetInt32
        "BIGINT",               "int64",                                    DbType.Int64,               nameof r.GetInt64
        "SMALLINT",             "int16",                                    DbType.Int16,               nameof r.GetInt16
        "TINYINT",              "byte",                                     DbType.Byte,                nameof r.GetByte
        "FLOAT",                "double",                                   DbType.Double,              nameof r.GetDouble
        "REAL",                 "System.Single",                            DbType.Single,              nameof r.GetFloat
        "DECIMAL",              "decimal",                                  DbType.Decimal,             nameof r.GetDecimal
        "NUMERIC",              "decimal",                                  DbType.Decimal,             nameof r.GetDecimal
        "MONEY",                "decimal",                                  DbType.Decimal,             nameof r.GetDecimal
        "SMALLMONEY",           "decimal",                                  DbType.Decimal,             nameof r.GetDecimal
        "VARCHAR",              "string",                                   DbType.String,              nameof r.GetString
        "NVARCHAR",             "string",                                   DbType.String,              nameof r.GetString
        "CHAR",                 "string",                                   DbType.String,              nameof r.GetString
        "NCHAR",                "string",                                   DbType.StringFixedLength,   nameof r.GetString
        "TEXT",                 "string",                                   DbType.String,              nameof r.GetString
        "NTEXT",                "string",                                   DbType.String,              nameof r.GetString
        "DATETIMEOFFSET",       "System.DateTimeOffset",                    DbType.DateTimeOffset,      nameof r.GetDateTimeOffset
#if NET5_0
        "DATE",                 "System.DateTime",                          DbType.Date,                nameof r.GetDateTime
        "TIME",                 "System.TimeSpan",                          DbType.Time,                nameof r.GetTimeSpan
#endif
#if NET6_0_OR_GREATER
        "DATE",                 "System.DateOnly",                          DbType.Date,                "GetDateOnly"
        "TIME",                 "System.TimeOnly",                          DbType.Time,                nameof r.GetFieldValue
#endif
        "DATETIME",             "System.DateTime",                          DbType.DateTime,            nameof r.GetDateTime
        "DATETIME2",            "System.DateTime",                          DbType.DateTime2,           nameof r.GetDateTime
        "SMALLDATETIME",        "System.DateTime",                          DbType.DateTime,            nameof r.GetDateTime        
        "VARBINARY",            "byte[]",                                   DbType.Binary,              nameof r.GetValue
        "BINARY",               "byte[]",                                   DbType.Binary,              nameof r.GetValue
        "IMAGE",                "byte[]",                                   DbType.Binary,              nameof r.GetValue
        "ROWVERSION",           "byte[]",                                   DbType.Binary,              nameof r.GetValue
        "SQL_VARIANT",          "obj",                                      DbType.Object,              nameof r.GetValue
        
        // UNSUPPORTED COLUMN TYPES
        //"XML",                  "System.Data.SqlTypes.SqlXml",              DbType.Xml,               nameof r.GetSqlXml
        //"GEOGRAPHY",            "Microsoft.SqlServer.Types.SqlGeography",   DbType.Object,              None
        //"GEOMETRY",             "Microsoft.SqlServer.Types.SqlGeometry",    DbType.Object,              None
        //"HIERARCHYID",          "Microsoft.SqlServer.Types.SqlHierarchyId", DbType.Object,              None
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

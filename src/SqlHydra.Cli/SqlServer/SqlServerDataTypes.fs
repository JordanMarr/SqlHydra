module SqlHydra.SqlServer.SqlServerDataTypes

open System.Data
open SqlHydra.Domain

let private r : Microsoft.Data.SqlClient.SqlDataReader = null

// https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
/// A list of supported column type mappings
let supportedTypeMappings =
    [// ColumnTypeAlias         ClrType                                     DbType                      ProviderDbType                  ReaderMethod
        "UNIQUEIDENTIFIER",     "System.Guid",                              DbType.Guid,                None,                           nameof r.GetGuid
        "BIT",                  "bool",                                     DbType.Boolean,             None,                           nameof r.GetBoolean
        "INT",                  "int",                                      DbType.Int32,               None,                           nameof r.GetInt32
        "BIGINT",               "int64",                                    DbType.Int64,               None,                           nameof r.GetInt64
        "SMALLINT",             "int16",                                    DbType.Int16,               None,                           nameof r.GetInt16
        "TINYINT",              "byte",                                     DbType.Byte,                None,                           nameof r.GetByte
        "FLOAT",                "double",                                   DbType.Double,              None,                           nameof r.GetDouble
        "REAL",                 "System.Single",                            DbType.Single,              None,                           nameof r.GetFloat
        "DECIMAL",              "decimal",                                  DbType.Decimal,             None,                           nameof r.GetDecimal
        "NUMERIC",              "decimal",                                  DbType.Decimal,             None,                           nameof r.GetDecimal
        "MONEY",                "decimal",                                  DbType.Decimal,             None,                           nameof r.GetDecimal
        "SMALLMONEY",           "decimal",                                  DbType.Decimal,             None,                           nameof r.GetDecimal
        "VARCHAR",              "string",                                   DbType.String,              None,                           nameof r.GetString
        "NVARCHAR",             "string",                                   DbType.String,              None,                           nameof r.GetString
        "CHAR",                 "string",                                   DbType.String,              None,                           nameof r.GetString
        "NCHAR",                "string",                                   DbType.StringFixedLength,   None,                           nameof r.GetString
        "TEXT",                 "string",                                   DbType.String,              None,                           nameof r.GetString
        "NTEXT",                "string",                                   DbType.String,              None,                           nameof r.GetString
        "DATETIMEOFFSET",       "System.DateTimeOffset",                    DbType.DateTimeOffset,      None,                           nameof r.GetDateTimeOffset
        "DATE",                 "System.DateOnly",                          DbType.Date,                None,                           "GetDateOnly"
        "TIME",                 "System.TimeOnly",                          DbType.Time,                None,                           "GetTimeOnly"
        "DATETIME",             "System.DateTime",                          DbType.DateTime,            Some (nameof DbType.DateTime),  nameof r.GetDateTime
        "DATETIME2",            "System.DateTime",                          DbType.DateTime2,           Some (nameof DbType.DateTime2), nameof r.GetDateTime
        "SMALLDATETIME",        "System.DateTime",                          DbType.DateTime,            Some (nameof DbType.DateTime),  nameof r.GetDateTime        
        "VARBINARY",            "byte[]",                                   DbType.Binary,              None,                           nameof r.GetValue
        "BINARY",               "byte[]",                                   DbType.Binary,              None,                           nameof r.GetValue
        "IMAGE",                "byte[]",                                   DbType.Binary,              None,                           nameof r.GetValue
        "ROWVERSION",           "byte[]",                                   DbType.Binary,              None,                           nameof r.GetValue
        "SQL_VARIANT",          "obj",                                      DbType.Object,              None,                           nameof r.GetValue
        
        // UNSUPPORTED COLUMN TYPES
        //"XML",                  "System.Data.SqlTypes.SqlXml",              DbType.Xml,               None,                           nameof r.GetSqlXml
        //"GEOGRAPHY",            "Microsoft.SqlServer.Types.SqlGeography",   DbType.Object,            None
        //"GEOMETRY",             "Microsoft.SqlServer.Types.SqlGeometry",    DbType.Object,            None
        //"HIERARCHYID",          "Microsoft.SqlServer.Types.SqlHierarchyId", DbType.Object,            None
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
    typeMappingsByName.TryFind (providerTypeName.ToUpper())
        
let primitiveTypeReaders = 
    supportedTypeMappings
    |> List.map (fun (_, clrType, _, _, readerMethod) ->
        { PrimitiveTypeReader.ClrType = clrType; PrimitiveTypeReader.ReaderMethod = readerMethod }
    )
    |> List.distinctBy (fun ptr -> ptr.ClrType)

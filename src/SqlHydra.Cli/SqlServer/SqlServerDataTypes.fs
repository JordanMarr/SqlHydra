module SqlHydra.SqlServer.SqlServerDataTypes

open System.Data
open SqlHydra.Domain

let private r : Microsoft.Data.SqlClient.SqlDataReader = null

// https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
/// A list of supported column type mappings
let supportedTypeMappings isLegacy =
    [// ColumnTypeAlias         ClrType                                     DbType                      ProviderDbType                                  ReaderMethod
        "UNIQUEIDENTIFIER",     "System.Guid",                              DbType.Guid,                Some (nameof SqlDbType.UniqueIdentifier),       nameof r.GetGuid
        "BIT",                  "bool",                                     DbType.Boolean,             Some (nameof SqlDbType.Bit),                    nameof r.GetBoolean
        "INT",                  "int",                                      DbType.Int32,               Some (nameof SqlDbType.Int),                    nameof r.GetInt32
        "BIGINT",               "int64",                                    DbType.Int64,               Some (nameof SqlDbType.BigInt),                 nameof r.GetInt64
        "SMALLINT",             "int16",                                    DbType.Int16,               Some (nameof SqlDbType.SmallInt),               nameof r.GetInt16
        "TINYINT",              "byte",                                     DbType.Byte,                Some (nameof SqlDbType.TinyInt),                nameof r.GetByte
        "FLOAT",                "double",                                   DbType.Double,              Some (nameof SqlDbType.Float),                  nameof r.GetDouble
        "REAL",                 "System.Single",                            DbType.Single,              Some (nameof SqlDbType.Real),                   nameof r.GetFloat
        "DECIMAL",              "decimal",                                  DbType.Decimal,             Some (nameof SqlDbType.Decimal),                nameof r.GetDecimal
        "NUMERIC",              "decimal",                                  DbType.Decimal,             Some (nameof SqlDbType.Decimal),                nameof r.GetDecimal
        "MONEY",                "decimal",                                  DbType.Decimal,             Some (nameof SqlDbType.Money),                  nameof r.GetDecimal
        "SMALLMONEY",           "decimal",                                  DbType.Decimal,             Some (nameof SqlDbType.SmallMoney),             nameof r.GetDecimal
        "VARCHAR",              "string",                                   DbType.String,              Some (nameof SqlDbType.VarChar),                nameof r.GetString
        "NVARCHAR",             "string",                                   DbType.String,              Some (nameof SqlDbType.NVarChar),               nameof r.GetString
        "CHAR",                 "string",                                   DbType.String,              Some (nameof SqlDbType.Char),                   nameof r.GetString
        "NCHAR",                "string",                                   DbType.StringFixedLength,   Some (nameof SqlDbType.NChar),                  nameof r.GetString
        "TEXT",                 "string",                                   DbType.String,              Some (nameof SqlDbType.Text),                   nameof r.GetString
        "NTEXT",                "string",                                   DbType.String,              Some (nameof SqlDbType.NText),                  nameof r.GetString
        "DATETIMEOFFSET",       "System.DateTimeOffset",                    DbType.DateTimeOffset,      Some (nameof SqlDbType.DateTimeOffset),         nameof r.GetDateTimeOffset
        
        if isLegacy then
         "DATE",                "System.DateTime",                          DbType.Date,                Some (nameof SqlDbType.Date),                   nameof r.GetDateTime
         "TIME",                "System.TimeSpan",                          DbType.Time,                Some (nameof SqlDbType.Time),                   nameof r.GetTimeSpan
        else
         "DATE",                "System.DateOnly",                          DbType.Date,                Some (nameof SqlDbType.Date),                   "GetDateOnly"
         "TIME",                "System.TimeOnly",                          DbType.Time,                Some (nameof SqlDbType.Time),                   "GetTimeOnly"

        "DATETIME",             "System.DateTime",                          DbType.DateTime,            Some (nameof SqlDbType.DateTime),               nameof r.GetDateTime
        "DATETIME2",            "System.DateTime",                          DbType.DateTime2,           Some (nameof SqlDbType.DateTime2),              nameof r.GetDateTime
        "SMALLDATETIME",        "System.DateTime",                          DbType.DateTime,            Some (nameof SqlDbType.SmallDateTime),          nameof r.GetDateTime        
        "VARBINARY",            "byte[]",                                   DbType.Binary,              Some (nameof SqlDbType.VarBinary),              nameof r.GetFieldValue
        "BINARY",               "byte[]",                                   DbType.Binary,              Some (nameof SqlDbType.Binary),                 nameof r.GetFieldValue
        "IMAGE",                "byte[]",                                   DbType.Binary,              Some (nameof SqlDbType.Image),                  nameof r.GetFieldValue
        "ROWVERSION",           "byte[]",                                   DbType.Binary,              Some (nameof SqlDbType.Binary),                 nameof r.GetFieldValue
        "TIMESTAMP",            "byte[]",                                   DbType.Binary,              Some (nameof SqlDbType.Binary),                 nameof r.GetFieldValue
        "SQL_VARIANT",          "obj",                                      DbType.Object,              Some (nameof SqlDbType.Variant),                nameof r.GetFieldValue
        
        // UNSUPPORTED COLUMN TYPES
        //"XML",                "System.Data.SqlTypes.SqlXml",              DbType.Xml,                 None,                                           nameof r.GetSqlXml
        //"GEOGRAPHY",          "Microsoft.SqlServer.Types.SqlGeography",   DbType.Object,              None
        //"GEOMETRY",           "Microsoft.SqlServer.Types.SqlGeometry",    DbType.Object,              None
        //"HIERARCHYID",        "Microsoft.SqlServer.Types.SqlHierarchyId", DbType.Object,              None
    ]

let typeMappingsByName isLegacy =
    supportedTypeMappings isLegacy
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
        
let tryFindTypeMapping isLegacy =
    let map = typeMappingsByName isLegacy
    let toUpper (str: string) = str.ToUpper()
    toUpper >> map.TryFind
        
let primitiveTypeReaders isLegacy = 
    supportedTypeMappings isLegacy
    |> List.map (fun (_, clrType, _, _, readerMethod) ->
        { PrimitiveTypeReader.ClrType = clrType; PrimitiveTypeReader.ReaderMethod = readerMethod }
    )
    |> List.distinctBy _.ClrType

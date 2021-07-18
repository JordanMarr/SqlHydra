module SqlHydra.SqlServer.SqlServerDataTypes

open System.Data
open SqlHydra.Schema

(* 
    Column types with a "ReaderMethod" will have a DataReader property generated if readers are enabled.
*)
let typeMappingsByName =
    [   // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
        "UNIQUEIDENTIFIER",     "System.Guid",                              DbType.Guid,                Some "GetGuid"
        "BIT",                  "bool",                                     DbType.Boolean,             Some "GetBoolean"
        "INT",                  "int",                                      DbType.Int32,               Some "GetInt32"
        "BIGINT",               "int64",                                    DbType.Int64,               Some "GetInt64"
        "SMALLINT",             "int16",                                    DbType.Int16,               Some "GetInt16"
        "TINYINT",              "byte",                                     DbType.Byte,                Some "GetByte"
        "FLOAT",                "double",                                   DbType.Double,              Some "GetDouble"
        "REAL",                 "System.Single",                            DbType.Single,              Some "GetDouble"
        "DECIMAL",              "decimal",                                  DbType.Decimal,             Some "GetDecimal"
        "NUMERIC",              "decimal",                                  DbType.Decimal,             Some "GetDecimal"
        "MONEY",                "decimal",                                  DbType.Decimal,             Some "GetDecimal"
        "SMALLMONEY",           "decimal",                                  DbType.Decimal,             Some "GetDecimal"
        "VARCHAR",              "string",                                   DbType.String,              Some "GetString"
        "NVARCHAR",             "string",                                   DbType.String,              Some "GetString"
        "CHAR",                 "string",                                   DbType.String,              Some "GetString"
        "NCHAR",                "string",                                   DbType.StringFixedLength,   Some "GetString"
        "TEXT",                 "string",                                   DbType.String,              Some "GetString"
        "NTEXT",                "string",                                   DbType.String,              Some "GetString"
        "DATETIMEOFFSET",       "System.DateTimeOffset",                    DbType.DateTimeOffset,      Some "GetDateTimeOffset"
        "DATE",                 "System.DateTime",                          DbType.Date,                Some "GetDateTime"
        "DATETIME",             "System.DateTime",                          DbType.DateTime,            Some "GetDateTime"
        "DATETIME2",            "System.DateTime",                          DbType.DateTime2,           Some "GetDateTime"
        "SMALLDATETIME",        "System.DateTime",                          DbType.DateTime,            Some "GetDateTime"
        "TIME",                 "System.TimeSpan",                          DbType.Time,                Some "GetTimeSpan"
        "VARBINARY",            "byte[]",                                   DbType.Binary,              Some "GetValue"
        "BINARY",               "byte[]",                                   DbType.Binary,              Some "GetValue"
        "IMAGE",                "byte[]",                                   DbType.Binary,              Some "GetValue"
        "ROWVERSION",           "byte[]",                                   DbType.Binary,              Some "GetValue"
        "SQL_VARIANT",          "obj",                                      DbType.Object,              Some "GetValue"
        "XML",                  "System.Xml.Linq.XElement",                 DbType.Xml,                 None
        "GEOGRAPHY",            "Microsoft.SqlServer.Types.SqlGeography",   DbType.Object,              None
        "GEOMETRY",             "Microsoft.SqlServer.Types.SqlGeometry",    DbType.Object,              None
        "HIERARCHYID",          "Microsoft.SqlServer.Types.SqlHierarchyId", DbType.Object,              None 
    ]
    |> List.map (fun (providerTypeName, clrType, dbType, readerMethod) ->
        providerTypeName,
        { 
            TypeMapping.ProviderTypeName = providerTypeName
            TypeMapping.ClrType = clrType
            TypeMapping.DbType = dbType
            TypeMapping.ReaderMethod = readerMethod
        }
    )
    |> Map.ofList
        
let findTypeMapping (providerTypeName: string) =
    typeMappingsByName.TryFind (providerTypeName.ToUpper())
    |> Option.defaultWith (fun () -> failwithf "Provider type not handled: %s" providerTypeName)
        

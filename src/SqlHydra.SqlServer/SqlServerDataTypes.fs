module SqlHydra.SqlServer.SqlServerDataTypes

open System.Data
open SqlHydra.Domain

let private r : System.Data.Common.DbDataReader = null

(* 
    Column types with a "ReaderMethod" will have a DataReader property generated if readers are enabled.
*)
let typeMappings =
    [   // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
        "UNIQUEIDENTIFIER",     "System.Guid",                              DbType.Guid,                Some <| nameof r.GetGuid
        "BIT",                  "bool",                                     DbType.Boolean,             Some <| nameof r.GetBoolean
        "INT",                  "int",                                      DbType.Int32,               Some <| nameof r.GetInt32
        "BIGINT",               "int64",                                    DbType.Int64,               Some <| nameof r.GetInt64
        "SMALLINT",             "int16",                                    DbType.Int16,               Some <| nameof r.GetInt16
        "TINYINT",              "byte",                                     DbType.Byte,                Some <| nameof r.GetByte
        "FLOAT",                "double",                                   DbType.Double,              Some <| nameof r.GetDouble
        "REAL",                 "System.Single",                            DbType.Single,              Some <| nameof r.GetFloat
        "DECIMAL",              "decimal",                                  DbType.Decimal,             Some <| nameof r.GetDecimal
        "NUMERIC",              "decimal",                                  DbType.Decimal,             Some <| nameof r.GetDecimal
        "MONEY",                "decimal",                                  DbType.Decimal,             Some <| nameof r.GetDecimal
        "SMALLMONEY",           "decimal",                                  DbType.Decimal,             Some <| nameof r.GetDecimal
        "VARCHAR",              "string",                                   DbType.String,              Some <| nameof r.GetString
        "NVARCHAR",             "string",                                   DbType.String,              Some <| nameof r.GetString
        "CHAR",                 "string",                                   DbType.String,              Some <| nameof r.GetString
        "NCHAR",                "string",                                   DbType.StringFixedLength,   Some <| nameof r.GetString
        "TEXT",                 "string",                                   DbType.String,              Some <| nameof r.GetString
        "NTEXT",                "string",                                   DbType.String,              Some <| nameof r.GetString
        "DATETIMEOFFSET",       "System.DateTimeOffset",                    DbType.DateTimeOffset,      Some <| "GetDateTimeOffset"     // *.Data.SqlClient
        "DATE",                 "System.DateTime",                          DbType.Date,                Some <| nameof r.GetDateTime
        "DATETIME",             "System.DateTime",                          DbType.DateTime,            Some <| nameof r.GetDateTime
        "DATETIME2",            "System.DateTime",                          DbType.DateTime2,           Some <| nameof r.GetDateTime
        "SMALLDATETIME",        "System.DateTime",                          DbType.DateTime,            Some <| nameof r.GetDateTime
        "TIME",                 "System.TimeSpan",                          DbType.Time,                Some <| "GetTimeSpan"           // *.Data.SqlClient
        "VARBINARY",            "byte[]",                                   DbType.Binary,              Some <| nameof r.GetValue
        "BINARY",               "byte[]",                                   DbType.Binary,              Some <| nameof r.GetValue
        "IMAGE",                "byte[]",                                   DbType.Binary,              Some <| nameof r.GetValue
        "ROWVERSION",           "byte[]",                                   DbType.Binary,              Some <| nameof r.GetValue
        "SQL_VARIANT",          "obj",                                      DbType.Object,              Some <| nameof r.GetValue
        "XML",                  "System.Xml.Linq.XElement",                 DbType.Xml,                 None
        "GEOGRAPHY",            "obj",                                      DbType.Object,              None
        "GEOMETRY",             "obj",                                      DbType.Object,              None
        "HIERARCHYID",          "obj",                                      DbType.Object,              None 
        //"GEOGRAPHY",            "Microsoft.SqlServer.Types.SqlGeography",   DbType.Object,              None
        //"GEOMETRY",             "Microsoft.SqlServer.Types.SqlGeometry",    DbType.Object,              None
        //"HIERARCHYID",          "Microsoft.SqlServer.Types.SqlHierarchyId", DbType.Object,              None 
    ]

let typeMappingsByName =
    typeMappings
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
        
let findTypeMapping (providerTypeName: string) =
    typeMappingsByName.TryFind (providerTypeName.ToUpper())
    |> Option.defaultWith (fun () -> failwithf "Column type not handled: %s" providerTypeName)
        
let primitiveTypeReaders = 
    typeMappings
    |> List.choose(fun (_, clrType, _, readerMethod) ->
        match readerMethod with
        | Some rm -> Some { PrimitiveTypeReader.ClrType = clrType; PrimitiveTypeReader.ReaderMethod = rm }
        | None -> None
    )
    |> List.distinctBy (fun ptr -> ptr.ClrType)

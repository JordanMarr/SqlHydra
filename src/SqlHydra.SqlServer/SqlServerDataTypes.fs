module SqlHydra.SqlServer.SqlServerDataTypes

open System.Data
open SqlHydra.Schema

let typeMappingsByName =
    let toInt = int >> Some
    // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
    [ "UNIQUEIDENTIFIER",   "System.Guid",                              DbType.Guid,                Some "GetGuid",             toInt SqlDbType.UniqueIdentifier
      "BIT",                "bool",                                     DbType.Boolean,             Some "GetBoolean",          toInt SqlDbType.Bit
      "INT",                "int",                                      DbType.Int32,               Some "GetInt32",            toInt SqlDbType.Int
      "BIGINT",             "int64",                                    DbType.Int64,               Some "GetInt64",            toInt SqlDbType.BigInt
      "SMALLINT",           "int16",                                    DbType.Int16,               Some "GetInt16",            toInt SqlDbType.SmallInt
      "TINYINT",            "byte",                                     DbType.Byte,                Some "GetByte",             toInt SqlDbType.TinyInt
      "FLOAT",              "double",                                   DbType.Double,              Some "GetDouble",           toInt SqlDbType.Float
      "REAL",               "System.Single",                            DbType.Single,              Some "GetDouble",           toInt SqlDbType.Real
      "DECIMAL",            "decimal",                                  DbType.Decimal,             Some "GetDecimal",          toInt SqlDbType.Decimal
      "NUMERIC",            "decimal",                                  DbType.Decimal,             Some "GetDecimal",          toInt SqlDbType.Decimal
      "MONEY",              "decimal",                                  DbType.Decimal,             Some "GetDecimal",          toInt SqlDbType.Money
      "SMALLMONEY",         "decimal",                                  DbType.Decimal,             Some "GetDecimal",          toInt SqlDbType.SmallMoney
      "VARCHAR",            "string",                                   DbType.String,              Some "GetString",           toInt SqlDbType.VarChar
      "NVARCHAR",           "string",                                   DbType.String,              Some "GetString",           toInt SqlDbType.NVarChar
      "CHAR",               "string",                                   DbType.String,              Some "GetString",           toInt SqlDbType.Char
      "NCHAR",              "string",                                   DbType.StringFixedLength,   Some "GetString",           toInt SqlDbType.NChar
      "TEXT",               "string",                                   DbType.String,              Some "GetString",           toInt SqlDbType.Text
      "NTEXT",              "string",                                   DbType.String,              Some "GetString",           toInt SqlDbType.NText
      "DATETIMEOFFSET",     "System.DateTimeOffset",                    DbType.DateTimeOffset,      Some "GetDateTimeOffset",   toInt SqlDbType.DateTimeOffset
      "DATE",               "System.DateTime",                          DbType.Date,                Some "GetDateTime",         toInt SqlDbType.Date
      "DATETIME",           "System.DateTime",                          DbType.DateTime,            Some "GetDateTime",         toInt SqlDbType.DateTime
      "DATETIME2",          "System.DateTime",                          DbType.DateTime2,           Some "GetDateTime",         toInt SqlDbType.DateTime2
      "SMALLDATETIME",      "System.DateTime",                          DbType.DateTime,            Some "GetDateTime",         toInt SqlDbType.SmallDateTime
      "TIME",               "System.TimeSpan",                          DbType.Time,                Some "GetTimeSpan",         toInt SqlDbType.Time
      "VARBINARY",          "byte[]",                                   DbType.Binary,              Some "GetValue",            toInt SqlDbType.VarBinary
      "BINARY",             "byte[]",                                   DbType.Binary,              Some "GetValue",            toInt SqlDbType.Binary
      "IMAGE",              "byte[]",                                   DbType.Binary,              Some "GetValue",            toInt SqlDbType.Image
      "ROWVERSION",         "byte[]",                                   DbType.Binary,              Some "GetValue",            None
      "XML",                "System.Xml.Linq.XElement",                 DbType.Xml,                 None,                       toInt SqlDbType.Xml
      "SQL_VARIANT",        "obj",                                      DbType.Object,              None,                       toInt SqlDbType.Variant
      "GEOGRAPHY",          "Microsoft.SqlServer.Types.SqlGeography",   DbType.Object,              None,                       Some 29
      "GEOMETRY",           "Microsoft.SqlServer.Types.SqlGeometry",    DbType.Object,              None,                       Some 29
      "HIERARCHYID",        "Microsoft.SqlServer.Types.SqlHierarchyId", DbType.Object,              None,                       Some 29 ]
    |> List.map (fun (providerTypeName, clrType, dbType, readerMethod, providerType) ->
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
        

module SqlHydra.Sqlite.SqliteDataTypes

open System.Data
open SqlHydra.Domain

(* 
    Column types with a "ReaderMethod" will have a DataReader property generated if readers are enabled.
*)
let typeMappingsByName =
    [ 
        "smallint",         "int16",            DbType.Int16,       Some "GetInt16"
        "int",              "int",              DbType.Int32,       Some "GetInt32"
        "real",             "double",           DbType.Double,      Some "GetDouble"
        "single",           "System.Single",    DbType.Single,      Some "GetDouble"
        "float",            "double",           DbType.Double,      Some "GetDouble"
        "double",           "double",           DbType.Double,      Some "GetDouble"
        "money",            "decimal",          DbType.Decimal,     Some "GetDecimal"
        "currency",         "decimal",          DbType.Decimal,     Some "GetDecimal"
        "decimal",          "decimal",          DbType.Decimal,     Some "GetDecimal"
        "numeric",          "decimal",          DbType.Decimal,     Some "GetDecimal"
        "bit",              "bool",             DbType.Boolean,     Some "GetBoolean"
        "yesno",            "bool",             DbType.Boolean,     Some "GetBoolean"
        "logical",          "bool",             DbType.Boolean,     Some "GetBoolean"
        "bool",             "bool",             DbType.Boolean,     Some "GetBoolean"
        "boolean",          "bool",             DbType.Boolean,     Some "GetBoolean"
        "tinyint",          "byte",             DbType.Byte,        Some "GetByte"
        "integer",          "int64",            DbType.Int64,       Some "GetInt64"
        "identity",         "int64",            DbType.Int64,       Some "GetInt64"
        "integer identity", "int64",            DbType.Int64,       Some "GetInt64"
        "counter",          "int64",            DbType.Int64,       Some "GetInt64"
        "autoincrement",    "int64",            DbType.Int64,       Some "GetInt64"
        "long",             "int64",            DbType.Int64,       Some "GetInt64"
        "bigint",           "int64",            DbType.Int64,       Some "GetInt64"
        "binary",           "byte[]",           DbType.Binary,      Some "GetValue"
        "varbinary",        "byte[]",           DbType.Binary,      Some "GetValue"
        "blob",             "byte[]",           DbType.Binary,      Some "GetValue"
        "image",            "byte[]",           DbType.Binary,      Some "GetValue"
        "general",          "byte[]",           DbType.Binary,      Some "GetValue"
        "oleobject",        "byte[]",           DbType.Binary,      Some "GetValue"
        "varchar",          "string",           DbType.String,      Some "GetString"
        "nvarchar",         "string",           DbType.String,      Some "GetString"
        "memo",             "string",           DbType.String,      Some "GetString"
        "longtext",         "string",           DbType.String,      Some "GetString"
        "note",             "string",           DbType.String,      Some "GetString"
        "text",             "string",           DbType.String,      Some "GetString"
        "ntext",            "string",           DbType.String,      Some "GetString"
        "string",           "string",           DbType.String,      Some "GetString"
        "char",             "string",           DbType.String,      Some "GetString"
        "nchar",            "string",           DbType.String,      Some "GetString"
        "xml",              "string",           DbType.Xml,         Some "GetString"
        "datetime",         "System.DateTime",  DbType.DateTime,    Some "GetDateTime"
        "smalldate",        "System.DateTime",  DbType.DateTime,    Some "GetDateTime" 
        "timestamp",        "System.DateTime",  DbType.DateTime,    Some "GetDateTime" 
        "date",             "System.DateTime",  DbType.DateTime,    Some "GetDateTime" 
        "time",             "System.DateTime",  DbType.DateTime,    Some "GetDateTime" 
        "uniqueidentifier", "System.Guid",      DbType.Guid,        Some "GetGuid"
        "guid",             "System.Guid",      DbType.Guid,        Some "GetGuid" 
    ]
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
    typeMappingsByName.TryFind(providerTypeName.ToLower().Trim())
    |> Option.defaultWith (fun () -> failwithf "Column type not handled: %s" providerTypeName)


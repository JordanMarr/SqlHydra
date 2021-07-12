module SqlHydra.Sqlite.SqliteDataTypes

open System.Data
open SqlHydra.Schema

let typeMappingsByName =
    [ "smallint"        ,"int16",           DbType.Int16,   "GetInt16"
      "int"             ,"int",             DbType.Int32,   "GetInt32"
      "real"            ,"double",          DbType.Double,  "GetDouble"
      "single"          ,"System.Single",   DbType.Single,  "GetDouble"
      "float"           ,"double",          DbType.Double,  "GetDouble"
      "double"          ,"double",          DbType.Double,  "GetDouble"
      "money"           ,"decimal",         DbType.Decimal, "GetDecimal"
      "currency"        ,"decimal",         DbType.Decimal, "GetDecimal"
      "decimal"         ,"decimal",         DbType.Decimal, "GetDecimal"
      "numeric"         ,"decimal",         DbType.Decimal, "GetDecimal"
      "bit"             ,"bool",            DbType.Boolean, "GetBoolean"
      "yesno"           ,"bool",            DbType.Boolean, "GetBoolean"
      "logical"         ,"bool",            DbType.Boolean, "GetBoolean"
      "bool"            ,"bool",            DbType.Boolean, "GetBoolean"
      "boolean"         ,"bool",            DbType.Boolean, "GetBoolean"
      "tinyint"         ,"byte",            DbType.Int16,   "GetInt16"
      "integer"         ,"int64",           DbType.Int64,   "GetInt64"
      "identity"        ,"int64",           DbType.Int64,   "GetInt64"
      "integer identity ","int64",           DbType.Int64,   "GetInt64"
      "counter"         ,"int64",           DbType.Int64,   "GetInt64"
      "autoincrement"   ,"int64",           DbType.Int64,   "GetInt64"
      "long"            ,"int64",           DbType.Int64,   "GetInt64"
      "bigint"          ,"int64",           DbType.Int64,   "GetInt64"
      "binary"          ,"byte[]",          DbType.Binary,  "GetValue"
      "varbinary"       ,"byte[]",          DbType.Binary,  "GetValue"
      "blob"            ,"byte[]",          DbType.Binary,  "GetValue"
      "image"           ,"byte[]",          DbType.Binary,  "GetValue"
      "general"         ,"byte[]",          DbType.Binary,  "GetValue"
      "oleobject"       ,"byte[]",          DbType.Binary,  "GetValue"
      "varchar"         ,"string",          DbType.String,  "GetString"
      "nvarchar"        ,"string",          DbType.String,  "GetString"
      "memo"            ,"string",          DbType.String,  "GetString"
      "longtext"        ,"string",          DbType.String,  "GetString"
      "note"            ,"string",          DbType.String,  "GetString"
      "text"            ,"string",          DbType.String,  "GetString"
      "ntext"           ,"string",          DbType.String,  "GetString"
      "string"          ,"string",          DbType.String,  "GetString"
      "char"            ,"string",          DbType.String,  "GetString"
      "nchar"           ,"string",          DbType.String,  "GetString"
      "datetime"        ,"System.DateTime", DbType.DateTime,"GetDateTime"
      "smalldate"       ,"System.DateTime", DbType.DateTime,"GetDateTime" 
      "timestamp"       ,"System.DateTime", DbType.DateTime,"GetDateTime" 
      "date"            ,"System.DateTime", DbType.DateTime,"GetDateTime" 
      "time"            ,"System.DateTime", DbType.DateTime,"GetDateTime" 
      "uniqueidentifier","System.Guid",     DbType.Guid,    "GetGuid"
      "guid"            ,"System.Guid",     DbType.Guid,    "GetGuid" ]
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
    typeMappingsByName.TryFind(providerTypeName.ToLower())
    |> Option.defaultWith (fun () -> failwithf "Provider type not handled: %s" providerTypeName)


module SqlHydra.Sqlite.SqliteDataTypes

open System.Data
open SqlHydra.Domain

let private r : System.Data.Common.DbDataReader = null

(* 
    Column types with a "ReaderMethod" will have a DataReader property generated if readers are enabled.
*)
let typeMappings =
    [ 
        "smallint",         "int16",            DbType.Int16,       Some <| nameof r.GetInt16
        "int",              "int",              DbType.Int32,       Some <| nameof r.GetInt32
        "real",             "double",           DbType.Double,      Some <| nameof r.GetDouble
        "single",           "System.Single",    DbType.Single,      Some <| nameof r.GetDouble
        "float",            "double",           DbType.Double,      Some <| nameof r.GetDouble
        "double",           "double",           DbType.Double,      Some <| nameof r.GetDouble
        "money",            "decimal",          DbType.Decimal,     Some <| nameof r.GetDecimal
        "currency",         "decimal",          DbType.Decimal,     Some <| nameof r.GetDecimal
        "decimal",          "decimal",          DbType.Decimal,     Some <| nameof r.GetDecimal
        "numeric",          "decimal",          DbType.Decimal,     Some <| nameof r.GetDecimal
        "bit",              "bool",             DbType.Boolean,     Some <| nameof r.GetBoolean
        "yesno",            "bool",             DbType.Boolean,     Some <| nameof r.GetBoolean
        "logical",          "bool",             DbType.Boolean,     Some <| nameof r.GetBoolean
        "bool",             "bool",             DbType.Boolean,     Some <| nameof r.GetBoolean
        "boolean",          "bool",             DbType.Boolean,     Some <| nameof r.GetBoolean
        "tinyint",          "byte",             DbType.Byte,        Some <| nameof r.GetByte
        "integer",          "int64",            DbType.Int64,       Some <| nameof r.GetInt64
        "identity",         "int64",            DbType.Int64,       Some <| nameof r.GetInt64
        "integer identity", "int64",            DbType.Int64,       Some <| nameof r.GetInt64
        "counter",          "int64",            DbType.Int64,       Some <| nameof r.GetInt64
        "autoincrement",    "int64",            DbType.Int64,       Some <| nameof r.GetInt64
        "long",             "int64",            DbType.Int64,       Some <| nameof r.GetInt64
        "bigint",           "int64",            DbType.Int64,       Some <| nameof r.GetInt64
        "binary",           "byte[]",           DbType.Binary,      Some <| nameof r.GetValue
        "varbinary",        "byte[]",           DbType.Binary,      Some <| nameof r.GetValue
        "blob",             "byte[]",           DbType.Binary,      Some <| nameof r.GetValue
        "image",            "byte[]",           DbType.Binary,      Some <| nameof r.GetValue
        "general",          "byte[]",           DbType.Binary,      Some <| nameof r.GetValue
        "oleobject",        "byte[]",           DbType.Binary,      Some <| nameof r.GetValue
        "varchar",          "string",           DbType.String,      Some <| nameof r.GetString
        "nvarchar",         "string",           DbType.String,      Some <| nameof r.GetString
        "memo",             "string",           DbType.String,      Some <| nameof r.GetString
        "longtext",         "string",           DbType.String,      Some <| nameof r.GetString
        "note",             "string",           DbType.String,      Some <| nameof r.GetString
        "text",             "string",           DbType.String,      Some <| nameof r.GetString
        "ntext",            "string",           DbType.String,      Some <| nameof r.GetString
        "string",           "string",           DbType.String,      Some <| nameof r.GetString
        "char",             "string",           DbType.String,      Some <| nameof r.GetString
        "nchar",            "string",           DbType.String,      Some <| nameof r.GetString
        "xml",              "string",           DbType.Xml,         Some <| nameof r.GetString
        "datetime",         "System.DateTime",  DbType.DateTime,    Some <| nameof r.GetDateTime
        "smalldate",        "System.DateTime",  DbType.DateTime,    Some <| nameof r.GetDateTime 
        "timestamp",        "System.DateTime",  DbType.DateTime,    Some <| nameof r.GetDateTime 
        "date",             "System.DateTime",  DbType.DateTime,    Some <| nameof r.GetDateTime 
        "time",             "System.DateTime",  DbType.DateTime,    Some <| nameof r.GetDateTime 
        "uniqueidentifier", "System.Guid",      DbType.Guid,        Some <| nameof r.GetGuid
        "guid",             "System.Guid",      DbType.Guid,        Some <| nameof r.GetGuid 
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
    typeMappingsByName.TryFind(providerTypeName.ToLower().Trim())
    |> Option.defaultWith (fun () -> failwithf "Column type not handled: %s" providerTypeName)

let primitiveTypeReaders = 
    typeMappings
    |> List.choose(fun (_, clrType, _, readerMethod) ->
        match readerMethod with
        | Some rm -> Some { PrimitiveTypeReader.ClrType = clrType; PrimitiveTypeReader.ReaderMethod = rm }
        | None -> None
    )
    |> List.distinctBy (fun ptr -> ptr.ClrType)

module SqlHydra.Sqlite.SqliteDataTypes

open System.Data
open SqlHydra.Domain

let private r : System.Data.Common.DbDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings =
    [ 
        "smallint",         "int16",            DbType.Int16,       nameof r.GetInt16
        "int",              "int",              DbType.Int32,       nameof r.GetInt32
        "real",             "double",           DbType.Double,      nameof r.GetDouble
        "single",           "System.Single",    DbType.Single,      nameof r.GetDouble
        "float",            "double",           DbType.Double,      nameof r.GetDouble
        "double",           "double",           DbType.Double,      nameof r.GetDouble
        "money",            "decimal",          DbType.Decimal,     nameof r.GetDecimal
        "currency",         "decimal",          DbType.Decimal,     nameof r.GetDecimal
        "decimal",          "decimal",          DbType.Decimal,     nameof r.GetDecimal
        "numeric",          "decimal",          DbType.Decimal,     nameof r.GetDecimal
        "bit",              "bool",             DbType.Boolean,     nameof r.GetBoolean
        "yesno",            "bool",             DbType.Boolean,     nameof r.GetBoolean
        "logical",          "bool",             DbType.Boolean,     nameof r.GetBoolean
        "bool",             "bool",             DbType.Boolean,     nameof r.GetBoolean
        "boolean",          "bool",             DbType.Boolean,     nameof r.GetBoolean
        "tinyint",          "byte",             DbType.Byte,        nameof r.GetByte
        "integer",          "int64",            DbType.Int64,       nameof r.GetInt64
        "identity",         "int64",            DbType.Int64,       nameof r.GetInt64
        "integer identity", "int64",            DbType.Int64,       nameof r.GetInt64
        "counter",          "int64",            DbType.Int64,       nameof r.GetInt64
        "autoincrement",    "int64",            DbType.Int64,       nameof r.GetInt64
        "long",             "int64",            DbType.Int64,       nameof r.GetInt64
        "bigint",           "int64",            DbType.Int64,       nameof r.GetInt64
        "binary",           "byte[]",           DbType.Binary,      nameof r.GetValue
        "varbinary",        "byte[]",           DbType.Binary,      nameof r.GetValue
        "blob",             "byte[]",           DbType.Binary,      nameof r.GetValue
        "image",            "byte[]",           DbType.Binary,      nameof r.GetValue
        "general",          "byte[]",           DbType.Binary,      nameof r.GetValue
        "oleobject",        "byte[]",           DbType.Binary,      nameof r.GetValue
        "varchar",          "string",           DbType.String,      nameof r.GetString
        "nvarchar",         "string",           DbType.String,      nameof r.GetString
        "memo",             "string",           DbType.String,      nameof r.GetString
        "longtext",         "string",           DbType.String,      nameof r.GetString
        "note",             "string",           DbType.String,      nameof r.GetString
        "text",             "string",           DbType.String,      nameof r.GetString
        "ntext",            "string",           DbType.String,      nameof r.GetString
        "string",           "string",           DbType.String,      nameof r.GetString
        "char",             "string",           DbType.String,      nameof r.GetString
        "nchar",            "string",           DbType.String,      nameof r.GetString
        "xml",              "string",           DbType.Xml,         nameof r.GetString
        "datetime",         "System.DateTime",  DbType.DateTime,    nameof r.GetDateTime
        "smalldate",        "System.DateTime",  DbType.DateTime,    nameof r.GetDateTime 
        "timestamp",        "System.DateTime",  DbType.DateTime,    nameof r.GetDateTime 
#if NET5_0
        "date",             "System.DateTime",  DbType.DateTime,    nameof r.GetDateTime 
        "time",             "System.DateTime",  DbType.DateTime,    nameof r.GetDateTime 
#endif
#if NET6_0
        "date",             "System.DateOnly",  DbType.DateTime,    "GetDateOnly"
        "time",             "System.TimeOnly",  DbType.DateTime,    nameof r.GetFieldValue 
#endif
        "uniqueidentifier", "System.Guid",      DbType.Guid,        nameof r.GetGuid
        "guid",             "System.Guid",      DbType.Guid,        nameof r.GetGuid 
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
    typeMappingsByName.TryFind(providerTypeName.ToLower().Trim())

let primitiveTypeReaders = 
    supportedTypeMappings
    |> List.map(fun (_, clrType, _, readerMethod) ->
        { PrimitiveTypeReader.ClrType = clrType; PrimitiveTypeReader.ReaderMethod = readerMethod }
    )
    |> List.distinctBy (fun ptr -> ptr.ClrType)

module SqlHydra.Sqlite.SqliteDataTypes

open System.Data

type TypeMapping = {
    ClrType: string
    DbType: DbType
    ProviderType: int option
    ProviderTypeName: string option
}

let typeMappingsByName =
    let toInt = int >> Some
    // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-data-type-mappings
    [ "UNIQUEIDENTIFIER", "System.Guid", DbType.Guid, toInt SqlDbType.UniqueIdentifier
      "BIT", "bool", DbType.Boolean, toInt SqlDbType.Bit
      "INT", "int", DbType.Int32, toInt SqlDbType.Int
      "BIGINT", "System.Int64", DbType.Int64, toInt SqlDbType.BigInt
      "SMALLINT", "System.Int16", DbType.Int16, toInt SqlDbType.SmallInt
      "TINYINT", "byte", DbType.Byte, toInt SqlDbType.TinyInt
      "FLOAT", "double", DbType.Double, toInt SqlDbType.Float
      "REAL", "System.Single", DbType.Single, toInt SqlDbType.Real
      "DECIMAL", "decimal", DbType.Decimal, toInt SqlDbType.Decimal
      "NUMERIC", "decimal", DbType.Decimal, toInt SqlDbType.Decimal
      "MONEY", "decimal", DbType.Decimal, toInt SqlDbType.Money
      "SMALLMONEY", "decimal", DbType.Decimal, toInt SqlDbType.SmallMoney
      "VARCHAR", "string", DbType.String, toInt SqlDbType.VarChar
      "NVARCHAR", "string", DbType.String, toInt SqlDbType.NVarChar
      "CHAR", "string", DbType.String, toInt SqlDbType.Char
      "NCHAR", "string", DbType.StringFixedLength, toInt SqlDbType.NChar
      "TEXT", "string", DbType.String, toInt SqlDbType.Text
      "NTEXT", "string", DbType.String, toInt SqlDbType.NText
      "DATETIMEOFFSET", "System.DateTimeOffset", DbType.DateTimeOffset, toInt SqlDbType.DateTimeOffset
      "DATE", "System.DateTime", DbType.Date, toInt SqlDbType.Date
      "DATETIME", "System.DateTime", DbType.DateTime, toInt SqlDbType.DateTime
      "DATETIME2", "System.DateTime", DbType.DateTime2, toInt SqlDbType.DateTime2
      "SMALLDATETIME", "System.DateTime", DbType.DateTime, toInt SqlDbType.SmallDateTime
      "TIME", "System.TimeSpan", DbType.Time, toInt SqlDbType.Time
      "VARBINARY", "byte[]", DbType.Binary, toInt SqlDbType.VarBinary
      "BINARY", "byte[]", DbType.Binary, toInt SqlDbType.Binary
      "IMAGE", "byte[]", DbType.Binary, toInt SqlDbType.Image
      "ROWVERSION", "byte[]", DbType.Binary, None
      "XML", "System.Xml.Linq.XElement", DbType.Xml, toInt SqlDbType.Xml
      "SQL_VARIANT", "obj", DbType.Object, toInt SqlDbType.Variant
      "GEOGRAPHY", "Microsoft.SqlServer.Types.SqlGeography", DbType.Object, Some 29
      "GEOMETRY", "Microsoft.SqlServer.Types.SqlGeometry", DbType.Object, Some 29
      "HIERARCHYID", "Microsoft.SqlServer.Types.SqlHierarchyId", DbType.Object, Some 29 ]
    |> List.map (fun (providerTypeName, clrType, dbType, providerType) ->
        providerTypeName,
        { TypeMapping.ProviderTypeName = Some providerTypeName
          TypeMapping.ClrType = clrType
          TypeMapping.DbType = dbType
          TypeMapping.ProviderType = providerType }
    )
    |> Map.ofList

let tryFindMapping (dataType: string) =
    typeMappingsByName.TryFind (dataType.ToUpper())


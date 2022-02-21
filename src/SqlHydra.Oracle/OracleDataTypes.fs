module SqlHydra.Oracle.OracleDataTypes

open System.Data
open SqlHydra.Domain

let private r : Oracle.ManagedDataAccess.Client.OracleDataReader = null

/// A list of supported column type mappings
let supportedTypeMappings =
    [   // https://docs.oracle.com/cd/B19306_01/win.102/b14306/appendixa.htm
        "PLS_INTEGER",                      "int",                          DbType.Int32,               nameof r.GetInt32
        "LONG",                             "int64",                        DbType.Int64,               nameof r.GetInt64
        "NUMBER",                           "decimal",                      DbType.Decimal,             nameof r.GetDecimal
        "FLOAT",                            "double",                       DbType.Double,              nameof r.GetDouble
        "BINARY_DOUBLE",                    "double",                       DbType.Double,              nameof r.GetDouble
        "BINARY_FLOAT",                     "System.Single",                DbType.Single,              nameof r.GetFieldValue
        "REAL",                             "System.Single",                DbType.Single,              nameof r.GetFieldValue
        "ROWID",                            "string",                       DbType.String,              nameof r.GetString
        "UROWID",                           "string",                       DbType.String,              nameof r.GetString
        "VARCHAR",                          "string",                       DbType.String,              nameof r.GetString
        "VARCHAR2",                         "string",                       DbType.String,              nameof r.GetString
        "NVARCHAR",                         "string",                       DbType.String,              nameof r.GetString
        "NVARCHAR2",                        "string",                       DbType.String,              nameof r.GetString
        "CHAR",                             "string",                       DbType.String,              nameof r.GetString
        "XMLType",                          "string",                       DbType.String,              nameof r.GetString
        "NCHAR",                            "string",                       DbType.StringFixedLength,   nameof r.GetString
        "TEXT",                             "string",                       DbType.String,              nameof r.GetString
        "NTEXT",                            "string",                       DbType.String,              nameof r.GetString
        "DATE",                             "System.DateTime",              DbType.Date,                nameof r.GetDateTime
        "TIMESTAMP",                        "System.DateTime",              DbType.Date,                nameof r.GetDateTime
        "TIMESTAMP WITH LOCAL TIME ZONE",   "System.DateTime",              DbType.Date,                nameof r.GetDateTime
        "TIMESTAMP WITH TIME ZONE",         "System.DateTime",              DbType.Date,                nameof r.GetDateTime
        "INTERVAL DAY TO SECOND",           "System.TimeSpan",              DbType.Time,                nameof r.GetTimeSpan
        "BFILE",                            "byte[]",                       DbType.Binary,              nameof r.GetValue
        "BLOB",                             "byte[]",                       DbType.Binary,              nameof r.GetValue
        "LONG RAW",                         "byte[]",                       DbType.Binary,              nameof r.GetValue
        "RAW",                              "byte[]",                       DbType.Binary,              nameof r.GetValue
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
        
let tryFindTypeMapping (providerTypeName: string, precisionMaybe: int option, scaleMaybe: int option) =
    typeMappingsByName.TryFind (providerTypeName.ToUpper())
    |> Option.map (fun mapping -> 
        // Precision and scale defaults:
        // https://docs.oracle.com/cd/B28359_01/server.111/b28318/datatype.htm#CNCPT313
        let precision = precisionMaybe |> Option.defaultValue 38
        let scale = scaleMaybe |> Option.defaultValue 0

        // NUMBER -> CLR mappings:
        // https://www.llblgen.com/Documentation/5.5/Designer/Databases/oracleodpnet.htm
        match mapping.ColumnTypeAlias, precision, scale with
        | "NUMBER", precision, 0 when 0 <= precision && precision < 5 -> 
            { mapping with ClrType = "int16"; DbType = DbType.Int16; ReaderMethod = nameof r.GetInt16 }

        | "NUMBER", precision, 0 when 5 <= precision && precision < 10 ->
            { mapping with ClrType = "int"; DbType = DbType.Int32; ReaderMethod = nameof r.GetInt32 }

        | "NUMBER", precision, 0 when 10 <= precision && precision < 19 ->
            { mapping with ClrType = "int64"; DbType = DbType.Int64; ReaderMethod = nameof r.GetInt64 }

        | "NUMBER", precision, 0 when precision >= 19 ->
            { mapping with ClrType = "decimal"; DbType = DbType.Decimal; ReaderMethod = nameof r.GetDecimal }

        | "NUMBER", precision, scale when 0 <= precision && precision < 8 && scale > 0 ->
            { mapping with ClrType = "System.Single"; DbType = DbType.Single; ReaderMethod = nameof r.GetFieldValue }

        | "NUMBER", precision, scale when 8 <= precision && precision < 16 && scale > 0 ->
            { mapping with ClrType = "double"; DbType = DbType.Double; ReaderMethod = nameof r.GetDouble }

        | "NUMBER", precision, scale when precision >= 16 && scale > 0 ->
            { mapping with ClrType = "decimal"; DbType = DbType.Decimal; ReaderMethod = nameof r.GetDecimal }

        | _ ->
            mapping
    )
        
let primitiveTypeReaders = 
    supportedTypeMappings
    |> List.map (fun (_, clrType, _, readerMethod) ->
        { PrimitiveTypeReader.ClrType = clrType; PrimitiveTypeReader.ReaderMethod = readerMethod }
    )
    |> List.distinctBy (fun ptr -> ptr.ClrType)

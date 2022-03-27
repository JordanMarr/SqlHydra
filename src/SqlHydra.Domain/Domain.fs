module SqlHydra.Domain

open System.Data

type AppInfo = {
    Name: string
    Command: string
    DefaultReaderType: string
    DefaultProvider: string
    Version: string
}

type TypeMapping = 
    {
        ClrType: string
        DbType: DbType
        ProviderDbType: string option
        ColumnTypeAlias: string
        ReaderMethod: string
    }

type Column = 
    {
        Name: string
        TypeMapping: TypeMapping
        IsNullable: bool
        IsPK: bool
    }

type TableType = 
    | Table = 0
    | View = 1

type Table = 
    {
        Catalog: string
        Schema: string
        Name: string
        Type: TableType
        Columns: Column list
        TotalColumns: int
    }

type EnumLabel = 
    {
        Name: string
        SortOrder: int
    }

type Enum = 
    {
        Name: string
        Schema: string
        Labels: EnumLabel list
    }

type PrimitiveTypeReader =
    {
        ClrType: string
        ReaderMethod: string
    }

type Schema = 
    {
        Tables: Table list

        /// Support for Postgres enums
        Enums: Enum list

        /// A distinct list of ClrTypes that have an associated data reader method. Ex: `"int", "GetInt32"`
        PrimitiveTypeReaders: PrimitiveTypeReader seq
    }

type ReadersConfig = 
    {
        /// A fully qualified reader type. Ex: "Microsoft.Data.SqlClient.SqlDataReader"
        ReaderType: string
    }

type FilterPatterns = 
    {
        Includes: string list
        Excludes: string list
    }
    static member Empty = { Includes = []; Excludes = [] }

type Config = 
    {
        ConnectionString: string
        OutputFile: string
        Namespace: string
        IsCLIMutable: bool
        Filters: FilterPatterns
        Readers: ReadersConfig option
    }

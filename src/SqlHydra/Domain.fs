module SqlHydra.Domain

open System.Data

type AppInfo = {
    Name: string
    Command: string
    DefaultReaderType: string
    Version: string
}

type TypeMapping = 
    {
        ClrType: string
        DbType: DbType
        ColumnTypeAlias: string
        ReaderMethod: string option
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
        Columns: Column array
    }

type Schema = 
    {
        Tables: Table array
    }

type ReadersConfig = 
    {
        /// A fully qualified reader type. Ex: "Microsoft.Data.SqlClient.SqlDataReader"
        ReaderType: string
    }

type Config = 
    {
        ConnectionString: string
        OutputFile: string
        Namespace: string
        IsCLIMutable: bool
        Readers: ReadersConfig option
    }

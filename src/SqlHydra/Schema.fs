module SqlHydra.Schema

open Argu
open System.Data

type TypeMapping = 
    {
        ClrType: string
        DbType: DbType
        ProviderTypeName: string
        ReaderMethod: string option
    }

type Column = 
    {
        Name: string
        TypeMapping: TypeMapping
        IsNullable: bool
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
        IsEnabled: bool
        /// A fully qualified reader type. Ex: "Microsoft.Data.SqlClient.SqlDataReader"
        ReaderType: string
    }

type Config = 
    {
        ConnectionString: string
        OutputFile: string
        Namespace: string
        IsCLIMutable: bool
        Readers: ReadersConfig
    }

type Arguments = 
    | [<Mandatory;  AltCommandLine("-c")>]      Connection of string
    | [<Mandatory;  AltCommandLine("-o")>]      Output of string
    | [<Mandatory;  AltCommandLine("-ns")>]     Namespace of string
    |                                           CLI_Mutable
    |                                           Readers of string option
    
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Connection _ -> "The database connection string."
            | Namespace _ -> "The namespace of the generated .fs output file."
            | Output _ -> "The file path where the .fs output file will be generated. (Relative paths are valid.)"
            | CLI_Mutable -> "If this argument exists, a 'CLIMutable' attribute will be added to each record."
            | Readers _ -> "If this argument exists, a 'Reader' class will be generated alongside each table record. Can optionally specify reader type. Ex: System.Data.SqlClient.SqlDataReader"

module SqlHydra.Schema

open Argu

type Column = {
    Name: string
    DataType: string
    ClrType: string
    IsNullable: bool
}

type TableType = 
    | Table = 0
    | View = 1

type Table = {
    Catalog: string
    Schema: string
    Name: string
    Type: TableType
    Columns: Column array
}

type Schema = {
    Tables: Table array
}

type Config = {
    ConnectionString: string
    OutputFile: string
    Namespace: string
    IsCLIMutable: bool
}

type Arguments = 
    | [<Mandatory>] Connection of string
    | [<Mandatory>] Output of string
    | Namespace of string
    | CLI_Mutable

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Connection _ -> "The database connection string."
            | Namespace _ -> "The namespace of the generated .fs output file."
            | Output _ -> "The file path where the .fs output file will be generated. (Relative paths are valid.)"
            | CLI_Mutable -> "If this argument exists, a 'CLIMutable' attribute will be added to each record."

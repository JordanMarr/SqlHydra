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

type PrimitiveTypeReader =
    {
        ClrType: string
        ReaderMethod: string
    }

type Schema = 
    {
        Tables: Table list
        /// A distinct list of ClrTypes that have an associated data reader method. Ex: `"int", "GetInt32"`
        PrimitiveTypeReaders: PrimitiveTypeReader seq
    }

type ReadersConfig = 
    {
        /// A fully qualified reader type. Ex: "Microsoft.Data.SqlClient.SqlDataReader"
        ReaderType: string
    }

type Filters = 
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
        Filters: Filters
        Readers: ReadersConfig option
    }

open GlobExpressions

let applyFilters (filters: Filters) (tables: Table list) = 
    match filters with
    | { Includes = []; Excludes = [] } -> 
        tables
    | _ -> 
        let getPath tbl = $"{tbl.Schema}/{tbl.Name}"
        let tablesByPath = tables |> List.map (fun t -> getPath t, t) |> Map.ofList
        let paths = tablesByPath |> Map.toList |> List.map fst

        let includePatterns = filters.Includes |> List.map Glob
        let excludePatterns = filters.Excludes |> List.map Glob
        
        let includedPaths = 
            includePatterns
            |> List.collect (fun pattern -> paths |> List.filter pattern.IsMatch)
            |> List.distinct
            |> Set.ofList

        let excludedPaths = 
            excludePatterns
            |> List.collect (fun pattern -> paths |> List.filter pattern.IsMatch)
            |> List.distinct
            |> Set.ofList
        
        let filteredPaths = includedPaths - excludedPaths
        let filteredTables = filteredPaths |> Seq.map (fun path -> tablesByPath.[path]) |> Seq.toList
        filteredTables

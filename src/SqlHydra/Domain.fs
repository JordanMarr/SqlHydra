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

let (|TableFilter|_|) (filter: string) =
    match filter.Contains "/", filter.Contains "." with
    | false, false -> Some (TableFilter (Glob filter))
    | true, false -> Some (TableFilter (Glob filter))
    | _, true -> None

let (|ColumnFilter|_|) (filter: string) =
    if filter.Contains "."
    then Some (ColumnFilter (Glob filter))
    else None

let filterTables (filters: Filters) (tables: Table list) = 
    let includePatterns = filters.Includes |> List.choose (function | TableFilter tf -> Some tf | _ -> None)
    let excludePatterns = filters.Excludes |> List.choose (function | TableFilter tf -> Some tf | _ -> None)

    match includePatterns, excludePatterns with
    | [], [] -> 
        tables
    | _ -> 
        let getPath tbl = $"{tbl.Schema}/{tbl.Name}"
        let tablesByPath = tables |> List.map (fun t -> getPath t, t) |> Map.ofList
        let paths = tablesByPath |> Map.toList |> List.map fst

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

let filterColumns (filters: Filters) (schema: string) (table: string) (columns: Column list) = 
    let includePatterns = filters.Includes |> List.choose (function | ColumnFilter cf -> Some cf | _ -> None)
    let excludePatterns = filters.Excludes |> List.choose (function | ColumnFilter cf -> Some cf | _ -> None)

    match includePatterns, excludePatterns with
    | [], [] -> 
        columns
    | _ -> 
        let getPath (col: Column) = $"{schema}/{table}.{col.Name}"
        let columnsByPath = columns |> List.map (fun c -> getPath c, c) |> Map.ofList
        let paths = columnsByPath |> Map.toList |> List.map fst
        
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
        let filteredColumns = filteredPaths |> Seq.map (fun path -> columnsByPath.[path]) |> Seq.toList
        filteredColumns


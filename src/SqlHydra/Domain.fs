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

let filterTables (filters: Filters) (tables: Table list) = 
    let isTableFilter (filter: string) = not (filter.Contains ".")
    let includes = filters.Includes |> List.filter isTableFilter
    let excludes = filters.Excludes |> List.filter isTableFilter

    match includes, excludes with
    | [], [] -> 
        tables
    | _ -> 
        let getPath tbl = $"{tbl.Schema}/{tbl.Name}"
        let tablesByPath = tables |> List.map (fun t -> getPath t, t) |> Map.ofList
        let paths = tablesByPath |> Map.toList |> List.map fst

        let includedPaths = 
            includes
            |> List.map Glob
            |> List.collect (fun pattern -> paths |> List.filter pattern.IsMatch)
            |> List.distinct
            |> Set.ofList

        let excludedPaths = 
            excludes
            |> List.map Glob
            |> List.collect (fun pattern -> paths |> List.filter pattern.IsMatch)
            |> List.distinct
            |> Set.ofList
        
        let filteredPaths = includedPaths - excludedPaths
        let filteredTables = filteredPaths |> Seq.map (fun path -> tablesByPath.[path]) |> Seq.toList
        filteredTables

let filterColumns (filters: Filters) (schema: string) (table: string) (columns: Column list) = 
    let isColumnFilter (filter: string) = filter.Contains "."
    let includes = filters.Includes |> List.filter isColumnFilter
    let excludes = filters.Excludes |> List.filter isColumnFilter

    match includes, excludes with
    | [], [] -> 
        columns
    | _ -> 
        let getPath (col: Column) = $"{schema}/{table}.{col.Name}"
        let columnsByPath = columns |> List.map (fun c -> getPath c, c) |> Map.ofList
        let paths = columnsByPath |> Map.toList |> List.map fst
        
        let includedPaths = 
            includes
            |> List.map Glob
            |> List.collect (fun pattern -> paths |> List.filter pattern.IsMatch)
            |> List.distinct
            |> Set.ofList

        let excludedPaths = 
            excludes
            |> List.map Glob
            |> List.collect (fun pattern -> paths |> List.filter pattern.IsMatch)
            |> List.distinct
            |> Set.ofList
        
        let filteredPaths = includedPaths - excludedPaths
        let filteredColumns = filteredPaths |> Seq.map (fun path -> columnsByPath.[path]) |> Seq.toList
        filteredColumns

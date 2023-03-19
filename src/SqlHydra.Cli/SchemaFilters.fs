module SqlHydra.SchemaFilters

open GlobExpressions
open SqlHydra.Domain

let filterTables (filters: FilterPatterns) (tables: Table list) = 
    let isTableFilter (filter: string) = not (filter.Contains ".")
    let includeFilters = filters.Includes |> List.filter isTableFilter
    let excludeFilters = filters.Excludes |> List.filter isTableFilter

    match includeFilters, excludeFilters with
    | [], [] -> 
        tables
    | _ -> 
        let getPath (tbl: Table) = $"{tbl.Schema}/{tbl.Name}"
        let tablesByPath = tables |> List.map (fun t -> getPath t, t) |> Map.ofList
        let tablePaths = tablesByPath |> Map.toList |> List.map fst

        let getMatchingTablePaths = 
            List.map Glob
            >> List.collect (fun pattern -> tablePaths |> List.filter pattern.IsMatch)
            >> List.distinct
            >> Set.ofList

        let includedPaths = includeFilters |> getMatchingTablePaths
        let excludedPaths = excludeFilters |> getMatchingTablePaths
        
        let filteredPaths = includedPaths - excludedPaths
        let filteredTables = filteredPaths |> Seq.map (fun path -> tablesByPath.[path]) |> Seq.toList
        filteredTables

let filterColumns (filters: FilterPatterns) (schema: string) (table: string) (columns: Column list) = 
    let isColumnFilter (filter: string) = filter.Contains "."
    let includeFilters = filters.Includes |> List.filter isColumnFilter
    let excludeFilters = filters.Excludes |> List.filter isColumnFilter

    match includeFilters, excludeFilters with
    | [], [] -> 
        columns
    | _ -> 
        let getPath (col: Column) = $"{schema}/{table}.{col.Name}"
        let columnsByPath = columns |> List.map (fun c -> getPath c, c) |> Map.ofList
        let columnPaths = columnsByPath |> Map.toList |> List.map fst
        
        let getMatchingColumnPaths = 
            List.map Glob
            >> List.collect (fun pattern -> columnPaths |> List.filter pattern.IsMatch)
            >> List.distinct
            >> Set.ofList

        let includedPaths = includeFilters |> getMatchingColumnPaths
        let excludedPaths = excludeFilters |> getMatchingColumnPaths
        
        let filteredPaths = includedPaths - excludedPaths
        let filteredColumns = filteredPaths |> Seq.map (fun path -> columnsByPath.[path]) |> Seq.toList
        filteredColumns

module SqlHydra.Filter

open GlobExpressions
open SqlHydra.Domain

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

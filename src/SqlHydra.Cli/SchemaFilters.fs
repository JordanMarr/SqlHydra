module SqlHydra.SchemaFilters

open GlobExpressions
open SqlHydra.Domain
open Spectre.Console

/// Applies glob include and exclude patterns to filter schemas and tables.
let inline filterTables (filters: Filters) (tables: 'Table seq when 'Table : (member Schema: string) and 'Table : (member Name: string)) = 
    let isTableFilter (filter: string) = not (filter.Contains ".")
    let includeFilters = filters.Includes |> List.filter isTableFilter
    let excludeFilters = filters.Excludes |> List.filter isTableFilter

    match includeFilters, excludeFilters with
    | [], [] -> 
        tables
    | _ -> 
        let getPath (tbl: 'Table) = $"{tbl.Schema}/{tbl.Name}"
        let tablesByPath = tables |> Seq.map (fun t -> getPath t, t) |> Map.ofSeq
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
        
        AnsiConsole.MarkupLineInterpolated($"[blue]-[/] Filters:")
        AnsiConsole.MarkupLineInterpolated($"  [blue]-[/] Include: [green][{filters.Includes}][/]")
        AnsiConsole.MarkupLineInterpolated($"  [blue]-[/] Exclude: [red][{filters.Excludes}][/]")
        AnsiConsole.MarkupLineInterpolated($"  [blue]-[/] Tables & Views: [deepskyblue1]{Seq.length filteredTables} of {Seq.length tables}[/]")

        filteredTables

/// Applies glob include and exclude patterns to filter columns.
let inline filterColumns (filters: Filters) (schema: string) (table: string) (columns: 'Column seq when 'Column : (member Name: string)) = 
    let isColumnFilter (filter: string) = filter.Contains "."
    let includeFilters = filters.Includes |> List.filter isColumnFilter
    let excludeFilters = filters.Excludes |> List.filter isColumnFilter

    match includeFilters, excludeFilters with
    | [], [] -> 
        columns
    | _ -> 
        let getPath (col: 'Column) = $"{schema}/{table}.{col.Name}"
        let columnsByPath = columns |> Seq.map (fun c -> getPath c, c) |> Map.ofSeq
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

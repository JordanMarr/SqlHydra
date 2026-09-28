module SqlHydra.SchemaFilters

open GlobExpressions
open SqlHydra.Domain
open Spectre.Console

/// True when the path matches any of the glob patterns.
let matchesAny (patterns: string list) =
    let globs = patterns |> List.map Glob
    fun (path: string) -> globs |> List.exists (fun g -> g.IsMatch path)

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
        let isIncluded = matchesAny includeFilters
        let isExcluded = matchesAny excludeFilters

        // No table-level includes means include all (e.g. excludes only).
        let filteredTables =
            tables
            |> Seq.filter (fun tbl ->
                let path = getPath tbl
                (includeFilters.IsEmpty || isIncluded path) && not (isExcluded path))
            |> Seq.toList
        
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
        let isIncluded = matchesAny includeFilters
        let isExcluded = matchesAny excludeFilters

        // No column-level includes means include all, so `include = [ "*" ]` with a column
        // exclude keeps every other column. Filtering in place preserves column order.
        columns
        |> Seq.filter (fun col ->
            let path = getPath col
            (includeFilters.IsEmpty || isIncluded path) && not (isExcluded path))

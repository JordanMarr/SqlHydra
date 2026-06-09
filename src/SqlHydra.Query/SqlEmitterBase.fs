namespace SqlHydra.Query

open System
open System.Text
open System.Text.RegularExpressions

/// Collects parameters during SQL emission.
type ParameterCollector(prefix: string) =
    let mutable counter = 0
    let parameters = ResizeArray<string * obj>()

    /// Adds a parameter and returns its placeholder name (e.g., @p0, :p0)
    member _.Add(value: obj) =
        let name = $"{prefix}{counter}"
        counter <- counter + 1
        parameters.Add(name, value)
        name

    /// Gets all collected parameters.
    member _.Parameters = parameters |> Seq.toList

/// Base class providing shared SQL rendering logic for all provider emitters.
[<AbstractClass>]
type SqlEmitterBase() =

    /// Quote a single identifier segment (table, column, alias).
    abstract QuoteIdentifier: string -> string

    /// Parameter prefix for this provider (e.g., "@p" for SQL Server, ":p" for Oracle)
    abstract ParameterPrefix: string

    /// Emit LIMIT/OFFSET pagination. Override for provider-specific syntax.
    abstract EmitPagination: skip: int option * take: int option * sb: StringBuilder * collector: ParameterCollector -> unit

    /// Emit the identity-returning portion of an INSERT. Returns empty string if not applicable.
    abstract EmitInsertIdentity: identityField: string -> string

    /// Emit multi-row INSERT. Override for Oracle INSERT ALL.
    abstract EmitMultiRowInsert: table: string * columns: string list * rows: obj[] list * collector: ParameterCollector -> string

    /// Emit upsert/conflict handling clauses. Default: no-op.
    /// `table` is the dotted INSERT target (e.g. "public.waitlist_entries") so DO UPDATE
    /// branches can disambiguate `EXCLUDED.col` vs the target table's `col` reference.
    abstract EmitInsertConflict: insertType: InsertType * table: string * insertSql: string * columns: string list * rows: obj[] list * collector: ParameterCollector -> string

    /// Emit OUTPUT clause for INSERT (SQL Server). Default: empty.
    abstract EmitInsertOutput: outputFields: OutputField list * insertSql: string -> string

    /// Emit OUTPUT clause for UPDATE (SQL Server). Default: no-op.
    abstract EmitUpdateOutput: outputFields: OutputField list * updateSql: string -> string

    /// Emit LIKE clause. Override for provider-specific behavior (e.g., Postgres ilike).
    abstract EmitLike: quotedCol: string * paramName: string -> string
    /// Emit NOT LIKE clause. Override for provider-specific behavior.
    abstract EmitNotLike: quotedCol: string * paramName: string -> string

    /// Creates a new ParameterCollector with this emitter's prefix.
    member this.CreateCollector() = ParameterCollector(this.ParameterPrefix)

    /// Replaces `?` placeholders in `fragment` with parameter names from `collector`, in order.
    /// Each `?` consumes one entry from `parms` (silently ignored if there are more parms than `?`s).
    member _.SubstituteParams(fragment: string, parms: obj[], collector: ParameterCollector) =
        let mutable result = fragment
        for p in parms do
            let name = collector.Add(p)
            let idx = result.IndexOf("?")
            if idx >= 0 then
                result <- result.Substring(0, idx) + name + result.Substring(idx + 1)
        result

    /// Splits an INSERT command into the insert-statement and an optional trailing identity-statement
    /// produced by EmitInsertIdentity. Used by EmitInsertConflict to weave conflict clauses between them.
    member _.SplitInsertAndIdentity(sql: string) =
        match sql.Split([| ";" |], StringSplitOptions.RemoveEmptyEntries) with
        | [| iq; idq |] -> iq, idq
        | _ -> sql, ""

    /// Bare table name (last dotted segment) for disambiguating `EXCLUDED.col` vs target-table `col`.
    member _.BareTableName(table: string) =
        let parts = table.Split('.')
        parts.[parts.Length - 1]

    /// Renders an `ON CONFLICT (...) DO UPDATE SET col=EXCLUDED."col"` upsert.
    member this.BuildOnConflictUpdate(insertSql: string, conflictFields: string list, updateFields: string list) =
        let insertQuery, identityQuery = this.SplitInsertAndIdentity(insertSql)
        let setLines =
            updateFields
            |> List.map (fun col -> $"{col}=EXCLUDED.\"{col}\"\n")
            |> fun lines -> String.Join(",", lines)
        let conflictCsv = String.Join(",", conflictFields)
        StringBuilder()
            .AppendLine(insertQuery)
            .AppendLine($"ON CONFLICT({conflictCsv}) DO UPDATE SET")
            .AppendLine(setLines).Append(";")
            .AppendLine(identityQuery)
            .ToString()

    /// Renders an `ON CONFLICT (...) DO UPDATE SET` upsert where `coalesceFields`
    /// become `col = COALESCE(EXCLUDED."col", "<table>"."col")`.
    member this.BuildOnConflictUpdateCoalesce(insertSql: string, table: string, conflictFields: string list, updateFields: string list, coalesceFields: string list) =
        let bareTable = this.BareTableName(table)
        let insertQuery, identityQuery = this.SplitInsertAndIdentity(insertSql)
        let coalesceSet = Set.ofList coalesceFields
        let setLines =
            updateFields
            |> List.map (fun col ->
                if coalesceSet.Contains col
                then $"\"{col}\" = COALESCE(EXCLUDED.\"{col}\", \"{bareTable}\".\"{col}\")\n"
                else $"\"{col}\" = EXCLUDED.\"{col}\"\n")
            |> fun lines -> String.Join(",", lines)
        let conflictCsv = String.Join(",", conflictFields)
        StringBuilder()
            .AppendLine(insertQuery)
            .AppendLine($"ON CONFLICT({conflictCsv}) DO UPDATE SET")
            .AppendLine(setLines).Append(";")
            .AppendLine(identityQuery)
            .ToString()

    /// Renders an `ON CONFLICT (...) DO NOTHING` upsert.
    member this.BuildOnConflictDoNothing(insertSql: string, conflictFields: string list) =
        let insertQuery, identityQuery = this.SplitInsertAndIdentity(insertSql)
        let conflictCsv = String.Join(",", conflictFields)
        StringBuilder()
            .AppendLine(insertQuery)
            .AppendLine($"ON CONFLICT({conflictCsv})")
            .AppendLine("DO NOTHING;")
            .AppendLine(identityQuery)
            .ToString()

    /// Quotes a dotted identifier like "Schema.Table" to "[Schema].[Table]".
    member this.QuoteDotted(ident: string) =
        ident.Split('.')
        |> Array.map this.QuoteIdentifier
        |> String.concat "."

    /// Parses and quotes a FROM/JOIN table spec like "Schema.Table as alias" or "Schema.Table".
    abstract QuoteTableSpec: string -> string
    default this.QuoteTableSpec(spec: string) =
        let parts = spec.Split([| " as "; " AS "; " As " |], StringSplitOptions.RemoveEmptyEntries)
        match parts with
        | [| table; alias |] ->
            $"{this.QuoteDotted(table.Trim())} AS {this.QuoteIdentifier(alias.Trim())}"
        | [| table |] ->
            this.QuoteDotted(table.Trim())
        | _ -> spec

    /// Quotes a column reference like "alias.Column" to "[alias].[Column]".
    member this.QuoteColumn(col: string) =
        this.QuoteDotted(col)

    /// Processes a raw SQL fragment, replacing {alias}.{column} templates with quoted identifiers.
    member this.QuoteRawFragment(fragment: string) =
        Regex.Replace(fragment, @"\{(\w+)\}\.\{(\w+)\}", fun m ->
            let alias = m.Groups.[1].Value
            let col = m.Groups.[2].Value
            $"{this.QuoteIdentifier(alias)}.{this.QuoteIdentifier(col)}"
        )

    /// Emits a SqlValue, returning the SQL fragment.
    member this.EmitValue(value: SqlValue, collector: ParameterCollector) =
        match value with
        | Parameter v ->
            let name = collector.Add(v)
            name
        | Null -> "NULL"
        | ColumnRef col -> this.QuoteColumn(col)
        | SubQuery ir ->
            let compiled = this.EmitSelectCore(ir)
            // Merge sub-query parameters into outer collector
            for (_, v) in compiled.Parameters do
                collector.Add(v) |> ignore
            $"({compiled.Sql})"
        | RawSql (fragment, parms) ->
            this.SubstituteParams(fragment, parms, collector)

    /// Emits a comparison operator.
    member _.EmitOp(op: ComparisonOp) =
        match op with
        | Eq -> "="
        | NotEq -> "<>"
        | Gt -> ">"
        | GtEq -> ">="
        | Lt -> "<"
        | LtEq -> "<="

    /// Emits a WHERE/HAVING/ON clause to a string (without outer wrapping parens).
    member this.EmitWhereInner(clause: WhereClause, collector: ParameterCollector) : string =
        match clause with
        | Empty -> ""
        | Compare (col, op, value) ->
            let quotedCol = this.QuoteColumn(col)
            let valueSql = this.EmitValue(value, collector)
            $"{quotedCol} {this.EmitOp(op)} {valueSql}"
        | CompareColumns (left, op, right) ->
            $"{this.QuoteColumn(left)} {this.EmitOp(op)} {this.QuoteColumn(right)}"
        | IsNull col ->
            $"{this.QuoteColumn(col)} IS NULL"
        | IsNotNull col ->
            $"{this.QuoteColumn(col)} IS NOT NULL"
        | InValues (col, values) when values.Length = 0 ->
            "1=0"
        | InValues (col, values) ->
            let quotedCol = this.QuoteColumn(col)
            let paramNames = values |> Array.map (fun v -> collector.Add(v)) |> String.concat ", "
            $"{quotedCol} IN ({paramNames})"
        | InSubQuery (col, subquery) ->
            let compiled = this.EmitSelectCore(subquery)
            for (_, v) in compiled.Parameters do
                collector.Add(v) |> ignore
            $"{this.QuoteColumn(col)} IN ({compiled.Sql})"
        | NotInValues (col, values) when values.Length = 0 ->
            "1=1"
        | NotInValues (col, values) ->
            let quotedCol = this.QuoteColumn(col)
            let paramNames = values |> Array.map (fun v -> collector.Add(v)) |> String.concat ", "
            $"{quotedCol} NOT IN ({paramNames})"
        | NotInSubQuery (col, subquery) ->
            let compiled = this.EmitSelectCore(subquery)
            for (_, v) in compiled.Parameters do
                collector.Add(v) |> ignore
            $"{this.QuoteColumn(col)} NOT IN ({compiled.Sql})"
        | Exists subquery ->
            let compiled = this.EmitSelectCore(subquery)
            for (_, v) in compiled.Parameters do
                collector.Add(v) |> ignore
            $"EXISTS ({compiled.Sql})"
        | NotExists subquery ->
            let compiled = this.EmitSelectCore(subquery)
            for (_, v) in compiled.Parameters do
                collector.Add(v) |> ignore
            $"NOT EXISTS ({compiled.Sql})"
        | Like (col, pattern) ->
            let quotedCol = this.QuoteColumn(col)
            let paramName = collector.Add(pattern)
            this.EmitLike(quotedCol, paramName)
        | NotLike (col, pattern) ->
            let quotedCol = this.QuoteColumn(col)
            let paramName = collector.Add(pattern)
            this.EmitNotLike(quotedCol, paramName)
        | Not inner ->
            let innerSql = this.EmitWhere(inner, collector)
            $"NOT {innerSql}"
        | Combined (left, logOp, right) ->
            let leftSql = this.EmitWhereInner(left, collector)
            let rightSql = this.EmitWhereInner(right, collector)
            let opStr = match logOp with And -> "AND" | Or -> "OR"
            $"{leftSql} {opStr} {rightSql}"
        | Grouped inner ->
            this.EmitWhere(inner, collector) // wrap in parens
        | RawWhere (fragment, parms) ->
            this.SubstituteParams(fragment, parms, collector)
        | BoolColumn (col, value) ->
            let quotedCol = this.QuoteColumn(col)
            this.EmitBoolColumn(quotedCol, value, collector)

    /// Emits a WHERE/HAVING clause wrapped in parentheses (for top-level usage).
    member this.EmitWhere(clause: WhereClause, collector: ParameterCollector) : string =
        let inner = this.EmitWhereInner(clause, collector)
        if inner = "" then "" else $"({inner})"

    /// Emits a SELECT query to SQL.
    member this.EmitSelectCore(ir: SelectQueryIR) : CompiledQuery =
        let collector = this.CreateCollector()
        let sb = StringBuilder()

        // WITH (CTEs)
        if ir.WithCtes.Length > 0 then
            sb.Append("WITH ") |> ignore
            ir.WithCtes
            |> List.mapi (fun i (alias, cteIR) ->
                let cteCompiled = this.EmitSelectCore(cteIR)
                for (_, v) in cteCompiled.Parameters do
                    collector.Add(v) |> ignore
                $"{this.QuoteIdentifier(alias)} AS ({cteCompiled.Sql})"
            )
            |> String.concat ", "
            |> sb.Append |> ignore
            sb.Append(" ") |> ignore

        // SELECT
        sb.Append("SELECT ") |> ignore
        if ir.DistinctOn.Length > 0 then
            let cols = ir.DistinctOn |> List.map this.QuoteColumn |> String.concat ", "
            sb.Append($"DISTINCT ON ({cols}) ") |> ignore
        elif ir.Distinct then
            sb.Append("DISTINCT ") |> ignore

        if ir.IsCount then
            sb.Append("COUNT(*) AS ") |> ignore
            sb.Append(this.QuoteIdentifier("count")) |> ignore
        else
            match ir.Select with
            | [] ->
                sb.Append("*") |> ignore
            | cols ->
                cols
                |> List.map (fun col ->
                    match col with
                    | AllColumns alias -> $"{this.QuoteIdentifier(alias)}.*"
                    | SpecificColumn name -> this.QuoteColumn(name)
                    | RawColumn (fragment, parms) ->
                        this.SubstituteParams(this.QuoteRawFragment(fragment), parms, collector)
                )
                |> String.concat ", "
                |> sb.Append |> ignore

        // FROM
        match ir.From with
        | Some from ->
            sb.Append(" FROM ") |> ignore
            sb.Append(this.QuoteTableSpec(from)) |> ignore
        | None -> ()

        // JOINs
        for join in ir.Joins do
            let joinKeyword =
                match join.Kind with
                | InnerJoin -> "INNER JOIN"
                | LeftJoin -> "LEFT JOIN"
                | LeftJoinLateral -> "LEFT JOIN LATERAL"
            let tableSpec =
                match join.Subquery with
                | Some subIR ->
                    let compiled = this.EmitSelectCore(subIR)
                    for (_, v) in compiled.Parameters do
                        collector.Add(v) |> ignore
                    $"({compiled.Sql}) AS {this.QuoteIdentifier(join.Table)}"
                | None ->
                    this.QuoteTableSpec(join.Table)
            sb.Append($" {joinKeyword} {tableSpec} ON ") |> ignore
            let condSql = this.EmitWhere(join.Condition, collector)
            sb.Append(condSql) |> ignore

        // WHERE
        let whereSql = this.EmitWhere(ir.Where, collector)
        if whereSql <> "" then
            sb.Append($" WHERE {whereSql}") |> ignore

        // GROUP BY
        if ir.GroupBy.Length > 0 then
            let cols = ir.GroupBy |> List.map this.QuoteColumn |> String.concat ", "
            sb.Append($" GROUP BY {cols}") |> ignore

        // HAVING
        let havingSql = this.EmitWhere(ir.Having, collector)
        if havingSql <> "" then
            sb.Append($" HAVING {havingSql}") |> ignore

        // ORDER BY
        if ir.OrderBy.Length > 0 then
            let nullsSuffix nulls =
                match nulls with
                | NullsDefault -> ""
                | NullsFirst -> " NULLS FIRST"
                | NullsLast -> " NULLS LAST"
            let orderCols =
                ir.OrderBy
                |> List.map (fun ob ->
                    match ob with
                    | OrderByColumn (col, Asc) -> this.QuoteColumn(col)
                    | OrderByColumn (col, Desc) -> $"{this.QuoteColumn(col)} DESC"
                    | OrderByColumnNulls (col, Asc, nulls) -> $"{this.QuoteColumn(col)}{nullsSuffix nulls}"
                    | OrderByColumnNulls (col, Desc, nulls) -> $"{this.QuoteColumn(col)} DESC{nullsSuffix nulls}"
                    | OrderByRaw (fragment, parms) ->
                        this.SubstituteParams(this.QuoteRawFragment(fragment), parms, collector)
                )
                |> String.concat ", "
            sb.Append($" ORDER BY {orderCols}") |> ignore

        // PAGINATION
        this.EmitPagination(ir.Skip, ir.Take, sb, collector)

        { Sql = sb.ToString(); Parameters = collector.Parameters }

    /// Emits a single-row INSERT.
    member this.EmitSingleInsert(table: string, columns: string list, values: obj[], collector: ParameterCollector) =
        let quotedTable = this.QuoteDotted(table)
        let quotedCols = columns |> List.map this.QuoteIdentifier |> String.concat ", "
        let paramNames = values |> Array.map (fun v -> collector.Add(v)) |> String.concat ", "
        $"INSERT INTO {quotedTable} ({quotedCols}) VALUES ({paramNames})"

    /// Emits an INSERT query (default implementation for most providers).
    member this.EmitInsertCore(ir: InsertQueryIR) : CompiledQuery =
        let collector = this.CreateCollector()

        let baseSql =
            match ir.FromSelect with
            | Some selectIR ->
                let quotedTable = this.QuoteDotted(ir.Table)
                let quotedCols = ir.Columns |> List.map this.QuoteIdentifier |> String.concat ", "
                let compiled = this.EmitSelectCore(selectIR)
                for (_, v) in compiled.Parameters do
                    collector.Add(v) |> ignore
                $"INSERT INTO {quotedTable} ({quotedCols}) {compiled.Sql}"
            | None ->
                match ir.Rows with
                | [] -> failwith "At least one row must be provided for INSERT."
                | [ row ] -> this.EmitSingleInsert(ir.Table, ir.Columns, row, collector)
                | rows -> this.EmitMultiRowInsert(ir.Table, ir.Columns, rows, collector)

        // Apply conflict handling
        let withConflict = this.EmitInsertConflict(ir.InsertType, ir.Table, baseSql, ir.Columns, ir.Rows, collector)

        // Apply output clause
        let withOutput =
            if ir.OutputFields.Length > 0 then
                this.EmitInsertOutput(ir.OutputFields, withConflict)
            else
                withConflict

        // Apply RETURNING (provider hook)
        let withReturning = this.EmitReturning(ir.Returning, withOutput)

        { Sql = withReturning; Parameters = collector.Parameters }

    /// Emits an UPDATE query.
    member this.EmitUpdateCore(ir: UpdateQueryIR) : CompiledQuery =
        let collector = this.CreateCollector()
        let sb = StringBuilder()

        let quotedTable = this.QuoteDotted(ir.Table)
        sb.Append($"UPDATE {quotedTable} SET ") |> ignore

        let setClauses =
            ir.SetColumns
            |> List.map (fun (col, value) ->
                let paramName = collector.Add(value)
                $"{this.QuoteIdentifier(col)} = {paramName}"
            )

        let rawSetClauses =
            ir.SetRaws
            |> List.map (fun (col, fragment, parms) ->
                let rendered = this.SubstituteParams(this.QuoteRawFragment(fragment), parms, collector)
                $"{this.QuoteIdentifier(col)} = {rendered}"
            )

        sb.Append(setClauses @ rawSetClauses |> String.concat ", ") |> ignore

        // WHERE
        let whereSql = this.EmitWhere(ir.Where, collector)
        if whereSql <> "" then
            sb.Append($" WHERE {whereSql}") |> ignore

        let baseSql = sb.ToString()

        // Apply output clause
        let withOutput =
            if ir.OutputFields.Length > 0 then
                this.EmitUpdateOutput(ir.OutputFields, baseSql)
            else
                baseSql

        // Apply RETURNING
        let withReturning = this.EmitReturning(ir.Returning, withOutput)

        { Sql = withReturning; Parameters = collector.Parameters }

    /// Emits a DELETE query.
    member this.EmitDeleteCore(ir: DeleteQueryIR) : CompiledQuery =
        let collector = this.CreateCollector()
        let sb = StringBuilder()

        let quotedTable = this.QuoteDotted(ir.Table)
        sb.Append($"DELETE FROM {quotedTable}") |> ignore

        let whereSql = this.EmitWhere(ir.Where, collector)
        if whereSql <> "" then
            sb.Append($" WHERE {whereSql}") |> ignore

        let baseSql = sb.ToString()
        let withReturning = this.EmitReturning(ir.Returning, baseSql)

        { Sql = withReturning; Parameters = collector.Parameters }

    // Default implementations for abstract members

    /// Default multi-row INSERT: comma-separated VALUES tuples.
    default this.EmitMultiRowInsert(table, columns, rows, collector) =
        let quotedTable = this.QuoteDotted(table)
        let quotedCols = columns |> List.map this.QuoteIdentifier |> String.concat ", "
        let rowsSql =
            rows
            |> List.map (fun row ->
                let paramNames = row |> Array.map (fun v -> collector.Add(v)) |> String.concat ", "
                $"({paramNames})"
            )
            |> String.concat ", "
        $"INSERT INTO {quotedTable} ({quotedCols}) VALUES {rowsSql}"

    /// Default: no identity returning.
    default _.EmitInsertIdentity(_) = ""

    /// Default: no conflict handling.
    default _.EmitInsertConflict(_, _, insertSql, _, _, _) = insertSql

    /// Append a RETURNING clause to a SQL command. Default: no-op (overridden by Postgres + Sqlite).
    abstract EmitReturning: returning: string list * sql: string -> string
    default _.EmitReturning(_, sql) = sql

    /// Shared implementation for `EmitReturning` on dialects that support standard RETURNING (Postgres, Sqlite).
    /// Inserts the clause before any trailing `;` produced by the identity-returning suffix.
    member this.AppendReturning(returning: string list, sql: string) =
        if returning.IsEmpty then sql
        else
            let cols = returning |> List.map this.QuoteIdentifier |> String.concat ", "
            let trimmed = sql.TrimEnd()
            if trimmed.EndsWith(";") then
                let body = trimmed.Substring(0, trimmed.Length - 1)
                $"{body} RETURNING {cols};"
            else
                $"{sql} RETURNING {cols}"

    /// Default: no output clause.
    default _.EmitInsertOutput(_, insertSql) = insertSql

    /// Default: no output clause.
    default _.EmitUpdateOutput(_, updateSql) = updateSql

    /// Default LIMIT/OFFSET pagination (works for most databases).
    default this.EmitPagination(skip, take, sb, collector) =
        match take with
        | Some t ->
            let paramName = collector.Add(box t)
            sb.Append($" LIMIT {paramName}") |> ignore
        | None -> ()
        match skip with
        | Some s ->
            let paramName = collector.Add(box s)
            sb.Append($" OFFSET {paramName}") |> ignore
        | None -> ()

    /// Default LIKE: case-insensitive via LOWER() on both sides
    default _.EmitLike(quotedCol, paramName) = $"LOWER({quotedCol}) like LOWER({paramName})"
    /// Default NOT LIKE: NOT (col like @p)
    default _.EmitNotLike(quotedCol, paramName) = $"NOT (LOWER({quotedCol}) like LOWER({paramName}))"

    /// Emit a boolean column comparison. Override for provider-specific behavior (e.g., SQL Server cast(x as bit)).
    abstract EmitBoolColumn: quotedCol: string * value: bool * collector: ParameterCollector -> string
    default this.EmitBoolColumn(quotedCol, value, collector) =
        let paramName = collector.Add(box value)
        $"{quotedCol} = {paramName}"

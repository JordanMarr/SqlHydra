namespace SqlHydra.Query

open System
open System.Text
open SqlHydra.Domain

// ─── SQL Server Emitter ───

type SqlServerEmitter() =
    inherit SqlEmitterBase()

    override _.QuoteIdentifier(name) = $"[{name}]"
    override _.ParameterPrefix = "@p"

    /// SQL Server pagination: uses OFFSET/FETCH when ORDER BY is present, TOP when not.
    /// Note: this is called after ORDER BY is already appended to sb.
    override this.EmitPagination(skip, take, sb, collector) =
        let hasOrderBy = sb.ToString().Contains("ORDER BY")
        match skip, take with
        | Some s, Some t when hasOrderBy ->
            let skipParam = collector.Add(box s)
            let takeParam = collector.Add(box t)
            sb.Append($" OFFSET {skipParam} ROWS FETCH NEXT {takeParam} ROWS ONLY") |> ignore
        | None, Some t when hasOrderBy ->
            let skipParam = collector.Add(box 0)
            let takeParam = collector.Add(box t)
            sb.Append($" OFFSET {skipParam} ROWS FETCH NEXT {takeParam} ROWS ONLY") |> ignore
        | Some s, None when hasOrderBy ->
            let skipParam = collector.Add(box s)
            sb.Append($" OFFSET {skipParam} ROWS") |> ignore
        | None, Some t ->
            // No ORDER BY: use TOP N (insert after SELECT keyword)
            let sql = sb.ToString()
            let selectIdx = sql.IndexOf("SELECT ", StringComparison.OrdinalIgnoreCase)
            if selectIdx >= 0 then
                let insertAt = selectIdx + "SELECT ".Length
                // Check for DISTINCT
                let afterSelect = sql.Substring(insertAt)
                let actualInsert =
                    if afterSelect.StartsWith("DISTINCT ", StringComparison.OrdinalIgnoreCase)
                    then insertAt + "DISTINCT ".Length
                    else insertAt
                let topClause = $"TOP ({t}) "
                sb.Clear() |> ignore
                sb.Append(sql.Insert(actualInsert, topClause)) |> ignore
        | _ -> ()

    // SQL Server emits boolean values as cast(x as bit) inline
    override _.EmitBoolColumn(quotedCol, value, _collector) =
        let bitVal = if value then "1" else "0"
        $"{quotedCol} = cast({bitVal} as bit)"

    override _.EmitInsertIdentity(_field) =
        ";SELECT scope_identity() as Id"

    override _.EmitInsertOutput(outputFields, insertSql) =
        let outputCsv =
            outputFields
            |> List.map (fun f -> $"INSERTED.{f.ColumnName}")
            |> String.concat ", "
        let outputClause = $"\nOUTPUT {outputCsv}\n"
        let valuesIndex = insertSql.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase)
        if valuesIndex > -1 then
            insertSql.Insert(valuesIndex, outputClause)
        else
            insertSql + outputClause

    override _.EmitUpdateOutput(outputFields, updateSql) =
        let outputCsv =
            outputFields
            |> List.map (fun f -> $"INSERTED.{f.ColumnName}")
            |> String.concat ", "
        let outputClause = $"\nOUTPUT {outputCsv}\n"
        let whereIndex = updateSql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase)
        if whereIndex > -1 then
            updateSql.Insert(whereIndex, outputClause)
        else
            updateSql + outputClause

    interface ISqlEmitter with
        member _.Provider = SqlServer
        member this.EmitSelect(ir) = this.EmitSelectCore(ir)
        member this.EmitInsert(ir) = this.EmitInsertCore(ir)
        member this.EmitUpdate(ir) = this.EmitUpdateCore(ir)
        member this.EmitDelete(ir) = this.EmitDeleteCore(ir)

// ─── PostgreSQL Emitter ───

type PostgresEmitter() =
    inherit SqlEmitterBase()

    override _.QuoteIdentifier(name) = $"\"{name}\""
    override _.ParameterPrefix = "@p"

    override _.EmitInsertIdentity(field) =
        $" RETURNING \"{field}\";"

    // Postgres uses case-insensitive ilike
    override _.EmitLike(quotedCol, paramName) = $"{quotedCol} ilike {paramName}"
    override _.EmitNotLike(quotedCol, paramName) = $"NOT ({quotedCol} ilike {paramName})"

    override this.EmitReturning(returning, sql) = this.AppendReturning(returning, sql)

    override this.EmitInsertConflict(insertType, table, insertSql, columns, _rows, collector) =
        match insertType with
        | OnConflictDoUpdate (conflictFields, updateFields) ->
            this.BuildOnConflictUpdate(insertSql, conflictFields, updateFields)

        | OnConflictDoUpdateCoalesce (conflictFields, updateFields, coalesceFields) ->
            this.BuildOnConflictUpdateCoalesce(insertSql, table, conflictFields, updateFields, coalesceFields)

        | OnConflictDoNothing conflictFields ->
            this.BuildOnConflictDoNothing(insertSql, conflictFields)

        | OnConflictDoNothingWhereRaw (conflictFields, whereFragment, parms) ->
            let insertQuery, identityQuery = this.SplitInsertAndIdentity(insertSql)
            let conflictCsv = String.Join(",", conflictFields)
            let rendered = this.SubstituteParams(whereFragment, parms, collector)
            StringBuilder()
                .AppendLine(insertQuery)
                .AppendLine($"ON CONFLICT({conflictCsv}) WHERE {rendered}")
                .AppendLine("DO NOTHING;")
                .AppendLine(identityQuery)
                .ToString()

        | OnConflictDoNothingRawTarget rawTarget ->
            let insertQuery, identityQuery = this.SplitInsertAndIdentity(insertSql)
            StringBuilder()
                .AppendLine(insertQuery)
                .AppendLine($"ON CONFLICT({rawTarget})")
                .AppendLine("DO NOTHING;")
                .AppendLine(identityQuery)
                .ToString()

        | _ -> insertSql

    interface ISqlEmitter with
        member _.Provider = Npgsql
        member this.EmitSelect(ir) = this.EmitSelectCore(ir)
        member this.EmitInsert(ir) = this.EmitInsertCore(ir)
        member this.EmitUpdate(ir) = this.EmitUpdateCore(ir)
        member this.EmitDelete(ir) = this.EmitDeleteCore(ir)

// ─── SQLite Emitter ───

type SqliteEmitter() =
    inherit SqlEmitterBase()

    override _.QuoteIdentifier(name) = $"\"{name}\""
    override _.ParameterPrefix = "@p"

    override _.EmitInsertIdentity(_field) =
        ";select last_insert_rowid() as id"

    override this.EmitReturning(returning, sql) = this.AppendReturning(returning, sql)

    override this.EmitInsertConflict(insertType, table, insertSql, _columns, _rows, _collector) =
        match insertType with
        | InsertOrReplace ->
            insertSql.Replace("INSERT", "INSERT OR REPLACE")

        | OnConflictDoUpdate (conflictFields, updateFields) ->
            this.BuildOnConflictUpdate(insertSql, conflictFields, updateFields)

        | OnConflictDoUpdateCoalesce (conflictFields, updateFields, coalesceFields) ->
            this.BuildOnConflictUpdateCoalesce(insertSql, table, conflictFields, updateFields, coalesceFields)

        | OnConflictDoNothing conflictFields ->
            this.BuildOnConflictDoNothing(insertSql, conflictFields)

        | _ -> insertSql

    interface ISqlEmitter with
        member _.Provider = Sqlite
        member this.EmitSelect(ir) = this.EmitSelectCore(ir)
        member this.EmitInsert(ir) = this.EmitInsertCore(ir)
        member this.EmitUpdate(ir) = this.EmitUpdateCore(ir)
        member this.EmitDelete(ir) = this.EmitDeleteCore(ir)

// ─── Oracle Emitter ───

type OracleEmitter() =
    inherit SqlEmitterBase()

    override _.QuoteIdentifier(name) = $"\"{name}\""
    override _.ParameterPrefix = ":p"

    // Oracle doesn't use AS for table aliases
    override this.QuoteTableSpec(spec: string) =
        let parts = spec.Split([| " as "; " AS "; " As " |], StringSplitOptions.RemoveEmptyEntries)
        match parts with
        | [| table; alias |] ->
            $"{this.QuoteDotted(table.Trim())} {this.QuoteIdentifier(alias.Trim())}"
        | [| table |] ->
            this.QuoteDotted(table.Trim())
        | _ -> spec

    override this.EmitPagination(skip, take, sb, collector) =
        match skip with
        | Some s ->
            let paramName = collector.Add(box s)
            sb.Append($" OFFSET {paramName} ROWS") |> ignore
        | None -> ()
        match take with
        | Some t ->
            let paramName = collector.Add(box t)
            sb.Append($" FETCH FIRST {paramName} ROWS ONLY") |> ignore
        | None -> ()

    override _.EmitInsertIdentity(field) =
        $" returning \"{field}\" into :outputParam"

    override this.EmitMultiRowInsert(table, columns, rows, collector) =
        // Oracle INSERT ALL syntax
        let quotedTable = this.QuoteDotted(table)
        let quotedCols = columns |> List.map this.QuoteIdentifier |> String.concat ", "
        let sb = StringBuilder()
        sb.AppendLine("INSERT ALL") |> ignore
        for row in rows do
            let paramNames = row |> Array.map (fun v -> collector.Add(v)) |> String.concat ", "
            sb.AppendLine($"INTO {quotedTable} ({quotedCols}) VALUES ({paramNames})") |> ignore
        sb.AppendLine("SELECT * FROM DUAL") |> ignore
        sb.ToString()

    interface ISqlEmitter with
        member _.Provider = Oracle
        member this.EmitSelect(ir) = this.EmitSelectCore(ir)
        member this.EmitInsert(ir) = this.EmitInsertCore(ir)
        member this.EmitUpdate(ir) = this.EmitUpdateCore(ir)
        member this.EmitDelete(ir) = this.EmitDeleteCore(ir)

// ─── MySQL Emitter ───

type MySqlEmitter() =
    inherit SqlEmitterBase()

    override _.QuoteIdentifier(name) = $"`{name}`"
    override _.ParameterPrefix = "@p"

    interface ISqlEmitter with
        member _.Provider = MySql
        member this.EmitSelect(ir) = this.EmitSelectCore(ir)
        member this.EmitInsert(ir) = this.EmitInsertCore(ir)
        member this.EmitUpdate(ir) = this.EmitUpdateCore(ir)
        member this.EmitDelete(ir) = this.EmitDeleteCore(ir)

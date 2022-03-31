/// Implementations for OnConflictDoUpdate, OnConflictDoNothing and InsertOrReplace.
module internal SqlHydra.Query.OnConflict

open SqlHydra.Query

let insertOrReplace (ctx: QueryContext) (iq: InsertQuery<'T, 'ReturnValue>) =
    async {
        let query = iq.ToKataQuery()
        let compiledQuery = ctx.Compiler.Compile query
        use cmd = ctx.BuildCommand compiledQuery
        cmd.CommandText <- compiledQuery.Sql.Replace("INSERT", "INSERT OR REPLACE")
        let! returnValue = cmd.ExecuteNonQueryAsync() |> Async.AwaitTask
        return System.Convert.ChangeType(returnValue, typeof<'InsertReturn>) :?> 'InsertReturn
    }
    |> Async.StartImmediateAsTask

let onConflictDoUpdate (ctx: QueryContext) (conflictColumns: string list) (updateColumns: string list) (iq: InsertQuery<'T, 'ReturnValue>) =
    async {
        let query = iq.ToKataQuery()
        let compiledQuery = ctx.Compiler.Compile(query)
        use cmd = ctx.BuildCommand compiledQuery

        // Get insert clase from the SqlKata query
        let insertClause = 
            query.Clauses 
            |> Seq.choose (function | :? SqlKata.InsertClause as ic -> Some ic | _ -> None)
            |> Seq.head

        // Create a lookup of SqlKata insert column indexes by column name
        let getColumnIdxByName = 
            insertClause.Columns
            |> Seq.mapi (fun idx colNm -> colNm, idx)
            |> Map.ofSeq            

        // Build upsert clause
        let setLinesStatement = 
            updateColumns
            |> List.map (fun colNm -> $"{colNm}=@p%i{getColumnIdxByName.[colNm]}\n")
            |> (fun lines -> System.String.Join(",", lines))
            
        let conflictColumnsCsv = System.String.Join(",", conflictColumns)
        let upsertQuery = 
            System.Text.StringBuilder(compiledQuery.Sql)
                .AppendLine($"ON CONFLICT({conflictColumnsCsv}) DO UPDATE SET")
                .AppendLine(setLinesStatement)
                .ToString()

        cmd.CommandText <- upsertQuery

        let! returnValue = cmd.ExecuteNonQueryAsync() |> Async.AwaitTask
        return System.Convert.ChangeType(returnValue, typeof<'InsertReturn>) :?> 'InsertReturn
    }
    |> Async.StartImmediateAsTask

let onConflictDoNothing (ctx: QueryContext) (conflictColumns: string list) (iq: InsertQuery<'T, 'ReturnValue>) =
    async {
        let query = iq.ToKataQuery()
        let compiledQuery = ctx.Compiler.Compile(query)
        use cmd = ctx.BuildCommand compiledQuery

        // Build upsert clause            
        let conflictColumnsCsv = System.String.Join(",", conflictColumns)
        let upsertQuery = 
            System.Text.StringBuilder(compiledQuery.Sql)
                .AppendLine($"ON CONFLICT({conflictColumnsCsv})")
                .AppendLine("DO NOTHING")
                .ToString()

        cmd.CommandText <- upsertQuery
        let! returnValue = cmd.ExecuteNonQueryAsync() |> Async.AwaitTask
        return System.Convert.ChangeType(returnValue, typeof<'InsertReturn>) :?> 'InsertReturn
    }
    |> Async.StartImmediateAsTask


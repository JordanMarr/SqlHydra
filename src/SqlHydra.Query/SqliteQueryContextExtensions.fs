module SqlHydra.Query.SqliteExtensions.SqliteQueryContextExtensions

open SqlHydra.Query

type QueryContext with
    member this.InsertOrReplace (iq: InsertQuery<'T, 'ReturnValue>) =
        async {
            let query = iq.ToKataQuery()
            let compiledQuery = this.Compiler.Compile query
            use cmd = this.BuildCommand compiledQuery
            cmd.CommandText <- compiledQuery.Sql.Replace("INSERT", "INSERT OR REPLACE")
            let! returnValue = cmd.ExecuteNonQueryAsync() |> Async.AwaitTask
            return System.Convert.ChangeType(returnValue, typeof<'InsertReturn>) :?> 'InsertReturn
        }
        |> Async.StartImmediateAsTask
        
    /// Transforms a regular INSERT query into an UPSERT by appending "ON CONFLICT DO UPDATE".
    /// NOTE: This can only be called on one record at a time.
    member this.OnConflictDoUpdate (onConflictColumns: string list) (columnsToUpdate: string list) (iq: InsertQuery<'T, 'ReturnValue>) =
        async {
            let query = iq.ToKataQuery()
            let compiledQuery = this.Compiler.Compile(query)
            use cmd = this.BuildCommand compiledQuery

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
                columnsToUpdate
                |> List.map (fun colNm -> $"{colNm}=@p%i{getColumnIdxByName.[colNm]}\n")
                |> (fun lines -> System.String.Join(",", lines))
            
            let conflictColumnsCsv = System.String.Join(",", onConflictColumns)
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

    /// Transforms a regular INSERT query into an INSERT or IGNORE by appending "ON CONFLICT DO NOTHING".
    /// NOTE: This can only be called on one record at a time.
    member this.OnConflictDoNothing (onConflictColumns: string list) (iq: InsertQuery<'T, 'ReturnValue>) =
        async {
            let query = iq.ToKataQuery()
            let compiledQuery = this.Compiler.Compile(query)
            use cmd = this.BuildCommand compiledQuery

            // Build upsert clause            
            let conflictColumnsCsv = System.String.Join(",", onConflictColumns)
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
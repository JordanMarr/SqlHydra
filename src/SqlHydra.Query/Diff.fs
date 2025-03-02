namespace SqlHydra.Query

/// Diffs two sequences of records of type 'T, using a primary key to match records.
type Diff<'T when 'T : equality>() =
    static member Compare(incoming, existing, getPK, ?hasChanges) : DiffResult<'T> = 
        let incomingMap = incoming |> Seq.map (fun p -> getPK p, p) |> Map.ofSeq
        let existingMap = existing |> Seq.map (fun p -> getPK p, p) |> Map.ofSeq
        let hasChanges = defaultArg hasChanges (fun incoming existing -> incoming <> existing)

        let added = incomingMap |> Map.filter (fun k _ -> not (existingMap.ContainsKey k)) |> Map.toSeq |> Seq.map snd
        let removed = existingMap |> Map.filter (fun k _ -> not (incomingMap.ContainsKey k)) |> Map.toSeq |> Seq.map snd
        let updated = 
            incomingMap 
            |> Map.filter (fun pk row ->                 
                match existingMap.TryFind pk with
                | Some existing -> hasChanges existing row
                | None -> false
            )            
            |> Map.toSeq 
            |> Seq.map snd

        DiffResult(added, updated, removed)

/// The result of diffing two sequences of records of type 'T with a given PK.
and DiffResult<'T>(added: 'T seq, changed: 'T seq, removed: 'T seq) =
    let mutable totalInserted = 0
    let mutable totalUpdated = 0
    let mutable totalDeleted = 0

    let mutable insertEachRow = None
    let mutable insertAllRows = None
    let mutable updateEachRow = None
    let mutable deleteEachRow = None

    member this.Added = added |> Seq.toList
    member this.Changed = changed |> Seq.toList
    member this.Removed = removed |> Seq.toList

    /// Allows the caller to insert each Added entity into the database.
    member this.Add<'TRow, 'Identity when 'Identity : struct>(insertQuery: 'T -> InsertQuery<'TRow, 'Identity>) = 
        let doInsertFn(ctx: QueryContext) =
            task {
                for record in this.Added do
                    let insertQuery = insertQuery record
                    let! _ = ctx.InsertAsync(insertQuery)
                    totalInserted <- totalInserted + 1
            }
        insertEachRow <- Some doInsertFn
        this

    /// Allows the caller to insert all Added entities into the database.
    member this.AddAll<'TRow, 'InsertReturn>(insertQuery: 'T seq -> InsertQuery<'TRow, 'InsertReturn>) = 
        let doInsertFn(ctx: QueryContext) =
            task {
                if not (Seq.isEmpty this.Added) then
                    let insertQuery = insertQuery this.Added
                    let! _ = ctx.InsertAsync(insertQuery)
                    totalInserted <- totalInserted + 1
                else 
                    ()
            }
        insertAllRows <- 
            if this.Added |> Seq.length >= 1 
            then Some doInsertFn 
            else None
        this

    /// Allows the caller to update each Changed entity in the database.
    member this.Change<'TRow, 'UpdateReturn>(updateQuery: 'T -> UpdateQuery<'TRow, 'UpdateReturn>) =
        let doUpdateFn(ctx: QueryContext) =
            task {
                for row in this.Changed do
                    let! result = ctx.UpdateAsync(updateQuery row)
                    if result.GetType() = typeof<int> then
                        totalUpdated <- totalUpdated + (result |> unbox<int>)
                    else
                        totalUpdated <- totalUpdated + 1
            }
        updateEachRow <- Some doUpdateFn
        this

    /// Allows the caller to delete each Removed entity from the database.
    member this.Remove<'TRow>(deleteQuery: 'T -> DeleteQuery<'TRow>) =
        let doDeleteFn(ctx: QueryContext) =
            task {
                for row in this.Removed do
                    let! rowsDeleted = ctx.DeleteAsync(deleteQuery row)
                    totalDeleted <- totalDeleted + rowsDeleted
            }
        deleteEachRow <- Some doDeleteFn
        this

    /// Saves the diffed records to the database. useTransaction defaults to true.
    member this.SaveTask(ctx: QueryContext, ?createTransaction) =
        task {
            let createTransaction = defaultArg createTransaction true

            try 
                if createTransaction then
                    ctx.BeginTransaction()

                if deleteEachRow.IsSome then
                    do! deleteEachRow.Value ctx

                if updateEachRow.IsSome then
                    do! updateEachRow.Value ctx

                if insertEachRow.IsSome then
                    do! insertEachRow.Value ctx

                if insertAllRows.IsSome then
                    do! insertAllRows.Value ctx

                if createTransaction then
                    ctx.CommitTransaction()
            with ex -> 
                if createTransaction then
                    ctx.RollbackTransaction()
                raise ex

            return { Inserted = totalInserted; Updated = totalUpdated; Deleted = totalDeleted }
        }

    /// Saves the diffed records to the database. useTransaction defaults to true.
    member this.SaveAsync(ctx: QueryContext, ?createTransaction) =
        this.SaveTask(ctx, ?createTransaction = createTransaction) 
        |> Async.AwaitTask

and SaveResult = { Inserted: int; Updated: int; Deleted: int }

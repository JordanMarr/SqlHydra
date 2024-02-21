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

    let mutable insertRow = None
    let mutable insertRows = None
    let mutable updateRow = None
    let mutable deleteRow = None

    member this.Added = added
    member this.Changed = changed
    member this.Removed = removed

    /// Allows the caller to insert each Added entity into the database.
    member this.Add<'TRow, 'Identity when 'Identity : struct>(insertQuery: 'T -> InsertQuery<'TRow, 'Identity>) = 
        let doInsertFn(ctx: QueryContext) =
            task {
                for record in this.Added do
                    let insertQuery = insertQuery record
                    let! _ = ctx.InsertAsync(insertQuery)
                    totalInserted <- totalInserted + 1
            }
        insertRow <- Some doInsertFn
        this

    /// Allows the caller to insert all Added entities into the database.
    member this.AddAll<'TRow, 'Identity when 'Identity : struct>(insertQuery: 'T seq -> InsertQuery<'TRow, 'Identity>) = 
        let doInsertFn(ctx: QueryContext) =
            task {
                if not (Seq.isEmpty this.Added) then
                    let insertQuery = insertQuery this.Added
                    let! _ = ctx.InsertAsync(insertQuery)
                    totalInserted <- totalInserted + 1
                else 
                    ()
            }
        insertRows <- 
            if this.Added |> Seq.length > 1 
            then Some doInsertFn 
            else None
        this

    /// Allows the caller to update each Changed entity in the database.
    member this.Change<'TRow>(updateQuery: 'T -> UpdateQuery<'TRow>) =
        let doUpdateFn(ctx: QueryContext) =
            task {
                for row in this.Changed do
                    let! rowsUpdated = ctx.UpdateAsync(updateQuery row)
                    totalUpdated <- totalUpdated + rowsUpdated
            }
        updateRow <- Some doUpdateFn
        this

    /// Allows the caller to delete each Removed entity from the database.
    member this.Remove<'TRow>(deleteQuery: 'T -> DeleteQuery<'TRow>) =
        let doDeleteFn(ctx: QueryContext) =
            task {
                for row in this.Removed do
                    let! rowsDeleted = ctx.DeleteAsync(deleteQuery row)
                    totalDeleted <- totalDeleted + rowsDeleted
            }
        deleteRow <- Some doDeleteFn
        this

    /// Saves the diffed records to the database. useTransaction defaults to true.
    member this.SaveTask(ctx: QueryContext, ?createTransaction) =
        task {
            let createTransaction = defaultArg createTransaction true

            try 
                if createTransaction then
                    ctx.BeginTransaction()

                if deleteRow.IsSome then
                    do! deleteRow.Value ctx

                if updateRow.IsSome then
                    do! updateRow.Value ctx

                if insertRow.IsSome then
                    do! insertRow.Value ctx

                if insertRows.IsSome then
                    do! insertRows.Value ctx

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

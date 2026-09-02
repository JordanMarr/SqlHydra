namespace SqlHydra.Query

open System
open System.Data.Common
open System.Threading
open SqlHydra.Domain

/// Indicates which execution strategy to use after command preparation.
[<NoComparison>]
type private InsertExecMode =
    | ExecNonQuery
    | ExecScalar
    | ExecOracleIdentity of DbParameter
    | ExecOutputClause of OutputField list
    /// PostgreSQL/SQLite RETURNING clause — read columns from the result reader.
    | ExecReturning of returningColumns: string list

/// Contains methods that compile and read a query.
type QueryContext(conn: DbConnection, emitter: ISqlEmitter) =

    let provider = emitter.Provider

    let setProviderDbType (param: DbParameter) (propertyName: string) (providerDbType: string) =
        let property = param.GetType().GetProperty(propertyName)
        let dbTypeSetter = property.GetSetMethod()
        let value = Enum.Parse(property.PropertyType, providerDbType)
        dbTypeSetter.Invoke(param, [|value|]) |> ignore

    let setParameterDbType (param: DbParameter) (qp: QueryParameter) =
        match provider, qp.ProviderDbType with
        | Npgsql, Some type' ->
            setProviderDbType param "NpgsqlDbType" type'
        | SqlServer, Some "SqlHierarchyId" ->
            param.GetType().GetProperty("UdtTypeName").SetValue(param, "hierarchyid") |> ignore
        | SqlServer, Some type' ->
            setProviderDbType param "SqlDbType" type'
        | _ -> ()

    /// Creates a DbParameter from a name and value, handling QueryParameter type and DateOnly/TimeOnly conversion.
    let createParam (cmd: DbCommand) (name: string) (value: obj) =
        let p = cmd.CreateParameter()
        p.ParameterName <- name
        p.Value <-
            match value with
            | :? QueryParameter as qp ->
                do setParameterDbType p qp
                qp.Value
            | _ -> value
            |> QueryUtils.convertIfDateOnlyTimeOnly
        p :> Data.IDbDataParameter


    let mutable logger = fun (cq: CompiledQuery) -> ()

    member this.Dispose() =
        conn.Dispose()
        this.Transaction |> Option.iter (fun t -> t.Dispose())
        this.Transaction <- None

#if NETSTANDARD2_1_OR_GREATER
    member this.DisposeAsync() =
        task {
            do! conn.DisposeAsync()
            match this.Transaction with
            | Some t -> do! t.DisposeAsync()
            | None -> ()
            this.Transaction <- None
        } |> ValueTask
#endif

    interface IDisposable with
        member this.Dispose() = this.Dispose()

#if NETSTANDARD2_1_OR_GREATER
    interface IAsyncDisposable with
        member this.DisposeAsync() = this.DisposeAsync()
#endif

    member this.Connection = conn
    member this.Emitter = emitter
    member this.Provider = provider

    /// Logs a compiled query with a user provided log function.
    /// Ex: queryContext.Logger <- printfn "SQL: %O"
    member this.Logger
        with get () = logger
        and set fn = logger <- fn

    member val Transaction : DbTransaction option = None with get,set

    member this.BeginTransaction(?isolationLevel: Data.IsolationLevel) =
        this.Transaction <-
            match isolationLevel with
            | Some il -> conn.BeginTransaction(il) |> Some
            | None -> conn.BeginTransaction() |> Some

#if NETSTANDARD2_1_OR_GREATER
    // Return ValueTask to mirror DbConnection.BeginTransactionAsync, so that if F# ever gets a ValueTask CE we can use it here
    member this.BeginTransactionAsync(?isolationLevel: Data.IsolationLevel, ?cancellationToken: CancellationToken) = ValueTask <| task {
        let! trans =
            match isolationLevel with
            | Some il -> conn.BeginTransactionAsync(il, ?cancellationToken = cancellationToken)
            | None -> conn.BeginTransactionAsync(?cancellationToken = cancellationToken)
        this.Transaction <- Some trans
    }
#endif

    member this.CommitTransaction() =
        match this.Transaction with
        | Some t -> t.Commit(); this.Transaction <- None
        | None -> failwith "No transaction was started."

#if NETSTANDARD2_1_OR_GREATER
    member this.CommitTransactionAsync(?cancellationToken: CancellationToken) = task {
        match this.Transaction with
        | Some t ->
            do! t.CommitAsync(?cancellationToken = cancellationToken)
            this.Transaction <- None
        | None -> failwith "No transaction was started."
    }
#endif

    member this.RollbackTransaction() =
        match this.Transaction with
        | Some t -> t.Rollback(); this.Transaction <- None
        | None -> failwith "No transaction was started."

#if NETSTANDARD2_1_OR_GREATER
    member this.RollbackTransactionAsync(?cancellationToken: CancellationToken) = task {
        match this.Transaction with
        | Some t ->
            do! t.RollbackAsync(?cancellationToken = cancellationToken)
            this.Transaction <- None
        | None -> failwith "No transaction was started."
    }
#endif

    member private this.ApplyCommandOptions (options: CommandOptions) (cmd: DbCommand) =
        match options.CommandTimeout with
        | Some timeout -> cmd.CommandTimeout <- timeout.TotalSeconds |> Math.Ceiling |> int
        | None -> ()

    member private this.TrySetTransaction(cmd: DbCommand) =
        this.Transaction |> Option.iter (fun t -> cmd.Transaction <- t)

    /// Builds a DbCommand from a CompiledQuery.
    member this.BuildCommandFromCompiled(compiled: CompiledQuery, ?log: bool) =
        let log = defaultArg log true
        if log then this.Logger compiled
        let cmd = conn.CreateCommand()
        cmd |> this.ApplyCommandOptions compiled.CommandOptions
        cmd |> this.TrySetTransaction
        cmd.CommandText <- compiled.Sql
        for (name, value) in compiled.Parameters do
            cmd.Parameters.Add(createParam cmd name value) |> ignore
        cmd

    /// Builds an ADO.NET DbCommand from a SelectQueryIR.
    member this.BuildCommand(ir: SelectQueryIR) =
        let compiled = emitter.EmitSelect(ir)
        this.BuildCommandFromCompiled(compiled)

    /// Returns an ADO.NET data reader for a given query.
    member this.GetReader<'T, 'Reader & #DbDataReader> (query: SelectQuery<'T>) =
        let cmd = this.BuildCommand(query.IR) // do not dispose cmd
        cmd.ExecuteReader() :?> 'Reader

    /// Returns an ADO.NET data reader for a given query.
    member this.GetReaderAsync<'T, 'Reader & #DbDataReader> (query: SelectQuery<'T>) =
        this.GetReaderAsyncWithOptions<'T, 'Reader>(query)

    /// Returns an ADO.NET data reader for a given query.
    member this.GetReaderAsyncWithOptions<'T, 'Reader & #DbDataReader> (query: SelectQuery<'T>, ?cancel: CancellationToken) =
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let cmd = this.BuildCommand(query.IR) // do not dispose cmd
            let! reader = cmd.ExecuteReaderAsync(cancel |> Option.defaultValue CancellationToken.None)
            return reader :?> 'Reader
        }

    /// Executes a select query and returns results using the Hydration module.
    member this.Select<'Entity> (query: SelectQuery<'Entity>) =
        use cmd = this.BuildCommand(query.IR)
        use reader = cmd.ExecuteReader()
        let readEntity = Hydration.buildRowReader<'Entity> this.Provider reader
        seq [|
            while reader.Read() do
                readEntity()
        |]

    /// Executes a select query asynchronously and returns results using the Hydration module.
    member this.SelectAsync<'Entity> (query: SelectQuery<'Entity>) =
        this.SelectAsyncWithOptions (query)

    /// Executes a select query asynchronously with optional args and returns results using the Hydration module.
    member this.SelectAsyncWithOptions<'Entity>(query: SelectQuery<'Entity>, ?cancel: CancellationToken) =
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let cancel = defaultArg cancel CancellationToken.None
            use cmd = this.BuildCommand(query.IR)
            use! reader = cmd.ExecuteReaderAsync(cancel)
            let readEntity = Hydration.buildRowReader<'Entity> this.Provider reader
            let results = ResizeArray<'Entity>()

            let! hasMore = reader.ReadAsync(cancel)
            let mutable hasMore = hasMore
            while hasMore && not cancel.IsCancellationRequested do
                results.Add(readEntity ())
                let! hasMore' = reader.ReadAsync(cancel)
                hasMore <- hasMore'

            return results :> seq<'Entity>
        }

    /// Executes a select query and returns a single (option) result using the Hydration module.
    member this.SelectOne<'Entity> (query: SelectQuery<'Entity>) =
        this.Select(query) |> Seq.tryHead

    /// Executes a select query asynchronously and returns a single (option) result using the Hydration module.
    member this.SelectOneAsync<'Entity> (query: SelectQuery<'Entity>) =
        this.SelectOneAsyncWithOptions(query)

    /// Executes a select query asynchronously for a single (option) result with optional args using the Hydration module.
    member this.SelectOneAsyncWithOptions<'Entity>(query: SelectQuery<'Entity>, ?cancel: CancellationToken) =
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let! entities = this.SelectAsyncWithOptions (query, cancel |> Option.defaultValue CancellationToken.None)
            return entities |> Seq.tryHead
        }

    /// Reads RETURNING clause columns into a single value or tuple matching 'Return.
    /// Column types come from 'Return — when 'Return is a tuple, each element type is read in order;
    /// when it's a single type, one column is read. Option/Nullable are unwrapped.
    member private _.ReadReturningValues<'Return> (cmd: DbCommand) (cancel: CancellationToken) (returningCols: string list) =
        task {
            use! reader = cmd.ExecuteReaderAsync(cancel)
            let! hasRow = reader.ReadAsync(cancel)
            if not hasRow then
                // ON CONFLICT DO NOTHING with no actual conflict-update returns zero rows.
                // Caller signature already accepts the default value; semantics mirror the
                // "insert was a no-op" outcome.
                return Unchecked.defaultof<'Return>
            else

            let elementTypes =
                let t = typeof<'Return>
                if FSharp.Reflection.FSharpType.IsTuple(t)
                then FSharp.Reflection.FSharpType.GetTupleElements(t)
                else [| t |]

            let unwrap (t: System.Type) =
                if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Option<_>>
                then Some t.GenericTypeArguments.[0]
                elif t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<System.Nullable<_>>
                then Some t.GenericTypeArguments.[0]
                else None

            let values =
                List.zip returningCols (List.ofArray elementTypes)
                |> List.map (fun (colName, retType) ->
                    let ord = reader.GetOrdinal(colName)
                    match unwrap retType with
                    | Some inner ->
                        if reader.IsDBNull(ord) then
                            if retType.GetGenericTypeDefinition() = typedefof<Option<_>>
                            then None |> box
                            else System.Activator.CreateInstance(retType) // Nullable default ()
                        else
                            let raw = reader[ord]
                            // Construct Option<T>(value) or Nullable<T>(value).
                            System.Activator.CreateInstance(retType, [| System.Convert.ChangeType(raw, inner) |])
                    | None ->
                        let raw = reader[ord]
                        System.Convert.ChangeType(raw, retType))
                |> List.toArray

            match values with
            | [| v |] -> return v :?> 'Return
            | _ ->
                let tupleType = FSharp.Reflection.FSharpType.MakeTupleType(elementTypes)
                let tuple = FSharp.Reflection.FSharpValue.MakeTuple(values, tupleType)
                return tuple :?> 'Return
        }

    /// Reads output values from a DbCommand execution (for OUTPUT clause support).
    member private _.ReadOutputValues<'InsertReturn> (cmd: DbCommand) (cancel: CancellationToken) (outputFields: OutputField list) =
        task {
            use! reader = cmd.ExecuteReaderAsync(cancel)
            let! _ = reader.ReadAsync(cancel)

            let outputValues =
                outputFields
                |> List.map (fun f ->
                    let ord = reader.GetOrdinal(f.ColumnName)
                    match f.Nullability with
                    | NotNullable ->
                        reader[ord]
                    | IsOptional ->
                        if reader.IsDBNull(ord)
                        then None
                        else Activator.CreateInstance(f.PropertyType, [| reader[ord] |])
                    | IsNullable ->
                        if reader.IsDBNull(ord)
                        then Nullable() |> box
                        else Activator.CreateInstance(f.PropertyType, [| reader[ord] |])
                )
                |> List.toArray

            let outputTypes =
                outputFields
                |> List.map _.PropertyType
                |> List.toArray

            match outputValues with
            | [| outputValue |] ->
                return outputValue :?> 'InsertReturn
            | outputValues ->
                // Convert array to a tuple
                let outputTupleType = FSharp.Reflection.FSharpType.MakeTupleType(outputTypes)
                let outputTuple = FSharp.Reflection.FSharpValue.MakeTuple(outputValues, outputTupleType)
                return outputTuple :?> 'InsertReturn
        }

    /// Prepares a DbCommand for an insert query, applying all SQL modifications.
    /// Returns the prepared command and an InsertExecMode indicating how to execute it.
    member private this.PrepareInsertCommand<'T, 'InsertReturn> (iq: InsertQuery<'T, 'InsertReturn>) =
        QueryUtils.failIfIdentityOnConflict iq.Spec
        let insertIR = QueryUtils.fromInsert iq.Spec
        let compiled = emitter.EmitInsert(insertIR)
        let cmd = this.BuildCommandFromCompiled(compiled, log = false)

        let logCompiled () =
            this.Logger { Sql = cmd.CommandText; Parameters = []; CommandOptions = iq.Spec.CommandOptions }

        // Handle InsertOrUpdateOnUnique separately (SQL Server TRY/CATCH pattern)
        match iq.Spec.InsertType with
        | InsertOrUpdateOnUnique (keyFields, updateFields) ->
            let entity = iq.Spec.Entities |> List.head
            let propMap = FSharp.Reflection.FSharpType.GetRecordFields(entity.GetType()) |> Array.map (fun p -> p.Name, p) |> Map.ofArray
            let getColumnValue col = QueryUtils.getQueryParameterForEntity entity propMap[col]
            let existingParams = [ for i in 0 .. cmd.Parameters.Count - 1 -> cmd.Parameters[i] :> Data.IDbDataParameter ]
            let newSql, allParams =
                InsertOrUpdateOnUnique.apply iq.Spec.Table keyFields updateFields cmd.CommandText existingParams (createParam cmd) getColumnValue
            cmd.CommandText <- newSql
            cmd.Parameters.Clear()
            for p in allParams do cmd.Parameters.Add(p) |> ignore
            logCompiled ()
            cmd, ExecNonQuery
        | _ ->

        match iq.Spec with
        | { IdentityField = Some identityField } ->
            // Add provider-specific identity-returning SQL
            if provider = SqlServer then
                if typeof<'InsertReturn> = typeof<System.Guid> then
                    // GUID needs OUTPUT INSERTED clause instead of scope_identity()
                    // Insert OUTPUT clause before VALUES
                    let valuesIdx = cmd.CommandText.IndexOf(" VALUES ", StringComparison.OrdinalIgnoreCase)
                    if valuesIdx > -1 then
                        cmd.CommandText <- cmd.CommandText.Insert(valuesIdx, $" OUTPUT INSERTED.{identityField}")
                else
                    cmd.CommandText <- cmd.CommandText + ";SELECT scope_identity() as Id"
            elif provider = Npgsql then
                cmd.CommandText <- cmd.CommandText + $" RETURNING \"{identityField}\";"
            elif provider = Oracle then
                cmd.CommandText <- cmd.CommandText + $" returning \"{identityField}\" into :outputParam"
            elif provider = Sqlite then
                cmd.CommandText <- cmd.CommandText + ";select last_insert_rowid() as id"

            logCompiled ()

            // Setup Oracle identity output parameter
            if provider = Oracle then
                let outputParam = cmd.CreateParameter()
                outputParam.ParameterName <- "outputParam"
                outputParam.DbType <- Data.DbType.Decimal
                outputParam.Direction <- Data.ParameterDirection.Output
                cmd.Parameters.Add(outputParam) |> ignore
                cmd, ExecOracleIdentity outputParam
            else
                cmd, ExecScalar

        | { OutputFields = outputFields } when outputFields.Length > 0 ->
            logCompiled ()
            cmd, ExecOutputClause outputFields

        | { Returning = returning } when returning.Length > 0 ->
            this.Logger { Sql = cmd.CommandText; Parameters = []; CommandOptions = CommandOptions.Default }
            cmd, ExecReturning returning

        | _ ->
            logCompiled ()
            cmd, ExecNonQuery

    member this.Insert<'T, 'InsertReturn> (iq: InsertQuery<'T, 'InsertReturn>) =
        let cmd, execMode = this.PrepareInsertCommand(iq)
        use cmd = cmd
        match execMode with
        | ExecNonQuery ->
            let results = cmd.ExecuteNonQuery()
            Convert.ChangeType(results, typeof<'InsertReturn>) :?> 'InsertReturn
        | ExecScalar ->
            let identity = cmd.ExecuteScalar()
            Convert.ChangeType(identity, typeof<'InsertReturn>) :?> 'InsertReturn
        | ExecOracleIdentity outputParam ->
            let _ = cmd.ExecuteNonQuery()
            Convert.ChangeType(outputParam.Value, typeof<'InsertReturn>) :?> 'InsertReturn
        | ExecOutputClause outputFields ->
            this.ReadOutputValues<'InsertReturn> cmd CancellationToken.None outputFields
            |> Async.AwaitTask |> Async.RunSynchronously
        | ExecReturning cols ->
            this.ReadReturningValues<'InsertReturn> cmd CancellationToken.None cols
            |> Async.AwaitTask |> Async.RunSynchronously

    member this.InsertAsync<'T, 'InsertReturn> (query: InsertQuery<'T, 'InsertReturn>) =
        this.InsertAsyncWithOptions(query)

    member this.InsertAsyncWithOptions<'T, 'InsertReturn> (iq: InsertQuery<'T, 'InsertReturn>, ?cancel: CancellationToken) =
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let cmd, execMode = this.PrepareInsertCommand(iq)
            use cmd = cmd
            let cancel = defaultArg cancel CancellationToken.None
            match execMode with
            | ExecNonQuery ->
                let! results = cmd.ExecuteNonQueryAsync(cancel)
                return Convert.ChangeType(results, typeof<'InsertReturn>) :?> 'InsertReturn
            | ExecScalar ->
                let! identity = cmd.ExecuteScalarAsync(cancel)
                return Convert.ChangeType(identity, typeof<'InsertReturn>) :?> 'InsertReturn
            | ExecOracleIdentity outputParam ->
                let! _ = cmd.ExecuteNonQueryAsync(cancel)
                return Convert.ChangeType(outputParam.Value, typeof<'InsertReturn>) :?> 'InsertReturn
            | ExecOutputClause outputFields ->
                let! outputValues = this.ReadOutputValues<'InsertReturn> cmd cancel outputFields
                return outputValues
            | ExecReturning cols ->
                let! returnValues = this.ReadReturningValues<'InsertReturn> cmd cancel cols
                return returnValues
        }

    member this.Update (query: UpdateQuery<'T, 'UpdateReturn>) =
        let updateIR = QueryUtils.fromUpdate query.Spec
        let compiled = emitter.EmitUpdate(updateIR)
        use cmd = this.BuildCommandFromCompiled(compiled)
        cmd.ExecuteNonQuery()

    member this.UpdateAsync (query: UpdateQuery<'T, 'UpdateReturn>) =
        this.UpdateAsyncWithOptions(query)

    member this.UpdateAsyncWithOptions (query: UpdateQuery<'T, 'UpdateReturn>, ?cancel: CancellationToken) =
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let cancel = defaultArg cancel CancellationToken.None
            let updateIR = QueryUtils.fromUpdate query.Spec

            if query.Spec.OutputFields.Length > 0 then
                // Emit update with output clause
                let compiled = emitter.EmitUpdate(updateIR)
                use cmd = this.BuildCommandFromCompiled(compiled)
                let! outputValues = this.ReadOutputValues<'UpdateReturn> cmd cancel query.Spec.OutputFields
                return outputValues
            else
                let compiled = emitter.EmitUpdate(updateIR)
                use cmd = this.BuildCommandFromCompiled(compiled)
                let! rowsInserted = cmd.ExecuteNonQueryAsync(cancel)
                return Convert.ChangeType(rowsInserted, typeof<'UpdateReturn>) :?> 'UpdateReturn
        }

    member this.Delete (query: DeleteQuery<'T>) =
        let compiled = emitter.EmitDelete(query.IR)
        use cmd = this.BuildCommandFromCompiled(compiled)
        cmd.ExecuteNonQuery()

    member this.DeleteAsync (query: DeleteQuery<'T>) =
        this.DeleteAsyncWithOptions(query)

    member this.DeleteAsyncWithOptions (query: DeleteQuery<'T>, ?cancel: CancellationToken) =
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let compiled = emitter.EmitDelete(query.IR)
            use cmd = this.BuildCommandFromCompiled(compiled)
            return! cmd.ExecuteNonQueryAsync(cancel |> Option.defaultValue CancellationToken.None)
        }

    member this.Count (query: SelectQuery<int>) =
        use cmd = this.BuildCommand(query.IR)
        match cmd.ExecuteScalar() with
        | :? int64 as count -> Convert.ToInt32 count
        | _  as count -> count :?> int

    member this.CountAsync (query: SelectQuery<int>) =
        this.CountAsyncWithOptions(query)

    member this.CountAsyncWithOptions (query: SelectQuery<int>, ?cancel: CancellationToken) =
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            use cmd = this.BuildCommand(query.IR)
            match! cmd.ExecuteScalarAsync(cancel |> Option.defaultValue CancellationToken.None) with
            | :? int64 as count -> return Convert.ToInt32 count
            | count -> return count :?> int
        }

namespace SqlHydra.Query

open System
open System.Data
open System.Data.Common
open System.IO
open System.Threading
open SqlKata

/// Contains methods that compile and read a query.
type QueryContext(conn: DbConnection, compiler: SqlKata.Compilers.Compiler) =
    let setProviderDbType (param: DbParameter) (propertyName: string) (providerDbType: string) =
        let property = param.GetType().GetProperty(propertyName)
        let dbTypeSetter = property.GetSetMethod()
        let value = Enum.Parse(property.PropertyType, providerDbType)
        dbTypeSetter.Invoke(param, [|value|]) |> ignore
        
    let setParameterDbType (param: DbParameter) (qp: QueryParameter) =
        match qp.ProviderDbType, compiler with
        | Some type', :? SqlKata.Compilers.PostgresCompiler ->
            setProviderDbType param "NpgsqlDbType" type'
        | Some "SqlHierarchyId", :? SqlKata.Compilers.SqlServerCompiler ->
            //setProviderDbType param "SqlDbType" "Udt"
            param.GetType().GetProperty("UdtTypeName").SetValue(param, "hierarchyid") |> ignore
        | Some type', :? SqlKata.Compilers.SqlServerCompiler ->
            setProviderDbType param "SqlDbType" type'    
        | _ -> ()

    let mutable logger = fun (r: SqlResult) -> ()
        
    interface IDisposable with
        member this.Dispose() = 
            conn.Dispose()
            this.Transaction |> Option.iter (fun t -> t.Dispose())
            this.Transaction <- None
    
#if NETSTANDARD2_1_OR_GREATER
    interface IAsyncDisposable with
        member this.DisposeAsync() =
            task {
                do! conn.DisposeAsync()
                match this.Transaction with
                | Some t -> do! t.DisposeAsync()
                | None -> ()
                this.Transaction <- None
            } |> ValueTask
#endif
    
    member this.Connection = conn
    member this.Compiler = compiler

    /// Logs a SqlKata compiled query with a user provided log function.
    /// Ex: queryContext.Logger <- printfn "SQL: %O"
    member this.Logger
        with get () = logger
        // Wrap the SqlResult to override query logging
        and set fn = logger <- LoggedSqlResult >> unbox<SqlResult> >> fn

    /// Updates the SqlResult with the latest cmd.CommandText and then logs the query.
    member private this.LogCommand (sqlResult: SqlResult, cmd: DbCommand) = 
        sqlResult.Sql <- cmd.CommandText
        this.Logger sqlResult

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

    member private this.TrySetTransaction(cmd: DbCommand) =
        this.Transaction |> Option.iter (fun t -> cmd.Transaction <- t)

    /// Builds a DbCommand with CommandText and Parameters from a SqlKata compiled query.
    member this.BuildCommand(compiledQuery: SqlResult, ?log: bool) = 
        let log = defaultArg log true
        if log then this.Logger compiledQuery
        let cmd = conn.CreateCommand()
        cmd |> this.TrySetTransaction
        cmd.CommandText <- compiledQuery.Sql
        for kvp in compiledQuery.NamedBindings do
            let p = cmd.CreateParameter()
            
            p.ParameterName <- kvp.Key
            
            p.Value <-
                match kvp.Value with
                | :? QueryParameter as qp ->
                    do setParameterDbType p qp
                    qp.Value
                | _ ->
                    kvp.Value
                
                // SqlHydra must manually handle DateOnly and TimeOnly conversions of all parameters
                |> KataUtils.convertIfDateOnlyTimeOnly
           
            cmd.Parameters.Add(p) |> ignore
        cmd

    /// Builds an ADO.NET DbCommand from a SqlKata query.
    member this.BuildCommand(query: Query) =
        let compiledQuery = compiler.Compile(query)
        this.BuildCommand(compiledQuery)

    /// Returns an ADO.NET data reader for a given query.
    member this.GetReader<'T, 'Reader & #DbDataReader> (query: SelectQuery<'T>) = 
        let cmd = this.BuildCommand(query.ToKataQuery()) // do not dispose cmd
        cmd.ExecuteReader() :?> 'Reader

    /// Returns an ADO.NET data reader for a given query.
    member this.GetReaderAsync<'T, 'Reader & #DbDataReader> (query: SelectQuery<'T>) = 
        this.GetReaderAsyncWithOptions<'T, 'Reader>(query)

    /// Returns an ADO.NET data reader for a given query.
    member this.GetReaderAsyncWithOptions<'T, 'Reader & #DbDataReader> (query: SelectQuery<'T>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let cmd = this.BuildCommand(query.ToKataQuery()) // do not dispose cmd
            let! reader = cmd.ExecuteReaderAsync(cancel |> Option.defaultValue CancellationToken.None)
            return reader :?> 'Reader
        }

    /// Executes a select query with a given readEntity builder function.
    member this.Read<'Entity, 'Reader & #DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Entity)) (query: SelectQuery<'Entity>) =
        use cmd = this.BuildCommand(query.ToKataQuery())
        use reader = cmd.ExecuteReader() :?> 'Reader
        let readEntity = readEntityBuilder reader
        seq [| 
            while reader.Read() do
                readEntity() 
        |] 

    /// Executes a select query with a given readEntity builder function.
    member this.ReadAsync<'Entity, 'Reader & #DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Entity)) (query: SelectQuery<'Entity>) = 
        this.ReadAsyncWithOptions (query, readEntityBuilder)

    /// Executes a select query with a given readEntity builder function and optional args.
    member this.ReadAsyncWithOptions<'Entity, 'Reader & #DbDataReader> 
        (query: SelectQuery<'Entity>, readEntityBuilder: 'Reader -> (unit -> 'Entity), ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let cancel = defaultArg cancel CancellationToken.None
            use cmd = this.BuildCommand (query.ToKataQuery())
            use! reader = cmd.ExecuteReaderAsync(cancel)
            let readEntity = readEntityBuilder (reader :?> 'Reader)
            let results = ResizeArray<'Entity>()
            
            let! hasMore = reader.ReadAsync(cancel)
            let mutable hasMore = hasMore
            while hasMore && not cancel.IsCancellationRequested do
                results.Add(readEntity ())
                let! hasMore' = reader.ReadAsync(cancel)
                hasMore <- hasMore'
            
            return results :> seq<'Entity>
        }

    /// Executes a select query with a given readEntity builder function for a single (option) result.
    member this.ReadOne<'Entity, 'Reader & #DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Entity)) (query: SelectQuery<'Entity>) =
        this.Read readEntityBuilder query |> Seq.tryHead

    /// Executes a select query with a given readEntity builder function for a single (option) result.
    member this.ReadOneAsync<'Entity, 'Reader & #DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Entity)) (query: SelectQuery<'Entity>) = 
        this.ReadOneAsyncWithOptions(query, readEntityBuilder)

    /// Executes a select query with a given readEntity builder function for a single (option) result with optional args.
    member this.ReadOneAsyncWithOptions<'Entity, 'Reader & #DbDataReader>
        (query: SelectQuery<'Entity>, readEntityBuilder: 'Reader -> (unit -> 'Entity), ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let! entities = this.ReadAsyncWithOptions (query, readEntityBuilder, cancel |> Option.defaultValue CancellationToken.None)
            return entities |> Seq.tryHead
        }

    member this.Insert<'T, 'InsertReturn> (iq: InsertQuery<'T, 'InsertReturn>) = 
        let compiledQuery = iq.ToKataQuery() |> compiler.Compile
        use cmd = this.BuildCommand(compiledQuery, log = false) // We will log manually below to capture query changes

        // Applies on conflict modifier if in spec
        let applyOnConflict =
            match iq.Spec.InsertType with
            | InsertOrReplace -> OnConflict.insertOrReplace
            | OnConflictDoUpdate (conflictFields, updateFields) -> OnConflict.onConflictDoUpdate conflictFields updateFields
            | OnConflictDoNothing conflictFields -> OnConflict.onConflictDoNothing conflictFields
            | Insert -> id

        KataUtils.failIfIdentityOnConflict iq.Spec

        // Did the user select an identity field?
        match iq.Spec.IdentityField with
        | Some identityField -> 
            // Try apply on conflict
            cmd.CommandText <- cmd.CommandText |> applyOnConflict

            // Fix postgres identity
            if compiler :? SqlKata.Compilers.PostgresCompiler 
            then cmd.CommandText <- cmd.CommandText |> Fixes.Postgres.fixIdentityQuery identityField 

            // Fix oracle identity
            elif compiler  :? SqlKata.Compilers.OracleCompiler 
            then cmd.CommandText <- cmd.CommandText |> Fixes.Oracle.fixIdentityQuery identityField 
                        
            // Fix mssql guid identity
            elif compiler :? SqlKata.Compilers.SqlServerCompiler && typeof<'InsertReturn> = typeof<System.Guid>
            then cmd.CommandText <- cmd.CommandText |> Fixes.MsSql.fixGuidIdentityQuery identityField

            this.LogCommand(compiledQuery, cmd)

            // Execute insert and return identity
            if compiler :? SqlKata.Compilers.OracleCompiler then
                let outputParam = cmd.CreateParameter()
                outputParam.ParameterName <- "outputParam"
                outputParam.DbType <- Data.DbType.Decimal
                outputParam.Direction <- Data.ParameterDirection.Output
                cmd.Parameters.Add(outputParam) |> ignore
                let _ = cmd.ExecuteNonQuery()
                // 'InsertReturn type set via `getId` in the builder
                Convert.ChangeType(outputParam.Value, typeof<'InsertReturn>) :?> 'InsertReturn
            else
                let identity = cmd.ExecuteScalar()
                // 'InsertReturn type set via `getId` in the builder
                Convert.ChangeType(identity, typeof<'InsertReturn>) :?> 'InsertReturn
        
        | None ->
            // Try apply on conflict
            cmd.CommandText <- cmd.CommandText |> applyOnConflict

            // Fix Oracle multi-insert query
            if compiler :? SqlKata.Compilers.OracleCompiler && iq.Spec.Entities.Length > 1 
            then cmd.CommandText <- cmd.CommandText |> Fixes.Oracle.fixMultiInsertQuery 
                    
            this.LogCommand(compiledQuery, cmd)

            let results = cmd.ExecuteNonQuery()
            // 'InsertReturn is `int` here -- NOTE: must include `'InsertReturn : struct` constraint
            Convert.ChangeType(results, typeof<'InsertReturn>) :?> 'InsertReturn

    member this.InsertAsync<'T, 'InsertReturn> (query: InsertQuery<'T, 'InsertReturn>) = 
        this.InsertAsyncWithOptions(query)
    
    member this.InsertAsyncWithOptions<'T, 'InsertReturn> (iq: InsertQuery<'T, 'InsertReturn>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let compiledQuery = iq.ToKataQuery() |> compiler.Compile
            use cmd = this.BuildCommand(compiledQuery, log = false) // We will log manually below to capture query changes
            let cancel = defaultArg cancel CancellationToken.None

            // Applies on conflict modifier if in spec
            let applyOnConflict =
                match iq.Spec.InsertType with
                | InsertOrReplace -> OnConflict.insertOrReplace
                | OnConflictDoUpdate (conflictFields, updateFields) -> OnConflict.onConflictDoUpdate conflictFields updateFields
                | OnConflictDoNothing conflictFields -> OnConflict.onConflictDoNothing conflictFields
                | Insert -> id

            KataUtils.failIfIdentityOnConflict iq.Spec

            // Did the user select an identity field?
            match iq.Spec with
            | { IdentityField = Some identityField } ->
                // Try apply on conflict
                cmd.CommandText <- cmd.CommandText |> applyOnConflict

                // Fix postgres identity
                if compiler :? SqlKata.Compilers.PostgresCompiler 
                then cmd.CommandText <- cmd.CommandText |> Fixes.Postgres.fixIdentityQuery identityField 

                // Fix oracle identity
                elif compiler :? SqlKata.Compilers.OracleCompiler 
                then cmd.CommandText <- cmd.CommandText |> Fixes.Oracle.fixIdentityQuery identityField

                // Fix mssql guid identity
                elif compiler :? SqlKata.Compilers.SqlServerCompiler && typeof<'InsertReturn> = typeof<System.Guid>
                then cmd.CommandText <- cmd.CommandText |> Fixes.MsSql.fixGuidIdentityQuery identityField
                
                this.LogCommand(compiledQuery, cmd)

                // Execute insert and return identity
                if compiler :? SqlKata.Compilers.OracleCompiler then
                    let outputParam = cmd.CreateParameter()
                    outputParam.ParameterName <- "outputParam"
                    outputParam.DbType <- Data.DbType.Decimal
                    outputParam.Direction <- Data.ParameterDirection.Output
                    cmd.Parameters.Add(outputParam) |> ignore
                    let! _ = cmd.ExecuteNonQueryAsync(cancel)
                    // 'InsertReturn type set via `getId` in the builder
                    return Convert.ChangeType(outputParam.Value, typeof<'InsertReturn>) :?> 'InsertReturn
                else
                    let! identity = cmd.ExecuteScalarAsync(cancel)
                    // 'InsertReturn type set via `getId` in the builder
                    return Convert.ChangeType(identity, typeof<'InsertReturn>) :?> 'InsertReturn
        
            | { OutputFields = outputFields } when outputFields.Length > 0 -> 
                // Try apply on conflict
                cmd.CommandText <- cmd.CommandText |> applyOnConflict

                // Append SQL Server output clause
                cmd.CommandText <- OutputClause.inserted outputFields cmd.CommandText

                // Fix Oracle multi-insert query
                if compiler :? SqlKata.Compilers.OracleCompiler && iq.Spec.Entities.Length > 1 
                then cmd.CommandText <- cmd.CommandText |> Fixes.Oracle.fixMultiInsertQuery 
                
                this.LogCommand(compiledQuery, cmd)

                let! outputValues = OutputClause.readValues<'InsertReturn> cmd cancel outputFields
                return outputValues
            | _ ->
                // Try apply on conflict
                cmd.CommandText <- cmd.CommandText |> applyOnConflict

                // Fix Oracle multi-insert query
                if compiler :? SqlKata.Compilers.OracleCompiler && iq.Spec.Entities.Length > 1 
                then cmd.CommandText <- cmd.CommandText |> Fixes.Oracle.fixMultiInsertQuery 
                
                this.LogCommand(compiledQuery, cmd)

                let! results = cmd.ExecuteNonQueryAsync(cancel)
                // 'InsertReturn is `int` here -- NOTE: must include `'InsertReturn : struct` constraint
                return Convert.ChangeType(results, typeof<'InsertReturn>) :?> 'InsertReturn
        }
    
    member this.Update (query: UpdateQuery<'T, 'UpdateReturn>) = 
        use cmd = this.BuildCommand(query.ToKataQuery())
        cmd.ExecuteNonQuery()

    member this.UpdateAsync (query: UpdateQuery<'T, 'UpdateReturn>) = 
        this.UpdateAsyncWithOptions(query)
    
    member this.UpdateAsyncWithOptions (query: UpdateQuery<'T, 'UpdateReturn>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            let cancel = defaultArg cancel CancellationToken.None

            use cmd = this.BuildCommand(query.ToKataQuery())
            
            if query.Spec.OutputFields.Length > 0 then 
                // Append SQL Server output clause
                cmd.CommandText <- OutputClause.updated query.Spec.OutputFields cmd.CommandText
                let! outputValues = OutputClause.readValues<'UpdateReturn> cmd cancel query.Spec.OutputFields
                return outputValues 
            else
                let! rowsInserted = cmd.ExecuteNonQueryAsync(cancel)
                return Convert.ChangeType(rowsInserted, typeof<'UpdateReturn>) :?> 'UpdateReturn
        }

    member this.Delete (query: DeleteQuery<'T>) = 
        use cmd = this.BuildCommand(query.ToKataQuery())
        cmd.ExecuteNonQuery()

    member this.DeleteAsync (query: DeleteQuery<'T>) = 
        this.DeleteAsyncWithOptions(query)

    member this.DeleteAsyncWithOptions (query: DeleteQuery<'T>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            use cmd = this.BuildCommand(query.ToKataQuery())
            return! cmd.ExecuteNonQueryAsync(cancel |> Option.defaultValue CancellationToken.None)
        }

    member this.Count (query: SelectQuery<int>) = 
        use cmd = this.BuildCommand(query.ToKataQuery())
        match cmd.ExecuteScalar() with
        | :? int64 as count -> Convert.ToInt32 count
        | _  as count -> count :?> int

    member this.CountAsync (query: SelectQuery<int>) = 
        this.CountAsyncWithOptions(query)

    member this.CountAsyncWithOptions (query: SelectQuery<int>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0_OR_GREATER
            use cmd = this.BuildCommand(query.ToKataQuery())
            match! cmd.ExecuteScalarAsync(cancel |> Option.defaultValue CancellationToken.None) with
            | :? int64 as count -> return Convert.ToInt32 count
            | count -> return count :?> int
        }
namespace SqlHydra.Query

open System.Data.Common
open System.Threading
open SqlKata
#if NET5_0
open FSharp.Control.Tasks.V2
#endif

/// Contains methods that compile and read a query.
type QueryContext(conn: DbConnection, compiler: SqlKata.Compilers.Compiler) =
    let setProviderDbType (param: DbParameter) (propertyName: string) (providerDbType: string) =
        let property = param.GetType().GetProperty(propertyName)
        let dbTypeSetter = property.GetSetMethod()
        
        let value = System.Enum.Parse(property.PropertyType, providerDbType)
        dbTypeSetter.Invoke(param, [|value|]) |> ignore
        
    let setParameterDbType (param: DbParameter) (qp: QueryParameter) =
        match qp.ProviderDbType, compiler with
        | Some type', :? SqlKata.Compilers.PostgresCompiler ->
            setProviderDbType param "NpgsqlDbType" type'
        | Some type', :? SqlKata.Compilers.SqlServerCompiler ->
            setProviderDbType param "SqlDbType" type'
        | _ -> ()    
        
    interface System.IDisposable with
        member this.Dispose() = 
            conn.Dispose()
            this.Transaction |> Option.iter (fun t -> t.Dispose())
            this.Transaction <- None

    member this.Connection = conn
    member this.Compiler = compiler

    member val Transaction : DbTransaction option = None with get,set

    member this.BeginTransaction(?isolationLevel: System.Data.IsolationLevel) = 
        this.Transaction <- 
            match isolationLevel with
            | Some il -> conn.BeginTransaction(il) |> Some
            | None -> conn.BeginTransaction() |> Some

    member this.CommitTransaction() = 
        match this.Transaction with
        | Some t -> t.Commit(); this.Transaction <- None
        | None -> failwith "No transaction was started."

    member this.RollbackTransaction() =
        match this.Transaction with
        | Some t -> t.Rollback(); this.Transaction <- None
        | None -> failwith "No transaction was started."

    member private this.TrySetTransaction(cmd: DbCommand) =
        this.Transaction |> Option.iter (fun t -> cmd.Transaction <- t)

    /// Builds a DbCommand with CommandText and Parameters from a SqlKata compiled query.
    member this.BuildCommand(compiledQuery: SqlResult) =        
        let cmd = conn.CreateCommand()
        cmd |> this.TrySetTransaction
        cmd.CommandText <- compiledQuery.Sql
        for kvp in compiledQuery.NamedBindings do
            let p = cmd.CreateParameter()
            p.ParameterName <- kvp.Key

            match kvp.Value with
            | :? QueryParameter as qp ->
                do setParameterDbType p qp
                p.Value <- qp.Value
            | _ ->
                p.Value <- kvp.Value
            cmd.Parameters.Add(p) |> ignore
        cmd

    /// Builds an ADO.NET DbCommand from a SqlKata query.
    member this.BuildCommand(query: Query) =
        let compiledQuery = query |> compiler.Compile
        this.BuildCommand(compiledQuery)

    /// Returns an ADO.NET data reader for a given query.
    member this.GetReader<'T, 'Reader when 'Reader :> DbDataReader> (query: SelectQuery<'T>) = 
        let cmd = this.BuildCommand(query.ToKataQuery()) // do not dispose cmd
        cmd.ExecuteReader() :?> 'Reader

    /// Returns an ADO.NET data reader for a given query.
    member this.GetReaderAsync<'T, 'Reader when 'Reader :> DbDataReader> (query: SelectQuery<'T>) = 
        this.GetReaderAsyncWithOptions<'T, 'Reader>(query)

    /// Returns an ADO.NET data reader for a given query.
    member this.GetReaderAsyncWithOptions<'T, 'Reader when 'Reader :> DbDataReader> (query: SelectQuery<'T>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0
            let cmd = this.BuildCommand(query.ToKataQuery()) // do not dispose cmd
            let! reader = cmd.ExecuteReaderAsync(cancel |> Option.defaultValue CancellationToken.None)
            return reader :?> 'Reader
        }

    /// Executes a select query with a given readEntity builder function.
    member this.Read<'Entity, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Entity)) (query: SelectQuery<'Entity>) =
        use cmd = this.BuildCommand(query.ToKataQuery())
        use reader = cmd.ExecuteReader() :?> 'Reader
        let readEntity = readEntityBuilder reader
        seq [| 
            while reader.Read() do
                readEntity() 
        |] 

    /// Executes a select query with a given readEntity builder function.
    member this.ReadAsync<'Entity, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Entity)) (query: SelectQuery<'Entity>) = 
        this.ReadAsyncWithOptions (query, readEntityBuilder)

    /// Executes a select query with a given readEntity builder function and optional args.
    member this.ReadAsyncWithOptions<'Entity, 'Reader when 'Reader :> DbDataReader> 
        (query: SelectQuery<'Entity>, readEntityBuilder: 'Reader -> (unit -> 'Entity), ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0
            use cmd = this.BuildCommand (query.ToKataQuery())
            use! reader = cmd.ExecuteReaderAsync(cancel |> Option.defaultValue CancellationToken.None)
            let readEntity = readEntityBuilder (reader :?> 'Reader)
            return
                seq [| 
                    while reader.Read() do
                        readEntity () 
                |]
        }

    /// Executes a select query with a given readEntity builder function for a single (option) result.
    member this.ReadOne<'Entity, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Entity)) (query: SelectQuery<'Entity>) =
        this.Read readEntityBuilder query |> Seq.tryHead

    /// Executes a select query with a given readEntity builder function for a single (option) result.
    member this.ReadOneAsync<'Entity, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Entity)) (query: SelectQuery<'Entity>) = 
        this.ReadOneAsyncWithOptions(query, readEntityBuilder)

    /// Executes a select query with a given readEntity builder function for a single (option) result with optional args.
    member this.ReadOneAsyncWithOptions<'Entity, 'Reader when 'Reader :> DbDataReader>
        (query: SelectQuery<'Entity>, readEntityBuilder: 'Reader -> (unit -> 'Entity), ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0
            let! entities = this.ReadAsyncWithOptions (query, readEntityBuilder, cancel |> Option.defaultValue CancellationToken.None)
            return entities |> Seq.tryHead
        }

    member this.Insert<'T, 'InsertReturn when 'InsertReturn : struct> (query: InsertQuery<'T, 'InsertReturn>) = 
        use cmd = this.BuildCommand(query.ToKataQuery())
        // Did the user select an identity field?
        match query.Spec.IdentityField with
        | Some identityField -> 
            if compiler :? SqlKata.Compilers.PostgresCompiler then 
                // Replace PostgreSQL identity query
                cmd.CommandText <- cmd.CommandText.Replace(";SELECT lastval() AS id", $" RETURNING {identityField};")

            let identity = cmd.ExecuteScalar()
            // 'InsertReturn type set via `getId` in the builder
            System.Convert.ChangeType(identity, typeof<'InsertReturn>) :?> 'InsertReturn
        
        | None ->
            let results = cmd.ExecuteNonQuery()
            // 'InsertReturn is `int` here -- NOTE: must include `'InsertReturn : struct` constraint
            System.Convert.ChangeType(results, typeof<'InsertReturn>) :?> 'InsertReturn

    member this.InsertAsync<'T, 'InsertReturn when 'InsertReturn : struct> (query: InsertQuery<'T, 'InsertReturn>) = 
        this.InsertAsyncWithOptions(query)
    
    member this.InsertAsyncWithOptions<'T, 'InsertReturn when 'InsertReturn : struct> (query: InsertQuery<'T, 'InsertReturn>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0
            use cmd = this.BuildCommand(query.ToKataQuery())
            // Did the user select an identity field?
            match query.Spec.IdentityField with
            | Some identityField -> 
                if compiler :? SqlKata.Compilers.PostgresCompiler then 
                    // Replace PostgreSQL identity query
                    cmd.CommandText <- cmd.CommandText.Replace(";SELECT lastval() AS id", $" RETURNING {identityField};")

                let! identity = cmd.ExecuteScalarAsync(cancel |> Option.defaultValue CancellationToken.None)
                // 'InsertReturn type set via `getId` in the builder
                return System.Convert.ChangeType(identity, typeof<'InsertReturn>) :?> 'InsertReturn
        
            | None ->
                let! results = cmd.ExecuteNonQueryAsync(cancel |> Option.defaultValue CancellationToken.None)
                // 'InsertReturn is `int` here -- NOTE: must include `'InsertReturn : struct` constraint
                return System.Convert.ChangeType(results, typeof<'InsertReturn>) :?> 'InsertReturn
        }
    
    member this.Update (query: UpdateQuery<'T>) = 
        use cmd = this.BuildCommand(query.ToKataQuery())
        cmd.ExecuteNonQuery()

    member this.UpdateAsync (query: UpdateQuery<'T>) = 
        this.UpdateAsyncWithOptions(query)
    
    member this.UpdateAsyncWithOptions (query: UpdateQuery<'T>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0
            use cmd = this.BuildCommand(query.ToKataQuery())
            return! cmd.ExecuteNonQueryAsync(cancel |> Option.defaultValue CancellationToken.None)
        }

    member this.Delete (query: DeleteQuery<'T>) = 
        use cmd = this.BuildCommand(query.ToKataQuery())
        cmd.ExecuteNonQuery()

    member this.DeleteAsync (query: DeleteQuery<'T>) = 
        this.DeleteAsyncWithOptions(query)

    member this.DeleteAsyncWithOptions (query: DeleteQuery<'T>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0
            use cmd = this.BuildCommand(query.ToKataQuery())
            return! cmd.ExecuteNonQueryAsync(cancel |> Option.defaultValue CancellationToken.None)
        }

    member this.Count (query: SelectQuery<int>) = 
        use cmd = this.BuildCommand(query.ToKataQuery())
        match cmd.ExecuteScalar() with
        | :? int64 as count -> System.Convert.ToInt32 count
        | _  as count -> count :?> int

    member this.CountAsync (query: SelectQuery<int>) = 
        this.CountAsyncWithOptions(query)

    member this.CountAsyncWithOptions (query: SelectQuery<int>, ?cancel: CancellationToken) = 
        task { // Must wrap in task to prevent `EndExecuteNonQuery` ex in NET6_0
            use cmd = this.BuildCommand(query.ToKataQuery())
            let! count = cmd.ExecuteScalarAsync(cancel |> Option.defaultValue CancellationToken.None)
            return 
                match count with
                | :? int64 as value -> System.Convert.ToInt32 value
                | _ -> count :?> int
        }
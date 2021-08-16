namespace SqlHydra.Query

open System.Data.Common
open FSharp.Control.Tasks.V2
open SqlKata
open KataBuilders

/// Contains methods that compile and read a query.
type QueryContext(conn: DbConnection, compiler: SqlKata.Compilers.Compiler) =

    interface System.IDisposable with
        member this.Dispose() = 
            conn.Dispose()
            this.Transaction |> Option.iter (fun t -> t.Dispose())
            this.Transaction <- None

    member this.Connection = conn
    member this.Compiler = compiler

    member val Transaction : DbTransaction option = None with get,set

    member this.BeginTransaction() = 
        this.Transaction <- conn.BeginTransaction() |> Some

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
            p.Value <- kvp.Value
            cmd.Parameters.Add(p) |> ignore
        cmd

    member private this.BuildCommand(query: Query) =
        let compiledQuery = query |> compiler.Compile
        this.BuildCommand(compiledQuery)

    member this.GetReader<'T, 'Reader when 'Reader :> DbDataReader> (query: TypedQuery<'T>) = 
        use cmd = this.BuildCommand(query.Query)
        cmd.ExecuteReader() :?> 'Reader

    member this.Read<'Entity, 'Reader when 'Reader :> DbDataReader> (getReaders: 'Reader -> (unit -> 'Entity)) (query: TypedQuery<'Entity>) =
        use cmd = this.BuildCommand(query.Query)
        use reader = cmd.ExecuteReader() :?> 'Reader
        let read = getReaders reader
        seq [| 
            while reader.Read() do
                read() 
        |] 

    member this.ReadOne<'Entity, 'Reader when 'Reader :> DbDataReader> (getReaders: 'Reader -> (unit -> 'Entity)) (query: TypedQuery<'Entity>) =
        this.Read getReaders query |> Seq.tryHead

    member this.GetReaderAsync<'T, 'Reader when 'Reader :> DbDataReader> (query: TypedQuery<'T>) = 
        task {
            use cmd = this.BuildCommand(query.Query)
            let! reader = cmd.ExecuteReaderAsync()
            return reader :?> 'Reader
        }

    member this.ReadAsync<'Entity, 'Reader when 'Reader :> DbDataReader> (getReaders: 'Reader -> (unit -> 'Entity)) (query: TypedQuery<'Entity>) = 
        task {
            use cmd = this.BuildCommand(query.Query)
            use! reader = cmd.ExecuteReaderAsync()
            let read = getReaders (reader :?> 'Reader)
            return
                seq [| 
                    while reader.Read() do
                        read() 
                |]
        }

    member this.ReadOneAsync<'Entity, 'Reader when 'Reader :> DbDataReader> (getReaders: 'Reader -> (unit -> 'Entity)) (query: TypedQuery<'Entity>) = 
        task {
            let! entities = this.ReadAsync getReaders query
            return entities |> Seq.tryHead
        }

    member private this.BuildInsertCommand (returnId: bool, insertQuerySpec: InsertQuerySpec<'T>) = 
        let kataQuery = KataUtils.fromInsert returnId insertQuerySpec
        let compiledQuery = compiler.Compile kataQuery
        this.BuildCommand(compiledQuery)

    member this.Insert (query: InsertQuerySpec<'T>) = 
        use cmd = this.BuildInsertCommand(false, query)
        cmd.ExecuteNonQuery()

    member this.InsertAsync (query: InsertQuerySpec<'T>) = 
        use cmd = this.BuildInsertCommand(false, query)
        cmd.ExecuteNonQueryAsync()

    member this.InsertGetId<'T, 'Identity> (query: InsertQuerySpec<'T>) =
        use cmd = this.BuildInsertCommand(true, query)
        let identity = cmd.ExecuteScalar()
        System.Convert.ChangeType(identity, typeof<'Identity>) :?> 'Identity

    member this.InsertGetIdAsync<'T, 'Identity> (query: InsertQuerySpec<'T>) = task {
        use cmd = this.BuildInsertCommand(true, query)
        let! identity = cmd.ExecuteScalarAsync()
        return System.Convert.ChangeType(identity, typeof<'Identity>) :?> 'Identity
    }
    
    member this.Update (query: UpdateQuerySpec<'T>) = 
        use cmd = query |> KataUtils.fromUpdate |> compiler.Compile |> this.BuildCommand
        cmd.ExecuteNonQuery()

    member this.UpdateAsync (query: UpdateQuerySpec<'T>) = 
        use cmd = query |> KataUtils.fromUpdate |> compiler.Compile |> this.BuildCommand
        cmd.ExecuteNonQueryAsync()

    member this.Delete (query: TypedQuery<'T>) = 
        use cmd = this.BuildCommand(query.Query)
        cmd.ExecuteNonQuery()

    member this.DeleteAsync (query: TypedQuery<'T>) = 
        use cmd = this.BuildCommand(query.Query)
        cmd.ExecuteNonQueryAsync()

    member this.Count (query: TypedQuery<int>) = 
        use cmd = this.BuildCommand(query.Query)
        let count = cmd.ExecuteScalar()
        count :?> int

    member this.CountAsync (query: TypedQuery<int>) = task {
        use cmd = this.BuildCommand(query.Query)
        let! count = cmd.ExecuteScalarAsync()
        return count :?> int
    }
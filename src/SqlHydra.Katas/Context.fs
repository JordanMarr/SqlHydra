namespace SqlHydra.Katas

open System.Data.Common
open FSharp.Control.Tasks.V2
open SqlKata
open Builders

/// Contains methods that compile and read a query.
type Context(conn: DbConnection, compiler: SqlKata.Compilers.Compiler) =

    let boxValue (value: obj) = 
        if isNull value then 
            box System.DBNull.Value
        else
            match value.GetType() with
            | t when t.IsGenericType && t.Name.StartsWith("FSharpOption") -> 
                t.GetProperty("Value").GetValue(value)
            | _ -> value
            |> function 
                | null -> box System.DBNull.Value 
                | o -> o

    interface System.IDisposable with
        member this.Dispose() = 
            conn.Dispose()
            this.Transaction |> Option.iter (fun t -> t.Dispose())
            this.Transaction <- None

    member this.Connection = conn

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

    member private this.BuildCommand(compiledQuery: SqlResult) =        
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

    member this.GetReader<'T, 'Reader when 'Reader :> DbDataReader> (query: Query<'T>) = 
        use cmd = this.BuildCommand(query.Query)
        cmd.ExecuteReader() :?> 'Reader

    member this.Read<'Entity, 'Reader when 'Reader :> DbDataReader> (getReaders: 'Reader -> (unit -> 'Entity)) (query: Query<'Entity>) =
        use cmd = this.BuildCommand(query.Query)
        use reader = cmd.ExecuteReader() :?> 'Reader
        let read = getReaders reader
        [ while reader.Read() do
            read() ]

    member this.ReadOne<'Entity, 'Reader when 'Reader :> DbDataReader> (getReaders: 'Reader -> (unit -> 'Entity)) (query: Query<'Entity>) =
        this.Read getReaders query |> List.tryHead

    member this.GetReaderAsync<'T, 'Reader when 'Reader :> DbDataReader> (query: Query<'T>) = task {
        use cmd = this.BuildCommand(query.Query)
        let! reader = cmd.ExecuteReaderAsync()
        return reader :?> 'Reader
    }

    member this.ReadAsync<'Entity, 'Reader when 'Reader :> DbDataReader> (getReaders: 'Reader -> (unit -> 'Entity)) (query: Query<'Entity>) = 
        task {
            use cmd = this.BuildCommand(query.Query)
            use! reader = cmd.ExecuteReaderAsync()
            let read = getReaders (reader :?> 'Reader)
            return
                [ while reader.Read() do
                    read() ]
        }

    member this.ReadOneAsync<'Entity, 'Reader when 'Reader :> DbDataReader> (getReaders: 'Reader -> (unit -> 'Entity)) (query: Query<'Entity>) = 
        task {
            let! entities = this.ReadAsync getReaders query
            return entities |> List.tryHead
        }

    member private this.BuildInsertCommand (returnId: bool, query: InsertQuery<'T>) = 
        let kata = 
            let kvps = 
                match query.Entity with
                | Some entity -> 
                    match query.Fields with 
                    | [] -> 
                        FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                        |> Array.map (fun p -> p.Name, p.GetValue(entity))
                        
                    | fields -> 
                        let included = fields |> Set.ofList
                        FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                        |> Array.filter (fun p -> included.Contains(p.Name)) 
                        |> Array.map (fun p -> p.Name, p.GetValue(entity))
                | None -> 
                    failwith "Value not set"

            // Handle option values
            let preparedKvps = 
                kvps 
                |> Seq.map (fun (key,value) -> key, boxValue value)
                |> dict
                |> Seq.map id

            Query(query.Table).AsInsert(preparedKvps, returnId = returnId)

        let compiledQuery = compiler.Compile kata
        this.BuildCommand(compiledQuery)

    member this.Insert (query: InsertQuery<'T>) = 
        use cmd = this.BuildInsertCommand(false, query)
        cmd.ExecuteNonQuery()

    member this.InsertAsync (query: InsertQuery<'T>) = 
        use cmd = this.BuildInsertCommand(false, query)
        cmd.ExecuteNonQueryAsync()

    member this.InsertGetId<'T, 'Identity> (query: InsertQuery<'T>) =
        use cmd = this.BuildInsertCommand(true, query)
        let identity = cmd.ExecuteScalar()
        System.Convert.ChangeType(identity, typeof<'Identity>) :?> 'Identity

    member this.InsertGetIdAsync<'T, 'Identity> (query: InsertQuery<'T>) = task {
        use cmd = this.BuildInsertCommand(true, query)
        let! identity = cmd.ExecuteScalarAsync()
        return System.Convert.ChangeType(identity, typeof<'Identity>) :?> 'Identity
    }
    
    member private this.BuildUpdateCommand (updateQuery: UpdateQuery<'T>) = 
        let kata = 
            let kvps = 
                match updateQuery.Entity, updateQuery.SetValues with
                | Some entity, [] -> 
                    match updateQuery.Fields with 
                    | [] -> 
                        FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                        |> Array.map (fun p -> p.Name, p.GetValue(entity))
                        
                    | fields -> 
                        let included = fields |> Set.ofList
                        FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) 
                        |> Array.filter (fun p -> included.Contains(p.Name)) 
                        |> Array.map (fun p -> p.Name, p.GetValue(entity))

                | Some _, _ -> failwith "Cannot have both `entity` and `set` operations in an `update` expression."
                | None, [] -> failwith "Either an `entity` or `set` operations must be present in an `update` expression."
                | None, setValues -> setValues |> List.toArray
                    
            // Handle option values
            let preparedKvps = 
                kvps 
                |> Seq.map (fun (key,value) -> key, boxValue value)
                |> dict
                |> Seq.map id

            let q = Query(updateQuery.Table).AsUpdate(preparedKvps)

            // Apply `where` clause
            match updateQuery.Where with
            | Some where -> q.Where(fun w -> where)
            | None -> q

        let compiledQuery = compiler.Compile kata
        this.BuildCommand(compiledQuery)

    member this.Update (query: UpdateQuery<'T>) = 
        use cmd = this.BuildUpdateCommand(query)
        cmd.ExecuteNonQuery()

    member this.UpdateAsync (query: UpdateQuery<'T>) = 
        use cmd = this.BuildUpdateCommand(query)
        cmd.ExecuteNonQueryAsync()

    member this.Delete (query: Query<'Entity>) = 
        use cmd = this.BuildCommand(query.Query)
        cmd.ExecuteNonQuery()

    member this.DeleteAsync (query: Query<'Entity>) = 
        use cmd = this.BuildCommand(query.Query)
        cmd.ExecuteNonQueryAsync()

    member this.Count (query: Query<int>) = 
        use cmd = this.BuildCommand(query.Query)
        let count = cmd.ExecuteScalar()
        count :?> int

    member this.CountAsync (query: Query<int>) = task {
        use cmd = this.BuildCommand(query.Query)
        let! count = cmd.ExecuteScalarAsync()
        return count :?> int
    }
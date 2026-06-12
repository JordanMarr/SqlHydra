module Sqlite.``Query Integration Tests``

open SqlHydra.Query
open DB
open SqlHydra.Query.SqliteExtensions
open type SqlFn
open Swensen.Unquote
open NUnit.Framework
open System.Threading.Tasks
#if NET8_0
open Sqlite.AdventureWorksNet8
#endif
#if NET9_0
open Sqlite.AdventureWorksNet9
#endif
#if NET10_0
open Sqlite.AdventureWorksNet10
#endif

/// Ad-hoc schema module for DateOnly/TimeOnly round-trip tests (table stored in SQLite as TEXT).
module dbo =
    [<CLIMutable>]
    type DateOnlyTest =
        { [<SqlHydra.ProviderDbType("Date")>]
          date: System.DateOnly }

    let DateOnlyTest = table<DateOnlyTest>

let stubbedErrorLog = 
    {
        main.ErrorLog.ErrorLogID = 0L // Exclude
        main.ErrorLog.ErrorTime = System.DateTime.Now
        main.ErrorLog.ErrorLine = None
        main.ErrorLog.ErrorMessage = "TEST INSERT ASYNC"
        main.ErrorLog.ErrorNumber = 400L
        main.ErrorLog.ErrorProcedure = (Some "Procedure 400")
        main.ErrorLog.ErrorSeverity = None
        main.ErrorLog.ErrorState = None
        main.ErrorLog.UserName = "jmarr"
    }

[<Test>]
let ``Where City Starts With S``() = task {
    let! addresses =
        selectTask db {
            for a in main.Address do
            where (a.City |=| [ "Seattle"; "Santa Cruz" ])
        }

    gt0 addresses
    Assert.IsTrue(addresses |> Seq.forall (fun a -> a.City = "Seattle" || a.City = "Santa Cruz"), "Expected only 'Seattle' or 'Santa Cruz'.")
}

[<Test>]
let ``Select City Column Where City Starts with S``() = task {
    let! cities =
        selectTask db {
            for a in main.Address do
            where (a.City =% "S%")
            select a.City
        }

    gt0 cities
    Assert.IsTrue(cities |> Seq.forall (fun city -> city.StartsWith "S"), "Expected all cities to start with 'S'.")
}

[<Test>]
let ``Inner Join Orders-Details``() = task {
    let! results =
        selectTask db {
            for o in main.SalesOrderHeader do
            join d in main.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            where (o.OnlineOrderFlag = 0L)
            select (o, d)
        }

    gt0 results
}

[<Test>]
let ``Where subqueryOne``() = task {
    let avgListPrice =
        select {
            for p in main.Product do
            select (avgBy p.ListPrice)
        }

    let! productsWithAboveAveragePrice =
        selectTask db {
            for p in main.Product do
            where (p.ListPrice > subqueryOne avgListPrice)
            select (p.Name, p.ListPrice)
        }

    gt0 productsWithAboveAveragePrice
}

[<Test>]
let ``Select with Timeout``() = task {
    use! ctx = db.OpenContextAsync()
    let q =
        select {
            for c in main.Customer do
            where (c.CustomerID = 1L)
            timeout (System.TimeSpan.FromSeconds 45.0)
        }
    use cmd = ctx.BuildCommand(q.IR)
    cmd.CommandTimeout =! 45
}

[<Test>]
let ``Select without Timeout Leaves DbCommand Default``() = task {
    use! ctx = db.OpenContextAsync()
    // Capture the provider default by building a no-options command.
    let defaultTimeout =
        use baseline = ctx.Connection.CreateCommand()
        baseline.CommandTimeout

    let q =
        select {
            for c in main.Customer do
            where (c.CustomerID = 1L)
        }
    use cmd = ctx.BuildCommand(q.IR)
    cmd.CommandTimeout =! defaultTimeout
}

[<Test>]
let ``Select with Multiple Timeouts``() = task {
    use! ctx = db.OpenContextAsync()
    let q =
        select {
            for c in main.Customer do
            where (c.CustomerID = 1L)
            timeout (System.TimeSpan.FromSeconds 5.0)
            timeout (System.TimeSpan.FromSeconds 60.0)
        }
    use cmd = ctx.BuildCommand(q.IR)
    cmd.CommandTimeout =! 60
}

[<Test>]
let ``Select with Sub-Second Timeout``() = task {
    use! ctx = db.OpenContextAsync()
    let q =
        select {
            for c in main.Customer do
            where (c.CustomerID = 1L)
            timeout (System.TimeSpan.FromMilliseconds 500.0)
        }
    use cmd = ctx.BuildCommand(q.IR)
    // DbCommand.CommandTimeout is an int (seconds); ApplyCommandOptions
    // calls Math.Ceiling, so 0.5s is rounded up to 1s rather than truncating to 0
    // (which DbCommand would otherwise interpret as "no timeout").
    cmd.CommandTimeout =! 1
}

[<Test>]
let ``Select with Zero Timeout``() = task {
    use! ctx = db.OpenContextAsync()
    let q =
        select {
            for c in main.Customer do
            where (c.CustomerID = 1L)
            timeout System.TimeSpan.Zero
        }
    use cmd = ctx.BuildCommand(q.IR)
    cmd.CommandTimeout =! 0
}

[<Test>]
let ``InsertGetId Test``() = task {
    let errorLog = 
        {
            main.ErrorLog.ErrorLogID = 0L // Exclude
            main.ErrorLog.ErrorTime = System.DateTime.Now
            main.ErrorLog.ErrorLine = None
            main.ErrorLog.ErrorMessage = "TEST"
            main.ErrorLog.ErrorNumber = 400L
            main.ErrorLog.ErrorProcedure = (Some "Procedure 400")
            main.ErrorLog.ErrorSeverity = None
            main.ErrorLog.ErrorState = None
            main.ErrorLog.UserName = "jmarr"
        }

    let! errorLogId = 
        insertTask db {
            for e in main.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }

    Assert.IsTrue(errorLogId > 0L, "Expected returned ID to be > 0")
}

[<Test>]
let ``InsertGetIdAsync Test``() = task {
    let errorLog = 
        {
            main.ErrorLog.ErrorLogID = 0L // Exclude
            main.ErrorLog.ErrorTime = System.DateTime.Now
            main.ErrorLog.ErrorLine = None
            main.ErrorLog.ErrorMessage = "TEST INSERT ASYNC"
            main.ErrorLog.ErrorNumber = 400L
            main.ErrorLog.ErrorProcedure = (Some "Procedure 400")
            main.ErrorLog.ErrorSeverity = None
            main.ErrorLog.ErrorState = None
            main.ErrorLog.UserName = "jmarr"
        }

    let! result = 
        insertTask db {
            for e in main.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }

    result >! 0L
}

[<Test>]
let ``Insert with Timeout``() = task {
    use! ctx = db.OpenContextAsync()
    let sample =
        { main.ErrorLog.ErrorLogID = 0L
          main.ErrorLog.ErrorTime = System.DateTime.Now
          main.ErrorLog.ErrorLine = None
          main.ErrorLog.ErrorMessage = "TIMEOUT TEST"
          main.ErrorLog.ErrorNumber = 400L
          main.ErrorLog.ErrorProcedure = Some "Procedure 400"
          main.ErrorLog.ErrorSeverity = None
          main.ErrorLog.ErrorState = None
          main.ErrorLog.UserName = "jmarr" }
    let q =
        insert {
            for e in main.ErrorLog do
            entity sample
            excludeColumn e.ErrorLogID
            timeout (System.TimeSpan.FromSeconds 45.0)
        }
    let ir = SqlHydra.Query.QueryUtils.fromInsert q.Spec
    let compiled = ctx.Emitter.EmitInsert(ir)
    use cmd = ctx.BuildCommandFromCompiled(compiled)
    cmd.CommandTimeout =! 45
}

[<Test>]
let ``Update Set Individual Fields``() = task {
    let! result = 
        updateTask db {
            for e in main.ErrorLog do
            set e.ErrorNumber 123L
            set e.ErrorMessage "ERROR #123"
            set e.ErrorLine (Some 999L)
            set e.ErrorProcedure None
            where (e.ErrorLogID = 1L)
        }

    printfn "result: %i" result
}

[<Test>]
let ``UpdateAsync Set Individual Fields``() = task {
    let! result = 
        updateTask db {
            for e in main.ErrorLog do
            set e.ErrorNumber 123L
            set e.ErrorMessage "ERROR #123"
            set e.ErrorLine (Some 999L)
            set e.ErrorProcedure None
            where (e.ErrorLogID = 1L)
        }

    printfn "result: %i" result
}

[<Test>]
let ``Update Entity``() = task {
    let errorLog = 
        {
            main.ErrorLog.ErrorLogID = 2L
            main.ErrorLog.ErrorTime = System.DateTime.Now
            main.ErrorLog.ErrorLine = Some 888L
            main.ErrorLog.ErrorMessage = "ERROR #2"
            main.ErrorLog.ErrorNumber = 500L
            main.ErrorLog.ErrorProcedure = None
            main.ErrorLog.ErrorSeverity = None
            main.ErrorLog.ErrorState = None
            main.ErrorLog.UserName = "jmarr"
        }

    let! result = 
        updateTask db {
            for e in main.ErrorLog do
            entity errorLog
            excludeColumn e.ErrorLogID
            where (e.ErrorLogID = errorLog.ErrorLogID)
        }

    printfn "result: %i" result
}

[<Test>]
let ``Update with Timeout``() = task {
    use! ctx = db.OpenContextAsync()
    let q =
        update {
            for c in main.Customer do
            set c.FirstName "John"
            where (c.CustomerID = -1L)
            timeout (System.TimeSpan.FromSeconds 45.0)
        }
    let ir = SqlHydra.Query.QueryUtils.fromUpdate q.Spec
    let compiled = ctx.Emitter.EmitUpdate(ir)
    use cmd = ctx.BuildCommandFromCompiled(compiled)
    cmd.CommandTimeout =! 45
}

[<Test>]
let ``Delete Test``() = task {
    let! result = 
        deleteTask db {
            for e in main.ErrorLog do
            where (e.ErrorLogID = 5L)
        }

    printfn "result: %i" result
}

[<Test>]
let ``DeleteAsync Test``() = task {
    let! result = 
        deleteTask db {
            for e in main.ErrorLog do
            where (e.ErrorLogID = 5L)
        }

    printfn "result: %i" result
}

[<Test>]
let ``Delete with Timeout``() = task {
    use! ctx = db.OpenContextAsync()
    let q =
        delete {
            for c in main.Customer do
            where (c.CustomerID = -1L)
            timeout (System.TimeSpan.FromSeconds 45.0)
        }
    let compiled = q.CompileWith(ctx.Emitter)
    use cmd = ctx.BuildCommandFromCompiled(compiled)
    cmd.CommandTimeout =! 45
}

[<Test>]
let ``Multiple Inserts``() = task {
    use! shared = db.OpenContextAsync()

    shared.BeginTransaction()

    let! _ = 
        deleteTask shared {
            for e in main.ErrorLog do
            deleteAll
        }

    let errorLogs = 
        [ 0L .. 2L ] 
        |> List.map (fun i -> 
            { stubbedErrorLog with ErrorNumber = stubbedErrorLog.ErrorNumber + i }
        )
        |> AtLeastOne.tryCreate
        
    match errorLogs with
    | Some errorLogs ->
        let! rowsInserted = 
            insert {
                for e in main.ErrorLog do
                entities errorLogs
                excludeColumn e.ErrorLogID
            }
            |> shared.InsertAsync

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

    | None -> 
        ()

    let! results =
        selectTask shared {
            for e in main.ErrorLog do
            select e.ErrorNumber
        }

    let errorNumbers = results |> Seq.toList
    
    errorNumbers =! [ 400L; 401L; 402L ]

    shared.RollbackTransaction()
}

[<Test>]
let ``Distinct Test``() = task {
    use! shared = db.OpenContextAsync()

    shared.BeginTransaction()

    let! _ = 
        deleteTask shared {
            for e in main.ErrorLog do
            deleteAll
        }

    let errorLogs = 
        [ 0L .. 2L ] 
        |> List.map (fun _ -> stubbedErrorLog)
        |> AtLeastOne.tryCreate
        
    match errorLogs with
    | Some errorLogs ->
        let! rowsInserted = 
            insert {
                for e in main.ErrorLog do
                entities errorLogs
                excludeColumn e.ErrorLogID
            }
            |> shared.InsertAsync

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

    | None -> 
        ()

    let! results =
        selectTask shared {
            for e in main.ErrorLog do
            select e.ErrorNumber
        }

    let! distinctResults =
        selectTask shared {
            for e in main.ErrorLog do
            select e.ErrorNumber
            distinct
        }

    results |> Seq.length =! 3
    distinctResults |> Seq.length =! 1

    shared.RollbackTransaction()
}

[<Test>]
let ``Count Test``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

    for i in [0..2] do
        let! result = 
            insert {
                for e in main.ErrorLog do
                entity stubbedErrorLog
                getId e.ErrorLogID
            }
            |> shared.InsertAsync
        ()

    let! count = 
        select {
            for e in main.ErrorLog do
            count
        }
        |> shared.CountAsync

    count >! 0
    shared.RollbackTransaction()
}

[<Test>]
let ``OnConflictDoUpdate``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

    let upsertAddress address = 
        insertTask shared {
            for a in main.Address do
            entity address
            onConflictDoUpdate a.AddressID (
                a.AddressLine1,
                a.AddressLine2,
                a.City,
                a.StateProvince,
                a.CountryRegion,
                a.PostalCode,
                a.ModifiedDate
            )
        } :> Task

    let queryAddress id = 
        selectTask shared {
            for a in main.Address do
            where (a.AddressID = id)
            toList
        }

    let newAddress = 
         { main.Address.AddressID = 5000
         ; main.Address.AddressLine1 = "123 Main St"
         ; main.Address.AddressLine2 = None
         ; main.Address.City = "Portland"
         ; main.Address.StateProvince = "OR"
         ; main.Address.CountryRegion = "United States"
         ; main.Address.PostalCode = "97205"
         ; main.Address.rowguid = ""
         ; main.Address.ModifiedDate = System.DateTime.Now }

    do! upsertAddress newAddress
    let! result1 = queryAddress 5000L

    let r1 = result1 : main.Address list
    r1.Length =! 1
    r1.[0] =! newAddress

    let updatedAddress = { newAddress with AddressLine2 = Some "Apt 1A" }

    do! upsertAddress updatedAddress
    let! result2 = queryAddress 5000L

    let r2 = result2 : main.Address list
    r2.Length =! 1
    r2.[0] =! updatedAddress

    shared.RollbackTransaction()
}

[<Test>]
let ``SqlFn - SQLite functions smoke test``() = task {
    let! results =
        selectTask db {
            for a in main.Address do
            select (a.City, length a.City, upper a.City, ifnull(a.AddressLine2, "N/A"))
            take 1
        }

    let city, len, upperCity, line2 = results |> Seq.head
    Assert.That(len, Is.GreaterThan(0))
    Assert.That(upperCity, Is.EqualTo(city.ToUpper()))
    Assert.That(line2, Is.Not.Null)
}

/// Reproduces https://github.com/JordanMarr/SqlHydra/issues/123
/// SQLite stores DateOnly as a datetime string (e.g. '2024-06-20 00:00:00'),
/// which causes DateOnly.Parse to fail with a FormatException on read-back.
[<Test>]
let ``DateOnly round-trip in SQLite``() = task {
    // Create an in-memory SQLite database with a DateOnly column (stored as text)
    use conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:")
    do! conn.OpenAsync()

    // Attach an in-memory database as 'dbo' so that [dbo].[DateOnlyTest] resolves
    use attachCmd = conn.CreateCommand(CommandText = "ATTACH DATABASE ':memory:' AS 'dbo'")
    attachCmd.ExecuteNonQuery() |> ignore

    use cmd = conn.CreateCommand(CommandText = "CREATE TABLE [dbo].[DateOnlyTest] (date TEXT NOT NULL)")
    cmd.ExecuteNonQuery() |> ignore

    let emitter = SqlHydra.Query.SqliteEmitter()
    use ctx = new QueryContext(conn, emitter)

    let expected = System.DateOnly(2024, 6, 20)

    let! _ =
        insertTask ctx {
            into dbo.DateOnlyTest
            entity { date = expected }
        }

    let! results =
        selectTask ctx {
            for row in dbo.DateOnlyTest do
            toArray
        }

    results.Length =! 1
    results.[0].date =! expected
}

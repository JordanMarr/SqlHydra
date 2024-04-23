module Sqlite.``Query Integration Tests``

open SqlHydra.Query
open DB
open SqlHydra.Query.SqliteExtensions
open Swensen.Unquote
open NUnit.Framework
open System.Threading.Tasks
#if NET6_0
open Sqlite.AdventureWorksNet6
#endif
#if NET8_0
open Sqlite.AdventureWorksNet8
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.SqliteCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)

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
    use ctx = openContext()
            
    let addresses =
        select {
            for a in main.Address do
            where (a.City |=| [ "Seattle"; "Santa Cruz" ])
        }
        |> ctx.Read HydraReader.Read

    gt0 addresses
    Assert.IsTrue(addresses |> Seq.forall (fun a -> a.City = "Seattle" || a.City = "Santa Cruz"), "Expected only 'Seattle' or 'Santa Cruz'.")
}

[<Test>]
let ``Select City Column Where City Starts with S``() = task {
    use ctx = openContext()

    let cities =
        select {
            for a in main.Address do
            where (a.City =% "S%")
            select a.City
        }
        |> ctx.Read HydraReader.Read

    gt0 cities
    Assert.IsTrue(cities |> Seq.forall (fun city -> city.StartsWith "S"), "Expected all cities to start with 'S'.")
}

[<Test>]
let ``Inner Join Orders-Details``() = task {
    use ctx = openContext()

    let query =
        select {
            for o in main.SalesOrderHeader do
            join d in main.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            where (o.OnlineOrderFlag = 0L)
            select (o, d)
        }

    let! results = query |> ctx.ReadAsync HydraReader.Read
    gt0 results
}

[<Test>]
let ``Where subqueryOne``() = task {
    use ctx = openContext()

    let avgListPrice = 
        select {
            for p in main.Product do
            select (avgBy p.ListPrice)
        } 

    let! productsWithAboveAveragePrice =
        select {
            for p in main.Product do
            where (p.ListPrice > subqueryOne avgListPrice)
            select (p.Name, p.ListPrice)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 productsWithAboveAveragePrice
}

[<Test>]
let ``InsertGetId Test``() = task {
    use ctx = openContext()

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

    let errorLogId = 
        insert {
            for e in main.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }
        |> ctx.Insert

    Assert.IsTrue(errorLogId > 0L, "Expected returned ID to be > 0")
}

[<Test>]
let ``InsertGetIdAsync Test``() = task {
    use ctx = openContext()

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
        insert {
            for e in main.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }
        |> ctx.InsertAsync

    result >! 0L
}

[<Test>]
let ``Update Set Individual Fields``() = task {
    use ctx = openContext()

    let result = 
        update {
            for e in main.ErrorLog do
            set e.ErrorNumber 123L
            set e.ErrorMessage "ERROR #123"
            set e.ErrorLine (Some 999L)
            set e.ErrorProcedure None
            where (e.ErrorLogID = 1L)
        }
        |> ctx.Update

    printfn "result: %i" result
}

[<Test>]
let ``UpdateAsync Set Individual Fields``() = task {
    use ctx = openContext()

    let! result = 
        update {
            for e in main.ErrorLog do
            set e.ErrorNumber 123L
            set e.ErrorMessage "ERROR #123"
            set e.ErrorLine (Some 999L)
            set e.ErrorProcedure None
            where (e.ErrorLogID = 1L)
        }
        |> ctx.UpdateAsync

    printfn "result: %i" result
}

[<Test>]
let ``Update Entity``() = task {
    use ctx = openContext()

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

    let result = 
        update {
            for e in main.ErrorLog do
            entity errorLog
            excludeColumn e.ErrorLogID
            where (e.ErrorLogID = errorLog.ErrorLogID)
        }
        |> ctx.Update

    printfn "result: %i" result
}

[<Test>]
let ``Delete Test``() = task {
    use ctx = openContext()

    let result = 
        delete {
            for e in main.ErrorLog do
            where (e.ErrorLogID = 5L)
        }
        |> ctx.Delete

    printfn "result: %i" result
}

[<Test>]
let ``DeleteAsync Test``() = task {
    use ctx = openContext()

    let! result = 
        delete {
            for e in main.ErrorLog do
            where (e.ErrorLogID = 5L)
        }
        |> ctx.DeleteAsync

    printfn "result: %i" result
}

[<Test>]
let ``Multiple Inserts``() = task {
    use ctx = openContext()

    ctx.BeginTransaction()

    let! _ = 
        deleteTask ctx {
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
            |> ctx.InsertAsync

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

    | None -> 
        ()

    let! results =
        select {
            for e in main.ErrorLog do
            select e.ErrorNumber
        }
        |> ctx.ReadAsync HydraReader.Read

    let errorNumbers = results |> Seq.toList
    
    errorNumbers =! [ 400L; 401L; 402L ]

    ctx.RollbackTransaction()
}

[<Test>]
let ``Distinct Test``() = task {
    use ctx = openContext()

    ctx.BeginTransaction()

    let! _ = 
        deleteTask ctx {
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
            |> ctx.InsertAsync

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

    | None -> 
        ()

    let! results =
        selectTask HydraReader.Read ctx {
            for e in main.ErrorLog do
            select e.ErrorNumber
        }

    let! distinctResults =
        selectTask HydraReader.Read ctx {
            for e in main.ErrorLog do
            select e.ErrorNumber
            distinct
        }

    results |> Seq.length =! 3
    distinctResults |> Seq.length =! 1

    ctx.RollbackTransaction()
}

[<Test>]
let ``Count Test``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    for i in [0..2] do
        let! result = 
            insert {
                for e in main.ErrorLog do
                entity stubbedErrorLog
                getId e.ErrorLogID
            }
            |> ctx.InsertAsync
        ()

    let! count = 
        select {
            for e in main.ErrorLog do
            count
        }
        |> ctx.CountAsync

    count >! 0
    ctx.RollbackTransaction()
}

[<Test>]
let ``OnConflictDoUpdate``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    let upsertAddress address = 
        insertTask ctx {
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
        selectTask HydraReader.Read ctx {
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

    ctx.RollbackTransaction()
}


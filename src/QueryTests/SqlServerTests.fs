module SqlServerTests

open SqlHydra.Query
open SqlUtils
open AdventureWorks
open System.Data.SqlClient
open NUnit.Framework
open System.Collections.Generic
open FSharp.Control.Tasks.V2
open SalesLT

// Tables
let customerTable =         table<SalesLT.Customer>         |> inSchema (nameof SalesLT)
let customerAddressTable =  table<SalesLT.CustomerAddress>  |> inSchema (nameof SalesLT)
let addressTable =          table<SalesLT.Address>          |> inSchema (nameof SalesLT)
let productTable =          table<SalesLT.Product>          |> inSchema (nameof SalesLT)
let categoryTable =         table<SalesLT.ProductCategory>  |> inSchema (nameof SalesLT)
let errorLogTable =         table<dbo.ErrorLog>

let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)

[<Test>]
let ``Where Like``() = 
    use ctx = openContext()

    let addresses =
        select {
            for a in addressTable do
            where (a.City =% "S%")
        }
        |> ctx.Read HydraReader.Read

    printfn "Results: %A" addresses

[<Test>]
let ``Where City Starts With S``() = 
    use ctx = openContext()

    let cities =
        select {
            for a in addressTable do
            where (a.City =% "S%")
        }
        |> ctx.Read HydraReader.Read
        |> List.map (fun address -> $"City: {address.City}, {address.StateProvince}")

    printfn "Results: %A" cities

[<Test>]
let ``Customers left join Addresses``() = task {
    use ctx = openContext()

    let query =
        select {
            for c in customerTable do
            leftJoin ca in customerAddressTable on (c.CustomerID = ca.Value.CustomerID)
            leftJoin a  in addressTable on (ca.Value.AddressID = a.Value.AddressID)
            where (c.CustomerID |=| [1;2;30018;29545]) // two without address, two with address
            orderBy c.CustomerID
            select (c, a)
        }

    query |> toSql |> printfn "%s"

    let! customersWithAddresses = query |> ctx.ReadAsync HydraReader.Read
    printfn "Record Count: %i" customersWithAddresses.Length

    customersWithAddresses
    |> printfn "Results: %A"
}

[<Test>]
let ``Product with Category Name``() = task {
    use ctx = openContext()

    let query = 
        select {
            for p in productTable do
            join c in categoryTable on (p.ProductCategoryID.Value = c.ProductCategoryID)
            select (c.Name, p)
            take 10
        }

    let! rows = query |> ctx.ReadAsync HydraReader.Read
    printfn "Results: %A" rows
    query |> toSql |> printfn "%s"
}


[<Test>]
let ``Customers inner join Addresses``() = task {
    use ctx = openContext()

    let query =
        select {
            for c in customerTable do
            join ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
            join a  in addressTable on (ca.AddressID = a.AddressID)
            where (c.CustomerID |=| [30018;29545;29954;29897;29503;29559])
            orderBy c.CustomerID
            select (c,a)
        }
        
    query |> toSql |> printfn "%s"

    let! customersWithAddresses = 
        query 
        |> ctx.ReadAsync HydraReader.Read
        //|> ctx.ReadAsync (fun reader -> 
        //    let hydra = HydraReader(reader)
        //    fun () -> hydra.Customer.Read(), hydra.Address.Read()
        //)

    printfn "Results: %A" customersWithAddresses
}

[<Test>]
let ``InsertGetId Test``() =
    use ctx = openContext()

    let errorLog = 
        {
            dbo.ErrorLog.ErrorLogID = 0 // Exclude
            dbo.ErrorLog.ErrorTime = System.DateTime.Now
            dbo.ErrorLog.ErrorLine = None
            dbo.ErrorLog.ErrorMessage = "TEST"
            dbo.ErrorLog.ErrorNumber = 400
            dbo.ErrorLog.ErrorProcedure = (Some "Procedure 400")
            dbo.ErrorLog.ErrorSeverity = None
            dbo.ErrorLog.ErrorState = None
            dbo.ErrorLog.UserName = "jmarr"
        }

    let result : int = 
        insert {
            for e in errorLogTable do
            entity errorLog
            excludeColumn e.ErrorLogID
        }
        |> ctx.InsertGetId

    printfn "Identity: %i" result

[<Test>]
let ``InsertGetIdAsync Test``() = task {
    use ctx = openContext()

    let errorLog = 
        {
            dbo.ErrorLog.ErrorLogID = 0 // Exclude
            dbo.ErrorLog.ErrorTime = System.DateTime.Now
            dbo.ErrorLog.ErrorLine = None
            dbo.ErrorLog.ErrorMessage = "TEST INSERT ASYNC"
            dbo.ErrorLog.ErrorNumber = 400
            dbo.ErrorLog.ErrorProcedure = (Some "Procedure 400")
            dbo.ErrorLog.ErrorSeverity = None
            dbo.ErrorLog.ErrorState = None
            dbo.ErrorLog.UserName = "jmarr"
        }

    let! result = 
        insert {
            for e in errorLogTable do
            entity errorLog
            excludeColumn e.ErrorLogID
        }
        |> ctx.InsertGetIdAsync

    printfn "Identity: %i" result
}

[<Test>]
let ``Update Set Individual Fields``() =
    use ctx = openContext()

    let result = 
        update {
            for e in errorLogTable do
            set e.ErrorNumber 123
            set e.ErrorMessage "ERROR #123"
            set e.ErrorLine (Some 999)
            set e.ErrorProcedure None
            where (e.ErrorLogID = 1)
        }
        |> ctx.Update

    printfn "result: %i" result

[<Test>]
let ``UpdateAsync Set Individual Fields``() = task {
    use ctx = openContext()

    let! result = 
        update {
            for e in errorLogTable do
            set e.ErrorNumber 123
            set e.ErrorMessage "ERROR #123"
            set e.ErrorLine (Some 999)
            set e.ErrorProcedure None
            where (e.ErrorLogID = 1)
        }
        |> ctx.UpdateAsync

    printfn "result: %i" result
}

[<Test>]
let ``Update Entity``() =
    use ctx = openContext()

    let errorLog = 
        {
            dbo.ErrorLog.ErrorLogID = 2
            dbo.ErrorLog.ErrorTime = System.DateTime.Now
            dbo.ErrorLog.ErrorLine = Some 888
            dbo.ErrorLog.ErrorMessage = "ERROR #2"
            dbo.ErrorLog.ErrorNumber = 500
            dbo.ErrorLog.ErrorProcedure = None
            dbo.ErrorLog.ErrorSeverity = None
            dbo.ErrorLog.ErrorState = None
            dbo.ErrorLog.UserName = "jmarr"
        }

    let result = 
        update {
            for e in errorLogTable do
            entity errorLog
            excludeColumn e.ErrorLogID
            where (e.ErrorLogID = errorLog.ErrorLogID)
        }
        |> ctx.Update

    printfn "result: %i" result

[<Test>]
let ``Delete Test``() =
    use ctx = openContext()

    let result = 
        delete {
            for e in errorLogTable do
            where (e.ErrorLogID = 5)
        }
        |> ctx.Delete

    printfn "result: %i" result

[<Test>]
let ``DeleteAsync Test``() = task {
    use ctx = openContext()

    let! result = 
        delete {
            for e in errorLogTable do
            where (e.ErrorLogID = 5)
        }
        |> ctx.DeleteAsync

    printfn "result: %i" result
}

let stubbedErrorLog = 
    {
        dbo.ErrorLog.ErrorLogID = 0 // Exclude
        dbo.ErrorLog.ErrorTime = System.DateTime.Now
        dbo.ErrorLog.ErrorLine = None
        dbo.ErrorLog.ErrorMessage = "TEST INSERT ASYNC"
        dbo.ErrorLog.ErrorNumber = 400
        dbo.ErrorLog.ErrorProcedure = (Some "Procedure 400")
        dbo.ErrorLog.ErrorSeverity = None
        dbo.ErrorLog.ErrorState = None
        dbo.ErrorLog.UserName = "jmarr"
    }

[<Test>]
let ``Distinct Test``() = task {
    use ctx = openContext()

    ctx.BeginTransaction()

    for i in [0..2] do
        let! result = 
            insert {
                for e in errorLogTable do
                entity stubbedErrorLog
                excludeColumn e.ErrorLogID
            }
            |> ctx.InsertGetIdAsync

        printfn "Identity: %i" result

    let! results =
        select {
            for e in errorLogTable do
            select (e.ErrorNumber)
        }
        |> ctx.ReadAsync (fun reader () ->
            reader.[0] :?> int
        )

    let! distinctResults =
        select {
            for e in errorLogTable do
            select (e.ErrorNumber)
            distinct
        }
        |> ctx.ReadAsync (fun reader () ->
            reader.[0] :?> int
        )

    printfn $"results: {results}; distinctResults: {distinctResults}"

    Assert.Greater(results.Length, distinctResults.Length, "`results` should be > `distinctResults`")

    ctx.RollbackTransaction()
}

[<Test>]
let ``Count Test``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    for i in [0..2] do
        let! result = 
            insert {
                for e in errorLogTable do
                entity stubbedErrorLog
                excludeColumn e.ErrorLogID
            }
            |> ctx.InsertGetIdAsync
        ()

    let! count = 
        select {
            for e in errorLogTable do
            count
        }
        |> ctx.CountAsync

    printfn "Count: %i" count
    Assert.Positive count

    ctx.RollbackTransaction()
}
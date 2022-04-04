module Sqlite.QueryIntegrationTests

open SqlHydra.Query
open Expecto
open DB
open SqlHydra.Query.SqliteExtensions
open Swensen.Unquote
#if NET5_0
open Sqlite.AdventureWorksNet5
#endif
#if NET6_0
open Sqlite.AdventureWorksNet6
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.SqliteCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)

// Tables
let addressTable =          table<main.Address>
let customerTable =         table<main.Customer>
let orderHeaderTable =      table<main.SalesOrderHeader>
let orderDetailTable =      table<main.SalesOrderDetail>
let productTable =          table<main.Product>
let categoryTable =         table<main.ProductCategory>
let errorLogTable =         table<main.ErrorLog>

[<Tests>]
let tests = 
    categoryList "Sqlite" "Query Integration Tests" [

        testTask "Where City Starts With S" {
            use ctx = openContext()
            
            let addresses =
                select {
                    for a in addressTable do
                    where (a.City |=| [ "Seattle"; "Santa Cruz" ])
                }
                |> ctx.Read HydraReader.Read

            gt0 addresses
            Expect.isTrue (addresses |> Seq.forall (fun a -> a.City = "Seattle" || a.City = "Santa Cruz")) "Expected only 'Seattle' or 'Santa Cruz'."
        }

        testTask "Select City Column Where City Starts with S" {
            use ctx = openContext()

            let cities =
                select {
                    for a in addressTable do
                    where (a.City =% "S%")
                    select a.City
                }
                |> ctx.Read HydraReader.Read

            gt0 cities
            Expect.isTrue (cities |> Seq.forall (fun city -> city.StartsWith "S")) "Expected all cities to start with 'S'."
        }

        testTask "Inner Join Orders-Details" {
            use ctx = openContext()

            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
                    where (o.OnlineOrderFlag = 0L)
                    select (o, d)
                }

            //query.ToKataQuery() |> toSql |> printfn "%s"

            let! results = query |> ctx.ReadAsync HydraReader.Read
            gt0 results
        }

        //testTask "Product with Category Name" {
        //    use ctx = openContext()

        //    let query = 
        //        select {
        //            for p in productTable do
        //            join sc in subCategoryTable on (p.ProductSubcategoryID = Some sc.ProductSubcategoryID)
        //            join c in categoryTable on (sc.ProductCategoryID = c.ProductCategoryID)
        //            select (c.Name, p)
        //            take 5
        //        }

        //    let! rows = query |> ctx.ReadAsync HydraReader.Read
        //    printfn "Results: %A" rows
        //    query.ToKataQuery() |> toSql |> printfn "%s"
        //    gt0 rows
        //}

        //testTask "Select Column Aggregates From Product IDs 1-3" {
        //    use ctx = openContext()

        //    let query =
        //        select {
        //            for p in productTable do
        //            where (p.ProductSubcategoryID <> None)
        //            groupBy p.ProductSubcategoryID
        //            where (p.ProductSubcategoryID.Value |=| [ 1; 2; 3 ])
        //            select (p.ProductSubcategoryID, minBy p.ListPrice, maxBy p.ListPrice, avgBy p.ListPrice, countBy p.ListPrice, sumBy p.ListPrice)
        //        }

        //    let! aggregates = query |> ctx.ReadAsync HydraReader.Read
        //    query.ToKataQuery() |> toSql |> printfn "%s"

        //    gt0 aggregates
            
        //    let aggByCatID = 
        //        aggregates 
        //        |> Seq.map (fun (catId, minPrice, maxPrice, avgPrice, priceCount, sumPrice) -> catId, (minPrice, maxPrice, avgPrice, priceCount, sumPrice)) 
        //        |> Map.ofSeq
        //    Expect.equal (539.99M, 3399.99M, 1683.365M, 32, 53867.6800M) aggByCatID.[Some 1] "Expected CatID: 1 aggregates to match."
        //    Expect.equal (539.99M, 3578.2700M, 1597.4500M, 43, 68690.3500M) aggByCatID.[Some 2] "Expected CatID: 2 aggregates to match."
        //    Expect.equal (742.3500M, 2384.0700M, 1425.2481M, 22, 31355.4600M) aggByCatID.[Some 3] "Expected CatID: 3 aggregates to match."
        //}

        //testTask "Aggregate Subquery One" {
        //    use ctx = openContext()

        //    let avgListPrice = 
        //        select {
        //            for p in productTable do
        //            select (avgBy p.ListPrice)
        //        }

        //    let! productsWithHigherThanAvgPrice = 
        //        select {
        //            for p in productTable do
        //            where (p.ListPrice > subqueryOne avgListPrice)
        //            orderByDescending p.ListPrice
        //            select (p.Name, p.ListPrice)
        //        }
        //        |> ctx.ReadAsync HydraReader.Read

        //    let avgListPrice = 438.6662M
            
        //    gt0 productsWithHigherThanAvgPrice
        //    Expect.isTrue (productsWithHigherThanAvgPrice |> Seq.forall (fun (nm, price) -> price > avgListPrice)) "Expected all prices to be > than avg price of $438.67."
        //}

        //testTask "Select Column Aggregates" {
        //    use ctx = openContext()

        //    let! aggregates = 
        //        select {
        //            for p in productTable do
        //            where (p.ProductSubcategoryID <> None)
        //            groupBy p.ProductSubcategoryID
        //            having (minBy p.ListPrice > 50M && maxBy p.ListPrice < 1000M)
        //            select (p.ProductSubcategoryID, minBy p.ListPrice, maxBy p.ListPrice)
        //        }
        //        |> ctx.ReadAsync HydraReader.Read

        //    gt0 aggregates
        //}

        //testTask "Sorted Aggregates - Top 5 categories with highest avg price products" {
        //    use ctx = openContext()

        //    let! aggregates = 
        //        select {
        //            for p in productTable do
        //            where (p.ProductSubcategoryID <> None)
        //            groupBy p.ProductSubcategoryID
        //            orderByDescending (avgBy p.ListPrice)
        //            select (p.ProductSubcategoryID, avgBy p.ListPrice)
        //            take 5
        //        }
        //        |> ctx.ReadAsync HydraReader.Read

        //    gt0 aggregates
        //}

        //testTask "Where subqueryMany" {
        //    use ctx = openContext()

        //    let top5CategoryIdsWithHighestAvgPrices = 
        //        select {
        //            for p in productTable do
        //            where (p.ProductSubcategoryID <> None)
        //            groupBy p.ProductSubcategoryID
        //            orderByDescending (avgBy p.ListPrice)
        //            select (p.ProductSubcategoryID)
        //            take 5
        //        }

        //    let! top5Categories =
        //        select {
        //            for c in categoryTable do
        //            where (Some c.ProductCategoryID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
        //            select c.Name
        //        }
        //        |> ctx.ReadAsync HydraReader.Read

        //    gt0 top5Categories
        //}

        testTask "Where subqueryOne" {
            use ctx = openContext()

            let avgListPrice = 
                select {
                    for p in productTable do
                    select (avgBy p.ListPrice)
                } 

            let! productsWithAboveAveragePrice =
                select {
                    for p in productTable do
                    where (p.ListPrice > subqueryOne avgListPrice)
                    select (p.Name, p.ListPrice)
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 productsWithAboveAveragePrice
        }

        //testTask "Select Columns with Option" {
        //    use ctx = openContext()

        //    let! values = 
        //        select {
        //            for p in productTable do
        //            where (p.ProductSubcategoryID <> None)
        //            select (p.ProductSubcategoryID, p.ListPrice)
        //        }
        //        |> ctx.ReadAsync HydraReader.Read

        //    gt0 values
        //    Expect.isTrue (values |> Seq.forall (fun (catId, price) -> catId <> None)) "Expected subcategories to all have a value."
        //}

        testTask "InsertGetId Test" {
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
                    for e in errorLogTable do
                    entity errorLog
                    getId e.ErrorLogID
                }
                |> ctx.Insert

            //printfn "Identity: %i" errorLogId
            Expect.isTrue (errorLogId > 0L) "Expected returned ID to be > 0"
        }

        testTask "InsertGetIdAsync Test" {
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
                    for e in errorLogTable do
                    entity errorLog
                    getId e.ErrorLogID
                }
                |> ctx.InsertAsync

            printfn "Identity: %i" result
        }

        testTask "Update Set Individual Fields" {
            use ctx = openContext()

            let result = 
                update {
                    for e in errorLogTable do
                    set e.ErrorNumber 123L
                    set e.ErrorMessage "ERROR #123"
                    set e.ErrorLine (Some 999L)
                    set e.ErrorProcedure None
                    where (e.ErrorLogID = 1L)
                }
                |> ctx.Update

            printfn "result: %i" result
        }

        testTask "UpdateAsync Set Individual Fields" {
            use ctx = openContext()

            let! result = 
                update {
                    for e in errorLogTable do
                    set e.ErrorNumber 123L
                    set e.ErrorMessage "ERROR #123"
                    set e.ErrorLine (Some 999L)
                    set e.ErrorProcedure None
                    where (e.ErrorLogID = 1L)
                }
                |> ctx.UpdateAsync

            printfn "result: %i" result
        }

        testTask "Update Entity" {
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
                    for e in errorLogTable do
                    entity errorLog
                    excludeColumn e.ErrorLogID
                    where (e.ErrorLogID = errorLog.ErrorLogID)
                }
                |> ctx.Update

            printfn "result: %i" result
        }

        testTask "Delete Test" {
            use ctx = openContext()

            let result = 
                delete {
                    for e in errorLogTable do
                    where (e.ErrorLogID = 5L)
                }
                |> ctx.Delete

            printfn "result: %i" result
        }

        testTask "DeleteAsync Test" {
            use ctx = openContext()

            let! result = 
                delete {
                    for e in errorLogTable do
                    where (e.ErrorLogID = 5L)
                }
                |> ctx.DeleteAsync

            printfn "result: %i" result
        }

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

        testTask "Multiple Inserts" {
            use ctx = openContext()

            ctx.BeginTransaction()

            let! _ = 
                delete {
                    for e in errorLogTable do
                    deleteAll
                }
                |> ctx.DeleteAsync

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
                        for e in errorLogTable do
                        entities errorLogs
                        excludeColumn e.ErrorLogID
                    }
                    |> ctx.InsertAsync

                Expect.equal rowsInserted 3 "Expected 3 rows to be inserted"

            | None -> ()

            let! results =
                select {
                    for e in errorLogTable do
                    select e.ErrorNumber
                }
                |> ctx.ReadAsync HydraReader.Read

            let errorNumbers = results |> Seq.toList
            
            Expect.equal errorNumbers [ 400L; 401L; 402L ] ""

            ctx.RollbackTransaction()
        }

        testTask "Distinct Test" {
            use ctx = openContext()

            ctx.BeginTransaction()

            let! _ = 
                deleteTask (Shared ctx) {
                    for e in errorLogTable do
                    deleteAll
                }

            let errorLogs = 
                [ 0L .. 2L ] 
                |> List.map (fun _ -> stubbedErrorLog)
                |> AtLeastOne.tryCreate
            
            match errorLogs with
            | Some errorLogs -> 
                let! rowsInserted = 
                    insertTask (Shared ctx) {
                        for e in errorLogTable do
                        entities errorLogs
                        excludeColumn e.ErrorLogID
                    }

                Expect.equal rowsInserted 3 "Expected 3 rows to be inserted"
            | None -> ()

            let! results =
                selectTask HydraReader.Read (Shared ctx) {
                    for e in errorLogTable do
                    select e.ErrorNumber
                }

            let! distinctResults =
                selectTask HydraReader.Read (Shared ctx) {
                    for e in errorLogTable do
                    select e.ErrorNumber
                    distinct
                }

            Expect.equal (results |> Seq.length) 3 ""
            Expect.equal (distinctResults |> Seq.length) 1 ""

            ctx.RollbackTransaction()
        }

        testTask "Count Test" {
            use ctx = openContext()
            ctx.BeginTransaction()

            for i in [0..2] do
                let! result = 
                    insert {
                        for e in errorLogTable do
                        entity stubbedErrorLog
                        getId e.ErrorLogID
                    }
                    |> ctx.InsertAsync
                ()

            let! count = 
                select {
                    for e in errorLogTable do
                    count
                }
                |> ctx.CountAsync

            printfn "Count: %i" count
            Expect.isTrue (count > 0) ""

            ctx.RollbackTransaction()
        }

        testTask "OnConflictDoUpdate" {
            use ctx = openContext()
            ctx.BeginTransaction()

            let upsertAddress address = 
                insertTask (Shared ctx) {
                    for a in addressTable do
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
                }

            let queryAddress id = 
                selectTask HydraReader.Read (Shared ctx) {
                    for a in addressTable do
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

    ]

﻿module SqlServer.QueryIntegrationTests

open SqlHydra.Query
open Expecto
open DB

#if NET5_0
open SqlServer.AdventureWorksNet5
#endif
#if NET6_0_OR_GREATER
open SqlServer.AdventureWorksNet6
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)

// Tables
let personTable =             table<Person.Person>                    |> inSchema (nameof Person)
let addressTable =            table<Person.Address>                   |> inSchema (nameof Person)
let customerTable =           table<Sales.Customer>                   |> inSchema (nameof Sales)
let orderHeaderTable =        table<Sales.SalesOrderHeader>           |> inSchema (nameof Sales)
let orderDetailTable =        table<Sales.SalesOrderDetail>           |> inSchema (nameof Sales)
let productTable =            table<Production.Product>               |> inSchema (nameof Production)
let subCategoryTable =        table<Production.ProductSubcategory>    |> inSchema (nameof Production)
let categoryTable =           table<Production.ProductCategory>       |> inSchema (nameof Production)
let errorLogTable =           table<dbo.ErrorLog>
let employeeTable =           table<HumanResources.Employee>          |> inSchema (nameof HumanResources)
let shiftTable =              table<HumanResources.Shift>             |> inSchema (nameof HumanResources)
let dateTime2SupportTable =   table<ext.DateTime2Support>             |> inSchema (nameof ext)

[<Tests>]
let tests = 
    categoryList "SqlServer" "Query Integration Tests" [

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
                    where (o.OnlineOrderFlag = true)
                    select (o, d)
                }

            //query.ToKataQuery() |> toSql |> printfn "%s"

            let! results = query |> ctx.ReadAsync HydraReader.Read
            gt0 results
        }

        testTask "Product with Category Name" {
            use ctx = openContext()

            let query = 
                select {
                    for p in productTable do
                    join sc in subCategoryTable on (p.ProductSubcategoryID = Some sc.ProductSubcategoryID)
                    join c in categoryTable on (sc.ProductCategoryID = c.ProductCategoryID)
                    select (c.Name, p)
                    take 5
                }

            let! rows = query |> ctx.ReadAsync HydraReader.Read
            //printfn "Results: %A" rows
            //query.ToKataQuery() |> toSql |> printfn "%s"
            gt0 rows
        }

        testTask "Select Column Aggregates From Product IDs 1-3" {
            use ctx = openContext()

            let query =
                select {
                    for p in productTable do
                    where (p.ProductSubcategoryID <> None)
                    groupBy p.ProductSubcategoryID
                    where (p.ProductSubcategoryID.Value |=| [ 1; 2; 3 ])
                    select (p.ProductSubcategoryID, minBy p.ListPrice, maxBy p.ListPrice, avgBy p.ListPrice, countBy p.ListPrice, sumBy p.ListPrice)
                }

            let! aggregates = query |> ctx.ReadAsync HydraReader.Read
            //query.ToKataQuery() |> toSql |> printfn "%s"

            gt0 aggregates
            
            let aggByCatID = 
                aggregates 
                |> Seq.map (fun (catId, minPrice, maxPrice, avgPrice, priceCount, sumPrice) -> catId, (minPrice, maxPrice, avgPrice, priceCount, sumPrice)) 
                |> Map.ofSeq
            Expect.equal (539.99M, 3399.99M, 1683.365M, 32, 53867.6800M) aggByCatID.[Some 1] "Expected CatID: 1 aggregates to match."
            Expect.equal (539.99M, 3578.2700M, 1597.4500M, 43, 68690.3500M) aggByCatID.[Some 2] "Expected CatID: 2 aggregates to match."
            Expect.equal (742.3500M, 2384.0700M, 1425.2481M, 22, 31355.4600M) aggByCatID.[Some 3] "Expected CatID: 3 aggregates to match."
        }

        testTask "Aggregate Subquery One" {
            use ctx = openContext()

            let avgListPrice = 
                select {
                    for p in productTable do
                    select (avgBy p.ListPrice)
                }

            let! productsWithHigherThanAvgPrice = 
                select {
                    for p in productTable do
                    where (p.ListPrice > subqueryOne avgListPrice)
                    orderByDescending p.ListPrice
                    select (p.Name, p.ListPrice)
                }
                |> ctx.ReadAsync HydraReader.Read

            let avgListPrice = 438.6662M
            
            gt0 productsWithHigherThanAvgPrice
            Expect.isTrue (productsWithHigherThanAvgPrice |> Seq.forall (fun (nm, price) -> price > avgListPrice)) "Expected all prices to be > than avg price of $438.67."
        }

        testTask "Select Column Aggregates" {
            use ctx = openContext()

            let! aggregates = 
                select {
                    for p in productTable do
                    where (p.ProductSubcategoryID <> None)
                    groupBy p.ProductSubcategoryID
                    having (minBy p.ListPrice > 50M && maxBy p.ListPrice < 1000M)
                    select (p.ProductSubcategoryID, minBy p.ListPrice, maxBy p.ListPrice)
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 aggregates
        }

        testTask "Sorted Aggregates - Top 5 categories with highest avg price products" {
            use ctx = openContext()

            let! aggregates = 
                select {
                    for p in productTable do
                    where (p.ProductSubcategoryID <> None)
                    groupBy p.ProductSubcategoryID
                    orderByDescending (avgBy p.ListPrice)
                    select (p.ProductSubcategoryID, avgBy p.ListPrice)
                    take 5
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 aggregates
        }

        testTask "Where subqueryMany" {
            use ctx = openContext()

            let top5CategoryIdsWithHighestAvgPrices = 
                select {
                    for p in productTable do
                    where (p.ProductSubcategoryID <> None)
                    groupBy p.ProductSubcategoryID
                    orderByDescending (avgBy p.ListPrice)
                    select (p.ProductSubcategoryID)
                    take 5
                }

            let! top5Categories =
                select {
                    for c in categoryTable do
                    where (Some c.ProductCategoryID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
                    select c.Name
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 top5Categories
        }

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

        testTask "Select Columns with Option" {
            use ctx = openContext()

            let! values = 
                select {
                    for p in productTable do
                    where (p.ProductSubcategoryID <> None)
                    select (p.ProductSubcategoryID, p.ListPrice)
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 values
            Expect.isTrue (values |> Seq.forall (fun (catId, price) -> catId <> None)) "Expected subcategories to all have a value."
        }

        testTask "InsertGetId Test" {
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

            let! errorLogId = 
                insertTask (Shared ctx) {
                    for e in errorLogTable do
                    entity errorLog
                    getId e.ErrorLogID
                }

            printfn "Identity: %i" errorLogId
            Expect.isTrue (errorLogId > 0) "Expected returned ID to be > 0"
        }

        testTask "InsertGetIdAsync Test" {
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
                insertTask (Shared ctx) {
                    for e in errorLogTable do
                    entity errorLog
                    getId e.ErrorLogID
                }

            printfn "Identity: %i" result
        }

        testTask "Update Set Individual Fields" {
            use ctx = openContext()

            let! result = 
                updateTask (Shared ctx) {
                    for e in errorLogTable do
                    set e.ErrorNumber 123
                    set e.ErrorMessage "ERROR #123"
                    set e.ErrorLine (Some 999)
                    set e.ErrorProcedure None
                    where (e.ErrorLogID = 1)
                }

            Expect.isTrue (result > 0) ""
        }

        testTask "UpdateAsync Set Individual Fields" {
            use ctx = openContext()

            let! result = 
                updateTask (Shared ctx) {
                    for e in errorLogTable do
                    set e.ErrorNumber 123
                    set e.ErrorMessage "ERROR #123"
                    set e.ErrorLine (Some 999)
                    set e.ErrorProcedure None
                    where (e.ErrorLogID = 1)
                }

            printfn "result: %i" result
        }

        testTask "Update Entity" {
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

            let! result = 
                updateTask (Shared ctx) {
                    for e in errorLogTable do
                    entity errorLog
                    excludeColumn e.ErrorLogID
                    where (e.ErrorLogID = errorLog.ErrorLogID)
                }

            printfn "result: %i" result
        }

        testTask "Delete Test" {
            use ctx = openContext()

            let! result = 
                deleteTask (Shared ctx) {
                    for e in errorLogTable do
                    where (e.ErrorLogID = 5)
                }

            printfn "result: %i" result
        }

        testTask "DeleteAsync Test" {
            use ctx = openContext()

            let! result = 
                deleteTask (Shared ctx) {
                    for e in errorLogTable do
                    where (e.ErrorLogID = 5)
                }

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

        testTask "Multiple Inserts" {
            use ctx = openContext()

            ctx.BeginTransaction()

            let! _ = 
                deleteTask (Shared ctx) {
                    for e in errorLogTable do
                    deleteAll
                }

            let errorLogs = 
                [ 0 .. 2 ] 
                |> List.map (fun i -> 
                    { stubbedErrorLog with ErrorNumber = stubbedErrorLog.ErrorNumber + i }
                )
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
                select {
                    for e in errorLogTable do
                    select e.ErrorNumber
                }
                |> ctx.ReadAsync HydraReader.Read

            let errorNumbers = results |> Seq.toList
    
            Expect.equal errorNumbers [ 400; 401; 402 ] ""

            ctx.RollbackTransaction()
        }

        testAsync "Distinct Test" {
            use ctx = openContext()

            ctx.BeginTransaction()

            let! deletedCount = 
                deleteAsync (Shared ctx) {
                    for e in errorLogTable do
                    deleteAll
                } 
                                
            let errorLogs = 
                [ 0 .. 2 ] 
                |> List.map (fun _ -> stubbedErrorLog)
                |> AtLeastOne.tryCreate
                
            match errorLogs with
            | Some errorLogs ->            
                let! rowsInserted = 
                    insertAsync (Shared ctx) {
                        for e in errorLogTable do
                        entities errorLogs
                        excludeColumn e.ErrorLogID
                    }

                Expect.equal rowsInserted 3 "Expected 3 rows to be inserted"
            | None -> ()

            let! results =
                selectAsync HydraReader.Read (Shared ctx)  {
                    for e in errorLogTable do
                    select e.ErrorNumber
                }

            let! distinctResults =
                selectAsync HydraReader.Read (Shared ctx) {
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
                    insertTask (Shared ctx) {
                        for e in errorLogTable do
                        entity stubbedErrorLog
                        getId e.ErrorLogID
                    }
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

        testTask "Count Test Task" {
            use ctx = openContext()
            ctx.BeginTransaction()

            for i in [0..2] do
                let! result = 
                    insertTask (Shared ctx) {
                        for e in errorLogTable do
                        entity stubbedErrorLog
                        getId e.ErrorLogID
                    }
                ()

            let! count = 
                selectTask HydraReader.Read (Shared ctx) {
                    for e in errorLogTable do
                    count
                }

            printfn "Count: %i" count
            Expect.isTrue (count > 0) ""

            ctx.RollbackTransaction()
        }
        
        testAsync "Count Test Async" {
            use ctx = openContext()
            ctx.BeginTransaction()
        
            for i in [0..2] do
                let! result = 
                    insertAsync (Shared ctx) {
                        for e in errorLogTable do
                        entity stubbedErrorLog
                        getId e.ErrorLogID
                    }
                ()
        
            let! count = 
                selectAsync HydraReader.Read (Shared ctx) {
                    for e in errorLogTable do
                    count
                }
        
            printfn "Count: %i" count
            Expect.isTrue (count > 0) ""
        
            ctx.RollbackTransaction()
        }

        testTask "Query Employee Record with DateOnly" {
            use ctx = openContext()
            
#if NET6_0_OR_GREATER
            let maxBirthDate = System.DateOnly(2005, 1, 1)
#else
            let maxBirthDate = System.DateTime(2005, 1, 1)
#endif

            let employees =
                select {
                    for e in employeeTable do
                    where (e.BirthDate < maxBirthDate)
                    select e
                }
                |> ctx.Read HydraReader.Read

            gt0 employees
        }
        
        testTask "Query Employee Column with DateOnly" {
            use ctx = openContext()
            
#if NET6_0_OR_GREATER
            let maxBirthDate = System.DateOnly(2005, 1, 1)
#else
            let maxBirthDate = System.DateTime(2005, 1, 1)
#endif

            let employeeBirthDates =
                select {
                    for e in employeeTable do
                    where (e.BirthDate < maxBirthDate)
                    select e.BirthDate
                }
                |> ctx.Read HydraReader.Read

            gt0 employeeBirthDates
        }

        testTask "Query Shift with TimeOnly" {
            use ctx = openContext()
            
#if NET6_0_OR_GREATER
            let minStartTime = System.TimeOnly(9, 30)
#else
            let minStartTime = System.TimeSpan(9, 30, 0)
#endif

            let shiftsAfter930AM =
                select {
                    for s in shiftTable do
                    where (s.StartTime >= minStartTime)
                }
                |> ctx.Read HydraReader.Read

            // There are 3 shifts: day, evening and night. 
            // Results should contain 2 shifts: evening and night
            gt0 shiftsAfter930AM
        }

#if NET6_0_OR_GREATER
        testTask "Update Employee DateOnly" {
            use ctx = openContext()
            ctx.BeginTransaction()
            
            let! employees =
                selectTask HydraReader.Read (Shared ctx) {
                    for e in employeeTable do
                    select e
                }

            gt0 employees

            let emp : HumanResources.Employee = employees |> Seq.head
            let birthDate = System.DateOnly(1980, 1, 1)

            let! result = 
                updateTask (Shared ctx) {
                    for e in employeeTable do
                    set e.BirthDate birthDate
                    where (e.BusinessEntityID = emp.BusinessEntityID)
                }

            Expect.isTrue (result = 1) "Should update exactly one record."

            let! refreshedEmp = 
                selectTask HydraReader.Read (Shared ctx) {
                    for e in employeeTable do
                    where (e.BusinessEntityID = emp.BusinessEntityID)                    
                    tryHead
                }

            let actualBirthDate = 
                (refreshedEmp : HumanResources.Employee option)
                |> Option.map (fun e -> e.BirthDate)
            
            Expect.isTrue (actualBirthDate = Some birthDate) ""
            
            ctx.RollbackTransaction()
        }
#endif

        testTask "Insert, update, and select with both datetime and datetime2 precision" {
            use ctx = openContext ()
            ctx.BeginTransaction()
            
            let baseTimestamp = System.DateTime(2022,07,22, 11,50,28)
            let timestamp = System.DateTime(baseTimestamp.Ticks + 1234567L)
            // Simple insert of one entity
            let entity': ext.DateTime2Support =
                {
                    ID = 0
                    LessPrecision = timestamp
                    MorePrecision = timestamp
                }

            let! _ = 
                insert {
                    into dateTime2SupportTable 
                    entity entity'
                }
                |> ctx.InsertAsync

            let! retrievedBack = 
                selectTask HydraReader.Read (Shared ctx) {
                    for row in dateTime2SupportTable do
                    select row
                }

            Expect.equal [timestamp] [for (row: ext.DateTime2Support) in retrievedBack -> row.MorePrecision] "INSERT: Expected DATETIME2 to be stored with full precision"
            Expect.notEqual [timestamp] [for (row: ext.DateTime2Support) in retrievedBack -> row.LessPrecision] "INSERT: Expected a loss of precision when storing a DATETIME"

            let! fullPrecisionQuery = 
                selectTask HydraReader.Read (Shared ctx) { 
                    for row in dateTime2SupportTable do
                    where (row.MorePrecision = timestamp)
                    count
                }

            let! lessPrecisionQuery = 
                selectTask HydraReader.Read (Shared ctx) { 
                    for row in dateTime2SupportTable do
                    where (row.LessPrecision = timestamp)
                    count
                }

            Expect.equal fullPrecisionQuery 1 "SELECT: Expected precision of a DATETIME2 query parameter to match the precision in the database"
            Expect.equal lessPrecisionQuery 1 "SELECT: Expected precision of a DATETIME query parameter to match the precision in the database"

            let newTimestamp = System.DateTime(baseTimestamp.Ticks + 2345678L)

            let! _ = 
                updateTask (Shared ctx) {
                    for row in dateTime2SupportTable do
                    set row.MorePrecision newTimestamp
                    where (row.MorePrecision = timestamp)
                }

            let! _ = 
                updateTask (Shared ctx) {
                    for row in dateTime2SupportTable do
                    set row.LessPrecision newTimestamp
                    where (row.LessPrecision = timestamp)
                }

            let! retrievedBack = 
                selectTask HydraReader.Read (Shared ctx) {
                    for row in dateTime2SupportTable do
                    select row
                }

            Expect.equal [newTimestamp] [for (row: ext.DateTime2Support) in retrievedBack -> row.MorePrecision] "UPDATE: Expected DATETIME2 to be stored with full precision"
            Expect.notEqual [newTimestamp] [for (row: ext.DateTime2Support) in retrievedBack -> row.LessPrecision] "UPDATE: Expected a loss of precision when storing a DATETIME"
            
            ctx.RollbackTransaction ()
        }
    ]

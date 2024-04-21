module SqlServer.``Query Nullable Integration Tests``

open SqlHydra.Query
open DB
open System
open NUnit.Framework
open System.Threading.Tasks
open Swensen.Unquote
#if NET6_0
open SqlServer.AdventureWorksNullableNet6
#endif
#if NET8_0
open SqlServer.AdventureWorksNullableNet8
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)

let stubbedErrorLog = 
    {
        dbo.ErrorLog.ErrorLogID = 0 // Exclude
        dbo.ErrorLog.ErrorTime = System.DateTime.Now
        dbo.ErrorLog.ErrorLine = Nullable()
        dbo.ErrorLog.ErrorMessage = "TEST INSERT ASYNC"
        dbo.ErrorLog.ErrorNumber = 400
        dbo.ErrorLog.ErrorProcedure = "Procedure 400"
        dbo.ErrorLog.ErrorSeverity = Nullable()
        dbo.ErrorLog.ErrorState = Nullable()
        dbo.ErrorLog.UserName = "jmarr"
    }

[<Test>]
let ``Select Column Aggregates From Product IDs 1-3``() = task {
    use ctx = openContext()

    let query =
        select {
            for p in Production.Product do
            where (isNotNullValue p.ProductSubcategoryID)
            groupBy p.ProductSubcategoryID
            where (p.ProductSubcategoryID.Value |=| [ 1; 2; 3 ])
            select (p.ProductSubcategoryID, minBy p.ListPrice, maxBy p.ListPrice, avgBy p.ListPrice, countBy p.ListPrice, sumBy p.ListPrice)
        }

    let! aggregates = query |> ctx.ReadAsync HydraReader.Read

    gt0 aggregates
            
    let aggByCatID = 
        aggregates 
        |> Seq.map (fun (catId, minPrice, maxPrice, avgPrice, priceCount, sumPrice) -> catId.Value, (minPrice, maxPrice, avgPrice, priceCount, sumPrice)) 
        |> Map.ofSeq

    Assert.AreEqual((539.99M, 3399.99M, 1683.365M, 32, 53867.6800M), aggByCatID.[1], "Expected CatID: 1 aggregates to match.")
    Assert.AreEqual((539.99M, 3578.2700M, 1597.4500M, 43, 68690.3500M), aggByCatID.[2], "Expected CatID: 2 aggregates to match.")
    Assert.AreEqual((742.3500M, 2384.0700M, 1425.2481M, 22, 31355.4600M), aggByCatID.[3], "Expected CatID: 3 aggregates to match.")
}

[<Test>]
let ``Select Column Aggregates``() = task {
    use ctx = openContext()

    let! aggregates = 
        select {
            for p in Production.Product do
            where (isNotNullValue p.ProductSubcategoryID)
            groupBy p.ProductSubcategoryID
            having (minBy p.ListPrice > 50M && maxBy p.ListPrice < 1000M)
            select (p.ProductSubcategoryID, minBy p.ListPrice, maxBy p.ListPrice)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 aggregates
}

[<Test>]
let ``Sorted Aggregates - Top 5 categories with highest avg price products``() = task {
    use ctx = openContext()

    let query = 
        select {
                for p in Production.Product do
                where (p.ProductSubcategoryID.HasValue = true)
                groupBy p.ProductSubcategoryID
                orderByDescending (avgBy p.ListPrice)
                select (p.ProductSubcategoryID, avgBy p.ListPrice)
                take 5
        }

    let! aggregates = query |> ctx.ReadAsync HydraReader.Read

    gt0 aggregates
}

[<Test>]
let ``Where subqueryMany``() = task {
    use ctx = openContext()

    let top5CategoryIdsWithHighestAvgPrices = 
        select {
            for p in Production.Product do
            where (isNotNullValue p.ProductSubcategoryID)
            groupBy p.ProductSubcategoryID
            orderByDescending (avgBy p.ListPrice)
            select (p.ProductSubcategoryID)
            take 5
        }

    let! top5Categories =
        select {
            for c in Production.ProductCategory do
            where (Nullable c.ProductCategoryID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
            select c.Name
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 top5Categories
}

[<Test>]
let ``Select Columns with Option``() = task {
    use ctx = openContext()

    let! values = 
        select {
            for p in Production.Product do
            where (p.ProductSubcategoryID.HasValue)
            select (p.ProductSubcategoryID, p.ListPrice)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 values
    Assert.IsTrue(values |> Seq.forall (fun (catId, price) -> catId.HasValue), "Expected subcategories to all have a value.")
}

[<Test>]
let ``InsertGetId Test``() = task {
    use ctx = openContext()

    let errorLog = 
        {
            dbo.ErrorLog.ErrorLogID = 0 // Exclude
            dbo.ErrorLog.ErrorTime = System.DateTime.Now
            dbo.ErrorLog.ErrorLine = Nullable()
            dbo.ErrorLog.ErrorMessage = "TEST"
            dbo.ErrorLog.ErrorNumber = 400
            dbo.ErrorLog.ErrorProcedure = "Procedure 400"
            dbo.ErrorLog.ErrorSeverity = Nullable()
            dbo.ErrorLog.ErrorState = Nullable()
            dbo.ErrorLog.UserName = "jmarr"
        }

    let! errorLogId = 
        insertTask ctx {
            for e in dbo.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }

    errorLogId >! 0
}

[<Test>]
let ``InsertGetIdAsync Test``() = task {
    use ctx = openContext()

    let errorLog = 
        {
            dbo.ErrorLog.ErrorLogID = 0 // Exclude
            dbo.ErrorLog.ErrorTime = System.DateTime.Now
            dbo.ErrorLog.ErrorLine = Nullable()
            dbo.ErrorLog.ErrorMessage = "TEST INSERT ASYNC"
            dbo.ErrorLog.ErrorNumber = 400
            dbo.ErrorLog.ErrorProcedure = "Procedure 400"
            dbo.ErrorLog.ErrorSeverity = Nullable()
            dbo.ErrorLog.ErrorState = Nullable()
            dbo.ErrorLog.UserName = "jmarr"
        }

    let! result = 
        insertTask ctx {
            for e in dbo.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }

    result >! 0
}

[<Test>]
let ``Update Set Individual Fields``() = task {
    use ctx = openContext()
        
    let! row = 
        selectTask HydraReader.Read ctx {
            for e in dbo.ErrorLog do
            head
        }

    let! result = 
        updateTask ctx {
            for e in dbo.ErrorLog do
            set e.ErrorNumber 123
            set e.ErrorMessage "ERROR #123"
            set e.ErrorLine 999
            set e.ErrorProcedure null
            where (e.ErrorLogID = row.ErrorLogID)
        }

    result =! 1
}

[<Test>]
let ``UpdateAsync Set Individual Fields``() = task {
    use ctx = openContext()

    let! row = 
        selectTask HydraReader.Read ctx {
            for e in dbo.ErrorLog do
            head
        }

    let! result = 
        updateTask ctx {
            for e in dbo.ErrorLog do
            set e.ErrorNumber (row.ErrorNumber + 1)
            set e.ErrorProcedure null
            where (e.ErrorLogID = row.ErrorLogID)
        }

    result =! 1
}

[<Test>]
let ``Update Entity``() = task {
    use ctx = openContext()
        
    let! row = 
        selectTask HydraReader.Read ctx {
            for e in dbo.ErrorLog do
            head
        }

    row.ErrorTime <- System.DateTime.Now
    row.ErrorLine <- 888
    row.ErrorMessage <- "ERROR #2"
    row.ErrorNumber <- 500
    row.ErrorProcedure <- null
    row.ErrorSeverity <- Nullable()
    row.ErrorState <- Nullable()
    row.UserName <- "jmarr"

    let! result = 
        updateTask ctx {
            for e in dbo.ErrorLog do
            entity row
            excludeColumn e.ErrorLogID
            where (e.ErrorLogID = row.ErrorLogID)
        }

    result =! 1
}

    
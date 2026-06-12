module SqlServer.``Query Integration Tests``

open System
open System.IO
open SqlHydra.Query
open SqlHydra.Query.SqlServerExtensions
open DB
open NUnit.Framework
open System.Threading.Tasks
open Swensen.Unquote
#if NET8_0
open SqlServer.AdventureWorksNet8
#endif
#if NET9_0
open SqlServer.AdventureWorksNet9
#endif
#if NET10_0
open SqlServer.AdventureWorksNet10
#endif
open Microsoft.SqlServer.Types

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
let ``Where City Starts With S``() = task {
    let! addresses =
        selectTask db {
            for a in Person.Address do
            where (a.City |=| [ "Seattle"; "Santa Cruz" ])
        }

    gt0 addresses
    Assert.IsTrue(addresses |> Seq.forall (fun a -> a.City = "Seattle" || a.City = "Santa Cruz"), "Expected only 'Seattle' or 'Santa Cruz'.")
}

[<Test>]
let ``Select with Timeout``() = task {
    use! ctx = db.OpenContextAsync()
    let q =
        select {
            for a in Person.Address do
            where (a.AddressID = 1)
            timeout (System.TimeSpan.FromSeconds 45.0)
        }
    use cmd = ctx.BuildCommand(q.IR)
    cmd.CommandTimeout =! 45
}

[<Test>]
let ``Select City Column Where City Starts with S``() = task {
    let! cities =
        selectTask db {
            for a in Person.Address do
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
            for o in Sales.SalesOrderHeader do
            join d in Sales.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            where o.OnlineOrderFlag
            select (o, d)
        }

    gt0 results
}

[<Test>]
let ``Product with Category Name``() = task {
    let! rows =
        selectTask db {
            for p in Production.Product do
            join sc in Production.ProductSubcategory on (p.ProductSubcategoryID = Some sc.ProductSubcategoryID)
            join c in Production.ProductCategory on (sc.ProductCategoryID = c.ProductCategoryID)
            select (c.Name, p)
            take 5
        }

    gt0 rows
}

[<Test>]
let ``Select Column Aggregates From Product IDs 1-3``() = task {
    let! aggregates =
        selectTask db {
            for p in Production.Product do
            where (p.ProductSubcategoryID <> None)
            groupBy p.ProductSubcategoryID
            where (p.ProductSubcategoryID.Value |=| [ 1; 2; 3 ])
            select (p.ProductSubcategoryID, minBy p.ListPrice, maxBy p.ListPrice, avgBy p.ListPrice, countBy p.ListPrice, sumBy p.ListPrice)
        }

    gt0 aggregates
            
    let aggByCatID = 
        aggregates 
        |> Seq.map (fun (catId, minPrice, maxPrice, avgPrice, priceCount, sumPrice) -> catId, (minPrice, maxPrice, avgPrice, priceCount, sumPrice)) 
        |> Map.ofSeq

    Assert.AreEqual((539.99M, 3399.99M, 1683.365M, 32, 53867.6800M), aggByCatID.[Some 1], "Expected CatID: 1 aggregates to match.")
    Assert.AreEqual((539.99M, 3578.2700M, 1597.4500M, 43, 68690.3500M), aggByCatID.[Some 2], "Expected CatID: 2 aggregates to match.")
    Assert.AreEqual((742.3500M, 2384.0700M, 1425.2481M, 22, 31355.4600M), aggByCatID.[Some 3], "Expected CatID: 3 aggregates to match.")
}

[<Test>]
let ``Aggregate Subquery One``() = task {
    let avgListPrice =
        select {
            for p in Production.Product do
            select (avgBy p.ListPrice)
        }

    let! productsWithHigherThanAvgPrice =
        selectTask db {
            for p in Production.Product do
            where (p.ListPrice > subqueryOne avgListPrice)
            orderByDescending p.ListPrice
            select (p.Name, p.ListPrice)
        }

    let avgListPrice = 438.6662M
            
    gt0 productsWithHigherThanAvgPrice
    Assert.IsTrue(productsWithHigherThanAvgPrice |> Seq.forall (fun (nm, price) -> price > avgListPrice), "Expected all prices to be > than avg price of $438.67.")
}

[<Test>]
let ``Select Column Aggregates``() = task {
    let! aggregates =
        selectTask db {
            for p in Production.Product do
            where (p.ProductSubcategoryID <> None)
            groupBy p.ProductSubcategoryID
            having (minBy p.ListPrice > 50M && maxBy p.ListPrice < 1000M)
            select (p.ProductSubcategoryID, minBy p.ListPrice, maxBy p.ListPrice)
        }

    gt0 aggregates
}

[<Test>]
let ``Sorted Aggregates - Top 5 categories with highest avg price products``() = task {
    let! aggregates =
        selectTask db {
            for p in Production.Product do
            where (p.ProductSubcategoryID <> None)
            groupBy p.ProductSubcategoryID
            orderByDescending (avgBy p.ListPrice)
            select (p.ProductSubcategoryID, avgBy p.ListPrice)
            take 5
        }

    gt0 aggregates
}

[<Test>]
let ``Where subqueryMany``() = task {
    let top5CategoryIdsWithHighestAvgPrices =
        select {
            for p in Production.Product do
            where (p.ProductSubcategoryID <> None)
            groupBy p.ProductSubcategoryID
            orderByDescending (avgBy p.ListPrice)
            select (p.ProductSubcategoryID)
            take 5
        }

    let! top5Categories =
        selectTask db {
            for c in Production.ProductCategory do
            where (Some c.ProductCategoryID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
            select c.Name
        }

    gt0 top5Categories
}

[<Test>]
let ``Where subqueryOne``() = task {
    let avgListPrice =
        select {
            for p in Production.Product do
            select (avgBy p.ListPrice)
        }

    let! productsWithAboveAveragePrice =
        selectTask db {
            for p in Production.Product do
            where (p.ListPrice > subqueryOne avgListPrice)
            select (p.Name, p.ListPrice)
        }

    gt0 productsWithAboveAveragePrice
}

[<Test>]
let ``Select Columns with Option``() = task {
    let! values =
        selectTask db {
            for p in Production.Product do
            where (p.ProductSubcategoryID <> None)
            select (p.ProductSubcategoryID, p.ListPrice)
        }

    gt0 values
    Assert.IsTrue(values |> Seq.forall (fun (catId, price) -> catId <> None), "Expected subcategories to all have a value.")
}

[<Test>]
let ``Insert with Output``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

    let now = DateTime.Now

    let row = 
        {
            dbo.ErrorLog.ErrorLogID = 0 // Exclude
            dbo.ErrorLog.ErrorTime = now
            dbo.ErrorLog.ErrorLine = Some 5
            dbo.ErrorLog.ErrorMessage = "TEST INSERT ASYNC"
            dbo.ErrorLog.ErrorNumber = 400
            dbo.ErrorLog.ErrorProcedure = (Some "Procedure 400")
            dbo.ErrorLog.ErrorSeverity = None
            dbo.ErrorLog.ErrorState = None
            dbo.ErrorLog.UserName = "jmarr"
        }
    let! (errorLogId, errorTime, errorLine) =
        insertTask shared {
            for e in dbo.ErrorLog do
            entity row
            excludeColumn e.ErrorLogID
            output (e.ErrorLogID, e.ErrorTime, e.ErrorLine)
        }

    errorLogId >! 0
    (errorTime.Month, errorTime.Day, errorTime.Year) =! (now.Month, now.Day, now.Year)
    errorLine =! row.ErrorLine

    shared.RollbackTransaction()
}

[<Test>]
let ``Update with Output``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

    let! row = 
        selectAsync shared {
            for e in dbo.ErrorLog do
            head
        }

    let now = DateTime.Now

    let row = 
        { row with
            dbo.ErrorLog.ErrorTime = now
            dbo.ErrorLog.ErrorLine = Some 888
            dbo.ErrorLog.ErrorMessage = "ERROR #2"
            dbo.ErrorLog.ErrorNumber = 500
            dbo.ErrorLog.ErrorProcedure = None
            dbo.ErrorLog.ErrorSeverity = None
            dbo.ErrorLog.ErrorState = None
            dbo.ErrorLog.UserName = "jmarr"
        }

    let! (errorLogId, errorTime, errorLine) = 
        updateTask shared {
            for e in dbo.ErrorLog do
            entity row
            excludeColumn e.ErrorLogID
            where (e.ErrorLogID = row.ErrorLogID)
            output (e.ErrorLogID, e.ErrorTime, e.ErrorLine)
        }

    errorLogId >! 0
    (errorTime.Month, errorTime.Day, errorTime.Year) =! (now.Month, now.Day, now.Year)
    errorLine =! row.ErrorLine

    shared.RollbackTransaction()
}

[<Test>]
let ``InsertGetId Test``() = task {
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
        insertTask db {
            for e in dbo.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }

    errorLogId >! 0
}

[<Test>]
let ``InsertGetIdAsync Test``() = task {
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
        insertTask db {
            for e in dbo.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }

    result >! 0
}

[<Test>]
let ``Update Set Individual Fields``() = task {
    use! shared = db.OpenContextAsync()

    let! row = 
        selectAsync shared {
            for e in dbo.ErrorLog do
            head
        }

    let! result = 
        updateTask shared {
            for e in dbo.ErrorLog do
            set e.ErrorNumber 123
            set e.ErrorMessage "ERROR #123"
            set e.ErrorLine (Some 999)
            set e.ErrorProcedure None
            where (e.ErrorLogID = row.ErrorLogID)
        }

    result >! 0
}

[<Test>]
let ``UpdateAsync Set Individual Fields``() = task {
    use! shared = db.OpenContextAsync()

    let! row = 
        selectAsync shared {
            for e in dbo.ErrorLog do
            head
        }

    let! result = 
        updateTask shared {
            for e in dbo.ErrorLog do
            set e.ErrorNumber 123
            set e.ErrorMessage "ERROR #123"
            set e.ErrorLine (Some 999)
            set e.ErrorProcedure None
            where (e.ErrorLogID = row.ErrorLogID)
        }

    result =! 1
}

[<Test>]
let ``Update Entity``() = task {
    use! shared = db.OpenContextAsync()

    let! row = 
        selectAsync shared {
            for e in dbo.ErrorLog do
            head
        }

    let errorLog = 
        { row with
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
        updateTask shared {
            for e in dbo.ErrorLog do
            entity errorLog
            excludeColumn e.ErrorLogID
            where (e.ErrorLogID = errorLog.ErrorLogID)
        }

    result =! 1
}

[<Test>]
let ``Delete Test``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

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
        insertTask shared {
            for e in dbo.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }

    result >! 0

    let! rowId = 
        selectAsync shared {
            for e in dbo.ErrorLog do
            select e.ErrorLogID
            head
        }

    let! result = 
        deleteTask shared {
            for e in dbo.ErrorLog do
            where (e.ErrorLogID = rowId)
        }

    result =! 1
    shared.RollbackTransaction()
}

[<Test>]
let ``DeleteAsync Test``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

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
        insertTask shared {
            for e in dbo.ErrorLog do
            entity errorLog
            getId e.ErrorLogID
        }

    result >! 0

    let! rowId = 
        selectAsync shared {
            for e in dbo.ErrorLog do
            select e.ErrorLogID
            head
        }

    let! result = 
        deleteTask shared {
            for e in dbo.ErrorLog do
            where (e.ErrorLogID = rowId)
        }

    result =! 1
    shared.RollbackTransaction()
}

[<Test>]
let ``Multiple Inserts``() = task {
    use! ctx = db.OpenContextAsync()

    ctx.BeginTransaction()

    let! _ = 
        deleteTask ctx {
            for e in dbo.ErrorLog do
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
            insertTask ctx {
                for e in dbo.ErrorLog do
                entities errorLogs
                excludeColumn e.ErrorLogID
            }

        rowsInserted =! 3
    | None -> ()

    let! results =
        selectTask ctx {
            for e in dbo.ErrorLog do
            select e.ErrorNumber
        }

    let errorNumbers = results |> Seq.toList
    
    errorNumbers =! [ 400; 401; 402 ]

    ctx.RollbackTransaction()
}

[<Test>]
let ``Distinct Test``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

    let! deletedCount = 
        deleteAsync shared {
            for e in dbo.ErrorLog do
            deleteAll
        } 
                                
    let errorLogs = 
        [ 0 .. 2 ] 
        |> List.map (fun _ -> stubbedErrorLog)
        |> AtLeastOne.tryCreate
                
    match errorLogs with
    | Some errorLogs ->            
        let! rowsInserted = 
            insertAsync shared {
                for e in dbo.ErrorLog do
                entities errorLogs
                excludeColumn e.ErrorLogID
            }

        rowsInserted =! 3
    | None -> ()

    let! results =
        selectAsync shared  {
            for e in dbo.ErrorLog do
            select e.ErrorNumber
        }

    let! distinctResults =
        selectAsync shared {
            for e in dbo.ErrorLog do
            select e.ErrorNumber
            distinct
        }

    results |> Seq.length =! 3
    distinctResults |> Seq.length =! 1

    shared.RollbackTransaction()
}

[<Test>]
let ``Count Test``() = task {
    use! ctx = db.OpenContextAsync()
    ctx.BeginTransaction()

    for i in [0..2] do
        let! result = 
            insertTask ctx {
                for e in dbo.ErrorLog do
                entity stubbedErrorLog
                getId e.ErrorLogID
            }
        ()

    let! count = 
        selectTask ctx {
            for e in dbo.ErrorLog do
            count
        }

    count >! 0
    ctx.RollbackTransaction()
}

[<Test>]
let ``Count Test Task``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

    for i in [0..2] do
        let! result = 
            insertTask shared {
                for e in dbo.ErrorLog do
                entity stubbedErrorLog
                getId e.ErrorLogID
            }
        ()

    let! count = 
        selectTask shared {
            for e in dbo.ErrorLog do
            count
        }

    count >! 0
    shared.RollbackTransaction()
}
        
[<Test>]
let ``Count Test Async``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()
        
    for i in [0..2] do
        let! result = 
            insertAsync shared {
                for e in dbo.ErrorLog do
                entity stubbedErrorLog
                getId e.ErrorLogID
            }
        ()
        
    let! count = 
        selectAsync shared {
            for e in dbo.ErrorLog do
            count
        }
        
    count >! 0        
    shared.RollbackTransaction()
}

[<Test>]
let ``Query Employee Record with DateOnly``() = task {
    let maxBirthDate = System.DateOnly(2005, 1, 1)

    let! employees =
        selectTask db {
            for e in HumanResources.Employee do
            where (e.BirthDate < maxBirthDate)
            select e
        }

    gt0 employees
}

[<Test>]
let ``Query Employee Column with DateOnly``() = task {
    let maxBirthDate = System.DateOnly(2005, 1, 1)

    let! employeeBirthDates =
        selectTask db {
            for e in HumanResources.Employee do
            where (e.BirthDate < maxBirthDate)
            select e.BirthDate
        }

    gt0 employeeBirthDates
}

[<Test>]
let ``Update Employee DateOnly``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()
            
    let! employees =
        selectTask shared {
            for e in HumanResources.Employee do
            select e
        }

    gt0 employees

    let emp : HumanResources.Employee = employees |> Seq.head
    let birthDate = System.DateOnly(1980, 1, 1)

    let! result = 
        updateTask shared {
            for e in HumanResources.Employee do
            set e.BirthDate birthDate
            where (e.BusinessEntityID = emp.BusinessEntityID)
        }

    result =! 1

    let! refreshedEmp = 
        selectTask shared {
            for e in HumanResources.Employee do
            where (e.BusinessEntityID = emp.BusinessEntityID)                    
            tryHead
        }

    let actualBirthDate = 
        (refreshedEmp : HumanResources.Employee option)
        |> Option.map (fun e -> e.BirthDate)
            
    actualBirthDate =! Some birthDate            
    shared.RollbackTransaction()
}

[<Test>]
let ``Query Shift Record with TimeOnly``() = task {
    let minStartTime = System.TimeOnly(9, 30)

    let! shiftsAfter930AM =
        selectTask db {
            for s in HumanResources.Shift do
            where (s.StartTime >= minStartTime)
        }

    // There are 3 shifts: day, evening and night. 
    // Results should contain 2 shifts: evening and night
    gt0 shiftsAfter930AM
}

[<Test>]
let ``Query Shift Column with TimeOnly``() = task {
    let minStartTime = System.TimeOnly(9, 30)

    let! shiftsAfter930AM =
        selectTask db {
            for s in HumanResources.Shift do
            where (s.StartTime >= minStartTime)
            select s.StartTime
        }

    // There are 3 shifts: day, evening and night. 
    // Results should contain 2 shifts: evening and night
    gt0 shiftsAfter930AM
}

[<Test>]
let ``Update Shift with TimeOnly``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()
            
    let minStartTime = System.TimeOnly(9, 30)
    let updatedStartTime = System.TimeOnly(10, 30)

    do! updateTask shared {
            for s in HumanResources.Shift do
            set s.StartTime updatedStartTime
            where (s.StartTime >= minStartTime)
        } :> Task

    let! shiftsat1030AM =
        selectTask shared {
            for s in HumanResources.Shift do
            where (s.StartTime = updatedStartTime)
        } 

    // There are 3 shifts: day, evening and night. 
    // Results should contain 2 shifts: evening and night
    gt0 shiftsat1030AM

    shared.RollbackTransaction()
}

[<Test>]
let ``Insert, update, and select with both datetime and datetime2 precision``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()
    
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
            into ext.DateTime2Support 
            entity entity'
        }
        |> shared.InsertAsync

    let! retrievedBack = 
        selectTask shared {
            for row in ext.DateTime2Support do
            select row
        }

    Assert.AreEqual([timestamp], [for (row: ext.DateTime2Support) in retrievedBack -> row.MorePrecision], "INSERT: Expected DATETIME2 to be stored with full precision")
    Assert.AreNotEqual([timestamp], [for (row: ext.DateTime2Support) in retrievedBack -> row.LessPrecision], "INSERT: Expected a loss of precision when storing a DATETIME")

    let! fullPrecisionQuery = 
        selectTask shared { 
            for row in ext.DateTime2Support do
            where (row.MorePrecision = timestamp)
            count
        }

    let! lessPrecisionQuery = 
        selectTask shared { 
            for row in ext.DateTime2Support do
            where (row.LessPrecision = timestamp)
            count
        }

    Assert.AreEqual(fullPrecisionQuery, 1, "SELECT: Expected precision of a DATETIME2 query parameter to match the precision in the database")
    Assert.AreEqual(lessPrecisionQuery, 1, "SELECT: Expected precision of a DATETIME query parameter to match the precision in the database")

    let newTimestamp = System.DateTime(baseTimestamp.Ticks + 2345678L)

    let! _ = 
        updateTask shared {
            for row in ext.DateTime2Support do
            set row.MorePrecision newTimestamp
            where (row.MorePrecision = timestamp)
        }

    let! _ = 
        updateTask shared {
            for row in ext.DateTime2Support do
            set row.LessPrecision newTimestamp
            where (row.LessPrecision = timestamp)
        }

    let! retrievedBack = 
        selectTask shared {
            for row in ext.DateTime2Support do
            select row
        }

    Assert.AreEqual([newTimestamp], [for (row: ext.DateTime2Support) in retrievedBack -> row.MorePrecision], "UPDATE: Expected DATETIME2 to be stored with full precision")
    Assert.AreNotEqual([newTimestamp], [for (row: ext.DateTime2Support) in retrievedBack -> row.LessPrecision], "UPDATE: Expected a loss of precision when storing a DATETIME")
    shared.RollbackTransaction ()
}

[<Test>]
let ``Guid getId Bug Repro Issue 38``() = task {
    let! guid = 
        insertAsync db {
            for row in ext.GetIdGuidRepro do
            entity
                {
                    ext.GetIdGuidRepro.Id = System.Guid.Empty // ignored
                    ext.GetIdGuidRepro.EmailAddress = "requestValues.EmailAddress"
                }

            getId row.Id
        }

    guid <>! System.Guid.Empty
}
    
[<Test>]
let ``HierarchyId not supported for MS SQL Issue 110``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()
    let parent = SqlHierarchyId.Parse("/1/1/")
    let child = SqlHierarchyId.Parse("/1/1/1/")
    let id_parent = Guid.NewGuid()
    do! insertTask shared {
            into ext.HierarchyIdSupport
            entity
                {
                    ext.HierarchyIdSupport.Id = id_parent
                    ext.HierarchyIdSupport.Hierarchy = parent
                }
        } : Task

    do! insertTask shared {
            into ext.HierarchyIdSupport
            entity
                {
                    ext.HierarchyIdSupport.Id = Guid.NewGuid()
                    ext.HierarchyIdSupport.Hierarchy = child
                }
        } : Task

    let node = child.GetAncestor(1)
    let! result = 
        selectTask shared { 
            for row in ext.HierarchyIdSupport do
            where (row.Id = id_parent && areEqual row.Hierarchy node)
            select row.Hierarchy
            toList
        }
      
    result.Length =! 1
    result.Head =! parent
    shared.RollbackTransaction()
}

[<Test>]
let ``insertOrUpdateOnUnique inserts when row absent``() = task {
    use! ctx = db.OpenContextAsync()
    ctx.BeginTransaction()

    let id = System.Guid.NewGuid()
    let row =
        {
            ext.GetIdGuidRepro.Id = id
            ext.GetIdGuidRepro.EmailAddress = "insert@test.com"
        }

    let! result =
        insertTask ctx {
            for e in ext.GetIdGuidRepro do
            entity row
            insertOrUpdateOnUnique e.Id e.EmailAddress
        }

    result =! 1

    let! found =
        selectTask ctx {
            for e in ext.GetIdGuidRepro do
            where (e.Id = id)
            select e.EmailAddress
            tryHead
        }

    found.IsSome =! true
    found.Value.TrimEnd() =! "insert@test.com"
    ctx.RollbackTransaction()
}

[<Test>]
let ``insertOrUpdateOnUnique updates when row present``() = task {
    use! ctx = db.OpenContextAsync()
    ctx.BeginTransaction()

    let id = System.Guid.NewGuid()
    let original =
        {
            ext.GetIdGuidRepro.Id = id
            ext.GetIdGuidRepro.EmailAddress = "original@test.com"
        }

    // First insert
    do! insertTask ctx {
            for e in ext.GetIdGuidRepro do
            entity original
        } :> Task

    // Upsert with same key but updated email
    let updated = { original with EmailAddress = "updated@test.com" }
    let! result =
        insertTask ctx {
            for e in ext.GetIdGuidRepro do
            entity updated
            insertOrUpdateOnUnique e.Id e.EmailAddress
        }

    result =! 1

    let! found =
        selectTask ctx {
            for e in ext.GetIdGuidRepro do
            where (e.Id = id)
            select e.EmailAddress
            tryHead
        }

    found.IsSome =! true
    found.Value.TrimEnd() =! "updated@test.com"
    ctx.RollbackTransaction()
}

[<Test>]
let ``insertOrUpdateOnUnique updates when nullable key column is NULL``() = task {
    use! ctx = db.OpenContextAsync()
    ctx.BeginTransaction()

    let key1 = System.Guid.NewGuid()
    let original =
        {
            ext.NullableKeyUpsert.Key1 = key1
            ext.NullableKeyUpsert.Key2 = None
            ext.NullableKeyUpsert.Value = "original"
        }

    // First insert
    do! insertTask ctx {
            for e in ext.NullableKeyUpsert do
            entity original
        } :> Task

    // Upsert with same composite key (Key1 = guid, Key2 = NULL) but updated Value
    let updated = { original with Value = "updated" }
    let! result =
        insertTask ctx {
            for e in ext.NullableKeyUpsert do
            entity updated
            insertOrUpdateOnUnique (e.Key1, e.Key2) e.Value
        }

    result =! 1

    let! found =
        selectTask ctx {
            for e in ext.NullableKeyUpsert do
            where (e.Key1 = key1)
            select e.Value
            tryHead
        }

    found.IsSome =! true
    found.Value =! "updated"
    ctx.RollbackTransaction()
}

[<Test>]
let ``Individual column from a leftJoin table should be optional if Some``() = task {
    use! ctx = db.OpenContextAsync()

    let! results = 
        select {
            for o in Sales.SalesOrderHeader do
            leftJoin sr in Sales.SalesOrderHeaderSalesReason on (o.SalesOrderID = sr.Value.SalesOrderID)
            leftJoin r in Sales.SalesReason on (sr.Value.SalesReasonID = r.Value.SalesReasonID)
            where (isNullValue r.Value.Name)
            //select (o.SalesOrderID, Some r.Value.ReasonType, Some r.Value.Name)           // v3 workaround no longer works in v4.
            select (o.SalesOrderID, r |> Option.map _.ReasonType, r |> Option.map _.Name)   // v4 proper handling
            take 10
        }
        |> ctx.SelectAsync

    let reasonsExist = 
        results 
        |> Seq.forall (fun (id, reasonType, name) -> 
            reasonType <> None && name <> None
        )

    gt0 results
    reasonsExist =! false
}
    
type Person = { Id: int; Name: string; Age: int }
let mkPerson id name age = { Id = id; Name = name; Age = age }

[<Test>]
let ``DiffService Diff`` () = 
    let today = System.DateTime.Today

    // Test DiffService.Diff using HumanResources.Department record
    let incoming : HumanResources.Department list = 
        [
            { DepartmentID = 1s; Name = "Engineering"; GroupName = "Research and Development"; ModifiedDate = today }
            { DepartmentID = 2s; Name = "Sales"; GroupName = "$ales"; ModifiedDate = today }
            { DepartmentID = 3s; Name = "Marketing"; GroupName = "Marketing"; ModifiedDate = today }
        ]

    let existing : HumanResources.Department list = 
        [
            { DepartmentID = 1s; Name = "Engineering"; GroupName = "Research and Development"; ModifiedDate = today }
            { DepartmentID = 2s; Name = "Sales"; GroupName = "Sales"; ModifiedDate = today }
            { DepartmentID = 4s; Name = "Finance"; GroupName = "Finance"; ModifiedDate = today }
        ]

    let diff = Diff.Compare(incoming, existing, _.DepartmentID)
    diff.Added =! [ { DepartmentID = 3s; Name = "Marketing"; GroupName = "Marketing"; ModifiedDate = today } ]
    diff.Removed =! [ { DepartmentID = 4s; Name = "Finance"; GroupName = "Finance"; ModifiedDate = today } ]
    diff.Changed =! [ { DepartmentID = 2s; Name = "Sales"; GroupName = "$ales"; ModifiedDate = today } ]

[<Test>]
let ``DiffService Save`` () = task {
    use! shared = db.OpenContextAsync()
    let today = System.DateTime.Today

    let! existingDepartments = 
        selectTask shared {
            for d in HumanResources.Department do
            toList
        }

    let updatedDepartments = 
        existingDepartments 
        |> List.map (fun d -> 
            if d.Name = "Engineering" 
            then { d with Name = "Eng. Dept." } // Update Engineering dept
            else d
        )
        |> List.append [ // Insert App Dev dept
            { DepartmentID = 17s; Name = "App Dev"; GroupName = "Software"; ModifiedDate = today } 
        ]
    
    shared.BeginTransaction()

    let! saveResults = 
        Diff.Compare(updatedDepartments, existingDepartments, _.DepartmentID)
            .AddAll(fun added -> 
                insert {
                    for row in HumanResources.Department do
                    entities added
                    excludeColumn row.DepartmentID
                }
            )
            .Change(fun changed -> 
                update {
                    for dept in HumanResources.Department do
                    set dept.Name changed.Name
                    where (dept.DepartmentID = changed.DepartmentID)
                }
            )
            .SaveTask(shared, createTransaction = false)

    saveResults.Deleted =! 0
    saveResults.Updated =! 1
    saveResults.Inserted =! 1

    // Pull departments again, verify, then try delete.
    let! existingDepartments = 
        selectTask shared {
            for d in HumanResources.Department do
            toList
        }

    let appDev = existingDepartments |> List.tryFind (fun d -> d.Name = "App Dev")
    appDev.IsSome =! true

    let updatedDepartments = 
        updatedDepartments
        |> List.filter (fun d -> d.Name <> "App Dev")

    let! saveResults = 
        Diff.Compare(updatedDepartments, existingDepartments, _.DepartmentID)
            .Remove(fun removed -> 
                delete {
                    for row in HumanResources.Department do
                    where (row.DepartmentID = removed.DepartmentID)
                }
            )
            .SaveTask(shared, createTransaction = false)

    saveResults.Deleted =! 1
    saveResults.Updated =! 0
    saveResults.Inserted =! 0

    shared.RollbackTransaction()
}

[<Test>]
let ``Multiple Joins Same Table`` () = task {
    let! order, sp, cp =
        selectTask db {
            for order in Sales.SalesOrderHeader do
            join s in Sales.SalesPerson on (order.SalesPersonID.Value = s.BusinessEntityID)
            join sp in Person.Person on (s.BusinessEntityID = sp.BusinessEntityID)
            join c in Sales.Customer on (order.CustomerID = c.CustomerID)
            join cp in Person.Person on (c.PersonID.Value = cp.BusinessEntityID)
            where (order.SalesOrderID = 43659)
            select (order, sp, cp)
            head
        }

    // Verify that same-table properties are read properly.
    sp.FirstName =! "Tsvi"
    cp.FirstName =! "James"
}

// SQL Function wrappers for testing
[<AutoOpen>]
module SqlFn =
    let LEN (s: string) : int = sqlFn
    let UPPER (s: string) : string = sqlFn
    let LOWER (s: string) : string = sqlFn
    let GETDATE () : DateTime = sqlFn
    let SUBSTRING (s: string, start: int, length: int) : string = sqlFn
    let CONCAT (s1: string, s2: string) : string = sqlFn

// Use the built-in SqlFn from SqlServerExtensions
open SqlHydra.Query.SqlServerExtensions
open type SqlFn

[<Test>]
let ``SQL Functions - Multiple functions in select`` () = task {
    let! results =
        selectTask db {
            for p in Person.Person do
            where (p.FirstName = "Ken")
            select (p.FirstName, LEN p.FirstName, UPPER p.FirstName, GETDATE())
            take 1
        }

    let firstName, len, upperName, serverDate = results |> Seq.head
    firstName =! "Ken"
    len =! 3
    upperName =! "KEN"
    // Server may be in different timezone, just verify it's a recent date (within 24 hours)
    Assert.That(serverDate, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromHours(24.0)))
}

[<Test>]
let ``SQL Functions - Nested function calls`` () = task {
    let! results =
        selectTask db {
            for p in Person.Person do
            where (p.FirstName = "Ken")
            select (LEN (UPPER p.FirstName))
            take 1
        }

    let lenOfUpper = results |> Seq.head
    lenOfUpper =! 3  // LEN(UPPER("Ken")) = LEN("KEN") = 3
}

[<Test>]
let ``SQL Functions - Multi-param functions`` () = task {
    let! results =
        selectTask db {
            for p in Person.Person do
            where (p.FirstName = "Ken")
            select (SUBSTRING(p.FirstName, 1, 2), CONCAT(p.FirstName, p.LastName))
            take 1
        }

    let substring, concat = results |> Seq.head
    substring =! "Ke"  // SUBSTRING("Ken", 1, 2) = "Ke"
    Assert.That(concat, Does.StartWith("Ken"))  // CONCAT("Ken", LastName)
}

[<Test>]
let ``SQL Functions - Static methods with open type`` () = task {
    // Using `open type SqlServerFn` allows unqualified access - looks like SQL!
    let! results =
        selectTask db {
            for p in Person.Person do
            where (p.FirstName = "Ken")
            select (LEN(p.FirstName), UPPER(p.FirstName), ISNULL(p.MiddleName, "N/A"))
            take 1
        }

    let len, upperName, middleName = results |> Seq.head
    len =! 3
    upperName =! "KEN"
    // Ken's middle name is NULL, so ISNULL returns "N/A"
    middleName =! "N/A"
}

[<Test>]
let ``SQL Functions - ISNULL with Option overload`` () = task {
    use! shared = db.OpenContextAsync()

    // Find a Ken with NULL middle name (most have NULL)
    let! nullResults =
        selectTask shared {
            for p in Person.Person do
            where (p.FirstName = "Ken" && isNullValue p.MiddleName)
            select (ISNULL(p.MiddleName, "NoMiddle"))
            take 1
        }

    let replaced = nullResults |> Seq.head
    replaced =! "NoMiddle"  // NULL should be replaced with "NoMiddle"

    // Find a Ken with non-NULL middle name
    let! nonNullResults =
        selectTask shared {
            for p in Person.Person do
            where (p.FirstName = "Ken" && isNotNullValue p.MiddleName)
            select (ISNULL(p.MiddleName, "NoMiddle"))
            take 1
        }

    let actual = nonNullResults |> Seq.head
    Assert.That(actual, Is.Not.EqualTo("NoMiddle"))  // Should return actual middle name, not replacement
}

[<Test>]
let ``SQL Functions - ISNULL with non-optional column`` () = task {
    // FirstName is non-optional (string, not Option<string>)
    // This uses the generic 'T overload
    let! results =
        selectTask db {
            for p in Person.Person do
            where (p.FirstName = "Ken")
            select (ISNULL(p.FirstName, "Unknown"))
            take 1
        }

    let firstName = results |> Seq.head
    firstName =! "Ken"  // FirstName is never NULL, so returns actual value
}

[<Test>]
let ``SQL Functions - Date and numeric functions`` () = task {
    let! results =
        selectTask db {
            for o in Sales.SalesOrderHeader do
            select (o.SalesOrderID, YEAR(o.OrderDate), MONTH(o.OrderDate), ABS(o.TotalDue), ROUND(o.TotalDue, 0))
            take 1
        }

    let orderId, year, month, absDue, roundedDue = results |> Seq.head
    Assert.That(orderId, Is.GreaterThan(0))
    Assert.That(year, Is.GreaterThan(2000))
    Assert.That(month, Is.GreaterThanOrEqualTo(1).And.LessThanOrEqualTo(12))
    Assert.That(absDue, Is.GreaterThanOrEqualTo(0m))
    Assert.That(roundedDue, Is.GreaterThanOrEqualTo(0m))
}

[<Test>]
let ``SQL Functions - In WHERE clause with value comparison`` () = task {
    let! results =
        selectTask db {
            for p in Person.Person do
            where (LEN(p.FirstName) > 3)
            select p.FirstName
            take 5
        }

    // All names should have length > 3
    for name in results do
        Assert.That(name.Length, Is.GreaterThan(3))
}

[<Test>]
let ``SQL Functions - In WHERE clause comparing two functions`` () = task {
    let! results =
        selectTask db {
            for p in Person.Person do
            where (LEN(p.FirstName) < LEN(p.LastName))
            select (p.FirstName, p.LastName)
            take 5
        }

    // FirstName should be shorter than LastName
    for firstName, lastName in results do
        Assert.That(firstName.Length, Is.LessThan(lastName.Length))
}

[<Test>]
let ``SQL Functions - In WHERE clause with UPPER`` () = task {
    let! results =
        selectTask db {
            for p in Person.Person do
            where (UPPER(p.FirstName) = "KEN")
            select p.FirstName
            take 1
        }

    let firstName = results |> Seq.head
    firstName =! "Ken"
}

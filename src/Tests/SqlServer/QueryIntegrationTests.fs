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
open Microsoft.SqlServer.Types
open HydraBuilders

let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)

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
    use ctx = openContext()
            
    let addresses =
        select {
            for a in Person.Address do
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
            for a in Person.Address do
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
            for o in Sales.SalesOrderHeader do
            join d in Sales.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            where o.OnlineOrderFlag
            select (o, d)
        }

    let! results = query |> ctx.ReadAsync HydraReader.Read
    gt0 results
}

[<Test>]
let ``Product with Category Name``() = task {
    use ctx = openContext()

    let query = 
        select {
            for p in Production.Product do
            join sc in Production.ProductSubcategory on (p.ProductSubcategoryID = Some sc.ProductSubcategoryID)
            join c in Production.ProductCategory on (sc.ProductCategoryID = c.ProductCategoryID)
            select (c.Name, p)
            take 5
        }

    let! rows = query |> ctx.ReadAsync HydraReader.Read
    gt0 rows
}

[<Test>]
let ``Select Column Aggregates From Product IDs 1-3``() = task {
    use ctx = openContext()

    let query =
        select {
            for p in Production.Product do
            where (p.ProductSubcategoryID <> None)
            groupBy p.ProductSubcategoryID
            where (p.ProductSubcategoryID.Value |=| [ 1; 2; 3 ])
            select (p.ProductSubcategoryID, minBy p.ListPrice, maxBy p.ListPrice, avgBy p.ListPrice, countBy p.ListPrice, sumBy p.ListPrice)
        }

    let! aggregates = query |> ctx.ReadAsync HydraReader.Read

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
    use ctx = openContext()

    let avgListPrice = 
        select {
            for p in Production.Product do
            select (avgBy p.ListPrice)
        }

    let! productsWithHigherThanAvgPrice = 
        select {
            for p in Production.Product do
            where (p.ListPrice > subqueryOne avgListPrice)
            orderByDescending p.ListPrice
            select (p.Name, p.ListPrice)
        }
        |> ctx.ReadAsync HydraReader.Read

    let avgListPrice = 438.6662M
            
    gt0 productsWithHigherThanAvgPrice
    Assert.IsTrue(productsWithHigherThanAvgPrice |> Seq.forall (fun (nm, price) -> price > avgListPrice), "Expected all prices to be > than avg price of $438.67.")
}

[<Test>]
let ``Select Column Aggregates``() = task {
    use ctx = openContext()

    let! aggregates = 
        select {
            for p in Production.Product do
            where (p.ProductSubcategoryID <> None)
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
                where (p.ProductSubcategoryID <> None)
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
            where (p.ProductSubcategoryID <> None)
            groupBy p.ProductSubcategoryID
            orderByDescending (avgBy p.ListPrice)
            select (p.ProductSubcategoryID)
            take 5
        }

    let! top5Categories =
        select {
            for c in Production.ProductCategory do
            where (Some c.ProductCategoryID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
            select c.Name
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 top5Categories
}

[<Test>]
let ``Where subqueryOne``() = task {
    use ctx = openContext()

    let avgListPrice = 
        select {
            for p in Production.Product do
            select (avgBy p.ListPrice)
        } 

    let! productsWithAboveAveragePrice =
        select {
            for p in Production.Product do
            where (p.ListPrice > subqueryOne avgListPrice)
            select (p.Name, p.ListPrice)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 productsWithAboveAveragePrice
}

[<Test>]
let ``Select Columns with Option``() = task {
    use ctx = openContext()

    let! values = 
        select {
            for p in Production.Product do
            where (p.ProductSubcategoryID <> None)
            select (p.ProductSubcategoryID, p.ListPrice)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 values
    Assert.IsTrue(values |> Seq.forall (fun (catId, price) -> catId <> None), "Expected subcategories to all have a value.")
}

[<Test>]
let ``Insert with Output``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

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
        insertTask ctx {
            for e in dbo.ErrorLog do
            entity row
            excludeColumn e.ErrorLogID
            output (e.ErrorLogID, e.ErrorTime, e.ErrorLine)
        }

    errorLogId >! 0
    (errorTime.Month, errorTime.Day, errorTime.Year) =! (now.Month, now.Day, now.Year)
    errorLine =! row.ErrorLine

    ctx.RollbackTransaction()
}

[<Test>]
let ``Update with Output``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    let! row = 
        selectAsync ctx {
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
        updateTask ctx {
            for e in dbo.ErrorLog do
            entity row
            excludeColumn e.ErrorLogID
            where (e.ErrorLogID = row.ErrorLogID)
            output (e.ErrorLogID, e.ErrorTime, e.ErrorLine)
        }

    errorLogId >! 0
    (errorTime.Month, errorTime.Day, errorTime.Year) =! (now.Month, now.Day, now.Year)
    errorLine =! row.ErrorLine

    ctx.RollbackTransaction()
}

[<Test>]
let ``InsertGetId Test``() = task {
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
            dbo.ErrorLog.ErrorLine = None
            dbo.ErrorLog.ErrorMessage = "TEST INSERT ASYNC"
            dbo.ErrorLog.ErrorNumber = 400
            dbo.ErrorLog.ErrorProcedure = (Some "Procedure 400")
            dbo.ErrorLog.ErrorSeverity = None
            dbo.ErrorLog.ErrorState = None
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
        selectAsync ctx {
            for e in dbo.ErrorLog do
            head
        }

    let! result = 
        updateTask ctx {
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
    use ctx = openContext()

    let! row = 
        selectAsync ctx {
            for e in dbo.ErrorLog do
            head
        }

    let! result = 
        updateTask ctx {
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
    use ctx = openContext()

    let! row = 
        selectAsync ctx {
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
        updateTask ctx {
            for e in dbo.ErrorLog do
            entity errorLog
            excludeColumn e.ErrorLogID
            where (e.ErrorLogID = errorLog.ErrorLogID)
        }

    result =! 1
}

[<Test>]
let ``Delete Test``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    let! rowId = 
        selectAsync ctx {
            for e in dbo.ErrorLog do
            select e.ErrorLogID
            head
        }

    let! result = 
        deleteTask ctx {
            for e in dbo.ErrorLog do
            where (e.ErrorLogID = rowId)
        }

    result =! 1
    ctx.RollbackTransaction()
}

[<Test>]
let ``DeleteAsync Test``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    let! rowId = 
        selectAsync ctx {
            for e in dbo.ErrorLog do
            select e.ErrorLogID
            head
        }

    let! result = 
        deleteTask ctx {
            for e in dbo.ErrorLog do
            where (e.ErrorLogID = rowId)
        }

    result =! 1
    ctx.RollbackTransaction()
}

[<Test>]
let ``Multiple Inserts``() = task {
    use ctx = openContext()

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
        select {
            for e in dbo.ErrorLog do
            select e.ErrorNumber
        }
        |> ctx.ReadAsync HydraReader.Read

    let errorNumbers = results |> Seq.toList
    
    errorNumbers =! [ 400; 401; 402 ]

    ctx.RollbackTransaction()
}

[<Test>]
let ``Distinct Test``() = task {
    use ctx = openContext()

    ctx.BeginTransaction()

    let! deletedCount = 
        deleteAsync ctx {
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
            insertAsync ctx {
                for e in dbo.ErrorLog do
                entities errorLogs
                excludeColumn e.ErrorLogID
            }

        rowsInserted =! 3
    | None -> ()

    let! results =
        selectAsync ctx  {
            for e in dbo.ErrorLog do
            select e.ErrorNumber
        }

    let! distinctResults =
        selectAsync ctx {
            for e in dbo.ErrorLog do
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
            insertTask ctx {
                for e in dbo.ErrorLog do
                entity stubbedErrorLog
                getId e.ErrorLogID
            }
        ()

    let! count = 
        select {
            for e in dbo.ErrorLog do
            count
        }
        |> ctx.CountAsync

    count >! 0
    ctx.RollbackTransaction()
}

[<Test>]
let ``Count Test Task``() = task {
    use ctx = openContext()
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
let ``Count Test Async``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()
        
    for i in [0..2] do
        let! result = 
            insertAsync ctx {
                for e in dbo.ErrorLog do
                entity stubbedErrorLog
                getId e.ErrorLogID
            }
        ()
        
    let! count = 
        selectAsync ctx {
            for e in dbo.ErrorLog do
            count
        }
        
    count >! 0        
    ctx.RollbackTransaction()
}

[<Test>]
let ``Query Employee Record with DateOnly``() = task {
    use ctx = openContext()
            
    let maxBirthDate = System.DateOnly(2005, 1, 1)

    let employees =
        select {
            for e in HumanResources.Employee do
            where (e.BirthDate < maxBirthDate)
            select e
        }
        |> ctx.Read HydraReader.Read

    gt0 employees
}

[<Test>]
let ``Query Employee Column with DateOnly``() = task {
    use ctx = openContext()
            
    let maxBirthDate = System.DateOnly(2005, 1, 1)

    let employeeBirthDates =
        select {
            for e in HumanResources.Employee do
            where (e.BirthDate < maxBirthDate)
            select e.BirthDate
        }
        |> ctx.Read HydraReader.Read

    gt0 employeeBirthDates
}

[<Test>]
let ``Update Employee DateOnly``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()
            
    let! employees =
        selectTask ctx {
            for e in HumanResources.Employee do
            select e
        }

    gt0 employees

    let emp : HumanResources.Employee = employees |> Seq.head
    let birthDate = System.DateOnly(1980, 1, 1)

    let! result = 
        updateTask ctx {
            for e in HumanResources.Employee do
            set e.BirthDate birthDate
            where (e.BusinessEntityID = emp.BusinessEntityID)
        }

    result =! 1

    let! refreshedEmp = 
        selectTask ctx {
            for e in HumanResources.Employee do
            where (e.BusinessEntityID = emp.BusinessEntityID)                    
            tryHead
        }

    let actualBirthDate = 
        (refreshedEmp : HumanResources.Employee option)
        |> Option.map (fun e -> e.BirthDate)
            
    actualBirthDate =! Some birthDate            
    ctx.RollbackTransaction()
}

[<Test>]
let ``Query Shift Record with TimeOnly``() = task {
    use ctx = openContext()
            
    let minStartTime = System.TimeOnly(9, 30)

    let shiftsAfter930AM =
        select {
            for s in HumanResources.Shift do
            where (s.StartTime >= minStartTime)
        }
        |> ctx.Read HydraReader.Read

    // There are 3 shifts: day, evening and night. 
    // Results should contain 2 shifts: evening and night
    gt0 shiftsAfter930AM
}

[<Test>]
let ``Query Shift Column with TimeOnly``() = task {
    use ctx = openContext()
            
    let minStartTime = System.TimeOnly(9, 30)

    let shiftsAfter930AM =
        select {
            for s in HumanResources.Shift do
            where (s.StartTime >= minStartTime)
            select s.StartTime
        }
        |> ctx.Read HydraReader.Read

    // There are 3 shifts: day, evening and night. 
    // Results should contain 2 shifts: evening and night
    gt0 shiftsAfter930AM
}

[<Test>]
let ``Update Shift with TimeOnly``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()
            
    let minStartTime = System.TimeOnly(9, 30)
    let updatedStartTime = System.TimeOnly(10, 30)

    do! updateTask ctx {
            for s in HumanResources.Shift do
            set s.StartTime updatedStartTime
            where (s.StartTime >= minStartTime)
        } :> Task

    let! shiftsat1030AM =
        selectTask ctx {
            for s in HumanResources.Shift do
            where (s.StartTime = updatedStartTime)
        } 

    // There are 3 shifts: day, evening and night. 
    // Results should contain 2 shifts: evening and night
    gt0 shiftsat1030AM

    ctx.RollbackTransaction()
}

[<Test>]
let ``Insert, update, and select with both datetime and datetime2 precision``() = task {
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
            into ext.DateTime2Support 
            entity entity'
        }
        |> ctx.InsertAsync

    let! retrievedBack = 
        selectTask ctx {
            for row in ext.DateTime2Support do
            select row
        }

    Assert.AreEqual([timestamp], [for (row: ext.DateTime2Support) in retrievedBack -> row.MorePrecision], "INSERT: Expected DATETIME2 to be stored with full precision")
    Assert.AreNotEqual([timestamp], [for (row: ext.DateTime2Support) in retrievedBack -> row.LessPrecision], "INSERT: Expected a loss of precision when storing a DATETIME")

    let! fullPrecisionQuery = 
        selectTask ctx { 
            for row in ext.DateTime2Support do
            where (row.MorePrecision = timestamp)
            count
        }

    let! lessPrecisionQuery = 
        selectTask ctx { 
            for row in ext.DateTime2Support do
            where (row.LessPrecision = timestamp)
            count
        }

    Assert.AreEqual(fullPrecisionQuery, 1, "SELECT: Expected precision of a DATETIME2 query parameter to match the precision in the database")
    Assert.AreEqual(lessPrecisionQuery, 1, "SELECT: Expected precision of a DATETIME query parameter to match the precision in the database")

    let newTimestamp = System.DateTime(baseTimestamp.Ticks + 2345678L)

    let! _ = 
        updateTask ctx {
            for row in ext.DateTime2Support do
            set row.MorePrecision newTimestamp
            where (row.MorePrecision = timestamp)
        }

    let! _ = 
        updateTask ctx {
            for row in ext.DateTime2Support do
            set row.LessPrecision newTimestamp
            where (row.LessPrecision = timestamp)
        }

    let! retrievedBack = 
        selectTask ctx {
            for row in ext.DateTime2Support do
            select row
        }

    Assert.AreEqual([newTimestamp], [for (row: ext.DateTime2Support) in retrievedBack -> row.MorePrecision], "UPDATE: Expected DATETIME2 to be stored with full precision")
    Assert.AreNotEqual([newTimestamp], [for (row: ext.DateTime2Support) in retrievedBack -> row.LessPrecision], "UPDATE: Expected a loss of precision when storing a DATETIME")
    ctx.RollbackTransaction ()
}

[<Test>]
let ``Guid getId Bug Repro Issue 38``() = task {
    use ctx = openContext()
    let! guid = 
        insertAsync ctx {
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
    use ctx = openContext()
    ctx.BeginTransaction()
    let parent = SqlHierarchyId.Parse("/1/1/")
    let child = SqlHierarchyId.Parse("/1/1/1/")
    let id_parent = Guid.NewGuid()
    do! insertTask ctx {
            into ext.HierarchyIdSupport
            entity
                {
                    ext.HierarchyIdSupport.Id = id_parent
                    ext.HierarchyIdSupport.Hierarchy = parent
                }
        } : Task

    do! insertTask ctx {
            into ext.HierarchyIdSupport
            entity
                {
                    ext.HierarchyIdSupport.Id = Guid.NewGuid()
                    ext.HierarchyIdSupport.Hierarchy = child
                }
        } : Task

    let node = child.GetAncestor(1)
    let! result = 
        selectTask ctx { 
            for row in ext.HierarchyIdSupport do
            where (row.Id = id_parent && areEqual row.Hierarchy node)
            select row.Hierarchy
            toList
        }
      
    result.Length =! 1
    result.Head =! parent
    ctx.RollbackTransaction()
}

[<Test>]
let ``Individual column from a leftJoin table should be optional if Some``() = task {
    let! results = 
        selectTask openContext  {
            for o in Sales.SalesOrderHeader do
            leftJoin sr in Sales.SalesOrderHeaderSalesReason on (o.SalesOrderID = sr.Value.SalesOrderID)
            leftJoin r in Sales.SalesReason on (sr.Value.SalesReasonID = r.Value.SalesReasonID)
            where (isNullValue r.Value.Name)
            select (o.SalesOrderID, Some r.Value.ReasonType, Some r.Value.Name)
            take 10
        }

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
    use ctx = openContext()
    let today = System.DateTime.Today

    let! existingDepartments = 
        selectTask ctx {
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
    
    ctx.BeginTransaction()

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
            .SaveTask(ctx, createTransaction = false)

    saveResults.Deleted =! 0
    saveResults.Updated =! 1
    saveResults.Inserted =! 1

    // Pull departments again, verify, then try delete.
    let! existingDepartments = 
        selectTask ctx {
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
            .SaveTask(ctx, createTransaction = false)

    saveResults.Deleted =! 1
    saveResults.Updated =! 0
    saveResults.Inserted =! 0

    ctx.RollbackTransaction()
}

[<Test>]
let ``Multiple Joins Same Table`` () = task {
    let! order, sp, cp = 
        selectTask openContext {
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
    
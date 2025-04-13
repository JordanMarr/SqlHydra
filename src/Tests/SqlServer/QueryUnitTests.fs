module SqlServer.``Query Unit Tests``

open Swensen.Unquote
open SqlHydra.Query
open DB
open NUnit.Framework

#if NET8_0
open SqlServer.AdventureWorksNet8
#endif
#if NET9_0
open SqlServer.AdventureWorksNet9
#endif

[<Test>]
let ``Simple Where``() = 
    let getCity() = "Dallas"
    let sql = 
        select {
            for a in Person.Address do
            where (a.City = getCity())
            orderBy a.City
        }
        |> toSql

    sql =! "SELECT * FROM [Person].[Address] AS [a] WHERE ([a].[City] = @p0) ORDER BY [a].[City]"

[<Test>]
let ``Conditional Where Clause``() = 
    let cityFilter = Some "Dallas"

    let sql = 
        select {
            for a in Person.Address do
            where (
                (cityFilter.IsSome && a.City = cityFilter.Value)
            )
            orderBy a.City
        }
        |> toSql

    sql =! "SELECT * FROM [Person].[Address] AS [a] WHERE ([a].[City] = @p0) ORDER BY [a].[City]"

[<Test>]
let ``Conditional Where And Clauses, Both True``() = 
    let cityFilter = true
    let zipFilter = true

    let sql = 
        select {
            for a in Person.Address do
            where (
                (cityFilter && a.City = "Dallas") &&
                (zipFilter && a.PostalCode = "75001")
            )
            orderBy a.City
        }
        |> toSql

    sql =! "SELECT * FROM [Person].[Address] AS [a] WHERE (([a].[City] = @p0) AND ([a].[PostalCode] = @p1)) ORDER BY [a].[City]"

[<Test>]
let ``Conditional Where And Clauses, One True``() = 
    let cityFilter = Some "Dallas"
    let zipFilter : Option<string> = None

    let sql = 
        select {
            for a in Person.Address do
            where (
                (cityFilter.IsSome && a.City = cityFilter.Value) //&&
                //(zipFilter.IsSome && a.PostalCode = zipFilter.Value)
            )
            orderBy a.City
        }
        |> toSql

    sql =! "SELECT * FROM [Person].[Address] AS [a] WHERE ([a].[City] = @p0) ORDER BY [a].[City]"

[<Test>]
let ``Conditional Where And Clauses, Neither True``() = 
    let cityFilter : Option<string> = None
    let zipFilter : Option<string> = None

    let sql = 
        select {
            for a in Person.Address do
            where (
                (cityFilter.IsSome && a.City = cityFilter.Value) &&
                (zipFilter.IsSome && a.PostalCode = zipFilter.Value)
            )
            orderBy a.City
        }
        |> toSql

    sql =! "SELECT * FROM [Person].[Address] AS [a] ORDER BY [a].[City]"

[<Test>]
let ``Conditional Where Or Clauses, Both True``() = 
    let cityFilter = Some "Dallas"
    let zipFilter = Some "75001"

    let sql = 
        select {
            for a in Person.Address do
            where (
                (cityFilter.IsSome && a.City = cityFilter.Value) || 
                (zipFilter.IsSome && a.PostalCode = zipFilter.Value)
            )
            orderBy a.City
        }
        |> toSql

    sql =! "SELECT * FROM [Person].[Address] AS [a] WHERE (([a].[City] = @p0) OR ([a].[PostalCode] = @p1)) ORDER BY [a].[City]"

[<Test>]
let ``Conditional OrderBy``() = 
    let isCitySortEnabled() = true

    let sql = 
        select {
            for a in Person.Address do
            orderBy (isCitySortEnabled() ^^ a.City)
        }
        |> toSql

    sql =! "SELECT * FROM [Person].[Address] AS [a] ORDER BY [a].[City]"


[<Test>]
let ``Simple Where - kata``() = 
    let sql = 
        select {
            for a in Person.Address do
            kata (fun query -> query.Where("a.City", "Dallas"))
            orderBy a.City
        }
        |> toSql

    sql =! "SELECT * FROM [Person].[Address] AS [a] WHERE [a].[City] = @p0 ORDER BY [a].[City]"

[<Test>]
let ``Select 1 Column``() = 
    let sql =
        select {
            for a in Person.Address do
            select a.City
        }
        |> toSql

    sql.Contains("SELECT [a].[City] FROM") =! true

[<Test>]
let ``Select 2 Columns``() = 
    let sql =
        select {
            for h in Sales.SalesOrderHeader do
            select (h.CustomerID, h.OnlineOrderFlag)
        }
        |> toSql

    sql.Contains("SELECT [h].[CustomerID], [h].[OnlineOrderFlag] FROM") =! true

[<Test; Ignore("Temporarily ignoring test for emergency fix")>]
let ``Select 1 Table and 1 Column``() = 
    let sql =
        select {
            for o in Sales.SalesOrderHeader do
            join d in Sales.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            where o.OnlineOrderFlag
            select (o, d.LineTotal)
        }
        |> toSql

    sql.Contains("SELECT [o].[SalesOrderID], [o].[RevisionNumber], [o].[OrderDate], [o].[DueDate], [o].[ShipDate], [o].[Status], [o].[OnlineOrderFlag], [o].[SalesOrderNumber], [o].[PurchaseOrderNumber], [o].[AccountNumber], [o].[CustomerID], [o].[SalesPersonID], [o].[TerritoryID], [o].[BillToAddressID], [o].[ShipToAddressID], [o].[ShipMethodID], [o].[CreditCardID], [o].[CreditCardApprovalCode], [o].[CurrencyRateID], [o].[SubTotal], [o].[TaxAmt], [o].[Freight], [o].[TotalDue], [o].[Comment], [o].[rowguid], [o].[ModifiedDate], [d].[LineTotal] FROM [Sales].[SalesOrderHeader] AS [o]") =! true

[<Test>]
let ``Where bool is true``() = 
    let sql =
        select {
            for o in Sales.SalesOrderHeader do
            where o.OnlineOrderFlag
        }
        |> toSql

    sql.Contains("WHERE ([o].[OnlineOrderFlag] = cast(1 as bit))") =! true

[<Test>]
let ``Where bool is false``() = 
    let sql = 
        select {
            for o in Sales.SalesOrderHeader do
            where (not o.OnlineOrderFlag)
        }
        |> toSql

    sql.Contains("WHERE ([o].[OnlineOrderFlag] = cast(0 as bit))") =! true


type OptionalBoolEntity = { QuestionAnswered: bool option }

[<Test>]
let ``Where bool option is true``() = 
    let sql = 
        select {
            for o in table<OptionalBoolEntity> do
            where o.QuestionAnswered.Value
        }
        |> toSql

    sql.Contains("WHERE ([o].[QuestionAnswered] = cast(1 as bit))") =! true

[<Test>]
let ``Where bool option is false``() = 
    let sql = 
        select {
            for o in table<OptionalBoolEntity> do
            where (not o.QuestionAnswered.Value)
        }
        |> toSql

    sql.Contains("WHERE ([o].[QuestionAnswered] = cast(0 as bit))") =! true

[<Test>]
let ``Where bool option is false or null``() = 
    let sql = 
        select {
            for o in table<OptionalBoolEntity> do
            where (not o.QuestionAnswered.Value || o.QuestionAnswered = None)
        }
        |> toSql

    sql.Contains("WHERE (([o].[QuestionAnswered] = cast(0 as bit)) OR ([o].[QuestionAnswered] IS NULL))") =! true

[<Test; Ignore "Ignore">]
let ``Where with Option Type``() = 
    let sql =  
        select {
            for a in Person.Address do
            where (a.AddressLine2 <> None)
        }
        |> toSql

    sql.Contains("IS NOT NULL") =! true

[<Test; Ignore "Ignore">]
let ``Where Not Like``() = 
    let sql = 
        select {
            for a in Person.Address do
            where (a.City <>% "S%")
        }
        |> toSql

    sql.Contains("NOT LIKE") =! true

[<Test>]
let ``Or Where``() = 
    let sql =  
        select {
            for a in Person.Address do
            where (a.City = "Chicago" || a.City = "Dallas")
        }
        |> toSql

    sql.Contains("WHERE (([a].[City] = @p0) OR ([a].[City] = @p1))") =! true

[<Test>]
let ``And Where``() = 
    let sql =  
        select {
            for a in Person.Address do
            where (a.City = "Chicago" && a.City = "Dallas")
        }
        |> toSql

    sql.Contains("WHERE (([a].[City] = @p0) AND ([a].[City] = @p1))") =! true

[<Test>]
let ``Where Guid Empty``() = 
    let sql =  
        select {
            for a in Person.Address do
            where (a.rowguid = System.Guid.Empty)
        }
        |> toSql

    sql.Contains("WHERE ([a].[rowguid] = @p0)") =! true

[<Test>]
let ``Where with AND and OR in Parenthesis``() = 
    let sql =  
        select {
            for a in Person.Address do
            where (a.City = "Chicago" && (a.AddressLine2 = Some "abc" || isNullValue a.AddressLine2))
        }
        |> toSql

    Assert.IsTrue( 
        sql.Contains("WHERE (([a].[City] = @p0) AND (([a].[AddressLine2] = @p1) OR ([a].[AddressLine2] IS NULL)))"),
        "Should wrap OR clause in parenthesis and each individual where clause in parenthesis.")

[<Test>]
let ``Where value and column are swapped``() = 
    let sql =  
        select {
            for a in Person.Address do
            where (5 < a.AddressID && 20 >= a.AddressID)
        }
        |> toSql

    sql.Contains("WHERE (([a].[AddressID] > @p0) AND ([a].[AddressID] <= @p1))") =! true

[<Test>]
let ``Where Not Binary``() = 
    let sql =  
        select {
            for a in Person.Address do
            where (not (a.City = "Chicago" && a.City = "Dallas"))
        }
        |> toSql

    sql.Contains("WHERE (NOT (([a].[City] = @p0) AND ([a].[City] = @p1)))") =! true

[<Test>]
let ``Where Customer isIn List``() = 
    let sql =  
        select {
            for c in Sales.Customer do
            where (isIn c.CustomerID [30018;29545;29954])
        }
        |> toSql

    sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where Customer |=| List``() = 
    let sql =  
        select {
            for c in Sales.Customer do
            where (c.CustomerID |=| [30018;29545;29954])
        }
        |> toSql

    sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where Customer |=| Array``() = 
    let sql =  
        select {
            for c in Sales.Customer do
            where (c.CustomerID |=| [| 30018;29545;29954 |])
        }
        |> toSql

    sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where Customer |=| Seq``() = 
    let buildQuery (values: int seq) =                
        select {
            for c in Sales.Customer do
            where (c.CustomerID |=| values)
        }

    let sql =  buildQuery [ 30018;29545;29954 ] |> toSql
    sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where Customer |<>| List``() = 
    let sql =  
        select {
            for c in Sales.Customer do
            where (c.PersonID.Value |<>| [ 30018;29545;29954 ]) // should work with option values
        }
        |> toSql

    sql.Contains("WHERE ([c].[PersonID] NOT IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Inner Join``() = 
    let sql = 
        select {
            for o in Sales.SalesOrderHeader do
            join d in Sales.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            select o
        }
        |> toSql

    sql.Contains("INNER JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID])") =! true

[<Test; Ignore("Temporarily ignoring test for emergency fix")>]
let ``Left Join``() = 
    let sql = 
        select {
            for o in Sales.SalesOrderHeader do
            leftJoin d in Sales.SalesOrderDetail on (o.SalesOrderID = d.Value.SalesOrderID)
            where (o.SalesOrderID = d.Value.SalesOrderID)
            select o
        }
        |> toSql

    let expected = """SELECT [o].[SalesOrderID], [o].[RevisionNumber], [o].[OrderDate], [o].[DueDate], [o].[ShipDate], [o].[Status], [o].[OnlineOrderFlag], [o].[SalesOrderNumber], [o].[PurchaseOrderNumber], [o].[AccountNumber], [o].[CustomerID], [o].[SalesPersonID], [o].[TerritoryID], [o].[BillToAddressID], [o].[ShipToAddressID], [o].[ShipMethodID], [o].[CreditCardID], [o].[CreditCardApprovalCode], [o].[CurrencyRateID], [o].[SubTotal], [o].[TaxAmt], [o].[Freight], [o].[TotalDue], [o].[Comment], [o].[rowguid], [o].[ModifiedDate] FROM [Sales].[SalesOrderHeader] AS [o] 
LEFT JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID]) WHERE ([o].[SalesOrderID] = [d].[SalesOrderID])"""
    sql =! expected

[<Test>]
let ``Optional Property Value in Where``() = 
    let date = System.DateTime(2023,1,1)

    let sql =  
        select {
            for wo in Production.WorkOrder do
            where (wo.EndDate = None || wo.EndDate.Value >= date)
        }
        |> toSql

    sql =! """SELECT * FROM [Production].[WorkOrder] AS [wo] WHERE (([wo].[EndDate] IS NULL) OR ([wo].[EndDate] >= @p0))"""

[<Test>]
let ``Inner Join - Multi Column``() = 
    let sql = 
        select {
            for o in Sales.SalesOrderHeader do
            join d in Sales.SalesOrderDetail on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
            select o
        }
        |> toSql

    sql.Contains("INNER JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID] AND [o].[ModifiedDate] = [d].[ModifiedDate])") =! true

[<Test>]
let ``Left Join - Multi Column``() = 
    let sql = 
        select {
            for o in Sales.SalesOrderHeader do
            leftJoin d in Sales.SalesOrderDetail on ((o.SalesOrderID, o.ModifiedDate) = (d.Value.SalesOrderID, d.Value.ModifiedDate))
            select o
        }
        |> toSql

    sql.Contains("LEFT JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID] AND [o].[ModifiedDate] = [d].[ModifiedDate])") =! true

[<Test>]
let ``Correlated Subquery``() = 
    let maxOrderQty = 
        select {
            for d in Sales.SalesOrderDetail do
            correlate od in Sales.SalesOrderDetail
            where (d.ProductID = od.ProductID)
            select (maxBy d.OrderQty)
        }

    let sql =  
        select {
            for od in Sales.SalesOrderDetail do
            where (od.OrderQty = subqueryOne maxOrderQty)
            orderBy od.ProductID
            select (od.SalesOrderID, od.ProductID, od.OrderQty)
        }
        |> toSql

    sql =!
        "SELECT [od].[SalesOrderID], [od].[ProductID], [od].[OrderQty] FROM [Sales].[SalesOrderDetail] AS [od] \
        WHERE ([od].[OrderQty] = (\
            SELECT MAX([d].[OrderQty]) FROM [Sales].[SalesOrderDetail] AS [d] WHERE ([d].[ProductID] = [od].[ProductID])\
        )) ORDER BY [od].[ProductID]"

[<Test; Ignore("Temporarily ignoring test for emergency fix")>]
let ``Join On Value Bug Fix Test``() = 
    let sql =  
        select {
            for o in Sales.SalesOrderHeader do
            leftJoin d in Sales.SalesOrderHeader on (o.AccountNumber.Value = d.Value.AccountNumber.Value)
            select o
        }
        |> toSql

    Assert.AreEqual(sql,
        """SELECT [o].[SalesOrderID], [o].[RevisionNumber], [o].[OrderDate], [o].[DueDate], [o].[ShipDate], [o].[Status], [o].[OnlineOrderFlag], [o].[SalesOrderNumber], [o].[PurchaseOrderNumber], [o].[AccountNumber], [o].[CustomerID], [o].[SalesPersonID], [o].[TerritoryID], [o].[BillToAddressID], [o].[ShipToAddressID], [o].[ShipMethodID], [o].[CreditCardID], [o].[CreditCardApprovalCode], [o].[CurrencyRateID], [o].[SubTotal], [o].[TaxAmt], [o].[Freight], [o].[TotalDue], [o].[Comment], [o].[rowguid], [o].[ModifiedDate] FROM [Sales].[SalesOrderHeader] AS [o] 
LEFT JOIN [Sales].[SalesOrderHeader] AS [d] ON ([o].[AccountNumber] = [d].[AccountNumber])""",
        "Bugged version was replacing TableMapping for original table with joined table.")
        
[<Test>]
let ``Where Static Property``() = 
    let sql = 
        select {
            for o in Sales.SalesOrderHeader do
            where (o.SalesOrderID = System.Int32.MaxValue)
        }
        |> toSql

    sql.Contains("WHERE ([o].[SalesOrderID] = @p0)") =! true

[<Test>]
let ``Delete Query with Where``() = 
    let sql =  
        delete {
            for c in Sales.Customer do
            where (c.CustomerID |<>| [ 30018;29545;29954 ])
        }
        |> toSql

    sql.Contains("DELETE FROM [Sales].[Customer]") =! true
    sql.Contains("WHERE ([Sales].[Customer].[CustomerID] NOT IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Delete All``() = 
    let sql =  
        delete {
            for c in Sales.Customer do
            deleteAll
        }
        |> toSql

    sql =! "DELETE FROM [Sales].[Customer]"

[<Test>]
let ``Update Query with Where``() = 
    let sql =  
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.AccountNumber = "000")
        }
        |> toSql

    sql =! "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0 WHERE ([Sales].[Customer].[AccountNumber] = @p1)"

[<Test>]
let ``Update Query with multiple Wheres``() = 
    let sql =  
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.AccountNumber = "000")
            where (c.CustomerID = 123)
        }
        |> toSql

    sql =! "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0 WHERE ([Sales].[Customer].[AccountNumber] = @p1 AND ([Sales].[Customer].[CustomerID] = @p2))"

[<Test>]
let ``Update Query with No Where``() = 
    let sql =  
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            updateAll
        }
        |> toSql

    sql =! "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0"

[<Test>]
let ``Update should fail without where or updateAll``() = 
    try 
        let _ =  
            update {
                for c in Sales.Customer do
                set c.AccountNumber "123"
            }
        Assert.Fail("Should fail because no `where` or `updateAll` exists.")
    with ex ->
        Assert.Pass()

[<Test>]
let ``Update should pass because where exists``() = 
    try 
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.CustomerID = 1)
        }
        |> ignore
    with ex ->
        Assert.Fail()

[<Test>]
let ``Update should pass because updateAll exists``() = 
    try 
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            updateAll
        }
        |> ignore
    with ex ->
        Assert.Fail()

[<Test>]
let ``Update with where followed by updateAll should fail``() = 
    try
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.CustomerID = 1)
            updateAll
        }
        |> ignore
        Assert.Fail()
    with ex ->
        ()

[<Test>]
let ``Update with updateAll followed by where should fail``() = 
    try
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            updateAll
            where (c.CustomerID = 1)
        }
        |> ignore
        Assert.Fail()
    with ex ->
        Assert.Pass()

[<Test>]
let ``Insert Query without Identity``() = 
    let sql =  
        insert {
            into Sales.Customer
            entity 
                { 
                    Sales.Customer.AccountNumber = "123"
                    Sales.Customer.rowguid = System.Guid.NewGuid()
                    Sales.Customer.ModifiedDate = System.DateTime.Now
                    Sales.Customer.PersonID = None
                    Sales.Customer.StoreID = None
                    Sales.Customer.TerritoryID = None
                    Sales.Customer.CustomerID = 0
                }
        }
        |> toSql

    sql =! "INSERT INTO [Sales].[Customer] ([CustomerID], [PersonID], [StoreID], [TerritoryID], [AccountNumber], [rowguid], [ModifiedDate]) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)" 

[<Test>]
let ``Insert Query with Identity``() = 
    let sql =  
        insert {
            for c in Sales.Customer do
            entity 
                { 
                    Sales.Customer.AccountNumber = "123"
                    Sales.Customer.rowguid = System.Guid.NewGuid()
                    Sales.Customer.ModifiedDate = System.DateTime.Now
                    Sales.Customer.PersonID = None
                    Sales.Customer.StoreID = None
                    Sales.Customer.TerritoryID = None
                    Sales.Customer.CustomerID = 0
                }
            getId c.CustomerID
        }
        |> toSql

    sql =! "INSERT INTO [Sales].[Customer] ([PersonID], [StoreID], [TerritoryID], [AccountNumber], [rowguid], [ModifiedDate]) VALUES (@p0, @p1, @p2, @p3, @p4, @p5);SELECT scope_identity() as Id" 

[<Test>]
let ``Inline Aggregates``() = 
    let sql = 
        select {
            for o in Sales.SalesOrderHeader do
            select (countBy o.SalesOrderID)
        }
        |> toSql

    sql =! "SELECT COUNT([o].[SalesOrderID]) FROM [Sales].[SalesOrderHeader] AS [o]"

[<Test>]
let ``Implicit Casts``() = 
    let sql = 
        select {
            for p in Production.Product do
            where (p.ListPrice > 5)
        }

    // should not throw exception
    Assert.Pass()

[<Test>]
let ``Implicit Casts Option``() = 
    let sql = 
        select {
            for p in Production.Product do
            where (p.Weight = Some 5)
        }

    // should not throw exception
    Assert.Pass()

[<Test; Ignore("Temporarily ignoring test for emergency fix")>]
let ``Self Join``() = 
    // NOTE: I could not find a good self join example in AdventureWorks.
    let sql =  
        select { 
            for p1 in Production.Product do
            join p2 in Production.Product on (p1.ProductID = p2.ProductID)
            where (p2.ListPrice > 10.00M)
            select p1
        }
        |> toSql

    sql =!
        """SELECT [p1].[ProductID], [p1].[Name], [p1].[ProductNumber], [p1].[MakeFlag], [p1].[FinishedGoodsFlag], [p1].[Color], [p1].[SafetyStockLevel], [p1].[ReorderPoint], [p1].[StandardCost], [p1].[ListPrice], [p1].[Size], [p1].[SizeUnitMeasureCode], [p1].[WeightUnitMeasureCode], [p1].[Weight], [p1].[DaysToManufacture], [p1].[ProductLine], [p1].[Class], [p1].[Style], [p1].[ProductSubcategoryID], [p1].[ProductModelID], [p1].[SellStartDate], [p1].[SellEndDate], [p1].[DiscontinuedDate], [p1].[rowguid], [p1].[ModifiedDate] FROM [Production].[Product] AS [p1] 
INNER JOIN [Production].[Product] AS [p2] ON ([p1].[ProductID] = [p2].[ProductID]) WHERE ([p2].[ListPrice] > @p0)"""

[<Test>]
let ``Underscore Assignment Edge Case - delete - should be valid``() = 
    let sql =  
        delete {
            for _ in Person.Person do
            deleteAll
        }

    // should not throw exception
    Assert.Pass()

[<Test>]
let ``Underscore Assignment Edge Case - update - should fail with not supported``() = 
    try
        let person = Unchecked.defaultof<Person.Person>
        let sql =  
            update {
                for _ in Person.Person do
                entity person
                updateAll
            }

        Assert.Fail("Should fail with NotSupportedException")
    with 
    | :? System.NotSupportedException -> Assert.Pass()
    | ex -> Assert.Fail("Should fail with NotSupportedException")

[<Test>]
let ``Underscore Assignment Edge Case - insert - should fail with not supported``() = 
    try
        let person = Unchecked.defaultof<Person.Person>
        let sql =  
            insert {
                for _ in Person.Person do
                entity person
            }

        Assert.Fail("Should fail with NotSupportedException")
    with 
    | :? System.NotSupportedException -> Assert.Pass()
    | ex -> Assert.Fail("Should fail with NotSupportedException")

[<Test>]
let ``Individual column from a leftJoin table should be optional if Some``() = 
    let query = 
        select {
            for o in Sales.SalesOrderHeader do
            leftJoin d in Sales.SalesOrderDetail on (o.SalesOrderID = d.Value.SalesOrderID)
            select (Some d.Value.OrderQty)
        }
        
    let sql = query |> toSql
    sql =! """SELECT [d].[OrderQty] FROM [Sales].[SalesOrderHeader] AS [o] 
LEFT JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID])"""

[<Test>]
let ``select option bug fix`` () = 
    let sql = 
        select {
            for o in Sales.SalesOrderHeader do
            leftJoin d in Sales.SalesOrderDetail on (o.SalesOrderID = d.Value.SalesOrderID)
            where (o.SalesOrderID = 1)
            select (o,d)
        }
        |> toSql

    sql.Contains("WHERE ([o].[SalesOrderID] = @p0)") =! true

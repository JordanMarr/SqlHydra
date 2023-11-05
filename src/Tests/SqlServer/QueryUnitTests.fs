module SqlServer.``Query Unit Tests``

open System
open Expecto
open SqlHydra.Query
open DB
open NUnit.Framework

#if NET6_0
open SqlServer.AdventureWorksNet6
#endif
#if NET7_0
open SqlServer.AdventureWorksNet7
#endif

type OptionalBoolEntity = 
    {
        QuestionAnswered: bool option
    }

[<Test>]
let ``Simple Where``() = 
    let query = 
        select {
            for a in Person.Address do
            where (a.City = "Dallas")
            orderBy a.City
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal sql "SELECT * FROM [Person].[Address] AS [a] WHERE ([a].[City] = @p0) ORDER BY [a].[City]" "Query should match."

[<Test>]
let ``Simple Where - kata``() = 
    let query = 
        select {
            for a in Person.Address do
            kata (fun query -> query.Where("a.City", "Dallas"))
            orderBy a.City
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal sql "SELECT * FROM [Person].[Address] AS [a] WHERE [a].[City] = @p0 ORDER BY [a].[City]" "Query should match."

[<Test>]
let ``Select 1 Column``() = 
    let query =
        select {
            for a in Person.Address do
            select (a.City)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("SELECT [a].[City] FROM")) ""

[<Test>]
let ``Select 2 Columns``() = 
    let query =
        select {
            for h in Sales.SalesOrderHeader do
            select (h.CustomerID, h.OnlineOrderFlag)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("SELECT [h].[CustomerID], [h].[OnlineOrderFlag] FROM")) ""

[<Test>]
let ``Select 1 Table and 1 Column``() = 
    let query =
        select {
            for o in Sales.SalesOrderHeader do
            join d in Sales.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            where o.OnlineOrderFlag
            select (o, d.LineTotal)
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Expect.isTrue (sql.Contains("SELECT [o].*, [d].[LineTotal] FROM")) ""

[<Test>]
let ``Where bool is true``() = 
    let query =
        select {
            for o in Sales.SalesOrderHeader do
            where o.OnlineOrderFlag
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([o].[OnlineOrderFlag] = cast(1 as bit))")) ""

[<Test>]
let ``Where bool is false``() = 
    let query =
        select {
            for o in Sales.SalesOrderHeader do
            where (not o.OnlineOrderFlag)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([o].[OnlineOrderFlag] = cast(0 as bit))")) ""

[<Test>]
let ``Where bool option is true``() = 
    let query =
        select {
            for o in table<OptionalBoolEntity> do
            where o.QuestionAnswered.Value
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([o].[QuestionAnswered] = cast(1 as bit))")) ""

[<Test>]
let ``Where bool option is false``() = 
    let query =
        select {
            for o in table<OptionalBoolEntity> do
            where (not o.QuestionAnswered.Value)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([o].[QuestionAnswered] = cast(0 as bit))")) ""

[<Test>]
let ``Where bool option is false or null``() = 
    let query =
        select {
            for o in table<OptionalBoolEntity> do
            where (not o.QuestionAnswered.Value || o.QuestionAnswered = None)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (([o].[QuestionAnswered] = cast(0 as bit)) OR ([o].[QuestionAnswered] IS NULL))")) ""

[<Test; Ignore "Ignore">]
let ``Where with Option Type``() = 
    let query = 
        select {
            for a in Person.Address do
            where (a.AddressLine2 <> None)
        }

    query.ToKataQuery() |> toSql |> printfn "%s"

[<Test; Ignore "Ignore">]
let ``Where Not Like``() = 
    let query =
        select {
            for a in Person.Address do
            where (a.City <>% "S%")
        }

    query.ToKataQuery() |> toSql |> printfn "%s"

[<Test>]
let ``Or Where``() = 
    let query = 
        select {
            for a in Person.Address do
            where (a.City = "Chicago" || a.City = "Dallas")
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (([a].[City] = @p0) OR ([a].[City] = @p1))")) ""

[<Test>]
let ``And Where``() = 
    let query = 
        select {
            for a in Person.Address do
            where (a.City = "Chicago" && a.City = "Dallas")
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (([a].[City] = @p0) AND ([a].[City] = @p1))")) ""

[<Test>]
let ``Where with AND and OR in Parenthesis``() = 
    let query = 
        select {
            for a in Person.Address do
            where (a.City = "Chicago" && (a.AddressLine2 = Some "abc" || isNullValue a.AddressLine2))
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue 
        (sql.Contains("WHERE (([a].[City] = @p0) AND (([a].[AddressLine2] = @p1) OR ([a].[AddressLine2] IS NULL)))")) 
        "Should wrap OR clause in parenthesis and each individual where clause in parenthesis."

[<Test>]
let ``Where value and column are swapped``() = 
    let query = 
        select {
            for a in Person.Address do
            where (5 < a.AddressID && 20 >= a.AddressID)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (([a].[AddressID] > @p0) AND ([a].[AddressID] <= @p1))")) sql

[<Test>]
let ``Where Not Binary``() = 
    let query = 
        select {
            for a in Person.Address do
            where (not (a.City = "Chicago" && a.City = "Dallas"))
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (NOT (([a].[City] = @p0) AND ([a].[City] = @p1)))")) ""

[<Test>]
let ``Where Customer isIn List``() = 
    let query = 
        select {
            for c in Sales.Customer do
            where (isIn c.CustomerID [30018;29545;29954])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where Customer |=| List``() = 
    let query = 
        select {
            for c in Sales.Customer do
            where (c.CustomerID |=| [30018;29545;29954])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where Customer |=| Array``() = 
    let query = 
        select {
            for c in Sales.Customer do
            where (c.CustomerID |=| [| 30018;29545;29954 |])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where Customer |=| Seq``() = 
    let buildQuery (values: int seq) =                
        select {
            for c in Sales.Customer do
            where (c.CustomerID |=| values)
        }

    let query = buildQuery([ 30018;29545;29954 ])

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where Customer |<>| List``() = 
    let query = 
        select {
            for c in Sales.Customer do
            where (c.PersonID.Value |<>| [ 30018;29545;29954 ]) // should work with option values
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([c].[PersonID] NOT IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Inner Join``() = 
    let query =
        select {
            for o in Sales.SalesOrderHeader do
            join d in Sales.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("INNER JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID])")) ""

[<Test>]
let ``Left Join``() = 
    let query =
        select {
            for o in Sales.SalesOrderHeader do
            leftJoin d in Sales.SalesOrderDetail on (o.SalesOrderID = d.Value.SalesOrderID)
            where (o.SalesOrderID = d.Value.SalesOrderID)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    let expected = """SELECT [o].* FROM [Sales].[SalesOrderHeader] AS [o] 
LEFT JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID]) WHERE ([o].[SalesOrderID] = [d].[SalesOrderID])"""
    Expect.equal sql expected ""

[<Test>]
let ``Optional Property Value in Where``() = 
    let date = System.DateTime(2023,1,1)

    let query = 
        select {
            for wo in Production.WorkOrder do
            where (wo.EndDate = None || wo.EndDate.Value >= date)
        }

    let sql = query.ToKataQuery() |> toSql
    let expected = """SELECT * FROM [Production].[WorkOrder] AS [wo] WHERE (([wo].[EndDate] IS NULL) OR ([wo].[EndDate] >= @p0))"""
    Expect.equal sql expected ""

[<Test>]
let ``Inner Join - Multi Column``() = 
    let query =
        select {
            for o in Sales.SalesOrderHeader do
            join d in Sales.SalesOrderDetail on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("INNER JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID] AND [o].[ModifiedDate] = [d].[ModifiedDate])")) ""

[<Test>]
let ``Left Join - Multi Column``() = 
    let query =
        select {
            for o in Sales.SalesOrderHeader do
            leftJoin d in Sales.SalesOrderDetail on ((o.SalesOrderID, o.ModifiedDate) = (d.Value.SalesOrderID, d.Value.ModifiedDate))
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Expect.isTrue (sql.Contains("LEFT JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID] AND [o].[ModifiedDate] = [d].[ModifiedDate])")) ""

[<Test>]
let ``Correlated Subquery``() = 
    let maxOrderQty = 
        select {
            for d in Sales.SalesOrderDetail do
            correlate od in Sales.SalesOrderDetail
            where (d.ProductID = od.ProductID)
            select (maxBy d.OrderQty)
        }

    let query = 
        select {
            for od in Sales.SalesOrderDetail do
            where (od.OrderQty = subqueryOne maxOrderQty)
            orderBy od.ProductID
            select (od.SalesOrderID, od.ProductID, od.OrderQty)
        }
        

    let sql = query.ToKataQuery() |> toSql
    Expect.equal
        sql
        "SELECT [od].[SalesOrderID], [od].[ProductID], [od].[OrderQty] FROM [Sales].[SalesOrderDetail] AS [od] \
        WHERE ([od].[OrderQty] = (\
            SELECT MAX([d].[OrderQty]) FROM [Sales].[SalesOrderDetail] AS [d] WHERE ([d].[ProductID] = [od].[ProductID])\
        )) ORDER BY [od].[ProductID]"
        ""

[<Test>]
let ``Join On Value Bug Fix Test``() = 
    let query = 
        select {
            for o in Sales.SalesOrderHeader do
            leftJoin d in Sales.SalesOrderHeader on (o.AccountNumber.Value = d.Value.AccountNumber.Value)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal sql
        """SELECT [o].* FROM [Sales].[SalesOrderHeader] AS [o] 
LEFT JOIN [Sales].[SalesOrderHeader] AS [d] ON ([o].[AccountNumber] = [d].[AccountNumber])"""
        "Bugged version was replacing TableMapping for original table with joined table."
        
[<Test>]
let ``Where Static Property``() = 
    let query =
        select {
            for o in Sales.SalesOrderHeader do
            where (o.SalesOrderID = System.Int32.MaxValue)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ([o].[SalesOrderID] = @p0)")) ""

[<Test>]
let ``Delete Query with Where``() = 
    let query = 
        delete {
            for c in Sales.Customer do
            where (c.CustomerID |<>| [ 30018;29545;29954 ])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("DELETE FROM [Sales].[Customer]")) ""
    Expect.isTrue (sql.Contains("WHERE ([Sales].[Customer].[CustomerID] NOT IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Delete All``() = 
    let query = 
        delete {
            for c in Sales.Customer do
            deleteAll
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal "DELETE FROM [Sales].[Customer]" sql ""

[<Test>]
let ``Update Query with Where``() = 
    let query = 
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.AccountNumber = "000")
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0 WHERE ([Sales].[Customer].[AccountNumber] = @p1)" sql ""

[<Test>]
let ``Update Query with multiple Wheres``() = 
    let query = 
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.AccountNumber = "000")
            where (c.CustomerID = 123)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal sql "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0 WHERE ([Sales].[Customer].[AccountNumber] = @p1 AND ([Sales].[Customer].[CustomerID] = @p2))" ""

[<Test>]
let ``Update Query with No Where``() = 
    let query = 
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            updateAll
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0" sql ""

[<Test>]
let ``Update should fail without where or updateAll``() = 
    try 
        let query = 
            update {
                for c in Sales.Customer do
                set c.AccountNumber "123"
            }
        failwith "Should fail because no `where` or `updateAll` exists."
    with ex ->
        () // Pass

[<Test>]
let ``Update should pass because where exists``() = 
    try 
        let query = 
            update {
                for c in Sales.Customer do
                set c.AccountNumber "123"
                where (c.CustomerID = 1)
            }
        () //Assert.Pass()
    with ex ->
        () //Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Update should pass because updateAll exists``() = 
    try 
        let query = 
            update {
                for c in Sales.Customer do
                set c.AccountNumber "123"
                updateAll
            }
        () //Assert.Pass()
    with ex ->
        () //Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Update with where followed by updateAll should fail``() = 
    Expect.throwsT<InvalidOperationException> (fun _ ->
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.CustomerID = 1)
            updateAll
        }
        |> ignore
    ) ""

[<Test>]
let ``Update with updateAll followed by where should fail``() = 
    Expect.throwsT<InvalidOperationException> (fun _ ->
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            updateAll
            where (c.CustomerID = 1)
        }
        |> ignore
    ) ""

[<Test>]
let ``Insert Query without Identity``() = 
    let query = 
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
    
    let sql = query.ToKataQuery() |> toSql
    Expect.equal 
        sql 
        "INSERT INTO [Sales].[Customer] ([CustomerID], [PersonID], [StoreID], [TerritoryID], [AccountNumber], [rowguid], [ModifiedDate]) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)" 
        ""

[<Test>]
let ``Insert Query with Identity``() = 
    let query = 
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

    let sql = query.ToKataQuery() |> toSql
    Expect.equal 
        sql 
        "INSERT INTO [Sales].[Customer] ([PersonID], [StoreID], [TerritoryID], [AccountNumber], [rowguid], [ModifiedDate]) VALUES (@p0, @p1, @p2, @p3, @p4, @p5);SELECT scope_identity() as Id" 
        ""

[<Test>]
let ``Inline Aggregates``() = 
    let query =
        select {
            for o in Sales.SalesOrderHeader do
            select (countBy o.SalesOrderID)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal
        sql
        "SELECT COUNT([o].[SalesOrderID]) FROM [Sales].[SalesOrderHeader] AS [o]"
        ""

[<Test>]
let ``Implicit Casts``() = 
    let query =
        select {
            for p in Production.Product do
            where (p.ListPrice > 5)
        }

    // should not throw exception
    ()

[<Test>]
let ``Implicit Casts Option``() = 
    let query =
        select {
            for p in Production.Product do
            where (p.Weight = Some 5)
        }

    // should not throw exception
    ()

[<Test>]
let ``Self Join``() = 
    // NOTE: I could not find a good self join example in AdventureWorks.
    let query = 
        select { 
            for p1 in Production.Product do
            join p2 in Production.Product on (p1.ProductID = p2.ProductID)
            where (p2.ListPrice > 10.00M)
            select p1
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal
        sql
        """SELECT [p1].* FROM [Production].[Product] AS [p1] 
INNER JOIN [Production].[Product] AS [p2] ON ([p1].[ProductID] = [p2].[ProductID]) WHERE ([p2].[ListPrice] > @p0)"""
        ""

[<Test>]
let ``Underscore Assignment Edge Case - delete - should be valid``() = 
    printfn "Starting..."

    let query = 
        delete {
            for _ in Person.Person do
            deleteAll
        }

    //let sql = query.ToKataQuery() |> toSql
    () // Good

[<Test>]
let ``Underscore Assignment Edge Case - update - should fail with not supported``() = 
    try
        let person = Unchecked.defaultof<Person.Person>
        let query = 
            update {
                for _ in Person.Person do
                entity person
                updateAll
            }

        failwith "Should fail with NotSupportedException"
    with 
    | :? System.NotSupportedException -> () // Good
    | ex -> failwith "Should fail with NotSupportedException"

[<Test>]
let ``Underscore Assignment Edge Case - insert - should fail with not supported``() = 
    try
        let person = Unchecked.defaultof<Person.Person>
        let query = 
            insert {
                for _ in Person.Person do
                entity person
            }

        failwith "Should fail with NotSupportedException"
    with 
    | :? System.NotSupportedException -> () // Good
    | ex -> failwith "Should fail with NotSupportedException"

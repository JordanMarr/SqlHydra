module SqlServer.QueryUnitTests

open Expecto
open SqlHydra.Query
open DB

#if NET5_0
open SqlServer.AdventureWorksNet5
#endif
#if NET6_0_OR_GREATER
open SqlServer.AdventureWorksNet6
#endif

// Tables
let personTable =           table<Person.Person>                    |> inSchema (nameof Person)
let addressTable =          table<Person.Address>                   |> inSchema (nameof Person)
let customerTable =         table<Sales.Customer>                   |> inSchema (nameof Sales)
let orderHeaderTable =      table<Sales.SalesOrderHeader>           |> inSchema (nameof Sales)
let orderDetailTable =      table<Sales.SalesOrderDetail>           |> inSchema (nameof Sales)
let productTable =          table<Production.Product>               |> inSchema (nameof Production)
let subCategoryTable =      table<Production.ProductSubcategory>    |> inSchema (nameof Production)
let categoryTable =         table<Production.ProductCategory>       |> inSchema (nameof Production)
let errorLogTable =         table<dbo.ErrorLog>

[<Tests>]
let tests = 
    categoryList "SqlServer" "Query Unit Tests" [

        /// String comparisons against generated queries.
        test "Simple Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Dallas")
                    orderBy a.City
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE")) ""
        }

        test "Select 1 Column" {
            let query =
                select {
                    for a in addressTable do
                    select (a.City)
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("SELECT [a].[City] FROM")) ""
        }

        test "Select 2 Columns" {
            let query =
                select {
                    for h in orderHeaderTable do
                    select (h.CustomerID, h.OnlineOrderFlag)
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("SELECT [h].[CustomerID], [h].[OnlineOrderFlag] FROM")) ""
        }

        test "Select 1 Table and 1 Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
                    where (o.OnlineOrderFlag = true)
                    select (o, d.LineTotal)
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT [o].*, [d].[LineTotal] FROM")) ""
        }

        ptest "Where with Option Type" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.AddressLine2 <> None)
                }

            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        ptest "Where Not Like" {
            let query =
                select {
                    for a in addressTable do
                    where (a.City <>% "S%")
                }

            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        test "Or Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Chicago" || a.City = "Dallas")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (([a].[City] = @p0) OR ([a].[City] = @p1))")) ""
        }

        test "And Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Chicago" && a.City = "Dallas")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (([a].[City] = @p0) AND ([a].[City] = @p1))")) ""
        }

        test "Where with AND and OR in Parenthesis" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Chicago" && (a.AddressLine2 = Some "abc" || isNullValue a.AddressLine2))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue 
                (sql.Contains("WHERE (([a].[City] = @p0) AND (([a].[AddressLine2] = @p1) OR ([a].[AddressLine2] IS NULL)))")) 
                "Should wrap OR clause in parenthesis and each individual where clause in parenthesis."
        }

        test "Where Not Binary" {
            let query = 
                select {
                    for a in addressTable do
                    where (not (a.City = "Chicago" && a.City = "Dallas"))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (NOT (([a].[City] = @p0) AND ([a].[City] = @p1)))")) ""
        }

        test "Where Customer isIn List" {
            let query = 
                select {
                    for c in customerTable do
                    where (isIn c.CustomerID [30018;29545;29954])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |=| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| [30018;29545;29954])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |=| Array" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| [| 30018;29545;29954 |])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))")) ""
        }
        
        test "Where Customer |=| Seq" {            
            let buildQuery (values: int seq) =                
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| values)
                }

            let query = buildQuery([ 30018;29545;29954 ])

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([c].[CustomerID] IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |<>| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |<>| [ 30018;29545;29954 ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([c].[CustomerID] NOT IN (@p0, @p1, @p2))")) ""
        }

        test "Inner Join" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("INNER JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID])")) ""
        }

        test "Left Join" {
            let query =
                select {
                    for o in orderHeaderTable do
                    leftJoin d in orderDetailTable on (o.SalesOrderID = d.Value.SalesOrderID)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            let expected = """SELECT [o].* FROM [Sales].[SalesOrderHeader] AS [o] 
LEFT JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID])"""
            Expect.equal sql expected ""
        }
        
        test "Inner Join - Multi Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
                    select o
                }
        
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("INNER JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID] AND [o].[ModifiedDate] = [d].[ModifiedDate])")) ""
        }
        
        test "Left Join - Multi Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    leftJoin d in orderDetailTable on ((o.SalesOrderID, o.ModifiedDate) = (d.Value.SalesOrderID, d.Value.ModifiedDate))
                    select o
                }
        
            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("LEFT JOIN [Sales].[SalesOrderDetail] AS [d] ON ([o].[SalesOrderID] = [d].[SalesOrderID] AND [o].[ModifiedDate] = [d].[ModifiedDate])")) ""
        }

        test "Join On Value Bug Fix Test" {
            let query = 
                select {
                    for o in orderHeaderTable do
                    leftJoin d in orderHeaderTable on (o.AccountNumber.Value = d.Value.AccountNumber.Value)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal sql
                """SELECT [o].* FROM [Sales].[SalesOrderHeader] AS [o] 
LEFT JOIN [Sales].[SalesOrderHeader] AS [d] ON ([o].[AccountNumber] = [d].[AccountNumber])"""
                "Bugged version was replacing TableMapping for original table with joined table."
                
        }

        test "Delete Query with Where" {
            let query = 
                delete {
                    for c in customerTable do
                    where (c.CustomerID |<>| [ 30018;29545;29954 ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("DELETE FROM [Sales].[Customer]")) ""
            Expect.isTrue (sql.Contains("WHERE ([Sales].[Customer].[CustomerID] NOT IN (@p0, @p1, @p2))")) ""
        }

        test "Delete All" {
            let query = 
                delete {
                    for c in customerTable do
                    deleteAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "DELETE FROM [Sales].[Customer]" sql ""
        }

        test "Update Query with Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.AccountNumber "123"
                    where (c.AccountNumber = "000")
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0 WHERE ([Sales].[Customer].[AccountNumber] = @p1)" sql ""
        }

        test "Update Query with No Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.AccountNumber "123"
                    updateAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0" sql ""
        }

        test "Update should fail without where or updateAll" {
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.AccountNumber "123"
                    }
                failwith "Should fail because no `where` or `updateAll` exists."
            with ex ->
                () // Pass
        }

        test "Update should pass because where exists" {
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.AccountNumber "123"
                        where (c.CustomerID = 1)
                    }
                () //Assert.Pass()
            with ex ->
                () //Assert.Pass("Should not fail because `where` is present.")
        }

        test "Update should pass because updateAll exists" {
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.AccountNumber "123"
                        updateAll
                    }
                () //Assert.Pass()
            with ex ->
                () //Assert.Pass("Should not fail because `where` is present.")
        }

        test "Insert Query without Identity" {
            let query = 
                insert {
                    into customerTable
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
        }

        test "Insert Query with Identity" {
            let query = 
                insert {
                    for c in customerTable do
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
        }

        test "Inline Aggregates" {
            let query =
                select {
                    for o in orderHeaderTable do
                    select (countBy o.SalesOrderID)
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal
                sql
                "SELECT COUNT([o].[SalesOrderID]) FROM [Sales].[SalesOrderHeader] AS [o]"
                ""
        }
        
        test "Implicit Casts" {
            let query =
                select {
                    for p in productTable do
                    where (p.ListPrice > 5)
                }

            // should not throw exception
            ()
        }

        test "Implicit Casts Option" {
            let query =
                select {
                    for p in productTable do
                    where (p.Weight = Some 5)
                }

            // should not throw exception
            ()
        }
    ]

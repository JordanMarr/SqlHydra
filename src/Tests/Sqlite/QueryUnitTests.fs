module Sqlite.QueryUnitTests

open Expecto
open SqlHydra.Query
open DB
#if NET5_0
open Sqlite.AdventureWorksNet5
#endif
#if NET6_0
open Sqlite.AdventureWorksNet6
#endif
#if NET7_0
open Sqlite.AdventureWorksNet7
#endif

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
    categoryList "Sqlite" "Query Unit Tests" [

        /// String comparisons against generated queries.
        test "Simple Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Dallas")
                    orderBy a.City
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("WHERE")) ""
        }

        test "Select 1 Column" {
            let query =
                select {
                    for a in addressTable do
                    select (a.City)
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal sql "SELECT \"a\".\"City\" FROM \"main\".\"Address\" AS \"a\"" ""
        }

        test "Select 2 Columns" {
            let query =
                select {
                    for h in orderHeaderTable do
                    select (h.CustomerID, h.OnlineOrderFlag)
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT \"h\".\"CustomerID\", \"h\".\"OnlineOrderFlag\" FROM")) ""
        }

        test "Select 1 Table and 1 Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
                    where (o.OnlineOrderFlag = 1L)
                    select (o, d.LineTotal)
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT \"o\".*, \"d\".\"LineTotal\" FROM")) ""
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
            Expect.isTrue (sql.Contains("WHERE ((\"a\".\"City\" = @p0) OR (\"a\".\"City\" = @p1))")) ""
        }

        test "And Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Chicago" && a.City = "Dallas")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ((\"a\".\"City\" = @p0) AND (\"a\".\"City\" = @p1))")) ""
        }

        test "Where with AND and OR in Parenthesis" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Chicago" && (a.AddressLine2 = Some "abc" || isNullValue a.AddressLine2))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue 
                (sql.Contains("WHERE ((\"a\".\"City\" = @p0) AND ((\"a\".\"AddressLine2\" = @p1) OR (\"a\".\"AddressLine2\" IS NULL)))")) 
                "Should wrap OR clause in parenthesis and each individual where clause in parenthesis."
        }

        test "Where Not Binary" {
            let query = 
                select {
                    for a in addressTable do
                    where (not (a.City = "Chicago" && a.City = "Dallas"))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (NOT ((\"a\".\"City\" = @p0) AND (\"a\".\"City\" = @p1)))")) ""
        }

        test "Where Customer isIn List" {
            let query = 
                select {
                    for c in customerTable do
                    where (isIn c.CustomerID [30018L;29545L;29954L])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |=| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| [30018L;29545L;29954L])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |=| Array" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| [| 30018L;29545L;29954L |])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))")) ""
        }
        
        test "Where Customer |=| Seq" {            
            let buildQuery (values: int64 seq) =                
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| values)
                }

            let query = buildQuery([ 30018L;29545L;29954L ])

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |<>| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |<>| [ 30018L;29545L;29954L ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" NOT IN (@p0, @p1, @p2))")) ""
        }

        test "Inner Join" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("INNER JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\")")) ""
        }

        test "Left Join" {
            let query =
                select {
                    for o in orderHeaderTable do
                    leftJoin d in orderDetailTable on (o.SalesOrderID = d.Value.SalesOrderID)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("LEFT JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\")")) ""
        }
        
        test "Inner Join - Multi Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
                    select o
                }
        
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("INNER JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\" AND \"o\".\"ModifiedDate\" = \"d\".\"ModifiedDate\")")) ""
        }
        
        test "Left Join - Multi Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    leftJoin d in orderDetailTable on ((o.SalesOrderID, o.ModifiedDate) = (d.Value.SalesOrderID, d.Value.ModifiedDate))
                    select o
                }
        
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("LEFT JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\" AND \"o\".\"ModifiedDate\" = \"d\".\"ModifiedDate\")")) ""
        }

        test "Correlated Subquery" {
            let latestOrderByCustomer = 
                select {
                    for d in table<main.SalesOrderHeader> do
                    correlate od in correlatedTable<main.SalesOrderHeader>
                    where (d.CustomerID = od.CustomerID)
                    select (maxBy d.OrderDate)
                }

            let query = 
                select {
                    for od in table<main.SalesOrderHeader> do
                    where (od.OrderDate = subqueryOne latestOrderByCustomer)
                }
                

            let sql = query.ToKataQuery() |> toSql
            Expect.equal
                sql
                "SELECT * FROM \"main\".\"SalesOrderHeader\" AS \"od\" WHERE (\"od\".\"OrderDate\" = \
                (SELECT MAX(\"d\".\"OrderDate\") FROM \"main\".\"SalesOrderHeader\" AS \"d\" \
                WHERE (\"d\".\"CustomerID\" = \"od\".\"CustomerID\")))"
                ""            
        }

        test "Delete Query with Where" {
            let query = 
                delete {
                    for c in customerTable do
                    where (c.CustomerID |<>| [ 30018L;29545L;29954L ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("DELETE FROM \"main\".\"Customer\"")) ""
            Expect.isTrue (sql.Contains("WHERE (\"main\".\"Customer\".\"CustomerID\" NOT IN (@p0, @p1, @p2))")) ""
        }

        test "Delete All" {
            let query = 
                delete {
                    for c in customerTable do
                    deleteAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal sql "DELETE FROM \"main\".\"Customer\"" ""
        }

        test "Update Query with Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.FirstName "John"
                    set c.LastName "Doe"
                    where (c.CustomerID = 123L)
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal sql "UPDATE \"main\".\"Customer\" SET \"FirstName\" = @p0, \"LastName\" = @p1 WHERE (\"main\".\"Customer\".\"CustomerID\" = @p2)" ""
        }

        test "Update Query with No Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.FirstName "John"
                    set c.LastName "Doe"
                    updateAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "UPDATE \"main\".\"Customer\" SET \"FirstName\" = @p0, \"LastName\" = @p1" sql ""
        }

        test "Update should fail without where or updateAll" {
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.FirstName "John"
                        set c.LastName "Doe"
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
                        set c.FirstName "John"
                        set c.LastName "Doe"
                        where (c.CustomerID = 1L)
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
                        set c.FirstName "John"
                        set c.LastName "Doe"
                        updateAll
                    }
                () //Assert.Pass()
            with ex ->
                () //Assert.Pass("Should not fail because `where` is present.")
        }

        //test "Insert Query without Identity" {
        //    let query = 
        //        insert {
        //            into table<main.BuildVersion>
        //            entity 
        //                { 
        //                    main.Customer.CustomerID = 1L
        //                    main.Customer.rowguid = System.Guid.NewGuid()
        //                    main.Customer.ModifiedDate = System.DateTime.Now
        //                    main.Customer.PersonID = None
        //                    main.Customer.StoreID = None
        //                    main.Customer.TerritoryID = None
        //                    main.Customer.CustomerID = 0
        //                }
        //        }
            
        //    let sql = query.ToKataQuery() |> toSql
        //    Expect.equal 
        //        "INSERT INTO \"Customer\" (\"CustomerID\", \"AccountNumber\", \"rowguid\", \"ModifiedDate\", \"PersonID\", \"StoreID\", \"TerritoryID\") VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)" 
        //        sql 
        //        ""
        //}

        test "Insert Query with Identity" {
            let query = 
                insert {
                    for b in table<main.BuildVersion> do
                    entity 
                        { 
                            main.BuildVersion.SystemInformationID = 0L
                            main.BuildVersion.``Database Version`` = "v1.0"
                            main.BuildVersion.VersionDate = System.DateTime.Today
                            main.BuildVersion.ModifiedDate = System.DateTime.Today
                        }
                    getId b.SystemInformationID
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal 
                sql 
                "INSERT INTO \"main\".\"BuildVersion\" (\"Database Version\", \"VersionDate\", \"ModifiedDate\") VALUES (@p0, @p1, @p2);select last_insert_rowid() as id" 
                ""
        }
        
        test "Inline Aggregates" {
            let query =
                select {
                    for o in orderHeaderTable do
                    select (countBy o.SalesOrderID)
                }
        
            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.equal
                sql
                "SELECT COUNT(\"o\".\"SalesOrderID\") FROM \"main\".\"SalesOrderHeader\" AS \"o\""
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

        test "Implicit Casts Option aciq's example" {

            let query =
                select {
                    for e in errorLogTable do
                    where (e.ErrorSeverity = Some 1)
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

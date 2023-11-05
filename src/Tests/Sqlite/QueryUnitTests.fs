
module Sqlite.``Query Unit Tests``

open System
open Expecto
open SqlHydra.Query
open DB
open NUnit.Framework
#if NET6_0
open Sqlite.AdventureWorksNet6
#endif
#if NET7_0
open Sqlite.AdventureWorksNet7
#endif

[<Test>]
let ``Simple Where``() = 
    let query = 
        select {
            for a in main.Address do
            where (a.City = "Dallas")
            orderBy a.City
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Expect.isTrue (sql.Contains("WHERE")) ""

[<Test>]
let ``Select 1 Column``() = 
    let query =
        select {
            for a in main.Address do
            select (a.City)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal sql "SELECT \"a\".\"City\" FROM \"main\".\"Address\" AS \"a\"" ""

[<Test>]
let ``Select 2 Columns``() = 
    let query =
        select {
            for h in main.SalesOrderHeader do
            select (h.CustomerID, h.OnlineOrderFlag)
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Expect.isTrue (sql.Contains("SELECT \"h\".\"CustomerID\", \"h\".\"OnlineOrderFlag\" FROM")) ""

[<Test>]
let ``Select 1 Table and 1 Column``() = 
    let query =
        select {
            for o in main.SalesOrderHeader do
            join d in main.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            where (o.OnlineOrderFlag = 1L)
            select (o, d.LineTotal)
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Expect.isTrue (sql.Contains("SELECT \"o\".*, \"d\".\"LineTotal\" FROM")) ""

[<Test>]
let ``Where with Option Type``() = 
    let query = 
        select {
            for a in main.Address do
            where (a.AddressLine2 <> None)
        }

    query.ToKataQuery() |> toSql |> printfn "%s"

[<Test>]
let ``Where Not Like``() = 
    let query =
        select {
            for a in main.Address do
            where (a.City <>% "S%")
        }

    query.ToKataQuery() |> toSql |> printfn "%s"

[<Test>]
let ``Or Where``() = 
    let query = 
        select {
            for a in main.Address do
            where (a.City = "Chicago" || a.City = "Dallas")
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ((\"a\".\"City\" = @p0) OR (\"a\".\"City\" = @p1))")) ""

[<Test>]
let ``And Where``() = 
    let query = 
        select {
            for a in main.Address do
            where (a.City = "Chicago" && a.City = "Dallas")
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ((\"a\".\"City\" = @p0) AND (\"a\".\"City\" = @p1))")) ""

[<Test>]
let ``Where with AND and OR in Parenthesis``() = 
    let query = 
        select {
            for a in main.Address do
            where (a.City = "Chicago" && (a.AddressLine2 = Some "abc" || isNullValue a.AddressLine2))
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue 
        (sql.Contains("WHERE ((\"a\".\"City\" = @p0) AND ((\"a\".\"AddressLine2\" = @p1) OR (\"a\".\"AddressLine2\" IS NULL)))")) 
        "Should wrap OR clause in parenthesis and each individual where clause in parenthesis."

[<Test>]
let ``Where value and column are swapped``() = 
    let query = 
        select {
            for a in main.Address do
            where (5L < a.AddressID && 20L >= a.AddressID)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ((\"a\".\"AddressID\" > @p0) AND (\"a\".\"AddressID\" <= @p1))")) sql

[<Test>]
let ``Where Not Binary``() = 
    let query = 
        select {
            for a in main.Address do
            where (not (a.City = "Chicago" && a.City = "Dallas"))
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (NOT ((\"a\".\"City\" = @p0) AND (\"a\".\"City\" = @p1)))")) ""

[<Test>]
let ``Where Customer isIn List``() = 
    let query = 
        select {
            for c in main.Customer do
            where (isIn c.CustomerID [30018L;29545L;29954L])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where Customer |=| List``() = 
    let query = 
        select {
            for c in main.Customer do
            where (c.CustomerID |=| [30018L;29545L;29954L])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where Customer |=| Array``() = 
    let query = 
        select {
            for c in main.Customer do
            where (c.CustomerID |=| [| 30018L;29545L;29954L |])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where Customer |=| Seq``() = 
    let buildQuery (values: int64 seq) =                
        select {
            for c in main.Customer do
            where (c.CustomerID |=| values)
        }

    let query = buildQuery([ 30018L;29545L;29954L ])

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where Customer |<>| List``() = 
    let query = 
        select {
            for c in main.Customer do
            where (c.CustomerID |<>| [ 30018L;29545L;29954L ])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"CustomerID\" NOT IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Inner Join``() = 
    let query =
        select {
            for o in main.SalesOrderHeader do
            join d in main.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("INNER JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\")")) ""

[<Test>]
let ``Left Join``() = 
    let query =
        select {
            for o in main.SalesOrderHeader do
            leftJoin d in main.SalesOrderDetail on (o.SalesOrderID = d.Value.SalesOrderID)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("LEFT JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\")")) ""

[<Test>]
let ``Inner Join - Multi Column``() = 
    let query =
        select {
            for o in main.SalesOrderHeader do
            join d in main.SalesOrderDetail on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("INNER JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\" AND \"o\".\"ModifiedDate\" = \"d\".\"ModifiedDate\")")) ""

[<Test>]
let ``Left Join - Multi Column``() = 
    let query =
        select {
            for o in main.SalesOrderHeader do
            leftJoin d in main.SalesOrderDetail on ((o.SalesOrderID, o.ModifiedDate) = (d.Value.SalesOrderID, d.Value.ModifiedDate))
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("LEFT JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\" AND \"o\".\"ModifiedDate\" = \"d\".\"ModifiedDate\")")) ""

[<Test>]
let ``Correlated Subquery``() = 
    let latestOrderByCustomer = 
        select {
            for d in main.SalesOrderHeader do
            correlate od in main.SalesOrderHeader
            where (d.CustomerID = od.CustomerID)
            select (maxBy d.OrderDate)
        }

    let query = 
        select {
            for od in main.SalesOrderHeader do
            where (od.OrderDate = subqueryOne latestOrderByCustomer)
        }
        

    let sql = query.ToKataQuery() |> toSql
    Expect.equal
        sql
        "SELECT * FROM \"main\".\"SalesOrderHeader\" AS \"od\" WHERE (\"od\".\"OrderDate\" = \
        (SELECT MAX(\"d\".\"OrderDate\") FROM \"main\".\"SalesOrderHeader\" AS \"d\" \
        WHERE (\"d\".\"CustomerID\" = \"od\".\"CustomerID\")))"
        ""            

[<Test>]
let ``Delete Query with Where``() = 
    let query = 
        delete {
            for c in main.Customer do
            where (c.CustomerID |<>| [ 30018L;29545L;29954L ])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("DELETE FROM \"main\".\"Customer\"")) ""
    Expect.isTrue (sql.Contains("WHERE (\"main\".\"Customer\".\"CustomerID\" NOT IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Delete All``() = 
    let query = 
        delete {
            for c in main.Customer do
            deleteAll
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal sql "DELETE FROM \"main\".\"Customer\"" ""

[<Test>]
let ``Update Query with Where``() = 
    let query = 
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            where (c.CustomerID = 123L)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal sql """UPDATE "main"."Customer" SET "FirstName" = @p0, "LastName" = @p1 WHERE ("main"."Customer"."CustomerID" = @p2)""" ""

[<Test>]
let ``Update Query with multiple Wheres``() = 
    let query = 
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            where (c.CustomerID = 123L)
            where (c.FirstName = "Bob")
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal sql """UPDATE "main"."Customer" SET "FirstName" = @p0, "LastName" = @p1 WHERE ("main"."Customer"."CustomerID" = @p2 AND ("main"."Customer"."FirstName" = @p3))""" ""

[<Test>]
let ``Update Query with No Where``() = 
    let query = 
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            updateAll
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal "UPDATE \"main\".\"Customer\" SET \"FirstName\" = @p0, \"LastName\" = @p1" sql ""

[<Test>]
let ``Update should fail without where or updateAll``() = 
    try 
        let query = 
            update {
                for c in main.Customer do
                set c.FirstName "John"
                set c.LastName "Doe"
            }
        failwith "Should fail because no `where` or `updateAll` exists."
    with ex ->
        () // Pass

[<Test>]
let ``Update should pass because where exists``() = 
    try 
        let query = 
            update {
                for c in main.Customer do
                set c.FirstName "John"
                set c.LastName "Doe"
                where (c.CustomerID = 1L)
            }
        () //Assert.Pass()
    with ex ->
        () //Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Update should pass because updateAll exists``() = 
    try 
        let query = 
            update {
                for c in main.Customer do
                set c.FirstName "John"
                set c.LastName "Doe"
                updateAll
            }
        () //Assert.Pass()
    with ex ->
        () //Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Update with where followed by updateAll should fail``() = 
    Expect.throwsT<InvalidOperationException> (fun _ ->
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            where (c.CustomerID = 1L)
            updateAll
        }
        |> ignore
    ) ""

[<Test>]
let ``Update with updateAll followed by where should fail``() = 
    Expect.throwsT<InvalidOperationException> (fun _ ->
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            updateAll
            where (c.CustomerID = 1L)
        }
        |> ignore
    ) ""

[<Test>]
let ``Insert Query with Identity``() = 
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

[<Test>]
let ``Inline Aggregates``() = 
    let query =
        select {
            for o in main.SalesOrderHeader do
            select (countBy o.SalesOrderID)
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Expect.equal
        sql
        "SELECT COUNT(\"o\".\"SalesOrderID\") FROM \"main\".\"SalesOrderHeader\" AS \"o\""
        ""

[<Test>]
let ``Implicit Casts``() = 
    let query =
        select {
            for p in main.Product do
            where (p.ListPrice > 5)
        }

    // should not throw exception
    ()

[<Test>]
let ``Implicit Casts Option aciq's example``() = 

    let query =
        select {
            for e in main.ErrorLog do
            where (e.ErrorSeverity = Some 1)
        }

    // should not throw exception
    ()

[<Test>]
let ``Implicit Casts Option``() = 
    let query =
        select {
            for p in main.Product do
            where (p.Weight = Some 5)
        }

    // should not throw exception
    ()


module Sqlite.``Query Unit Tests``

open SqlHydra.Query
open DB
open NUnit.Framework
open Swensen.Unquote
#if NET6_0
open Sqlite.AdventureWorksNet6
#endif
#if NET8_0
open Sqlite.AdventureWorksNet8
#endif

[<Test>]
let ``Simple Where``() = 
    let sql = 
        select {
            for a in main.Address do
            where (a.City = "Dallas")
            orderBy a.City
        }
        |> toSql
    
    sql.Contains("WHERE") =! true

[<Test>]
let ``Select 1 Column``() = 
    let sql =
        select {
            for a in main.Address do
            select (a.City)
        }
        |> toSql

    sql =! "SELECT \"a\".\"City\" FROM \"main\".\"Address\" AS \"a\""

[<Test>]
let ``Select 2 Columns``() = 
    let sql =
        select {
            for h in main.SalesOrderHeader do
            select (h.CustomerID, h.OnlineOrderFlag)
        }
        |> toSql

    sql.Contains("SELECT \"h\".\"CustomerID\", \"h\".\"OnlineOrderFlag\" FROM") =! true

[<Test>]
let ``Select 1 Table and 1 Column``() = 
    let sql =
        select {
            for o in main.SalesOrderHeader do
            join d in main.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            where (o.OnlineOrderFlag = 1L)
            select (o, d.LineTotal)
        }
        |> toSql

    sql.Contains("""SELECT "o"."SalesOrderID", "o"."RevisionNumber", "o"."OrderDate", "o"."DueDate", "o"."ShipDate", "o"."Status", "o"."OnlineOrderFlag", "o"."SalesOrderNumber", "o"."PurchaseOrderNumber", "o"."AccountNumber", "o"."CustomerID", "o"."ShipToAddressID", "o"."BillToAddressID", "o"."ShipMethod", "o"."CreditCardApprovalCode", "o"."SubTotal", "o"."TaxAmt", "o"."Freight", "o"."TotalDue", "o"."Comment", "o"."rowguid", "o"."ModifiedDate", "d"."LineTotal" FROM""") =! true

[<Test>]
let ``Where with Option Type``() = 
    let sql = 
        select {
            for a in main.Address do
            where (a.AddressLine2 <> None)
        }
        |> toSql

    sql.Contains("IS NOT NULL") =! true

[<Test>]
let ``Where Not Like``() = 
    let sql =
        select {
            for a in main.Address do
            where (a.City <>% "S%")
        }
        |> toSql

    sql =! """SELECT * FROM "main"."Address" AS "a" WHERE (NOT (LOWER("a"."City") like @p0))"""

[<Test>]
let ``Or Where``() = 
    let sql = 
        select {
            for a in main.Address do
            where (a.City = "Chicago" || a.City = "Dallas")
        }
        |> toSql

    sql.Contains("WHERE ((\"a\".\"City\" = @p0) OR (\"a\".\"City\" = @p1))") =! true

[<Test>]
let ``And Where``() = 
    let sql = 
        select {
            for a in main.Address do
            where (a.City = "Chicago" && a.City = "Dallas")
        }
        |> toSql

    sql.Contains("WHERE ((\"a\".\"City\" = @p0) AND (\"a\".\"City\" = @p1))") =! true

[<Test>]
let ``Where with AND and OR in Parenthesis``() = 
    let sql = 
        select {
            for a in main.Address do
            where (a.City = "Chicago" && (a.AddressLine2 = Some "abc" || isNullValue a.AddressLine2))
        }
        |> toSql

    Assert.IsTrue( 
        sql.Contains("WHERE ((\"a\".\"City\" = @p0) AND ((\"a\".\"AddressLine2\" = @p1) OR (\"a\".\"AddressLine2\" IS NULL)))"),
        "Should wrap OR clause in parenthesis and each individual where clause in parenthesis.")

[<Test>]
let ``Where value and column are swapped``() = 
    let sql = 
        select {
            for a in main.Address do
            where (5L < a.AddressID && 20L >= a.AddressID)
        }
        |> toSql

    sql.Contains("WHERE ((\"a\".\"AddressID\" > @p0) AND (\"a\".\"AddressID\" <= @p1))") =! true

[<Test>]
let ``Where Not Binary``() = 
    let sql = 
        select {
            for a in main.Address do
            where (not (a.City = "Chicago" && a.City = "Dallas"))
        }
        |> toSql

    sql.Contains("WHERE (NOT ((\"a\".\"City\" = @p0) AND (\"a\".\"City\" = @p1)))") =! true

[<Test>]
let ``Where Customer isIn List``() = 
    let sql = 
        select {
            for c in main.Customer do
            where (isIn c.CustomerID [30018L;29545L;29954L])
        }
        |> toSql

    sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where Customer |=| List``() = 
    let sql = 
        select {
            for c in main.Customer do
            where (c.CustomerID |=| [30018L;29545L;29954L])
        }
        |> toSql

    sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where Customer |=| Array``() = 
    let sql = 
        select {
            for c in main.Customer do
            where (c.CustomerID |=| [| 30018L;29545L;29954L |])
        }
        |> toSql

    sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where Customer |=| Seq``() = 
    let buildQuery (values: int64 seq) =                
        select {
            for c in main.Customer do
            where (c.CustomerID |=| values)
        }

    let query = buildQuery [ 30018L;29545L;29954L ]
    let sql = toSql query
    sql.Contains("WHERE (\"c\".\"CustomerID\" IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where Customer |<>| List``() = 
    let sql = 
        select {
            for c in main.Customer do
            where (c.CustomerID |<>| [ 30018L;29545L;29954L ])
        }
        |> toSql

    sql.Contains("WHERE (\"c\".\"CustomerID\" NOT IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Inner Join``() = 
    let sql =
        select {
            for o in main.SalesOrderHeader do
            join d in main.SalesOrderDetail on (o.SalesOrderID = d.SalesOrderID)
            select o
        }
        |> toSql

    sql.Contains("INNER JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\")") =! true

[<Test>]
let ``Left Join``() = 
    let sql =
        select {
            for o in main.SalesOrderHeader do
            leftJoin d in main.SalesOrderDetail on (o.SalesOrderID = d.Value.SalesOrderID)
            select o
        }
        |> toSql

    sql.Contains("LEFT JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\")") =! true

[<Test>]
let ``Inner Join - Multi Column``() = 
    let sql =
        select {
            for o in main.SalesOrderHeader do
            join d in main.SalesOrderDetail on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
            select o
        }
        |> toSql

    sql.Contains("INNER JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\" AND \"o\".\"ModifiedDate\" = \"d\".\"ModifiedDate\")") =! true

[<Test>]
let ``Left Join - Multi Column``() = 
    let sql =
        select {
            for o in main.SalesOrderHeader do
            leftJoin d in main.SalesOrderDetail on ((o.SalesOrderID, o.ModifiedDate) = (d.Value.SalesOrderID, d.Value.ModifiedDate))
            select o
        }
        |> toSql

    sql.Contains("LEFT JOIN \"main\".\"SalesOrderDetail\" AS \"d\" ON (\"o\".\"SalesOrderID\" = \"d\".\"SalesOrderID\" AND \"o\".\"ModifiedDate\" = \"d\".\"ModifiedDate\")") =! true

[<Test>]
let ``Correlated Subquery``() = 
    let latestOrderByCustomer = 
        select {
            for d in main.SalesOrderHeader do
            correlate od in main.SalesOrderHeader
            where (d.CustomerID = od.CustomerID)
            select (maxBy d.OrderDate)
        }

    let sql = 
        select {
            for od in main.SalesOrderHeader do
            where (od.OrderDate = subqueryOne latestOrderByCustomer)
        }        
        |> toSql

    sql =!
        "SELECT * FROM \"main\".\"SalesOrderHeader\" AS \"od\" WHERE (\"od\".\"OrderDate\" = \
        (SELECT MAX(\"d\".\"OrderDate\") FROM \"main\".\"SalesOrderHeader\" AS \"d\" \
        WHERE (\"d\".\"CustomerID\" = \"od\".\"CustomerID\")))"

[<Test>]
let ``Delete Query with Where``() = 
    let sql = 
        delete {
            for c in main.Customer do
            where (c.CustomerID |<>| [ 30018L;29545L;29954L ])
        }
        |> toSql

    sql.Contains("DELETE FROM \"main\".\"Customer\"") =! true
    sql.Contains("WHERE (\"main\".\"Customer\".\"CustomerID\" NOT IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Delete All``() = 
    let sql = 
        delete {
            for c in main.Customer do
            deleteAll
        }
        |> toSql

    sql =! "DELETE FROM \"main\".\"Customer\""

[<Test>]
let ``Update Query with Where``() = 
    let sql = 
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            where (c.CustomerID = 123L)
        }
        |> toSql

    sql =! """UPDATE "main"."Customer" SET "FirstName" = @p0, "LastName" = @p1 WHERE ("main"."Customer"."CustomerID" = @p2)"""

[<Test>]
let ``Update Query with multiple Wheres``() = 
    let sql = 
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            where (c.CustomerID = 123L)
            where (c.FirstName = "Bob")
        }
        |> toSql

    sql =! """UPDATE "main"."Customer" SET "FirstName" = @p0, "LastName" = @p1 WHERE ("main"."Customer"."CustomerID" = @p2 AND ("main"."Customer"."FirstName" = @p3))"""

[<Test>]
let ``Update Query with No Where``() = 
    let sql = 
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            updateAll
        }
        |> toSql

    "UPDATE \"main\".\"Customer\" SET \"FirstName\" = @p0, \"LastName\" = @p1" =! sql

[<Test>]
let ``Update should fail without where or updateAll``() = 
    try 
        let _ = 
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
        let _ = 
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
    update {
        for c in main.Customer do
        set c.FirstName "John"
        set c.LastName "Doe"
        updateAll
    }
    |> ignore
    Assert.Pass()    

[<Test>]
let ``Update with where followed by updateAll should fail``() = 
    try
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            where (c.CustomerID = 1L)
            updateAll
        } 
        |> ignore
        Assert.Fail()
    with ex ->
        Assert.Pass()

[<Test>]
let ``Update with updateAll followed by where should fail``() = 
    try
        update {
            for c in main.Customer do
            set c.FirstName "John"
            set c.LastName "Doe"
            updateAll
            where (c.CustomerID = 1L)
        }
        |> ignore
        Assert.Fail()
    with ex ->
        Assert.Pass()    

[<Test>]
let ``Insert Query with Identity``() = 
    let sql = 
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
        |> toSql

    sql =! "INSERT INTO \"main\".\"BuildVersion\" (\"Database Version\", \"VersionDate\", \"ModifiedDate\") VALUES (@p0, @p1, @p2);select last_insert_rowid() as id" 

[<Test>]
let ``Inline Aggregates``() = 
    let sql =
        select {
            for o in main.SalesOrderHeader do
            select (countBy o.SalesOrderID)
        }
        |> toSql

    sql =! "SELECT COUNT(\"o\".\"SalesOrderID\") FROM \"main\".\"SalesOrderHeader\" AS \"o\""

[<Test>]
let ``Implicit Casts``() = 
    let _ =
        select {
            for p in main.Product do
            where (p.ListPrice > 5)
        }

    // should not throw exception
    ()

[<Test>]
let ``Implicit Casts Option aciq's example``() = 
    let _ =
        select {
            for e in main.ErrorLog do
            where (e.ErrorSeverity = Some 1)
        }

    // should not throw exception
    ()

[<Test>]
let ``Implicit Casts Option``() = 
    let _ =
        select {
            for p in main.Product do
            where (p.Weight = Some 5)
        }

    // should not throw exception
    ()

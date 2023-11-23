module Npgsql.``Query Unit Tests``

open Swensen.Unquote
open SqlHydra.Query
open NUnit.Framework
open DB
#if NET6_0
open Npgsql.AdventureWorksNet6
#endif
#if NET8_0
open Npgsql.AdventureWorksNet8
#endif

[<Test>]
let ``Simple Where``() = 
    let sql =  
        select {
            for a in person.address do
            where (a.city = "Dallas")
            orderBy a.city
        }
        |> toSql

    sql.Contains("WHERE") =! true

[<Test>]
let ``Select 1 Column``() = 
    let sql = 
        select {
            for a in person.address do
            select (a.city)
        }
        |> toSql

    sql.Contains("SELECT \"a\".\"city\" FROM") =! true

[<Test>]
let ``Select 2 Columns``() = 
    let sql = 
        select {
            for h in sales.salesorderheader do
            select (h.customerid, h.onlineorderflag)
        }
        |> toSql

    sql.Contains("SELECT \"h\".\"customerid\", \"h\".\"onlineorderflag\" FROM") =! true

[<Test>]
let ``Select 1 Table and 1 Column``() = 
    let sql = 
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            where o.onlineorderflag
            select (o, d.unitprice)
        }
        |> toSql

    sql.Contains("SELECT \"o\".*, \"d\".\"unitprice\" FROM") =! true

[<Test>]
let ``Where with Option Type``() = 
    let sql =  
        select {
            for a in person.address do
            where (a.addressline2 <> None)
        }
        |> toSql

    sql.Contains("IS NOT NULL") =! true

[<Test>]
let ``Where Not Like``() = 
    let sql = 
        select {
            for a in person.address do
            where (a.city <>% "S%")
        }
        |> toSql

    sql =! """SELECT * FROM "person"."address" AS "a" WHERE (NOT ("a"."city" ilike @p0))"""

[<Test>]
let ``Or Where``() = 
    let sql =  
        select {
            for a in person.address do
            where (a.city = "Chicago" || a.city = "Dallas")
        }
        |> toSql

    sql.Contains("WHERE ((\"a\".\"city\" = @p0) OR (\"a\".\"city\" = @p1))") =! true

[<Test>]
let ``And Where``() = 
    let sql =  
        select {
            for a in person.address do
            where (a.city = "Chicago" && a.city = "Dallas")
        }
        |> toSql

    sql.Contains("WHERE ((\"a\".\"city\" = @p0) AND (\"a\".\"city\" = @p1))") =! true

[<Test>]
let ``Where with AND and OR in Parenthesis``() = 
    let sql =  
        select {
            for a in person.address do
            where (a.city = "Chicago" && (a.addressline2 = Some "abc" || isNullValue a.addressline2))
        }
        |> toSql

    Assert.IsTrue( 
        sql.Contains("WHERE ((\"a\".\"city\" = @p0) AND ((\"a\".\"addressline2\" = @p1) OR (\"a\".\"addressline2\" IS NULL)))"),
        "Should wrap OR clause in parenthesis and each individual where clause in parenthesis.")

[<Test>]
let ``Where value and column are swapped``() = 
    let sql =  
        select {
            for a in person.address do
            where (5 < a.addressid && 20 >= a.addressid)
        }
        |> toSql

    sql.Contains("WHERE ((\"a\".\"addressid\" > @p0) AND (\"a\".\"addressid\" <= @p1))") =! true

[<Test>]
let ``Where Not Binary``() = 
    let sql =  
        select {
            for a in person.address do
            where (not (a.city = "Chicago" && a.city = "Dallas"))
        }
        |> toSql

    sql.Contains("WHERE (NOT ((\"a\".\"city\" = @p0) AND (\"a\".\"city\" = @p1)))") =! true

[<Test>]
let ``Where customer isIn List``() = 
    let sql =  
        select {
            for c in sales.customer do
            where (isIn c.customerid [30018;29545;29954])
        }
        |> toSql

    sql.Contains("WHERE (\"c\".\"customerid\" IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where customer |=| List``() = 
    let sql =  
        select {
            for c in sales.customer do
            where (c.customerid |=| [30018;29545;29954])
        }
        |> toSql

    sql.Contains("WHERE (\"c\".\"customerid\" IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where customer |=| Array``() = 
    let sql =  
        select {
            for c in sales.customer do
            where (c.customerid |=| [| 30018;29545;29954 |])
        }
        |> toSql

    sql.Contains("WHERE (\"c\".\"customerid\" IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where customer |=| Seq``() = 
    let buildQuery (values: int seq) = 
        select {
            for c in sales.customer do
            where (c.customerid |=| values)
        }

    let sql =  buildQuery([ 30018;29545;29954 ]) |> toSql
    sql.Contains("WHERE (\"c\".\"customerid\" IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Where customer |<>| List``() = 
    let sql =  
        select {
            for c in sales.customer do
            where (c.customerid |<>| [ 30018;29545;29954 ])
        }
        |> toSql

    sql.Contains("WHERE (\"c\".\"customerid\" NOT IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Inner Join``() = 
    let sql = 
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            select o
        }
        |> toSql

    sql.Contains("INNER JOIN \"sales\".\"salesorderdetail\" AS \"d\" ON (\"o\".\"salesorderid\" = \"d\".\"salesorderid\")") =! true

[<Test>]
let ``Left Join``() = 
    let sql = 
        select {
            for o in sales.salesorderheader do
            leftJoin d in sales.salesorderdetail on (o.salesorderid = d.Value.salesorderid)
            select o
        }
        |> toSql

    sql.Contains("LEFT JOIN \"sales\".\"salesorderdetail\" AS \"d\" ON (\"o\".\"salesorderid\" = \"d\".\"salesorderid\")") =! true

[<Test>]
let ``Inner Join - Multi Column``() = 
    let sql = 
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on ((o.salesorderid, o.modifieddate) = (d.salesorderid, d.modifieddate))
            select o
        }
        |> toSql

    sql.Contains("INNER JOIN \"sales\".\"salesorderdetail\" AS \"d\" ON (\"o\".\"salesorderid\" = \"d\".\"salesorderid\" AND \"o\".\"modifieddate\" = \"d\".\"modifieddate\")") =! true

[<Test>]
let ``Left Join - Multi Column``() = 
    let sql = 
        select {
            for o in sales.salesorderheader do
            leftJoin d in sales.salesorderdetail on ((o.salesorderid, o.modifieddate) = (d.Value.salesorderid, d.Value.modifieddate))
            select o
        }
        |> toSql

    sql.Contains("LEFT JOIN \"sales\".\"salesorderdetail\" AS \"d\" ON (\"o\".\"salesorderid\" = \"d\".\"salesorderid\" AND \"o\".\"modifieddate\" = \"d\".\"modifieddate\")") =! true

[<Test>]
let ``Correlated Subquery``() = 
    let latestOrderByCustomer = 
        select {
            for d in sales.salesorderheader do
            correlate od in sales.salesorderheader
            where (d.customerid = od.customerid)
            select (maxBy d.orderdate)
        }

    let sql =  
        select {
            for od in sales.salesorderheader do
            where (od.orderdate = subqueryOne latestOrderByCustomer)
        }
        |> toSql

    sql =!
        "SELECT * FROM \"sales\".\"salesorderheader\" AS \"od\" WHERE (\"od\".\"orderdate\" = \
        (SELECT MAX(\"d\".\"orderdate\") FROM \"sales\".\"salesorderheader\" AS \"d\" \
        WHERE (\"d\".\"customerid\" = \"od\".\"customerid\")))"

[<Test>]
let ``Delete Query with Where``() = 
    let sql =  
        delete {
            for c in sales.customer do
            where (c.customerid |<>| [ 30018;29545;29954 ])
        }
        |> toSql

    sql.Contains("DELETE FROM \"sales\".\"customer\"") =! true
    sql.Contains("WHERE (\"sales\".\"customer\".\"customerid\" NOT IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Delete All``() = 
    let sql =  
        delete {
            for c in sales.customer do
            deleteAll
        }
        |> toSql

    sql =! "DELETE FROM \"sales\".\"customer\""

[<Test>]
let ``Update Query with Where``() = 
    let sql =  
        update {
            for c in sales.customer do
            set c.personid (Some 123)
            where (c.personid = Some 456)
        }
        |> toSql

    sql =! "UPDATE \"sales\".\"customer\" SET \"personid\" = @p0 WHERE (\"sales\".\"customer\".\"personid\" = @p1)"

[<Test>]
let ``Update Query with multiple Wheres``() = 
    let sql =  
        update {
            for c in sales.customer do
            set c.personid (Some 123)
            where (c.personid = Some 456)
            where (c.customerid = 789)
        }
        |> toSql

    sql =! """UPDATE "sales"."customer" SET "personid" = @p0 WHERE ("sales"."customer"."personid" = @p1 AND ("sales"."customer"."customerid" = @p2))"""

[<Test>]
let ``Update Query with No Where``() = 
    let sql =  
        update {
            for c in sales.customer do
            set c.customerid 123
            updateAll
        }
        |> toSql

    sql =! "UPDATE \"sales\".\"customer\" SET \"customerid\" = @p0"

[<Test>]
let ``Update should fail without where or updateAll``() = 
    try 
        let sql =  
            update {
                for c in sales.customer do
                set c.customerid 123
            }
        failwith "Should fail because no `where` or `updateAll` exists."
    with ex ->
        () // Pass

[<Test>]
let ``Update should pass because where exists``() = 
    update {
        for c in sales.customer do
        set c.customerid 123
        where (c.customerid = 1)
    }
    |> ignore

[<Test>]
let ``Update should pass because updateAll exists``() = 
    update {
        for c in sales.customer do
        set c.customerid 123
        updateAll
    }
    |> ignore

[<Test>]
let ``Update with where followed by updateAll should fail``() = 
    try
        update {
            for c in sales.customer do
            set c.customerid 123
            where (c.customerid = 1)
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
            for c in sales.customer do
            set c.customerid 123
            updateAll
            where (c.customerid = 1)
        }
        |> ignore
        Assert.Fail()
    with ex ->
        ()

[<Test>]
let ``Insert Query``() = 
    let sql =  
        insert {
            into sales.customer
            entity 
                { 
                    sales.customer.modifieddate = System.DateTime.Today
                    sales.customer.territoryid = None
                    sales.customer.storeid = None
                    sales.customer.personid = Some 1
                    sales.customer.rowguid = System.Guid.NewGuid()
                    sales.customer.customerid = 0
                }
        }
        |> toSql

    sql =! "INSERT INTO \"sales\".\"customer\" (\"customerid\", \"personid\", \"storeid\", \"territoryid\", \"rowguid\", \"modifieddate\") VALUES (@p0, @p1, @p2, @p3, @p4, @p5)" 

[<Test>]
let ``Inline Aggregates``() = 
    let sql = 
        select {
            for o in sales.salesorderheader do
            select (countBy o.salesorderid)
        }
        |> toSql

    sql =! "SELECT COUNT(\"o\".\"salesorderid\") FROM \"sales\".\"salesorderheader\" AS \"o\""

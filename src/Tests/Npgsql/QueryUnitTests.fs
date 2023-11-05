﻿module Npgsql.``Query Unit Tests``

open System
open Expecto
open SqlHydra.Query
open NUnit.Framework
open DB
#if NET6_0
open Npgsql.AdventureWorksNet6
#endif
#if NET7_0
open Npgsql.AdventureWorksNet7
#endif

[<Test>]
let ``Simple Where``() = 
    let query = 
        select {
            for a in person.address do
            where (a.city = "Dallas")
            orderBy a.city
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Expect.isTrue (sql.Contains("WHERE")) ""

[<Test>]
let ``Select 1 Column``() = 
    let query =
        select {
            for a in person.address do
            select (a.city)
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Expect.isTrue (sql.Contains("SELECT \"a\".\"city\" FROM")) ""

[<Test>]
let ``Select 2 Columns``() = 
    let query =
        select {
            for h in sales.salesorderheader do
            select (h.customerid, h.onlineorderflag)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("SELECT \"h\".\"customerid\", \"h\".\"onlineorderflag\" FROM")) ""

[<Test>]
let ``Select 1 Table and 1 Column``() = 
    let query =
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            where o.onlineorderflag
            select (o, d.unitprice)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("SELECT \"o\".*, \"d\".\"unitprice\" FROM")) ""

[<Test>]
let ``Where with Option Type``() = 
    let query = 
        select {
            for a in person.address do
            where (a.addressline2 <> None)
        }

    query.ToKataQuery() |> toSql |> printfn "%s"

[<Test>]
let ``Where Not Like``() = 
    let query =
        select {
            for a in person.address do
            where (a.city <>% "S%")
        }

    query.ToKataQuery() |> toSql |> printfn "%s"

[<Test>]
let ``Or Where``() = 
    let query = 
        select {
            for a in person.address do
            where (a.city = "Chicago" || a.city = "Dallas")
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ((\"a\".\"city\" = @p0) OR (\"a\".\"city\" = @p1))")) ""

[<Test>]
let ``And Where``() = 
    let query = 
        select {
            for a in person.address do
            where (a.city = "Chicago" && a.city = "Dallas")
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ((\"a\".\"city\" = @p0) AND (\"a\".\"city\" = @p1))")) ""

[<Test>]
let ``Where with AND and OR in Parenthesis``() = 
    let query = 
        select {
            for a in person.address do
            where (a.city = "Chicago" && (a.addressline2 = Some "abc" || isNullValue a.addressline2))
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue 
        (sql.Contains("WHERE ((\"a\".\"city\" = @p0) AND ((\"a\".\"addressline2\" = @p1) OR (\"a\".\"addressline2\" IS NULL)))")) 
        "Should wrap OR clause in parenthesis and each individual where clause in parenthesis."

[<Test>]
let ``Where value and column are swapped``() = 
    let query = 
        select {
            for a in person.address do
            where (5 < a.addressid && 20 >= a.addressid)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE ((\"a\".\"addressid\" > @p0) AND (\"a\".\"addressid\" <= @p1))")) sql

[<Test>]
let ``Where Not Binary``() = 
    let query = 
        select {
            for a in person.address do
            where (not (a.city = "Chicago" && a.city = "Dallas"))
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (NOT ((\"a\".\"city\" = @p0) AND (\"a\".\"city\" = @p1)))")) ""

[<Test>]
let ``Where customer isIn List``() = 
    let query = 
        select {
            for c in sales.customer do
            where (isIn c.customerid [30018;29545;29954])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"customerid\" IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where customer |=| List``() = 
    let query = 
        select {
            for c in sales.customer do
            where (c.customerid |=| [30018;29545;29954])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"customerid\" IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where customer |=| Array``() = 
    let query = 
        select {
            for c in sales.customer do
            where (c.customerid |=| [| 30018;29545;29954 |])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"customerid\" IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where customer |=| Seq``() = 
    let buildQuery (values: int seq) = 
        select {
            for c in sales.customer do
            where (c.customerid |=| values)
        }

    let query = buildQuery([ 30018;29545;29954 ])

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"customerid\" IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Where customer |<>| List``() = 
    let query = 
        select {
            for c in sales.customer do
            where (c.customerid |<>| [ 30018;29545;29954 ])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("WHERE (\"c\".\"customerid\" NOT IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Inner Join``() = 
    let query =
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("INNER JOIN \"sales\".\"salesorderdetail\" AS \"d\" ON (\"o\".\"salesorderid\" = \"d\".\"salesorderid\")")) ""

[<Test>]
let ``Left Join``() = 
    let query =
        select {
            for o in sales.salesorderheader do
            leftJoin d in sales.salesorderdetail on (o.salesorderid = d.Value.salesorderid)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("LEFT JOIN \"sales\".\"salesorderdetail\" AS \"d\" ON (\"o\".\"salesorderid\" = \"d\".\"salesorderid\")")) ""

[<Test>]
let ``Inner Join - Multi Column``() = 
    let query =
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on ((o.salesorderid, o.modifieddate) = (d.salesorderid, d.modifieddate))
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("INNER JOIN \"sales\".\"salesorderdetail\" AS \"d\" ON (\"o\".\"salesorderid\" = \"d\".\"salesorderid\" AND \"o\".\"modifieddate\" = \"d\".\"modifieddate\")")) ""

[<Test>]
let ``Left Join - Multi Column``() = 
    let query =
        select {
            for o in sales.salesorderheader do
            leftJoin d in sales.salesorderdetail on ((o.salesorderid, o.modifieddate) = (d.Value.salesorderid, d.Value.modifieddate))
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("LEFT JOIN \"sales\".\"salesorderdetail\" AS \"d\" ON (\"o\".\"salesorderid\" = \"d\".\"salesorderid\" AND \"o\".\"modifieddate\" = \"d\".\"modifieddate\")")) ""

[<Test>]
let ``Correlated Subquery``() = 
    let latestOrderByCustomer = 
        select {
            for d in sales.salesorderheader do
            correlate od in sales.salesorderheader
            where (d.customerid = od.customerid)
            select (maxBy d.orderdate)
        }

    let query = 
        select {
            for od in sales.salesorderheader do
            where (od.orderdate = subqueryOne latestOrderByCustomer)
        }
        

    let sql = query.ToKataQuery() |> toSql
    Expect.equal
        sql
        "SELECT * FROM \"sales\".\"salesorderheader\" AS \"od\" WHERE (\"od\".\"orderdate\" = \
        (SELECT MAX(\"d\".\"orderdate\") FROM \"sales\".\"salesorderheader\" AS \"d\" \
        WHERE (\"d\".\"customerid\" = \"od\".\"customerid\")))"
        ""            

[<Test>]
let ``Delete Query with Where``() = 
    let query = 
        delete {
            for c in sales.customer do
            where (c.customerid |<>| [ 30018;29545;29954 ])
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.isTrue (sql.Contains("DELETE FROM \"sales\".\"customer\"")) ""
    Expect.isTrue (sql.Contains("WHERE (\"sales\".\"customer\".\"customerid\" NOT IN (@p0, @p1, @p2))")) ""

[<Test>]
let ``Delete All``() = 
    let query = 
        delete {
            for c in sales.customer do
            deleteAll
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal "DELETE FROM \"sales\".\"customer\"" sql ""

[<Test>]
let ``Update Query with Where``() = 
    let query = 
        update {
            for c in sales.customer do
            set c.personid (Some 123)
            where (c.personid = Some 456)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal "UPDATE \"sales\".\"customer\" SET \"personid\" = @p0 WHERE (\"sales\".\"customer\".\"personid\" = @p1)" sql ""

[<Test>]
let ``Update Query with multiple Wheres``() = 
    let query = 
        update {
            for c in sales.customer do
            set c.personid (Some 123)
            where (c.personid = Some 456)
            where (c.customerid = 789)
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal """UPDATE "sales"."customer" SET "personid" = @p0 WHERE ("sales"."customer"."personid" = @p1 AND ("sales"."customer"."customerid" = @p2))""" sql ""

[<Test>]
let ``Update Query with No Where``() = 
    let query = 
        update {
            for c in sales.customer do
            set c.customerid 123
            updateAll
        }

    let sql = query.ToKataQuery() |> toSql
    Expect.equal "UPDATE \"sales\".\"customer\" SET \"customerid\" = @p0" sql ""

[<Test>]
let ``Update should fail without where or updateAll``() = 
    try 
        let query = 
            update {
                for c in sales.customer do
                set c.customerid 123
            }
        failwith "Should fail because no `where` or `updateAll` exists."
    with ex ->
        () // Pass

[<Test>]
let ``Update should pass because where exists``() = 
    try 
        let query = 
            update {
                for c in sales.customer do
                set c.customerid 123
                where (c.customerid = 1)
            }
        () //Assert.Pass()
    with ex ->
        () //Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Update should pass because updateAll exists``() = 
    try 
        let query = 
            update {
                for c in sales.customer do
                set c.customerid 123
                updateAll
            }
        () //Assert.Pass()
    with ex ->
        () //Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Update with where followed by updateAll should fail``() = 
    Expect.throwsT<InvalidOperationException> (fun _ ->
        update {
            for c in sales.customer do
            set c.customerid 123
            where (c.customerid = 1)
            updateAll
        }
        |> ignore
    ) ""

[<Test>]
let ``Update with updateAll followed by where should fail``() = 
    Expect.throwsT<InvalidOperationException> (fun _ ->
        update {
            for c in sales.customer do
            set c.customerid 123
            updateAll
            where (c.customerid = 1)
        }
        |> ignore
    ) ""

[<Test>]
let ``Insert Query``() = 
    let query = 
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

    let sql = query.ToKataQuery() |> toSql
    Expect.equal 
        sql 
        "INSERT INTO \"sales\".\"customer\" (\"customerid\", \"personid\", \"storeid\", \"territoryid\", \"rowguid\", \"modifieddate\") VALUES (@p0, @p1, @p2, @p3, @p4, @p5)" 
        ""

[<Test>]
let ``Inline Aggregates``() = 
    let query =
        select {
            for o in sales.salesorderheader do
            select (countBy o.salesorderid)
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Expect.equal 
        sql 
        "SELECT COUNT(\"o\".\"salesorderid\") FROM \"sales\".\"salesorderheader\" AS \"o\""
        ""


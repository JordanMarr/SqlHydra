﻿module Oracle.``Query Unit Tests``

open System
open SqlHydra.Query
open NUnit.Framework
open DB

#if NET6_0
open Oracle.AdventureWorksNet6
#endif
#if NET7_0
open Oracle.AdventureWorksNet7
#endif

[<Test>]
let ``Simple Where``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (c.NAME = "John Doe")
        }

    let sql = query.ToKataQuery() |> toSql
    //printfn "%s" sql
    Assert.IsTrue (sql.Contains("WHERE"))

[<Test>]
let ``Select 1 Column``() = 
    let query =
        select {
            for c in OT.CUSTOMERS do
            select c.NAME
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("SELECT \"c\".\"NAME\" FROM"))

[<Test>]
let ``Select 2 Columns``() = 
    let query =
        select {
            for o in OT.ORDERS do
            select (o.CUSTOMER_ID, o.STATUS)
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("SELECT \"o\".\"CUSTOMER_ID\", \"o\".\"STATUS\" FROM"))

[<Test>]
let ``Select 1 Table and 1 Column``() = 
    let query =
        select {
            for o in OT.ORDERS do
            join d in OT.ORDER_ITEMS on (o.ORDER_ID = d.ORDER_ID)
            where (o.STATUS = "Pending")
            select (o, d.UNIT_PRICE)
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("SELECT \"o\".*, \"d\".\"UNIT_PRICE\" FROM"))

[<Test>]
let ``Where with Option Type``() = 
    let query = 
        select {
            for c in OT.CONTACTS do
            where (c.PHONE <> None)
        }

    query.ToKataQuery() |> toSql |> printfn "%s"

[<Test>]
let ``Where Not Like``() = 
    let query =
        select {
            for c in OT.CUSTOMERS do
            where (c.ADDRESS <>% "S%")
        }

    query.ToKataQuery() |> toSql |> printfn "%s"

[<Test>]
let ``Or Where``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (c.NAME = "Smith" || c.NAME = "Doe")
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("WHERE ((\"c\".\"NAME\" = :p0) OR (\"c\".\"NAME\" = :p1))"))

[<Test>]
let ``And Where``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (c.NAME = "Smith" && c.NAME = "Doe")
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("WHERE ((\"c\".\"NAME\" = :p0) AND (\"c\".\"NAME\" = :p1))"))

[<Test>]
let ``Where with AND and OR in Parenthesis``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (c.NAME = "John" && (c.NAME = "Smith" || isNullValue c.ADDRESS))
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue(
        sql.Contains("WHERE ((\"c\".\"NAME\" = :p0) AND ((\"c\".\"NAME\" = :p1) OR (\"c\".\"ADDRESS\" IS NULL)))"),
        "Should wrap OR clause in parenthesis and each individual where clause in parenthesis.")

[<Test>]
let ``Where value and column are swapped``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (5L < c.CUSTOMER_ID && 20L >= c.CUSTOMER_ID)
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("WHERE ((\"c\".\"CUSTOMER_ID\" > :p0) AND (\"c\".\"CUSTOMER_ID\" <= :p1))"))

[<Test>]
let ``Where Not Binary``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (not (c.NAME = "Smith" && c.NAME = "Doe"))
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("WHERE (NOT ((\"c\".\"NAME\" = :p0) AND (\"c\".\"NAME\" = :p1)))"))

[<Test>]
let ``Where Customer isIn List``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (isIn c.CUSTOMER_ID [1L;2L;3L])
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("WHERE (\"c\".\"CUSTOMER_ID\" IN (:p0, :p1, :p2))"))

[<Test>]
let ``Where Customer |=| List``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (c.CUSTOMER_ID |=| [1L;2L;3L])
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("WHERE (\"c\".\"CUSTOMER_ID\" IN (:p0, :p1, :p2))"))

[<Test>]
let ``Where Customer |=| Array``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (c.CUSTOMER_ID |=| [| 1L;2L;3L |])
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("WHERE (\"c\".\"CUSTOMER_ID\" IN (:p0, :p1, :p2))"))

[<Test>]
let ``Where Customer |=| Seq``() = 
    let buildQuery (values: int64 seq) =                
        select {
            for c in OT.CUSTOMERS do
            where (c.CUSTOMER_ID |=| values)
        }

    let query = buildQuery([ 1L;2L;3L ])

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("WHERE (\"c\".\"CUSTOMER_ID\" IN (:p0, :p1, :p2))"))

[<Test>]
let ``Where Customer |<>| List``() = 
    let query = 
        select {
            for c in OT.CUSTOMERS do
            where (c.CUSTOMER_ID |<>| [ 1L;2L;3L ])
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("WHERE (\"c\".\"CUSTOMER_ID\" NOT IN (:p0, :p1, :p2))"))

[<Test>]
let ``Inner Join``() = 
    let query =
        select {
            for o in OT.ORDERS do
            join d in OT.ORDER_ITEMS on (o.ORDER_ID = d.ORDER_ID)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("INNER JOIN \"OT\".\"ORDER_ITEMS\" \"d\" ON (\"o\".\"ORDER_ID\" = \"d\".\"ORDER_ID\")"))

[<Test>]
let ``Left Join``() = 
    let query =
        select {
            for o in OT.ORDERS do
            leftJoin d in OT.ORDER_ITEMS on (o.ORDER_ID = d.Value.ORDER_ID)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("LEFT JOIN \"OT\".\"ORDER_ITEMS\" \"d\" ON (\"o\".\"ORDER_ID\" = \"d\".\"ORDER_ID\")"))

[<Test>]
let ``Correlated Subquery``() = 
    let latestOrderByCustomer = 
        select {
            for d in OT.ORDERS do
            correlate od in OT.ORDERS
            where (d.CUSTOMER_ID = od.CUSTOMER_ID)
            select (maxBy d.ORDER_DATE)
        }

    let query = 
        select {
            for od in OT.ORDERS do
            where (od.ORDER_DATE = subqueryOne latestOrderByCustomer)
        }
        

    let sql = query.ToKataQuery() |> toSql
    Assert.AreEqual(
        sql,
        "SELECT * FROM \"OT\".\"ORDERS\" \"od\" WHERE (\"od\".\"ORDER_DATE\" = \
        (SELECT MAX(\"d\".\"ORDER_DATE\") FROM \"OT\".\"ORDERS\" \"d\" \
        WHERE (\"d\".\"CUSTOMER_ID\" = \"od\".\"CUSTOMER_ID\")))")

[<Test>]
let ``Join On Value Bug Fix Test``() = 
    let query = 
        select {
            for o in OT.ORDERS do
            leftJoin d in OT.ORDERS on (o.SALESMAN_ID.Value = d.Value.SALESMAN_ID.Value)
            select o
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsNotNull sql

[<Test>]
let ``Delete Query with Where``() = 
    let query = 
        delete {
            for c in OT.CUSTOMERS do
            where (c.CUSTOMER_ID |<>| [ 1L;2L;3L ])
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.IsTrue (sql.Contains("DELETE FROM \"OT\".\"CUSTOMERS\""))
    Assert.IsTrue (sql.Contains("WHERE (\"OT\".\"CUSTOMERS\".\"CUSTOMER_ID\" NOT IN (:p0, :p1, :p2))"))

[<Test>]
let ``Delete All``() = 
    let query = 
        delete {
            for c in OT.CUSTOMERS do
            deleteAll
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.AreEqual("DELETE FROM \"OT\".\"CUSTOMERS\"", sql)

[<Test>]
let ``Update Query with Where``() = 
    let query = 
        update {
            for c in OT.CUSTOMERS do
            set c.NAME "Smith"
            where (c.NAME = "Doe")
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.AreEqual("""UPDATE "OT"."CUSTOMERS" SET "NAME" = :p0 WHERE ("OT"."CUSTOMERS"."NAME" = :p1)""", sql)

[<Test>]
let ``Update Query with multiple Wheres``() = 
    let query = 
        update {
            for c in OT.CUSTOMERS do
            set c.NAME "Smith"
            where (c.NAME = "Doe")
            where (c.CUSTOMER_ID = 123L)
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.AreEqual("""UPDATE "OT"."CUSTOMERS" SET "NAME" = :p0 WHERE ("OT"."CUSTOMERS"."NAME" = :p1 AND ("OT"."CUSTOMERS"."CUSTOMER_ID" = :p2))""", sql)

[<Test>]
let ``Update Query with No Where``() = 
    let query = 
        update {
            for c in OT.CUSTOMERS do
            set c.NAME "Smith"
            updateAll
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.AreEqual("UPDATE \"OT\".\"CUSTOMERS\" SET \"NAME\" = :p0", sql)

[<Test>]
let ``Update should fail without where or updateAll``() = 
    try 
        let query = 
            update {
                for c in OT.CUSTOMERS do
                set c.NAME "Smith"
            }
        failwith "Should fail because no `where` or `updateAll` exists."
    with ex ->
        () // Pass

[<Test>]
let ``Update should pass because where exists``() = 
    try 
        let query = 
            update {
                for c in OT.CUSTOMERS do
                set c.NAME "Smith"
                where (c.CUSTOMER_ID = 1)
            }
        () //Assert.Pass()
    with ex ->
        () //Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Update should pass because updateAll exists``() = 
    try 
        let query = 
            update {
                for c in OT.CUSTOMERS do
                set c.NAME "Smith"
                updateAll
            }
        () //Assert.Pass()
    with ex ->
        () //Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Update with where followed by updateAll should fail``() = 
    Assert.Throws<InvalidOperationException> (fun _ ->
        update {
            for c in OT.CUSTOMERS do
            set c.NAME "Smith"
            where (c.CUSTOMER_ID = 1)
            updateAll
        } |> ignore
    ) |> ignore

[<Test>]
let ``Update with updateAll followed by where should fail``() = 
    Assert.Throws<InvalidOperationException> (fun _ ->
        update {
            for c in OT.CUSTOMERS do
            set c.NAME "Smith"
            updateAll
            where (c.CUSTOMER_ID = 1)
        } |> ignore
    ) |> ignore

[<Test>]
let ``Insert Query without Identity``() = 
    let query = 
        insert {
            into OT.COUNTRIES
            entity
                { 
                    OT.COUNTRIES.COUNTRY_ID = "WL"
                    OT.COUNTRIES.REGION_ID = Some 2
                    OT.COUNTRIES.COUNTRY_NAME = "Wonderland"
                }
        }
    
    let sql = query.ToKataQuery() |> toSql
    Assert.AreEqual(
        sql,
        "INSERT INTO \"OT\".\"COUNTRIES\" (\"COUNTRY_ID\", \"COUNTRY_NAME\", \"REGION_ID\") VALUES (:p0, :p1, :p2)")

[<Test>]
let ``Insert Query with Identity``() = 
    let query = 
        insert {
            for r in OT.REGIONS do
            entity 
                { 
                    OT.REGIONS.REGION_ID = 0
                    OT.REGIONS.REGION_NAME = "Outlands"
                }
            getId r.REGION_ID
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.AreEqual(
        sql,
        "INSERT INTO \"OT\".\"REGIONS\" (\"REGION_NAME\") VALUES (:p0)")

[<Test>]
let ``Multiple Inserts Fix``() = 
    let countriesAL1 = 
        [ 0 .. 2 ] 
        |> List.map (fun i -> 
            {
                OT.COUNTRIES.COUNTRY_ID = $"X{i}"
                OT.COUNTRIES.COUNTRY_NAME = $"Country-{i}"
                OT.COUNTRIES.REGION_ID = Some 2
            }
        )
        |> AtLeastOne.tryCreate

    match countriesAL1 with
    | Some countries ->
        let query = 
            insert {
                into OT.COUNTRIES
                entities countries
            }

        let sql = query.ToKataQuery() |> toSql
        let expected = 
            let sb = new System.Text.StringBuilder()
            sb.AppendLine("INSERT ALL") |> ignore
            sb.AppendLine("INTO \"OT\".\"COUNTRIES\" (\"COUNTRY_ID\", \"COUNTRY_NAME\", \"REGION_ID\") VALUES (:p0, :p1, :p2)") |> ignore
            sb.AppendLine("INTO \"OT\".\"COUNTRIES\" (\"COUNTRY_ID\", \"COUNTRY_NAME\", \"REGION_ID\") VALUES (:p3, :p4, :p5)") |> ignore
            sb.AppendLine("INTO \"OT\".\"COUNTRIES\" (\"COUNTRY_ID\", \"COUNTRY_NAME\", \"REGION_ID\") VALUES (:p6, :p7, :p8)") |> ignore
            sb.AppendLine("SELECT * FROM DUAL") |> ignore
            sb.ToString()

        let fixedQuery = Fixes.Oracle.fixMultiInsertQuery sql

        Assert.AreEqual(fixedQuery, expected, "SqlKata multiple insert query should be overriden to match.")
        
    | None -> ()

[<Test>]
let ``Inline Aggregates``() = 
    let query =
        select {
            for o in OT.ORDERS do
            select (countBy o.ORDER_ID)
        }

    let sql = query.ToKataQuery() |> toSql
    Assert.AreEqual(sql, "SELECT COUNT(\"o\".\"ORDER_ID\") FROM \"OT\".\"ORDERS\" \"o\"")

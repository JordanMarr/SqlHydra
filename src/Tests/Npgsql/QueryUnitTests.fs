﻿module Npgsql.QueryUnitTests

open Expecto
open SqlHydra.Query
open DB
#if NET5_0
open Npgsql.AdventureWorksNet5
#endif
#if NET6_0_OR_GREATER
open Npgsql.AdventureWorksNet6
#endif

// Tables
let personTable =           table<person.person>
let addressTable =          table<person.address>
let customerTable =         table<sales.customer>
let orderHeaderTable =      table<sales.salesorderheader>
let orderDetailTable =      table<sales.salesorderdetail>
let productTable =          table<production.product>
let subCategoryTable =      table<production.productsubcategory>
let categoryTable =         table<production.productcategory>
let productReviewTable =    table<production.productreview>

[<Tests>]
let tests = 
    categoryList "Npgsql" "Query Unit Tests" [

        /// String comparisons against generated queries.
        test "Simple Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.city = "Dallas")
                    orderBy a.city
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("WHERE")) ""
        }

        test "Select 1 Column" {
            let query =
                select {
                    for a in addressTable do
                    select (a.city)
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT \"person\".\"address\".\"city\" FROM")) ""
        }

        test "Select 2 Columns" {
            let query =
                select {
                    for h in orderHeaderTable do
                    select (h.customerid, h.onlineorderflag)
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT \"sales\".\"salesorderheader\".\"customerid\", \"sales\".\"salesorderheader\".\"onlineorderflag\" FROM")) ""
        }

        test "Select 1 Table and 1 Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.salesorderid = d.salesorderid)
                    where (o.onlineorderflag = true)
                    select (o, d.unitprice)
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT \"sales\".\"salesorderheader\".*, \"sales\".\"salesorderdetail\".\"unitprice\" FROM")) ""
        }

        ptest "Where with Option Type" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.addressline2 <> None)
                }

            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        ptest "Where Not Like" {
            let query =
                select {
                    for a in addressTable do
                    where (a.city <>% "S%")
                }

            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        test "Or Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.city = "Chicago" || a.city = "Dallas")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ((\"person\".\"address\".\"city\" = @p0) OR (\"person\".\"address\".\"city\" = @p1))")) ""
        }

        test "And Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.city = "Chicago" && a.city = "Dallas")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ((\"person\".\"address\".\"city\" = @p0) AND (\"person\".\"address\".\"city\" = @p1))")) ""
        }

        test "Where with AND and OR in Parenthesis" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.city = "Chicago" && (a.addressline2 = Some "abc" || isNullValue a.addressline2))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue 
                (sql.Contains("WHERE ((\"person\".\"address\".\"city\" = @p0) AND ((\"person\".\"address\".\"addressline2\" = @p1) OR (\"person\".\"address\".\"addressline2\" IS NULL)))")) 
                "Should wrap OR clause in parenthesis and each individual where clause in parenthesis."
        }

        test "Where Not Binary" {
            let query = 
                select {
                    for a in addressTable do
                    where (not (a.city = "Chicago" && a.city = "Dallas"))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (NOT ((\"person\".\"address\".\"city\" = @p0) AND (\"person\".\"address\".\"city\" = @p1)))")) ""
        }

        test "Where customer isIn List" {
            let query = 
                select {
                    for c in customerTable do
                    where (isIn c.customerid [30018;29545;29954])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"sales\".\"customer\".\"customerid\" IN (@p0, @p1, @p2))")) ""
        }

        test "Where customer |=| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.customerid |=| [30018;29545;29954])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"sales\".\"customer\".\"customerid\" IN (@p0, @p1, @p2))")) ""
        }

        test "Where customer |=| Array" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.customerid |=| [| 30018;29545;29954 |])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"sales\".\"customer\".\"customerid\" IN (@p0, @p1, @p2))")) ""
        }
        
        test "Where customer |=| Seq" {            
            let buildQuery (values: int seq) =                
                select {
                    for c in customerTable do
                    where (c.customerid |=| values)
                }

            let query = buildQuery([ 30018;29545;29954 ])

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"sales\".\"customer\".\"customerid\" IN (@p0, @p1, @p2))")) ""
        }

        test "Where customer |<>| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.customerid |<>| [ 30018;29545;29954 ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"sales\".\"customer\".\"customerid\" NOT IN (@p0, @p1, @p2))")) ""
        }
        
        test "Inner Join" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.salesorderid = d.salesorderid)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("INNER JOIN \"sales\".\"salesorderdetail\" ON (\"sales\".\"salesorderheader\".\"salesorderid\" = \"sales\".\"salesorderdetail\".\"salesorderid\")")) ""
        }

        test "Left Join" {
            let query =
                select {
                    for o in orderHeaderTable do
                    leftJoin d in orderDetailTable on (o.salesorderid = d.Value.salesorderid)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("LEFT JOIN \"sales\".\"salesorderdetail\" ON (\"sales\".\"salesorderheader\".\"salesorderid\" = \"sales\".\"salesorderdetail\".\"salesorderid\")")) ""
        }
        
        test "Inner Join - Multi Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on ((o.salesorderid, o.modifieddate) = (d.salesorderid, d.modifieddate))
                    select o
                }
        
            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("INNER JOIN \"sales\".\"salesorderdetail\" ON (\"sales\".\"salesorderheader\".\"salesorderid\" = \"sales\".\"salesorderdetail\".\"salesorderid\" AND \"sales\".\"salesorderheader\".\"modifieddate\" = \"sales\".\"salesorderdetail\".\"modifieddate\")")) ""
        }
        
        test "Left Join - Multi Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    leftJoin d in orderDetailTable on ((o.salesorderid, o.modifieddate) = (d.Value.salesorderid, d.Value.modifieddate))
                    select o
                }
        
            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("LEFT JOIN \"sales\".\"salesorderdetail\" ON (\"sales\".\"salesorderheader\".\"salesorderid\" = \"sales\".\"salesorderdetail\".\"salesorderid\" AND \"sales\".\"salesorderheader\".\"modifieddate\" = \"sales\".\"salesorderdetail\".\"modifieddate\")")) ""
        }

        test "Delete Query with Where" {
            let query = 
                delete {
                    for c in customerTable do
                    where (c.customerid |<>| [ 30018;29545;29954 ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("DELETE FROM \"sales\".\"customer\"")) ""
            Expect.isTrue (sql.Contains("WHERE (\"sales\".\"customer\".\"customerid\" NOT IN (@p0, @p1, @p2))")) ""
        }

        test "Delete All" {
            let query = 
                delete {
                    for c in customerTable do
                    deleteAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "DELETE FROM \"sales\".\"customer\"" sql ""
        }

        test "Update Query with Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.personid (Some 123)
                    where (c.personid = Some 456)
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "UPDATE \"sales\".\"customer\" SET \"personid\" = @p0 WHERE (\"sales\".\"customer\".\"personid\" = @p1)" sql ""
        }

        test "Update Query with No Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.customerid 123
                    updateAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "UPDATE \"sales\".\"customer\" SET \"customerid\" = @p0" sql ""
        }

        test "Update should fail without where or updateAll" {
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.customerid 123
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
                        set c.customerid 123
                        where (c.customerid = 1)
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
                        set c.customerid 123
                        updateAll
                    }
                () //Assert.Pass()
            with ex ->
                () //Assert.Pass("Should not fail because `where` is present.")
        }
                
        test "Insert Query" {
            let query = 
                insert {
                    into customerTable
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
        }
        
        test "Inline Aggregates" {
            let query =
                select {
                    for o in orderHeaderTable do
                    select (countBy o.salesorderid)
                }
        
            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.equal 
                sql 
                "SELECT COUNT(\"sales\".\"salesorderheader\".\"salesorderid\") FROM \"sales\".\"salesorderheader\""
                ""
        }
    ]


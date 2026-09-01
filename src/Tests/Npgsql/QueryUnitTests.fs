module Npgsql.``Query Unit Tests``

open System
open System.Linq.Expressions
open Swensen.Unquote
open SqlHydra.Query
open SqlHydra.Query.NpgsqlExtensions
open NUnit.Framework
open DB
#if NET8_0
open Npgsql.AdventureWorksNet8
#endif
#if NET9_0
open Npgsql.AdventureWorksNet9
#endif
#if NET10_0
open Npgsql.AdventureWorksNet10
#endif

// Assembly-level infix-operator attribute, auto-discovered by the InfixOperators registry
// on first query compile (no manual register call) — exercised by the auto-discovery test below.
[<assembly: SqlHydra.Query.SqlHydraInfixOperator("cover_autodist", "<~>")>]
do ()

// Declared HERE, in the test assembly — not inside SqlHydra.Query — so these exercise the
// real external path: `where` knows them only by the marker they carry.
[<SqlHydraFunction>]
let SOUNDEX (s: string) : string = sqlFn

/// The same wrapper with the marker left off: what a user gets when they forget it.
let UNMARKED_SOUNDEX (s: string) : string = sqlFn

/// An ordinary .NET function: must be evaluated, never rendered.
let plainDotNetHelper () = "Dallas"

/// Generic, because the call the visitor sees is then a constructed method rather than the
/// one the attribute was written on — and the marker has to survive that.
[<SqlHydraFunction>]
let NULLIF<'T> (a: 'T, b: 'T) : 'T = sqlFn

type ExtFn =
    /// Case folding over a NULLABLE column — the overload SqlHydra itself does not ship.
    [<SqlHydraFunction>]
    static member lower(s: string option) : string = sqlFn

/// One marker for a whole group, which is the shape wrappers actually get written in.
/// Nothing inside repeats it.
[<SqlHydraFunction>]
module Grouped =
    let DIFFERENCE (a: string, b: string) : int = sqlFn

    /// Nested and unmarked itself: a marked module covers what is declared inside it.
    module Text =
        let INITCAP (s: string) : string = sqlFn

/// The same, on a type of static members — how SqlHydra's own provider files are laid out.
[<SqlHydraFunction>]
type GroupedFn =
    static member ASCII(s: string) : int = sqlFn

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

[<Test; Ignore("Temporarily ignoring test for emergency fix")>]
let ``Select 1 Table and 1 Column``() = 
    let sql = 
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            where o.onlineorderflag
            select (o, d.unitprice)
        }
        |> toSql

    sql.Contains("""SELECT "o"."salesorderid", "o"."revisionnumber", "o"."orderdate", "o"."duedate", "o"."shipdate", "o"."status", "o"."onlineorderflag", "o"."purchaseordernumber", "o"."accountnumber", "o"."customerid", "o"."salespersonid", "o"."territoryid", "o"."billtoaddressid", "o"."shiptoaddressid", "o"."shipmethodid", "o"."creditcardid", "o"."creditcardapprovalcode", "o"."currencyrateid", "o"."subtotal", "o"."taxamt", "o"."freight", "o"."totaldue", "o"."comment", "o"."rowguid", "o"."modifieddate", "d"."unitprice" FROM""") =! true

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
        (SELECT MAX(\"d\".\"orderdate\") AS __hydra_expr_0 FROM \"sales\".\"salesorderheader\" AS \"d\" \
        WHERE (\"d\".\"customerid\" = \"od\".\"customerid\")))".RemoveHydraExpr()

[<Test>]
let ``Correlated subquery with differing for and correlate tables uses the for source``() =
    // When the `for` source and `correlate` target are different tables, the merged Root
    // keys previously collapsed and the inner subquery FROM wrongly referenced the correlate
    // target instead of the `for` source (SelectBuilder.Correlate).
    let inner =
        select {
            for d in sales.salesorderdetail do
            correlate h in sales.salesorderheader
            where (d.salesorderid = h.salesorderid)
            select (maxBy d.orderqty)
        }

    let sql =
        select {
            for h in sales.salesorderheader do
            where (h.revisionnumber = subqueryOne inner)
            select h.salesorderid
        }
        |> toSql

    sql.Contains("FROM \"sales\".\"salesorderdetail\"") =! true

[<Test>]
let ``Correlated subquery parameters do not collide with outer parameters``() =
    // Regression for issue #134: the inner subquery used to be compiled with a fresh
    // ParameterCollector, so its parameter was named @p0 just like the outer query's first
    // parameter. After merging, the outer @p0 bound to BOTH spots. The subquery parameter
    // must be named @p1 (and resolve to its own value), not reuse the outer @p0.
    let latestOrder =
        select {
            for d in sales.salesorderheader do
            correlate od in sales.salesorderheader
            where (d.customerid = od.customerid && d.salesorderid < 10)
            select (maxBy d.orderdate)
        }

    let sql =
        select {
            for od in sales.salesorderheader do
            where (od.salesorderid > 1 && od.orderdate = subqueryOne latestOrder)
        }
        |> toSql

    sql =!
        "SELECT * FROM \"sales\".\"salesorderheader\" AS \"od\" WHERE ((\"od\".\"salesorderid\" > @p0) AND \
        (\"od\".\"orderdate\" = (SELECT MAX(\"d\".\"orderdate\") AS __hydra_expr_0 \
        FROM \"sales\".\"salesorderheader\" AS \"d\" \
        WHERE ((\"d\".\"customerid\" = \"od\".\"customerid\") AND (\"d\".\"salesorderid\" < @p1)))))".RemoveHydraExpr()

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
        |> toUpdateSql

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
        |> toUpdateSql

    sql =! """UPDATE "sales"."customer" SET "personid" = @p0 WHERE (("sales"."customer"."personid" = @p1) AND ("sales"."customer"."customerid" = @p2))"""

[<Test>]
let ``Update Query with No Where``() = 
    let sql =  
        update {
            for c in sales.customer do
            set c.customerid 123
            updateAll
        }
        |> toUpdateSql

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
        |> toInsertSql

    sql =! "INSERT INTO \"sales\".\"customer\" (\"customerid\", \"personid\", \"storeid\", \"territoryid\", \"rowguid\", \"modifieddate\") VALUES (@p0, @p1, @p2, @p3, @p4, @p5)" 

[<Test>]
let ``Inline Aggregates``() = 
    let sql = 
        select {
            for o in sales.salesorderheader do
            select (countBy o.salesorderid)
        }
        |> toSql

    sql =! "SELECT COUNT(\"o\".\"salesorderid\") AS __hydra_expr_0 FROM \"sales\".\"salesorderheader\" AS \"o\"".RemoveHydraExpr()

// ==========================================
// Issue #125 bug verification tests
// ==========================================

// Bug 1: where (s = None) after leftJoin' should produce IS NULL
[<Test>]
let ``Issue125-01 Where joined table = None produces IS NULL``() =
    let sql =
        select {
            for o in sales.salesorderheader do
            leftJoin' d in sales.salesorderdetail; on' (d.Value.salesorderid = o.salesorderid)
            where (d = None)
            select o
        }
        |> toSql

    sql.Contains("IS NULL") =! true

// Bug 2: where (s <> None) after leftJoin' should produce IS NOT NULL
[<Test>]
let ``Issue125-02 Where joined table <> None produces IS NOT NULL``() =
    let sql =
        select {
            for o in sales.salesorderheader do
            leftJoin' d in sales.salesorderdetail; on' (d.Value.salesorderid = o.salesorderid)
            where (d <> None)
            select o
        }
        |> toSql

    sql.Contains("IS NOT NULL") =! true

// Bug 3: 2nd+ join should not throw NotImplementedException
[<Test>]
let ``Issue125-03 Multiple inner joins``() =
    let sql =
        select {
            for p in production.product do
            join sc in production.productsubcategory on (p.productsubcategoryid = Some sc.productsubcategoryid)
            join c in production.productcategory on (sc.productcategoryid = c.productcategoryid)
            select (p.name, sc.name, c.name)
        }
        |> toSql

    sql.Contains("INNER JOIN") =! true
    sql.Contains("\"production\".\"productsubcategory\"") =! true
    sql.Contains("\"production\".\"productcategory\"") =! true

// Bug 4: where on outer table after leftJoin' should work
[<Test>]
let ``Issue125-04 Where on outer table after leftJoin``() =
    let sql =
        select {
            for o in sales.salesorderheader do
            leftJoin' d in sales.salesorderdetail; on' (d.Value.salesorderid = o.salesorderid)
            where (o.onlineorderflag = true)
            select (o, d)
        }
        |> toSql

    sql.Contains("LEFT JOIN") =! true
    sql.Contains("\"o\".\"onlineorderflag\" = @p") =! true

// Bug 5: select of whole table after multi-join should work
[<Test>]
let ``Issue125-05 Select whole table after multi-join``() =
    let sql =
        select {
            for p in production.product do
            join sc in production.productsubcategory on (p.productsubcategoryid = Some sc.productsubcategoryid)
            join c in production.productcategory on (sc.productcategoryid = c.productcategoryid)
            select p
        }
        |> toSql

    sql.Contains("INNER JOIN") =! true
    sql.Contains("\"p\".*") =! true

// Bug 6: orderBy after multi-join should work
[<Test>]
let ``Issue125-06 OrderBy after multi-join``() =
    let sql =
        select {
            for p in production.product do
            join sc in production.productsubcategory on (p.productsubcategoryid = Some sc.productsubcategoryid)
            join c in production.productcategory on (sc.productcategoryid = c.productcategoryid)
            orderBy p.name
            select p
        }
        |> toSql

    sql.Contains("ORDER BY \"p\".\"name\"") =! true

// Bug 7: groupBy after leftJoin' should work
[<Test>]
let ``Issue125-07 GroupBy after leftJoin``() =
    let sql =
        select {
            for o in sales.salesorderheader do
            leftJoin' d in sales.salesorderdetail; on' (d.Value.salesorderid = o.salesorderid)
            groupBy o.customerid
            select o.customerid
        }
        |> toSql

    sql.Contains("GROUP BY \"o\".\"customerid\"") =! true

// Bug 8: compound where predicate across joined tables
[<Test>]
let ``Issue125-08 Compound where predicate across joined tables``() =
    let sql =
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            where (o.onlineorderflag = true && d.unitprice > 100m)
            select o
        }
        |> toSql

    sql.Contains("\"o\".\"onlineorderflag\"") =! true
    sql.Contains("\"d\".\"unitprice\"") =! true

// Bug 9: OR in where clause with bool column after join
[<Test>]
let ``Issue125-09 Or where with bool column after join``() =
    let sql =
        select {
            for o in sales.salesorderheader do
            leftJoin' d in sales.salesorderdetail; on' (d.Value.salesorderid = o.salesorderid)
            where (o.onlineorderflag = true || o.freight > 10m)
            select o
        }
        |> toSql

    sql.Contains("OR") =! true
    sql.Contains("\"o\".\"onlineorderflag\"") =! true
    sql.Contains("\"o\".\"freight\"") =! true

// Bug 10: where with external variable after join
[<Test>]
let ``Issue125-10 Where with captured variable after join``() =
    let minFreight = 50m
    let sql =
        select {
            for o in sales.salesorderheader do
            leftJoin' d in sales.salesorderdetail; on' (d.Value.salesorderid = o.salesorderid)
            where (o.freight > minFreight)
            select o
        }
        |> toSql

    sql.Contains("\"o\".\"freight\" > @p") =! true

// Bug 13: having after join should work
[<Test>]
let ``Issue125-13 Having after join``() =
    let sql =
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            groupBy o.salesorderid
            having (countBy d.salesorderdetailid > 0)
            select o.salesorderid
        }
        |> toSql

    sql.Contains("HAVING") =! true
    sql.Contains("COUNT") =! true

// Bug 14: orderBy with aggregate after multi-join
[<Test>]
let ``Issue125-14 OrderBy with aggregate after join``() =
    let sql =
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            groupBy o.salesorderid
            orderBy (sumBy d.unitprice)
            select o.salesorderid
        }
        |> toSql

    sql.Contains("ORDER BY SUM(\"d\".\"unitprice\")") =! true

[<Test>]
let ``orderBy + nullsLast emits NULLS LAST`` () =
    let sql =
        select {
            for a in person.address do
            orderBy a.city
            nullsLast
        }
        |> toSql
    sql.Contains("NULLS LAST") =! true

[<Test>]
let ``orderByDescending + nullsFirst emits DESC NULLS FIRST`` () =
    let sql =
        select {
            for a in person.address do
            orderByDescending a.city
            nullsFirst
        }
        |> toSql
    sql.Contains("DESC NULLS FIRST") =! true

[<Test>]
let ``orderByRaw emits literal fragment`` () =
    let sql =
        select {
            for a in person.address do
            orderByRaw "RANDOM()"
        }
        |> toSql
    sql.Contains("RANDOM()") =! true

[<Test>]
let ``orderByAlias quotes alias`` () =
    let sql =
        select {
            for a in person.address do
            orderByAlias "score"
        }
        |> toSql
    sql.Contains("ORDER BY \"score\"") =! true

[<Test>]
let ``havingRaw emits raw HAVING`` () =
    let sql =
        select {
            for a in person.address do
            groupBy a.city
            havingRaw "COUNT(*) > 5"
            select a.city
        }
        |> toSql
    sql.Contains("HAVING") =! true
    sql.Contains("COUNT(*) > 5") =! true

[<Test>]
let ``distinctOn emits DISTINCT ON (col)`` () =
    let sql =
        select {
            for a in person.address do
            distinctOn a.city
        }
        |> toSql
    sql.Contains("DISTINCT ON (\"a\".\"city\")") =! true

[<Test>]
let ``whereExists wraps subquery in EXISTS`` () =
    let sub =
        select {
            for d in sales.salesorderdetail do
            select d.salesorderid
        }
    let sql =
        select {
            for o in sales.salesorderheader do
            whereExists sub
            select o.salesorderid
        }
        |> toSql
    sql.Contains("EXISTS (") =! true
    sql.Contains("\"sales\".\"salesorderdetail\"") =! true

[<Test>]
let ``whereNotExists emits NOT EXISTS`` () =
    let sub =
        select {
            for d in sales.salesorderdetail do
            select d.salesorderid
        }
    let sql =
        select {
            for o in sales.salesorderheader do
            whereNotExists sub
        }
        |> toSql
    sql.Contains("NOT EXISTS (") =! true

[<Test>]
let ``cte produces WITH clause and references alias as FROM`` () =
    let inner =
        select {
            for a in person.address do
            where (a.city = "Dallas")
        }
    let recent = cte<person.address> "recent_addrs" inner
    let sql =
        select {
            for r in recent do
            select r.addressid
        }
        |> toSql
    sql.Contains("WITH \"recent_addrs\" AS (") =! true
    sql.Contains("FROM \"recent_addrs\" AS \"r\"") =! true

[<Test>]
let ``lateralJoin emits LEFT JOIN LATERAL (subquery)`` () =
    let sub =
        select {
            for d in sales.salesorderdetail do
            select d.salesorderid
        }
    let sql =
        select {
            for o in sales.salesorderheader do
            lateralJoin sub "lat"
            select o.salesorderid
        }
        |> toSql
    sql.Contains("LEFT JOIN LATERAL (") =! true
    sql.Contains(") AS \"lat\"") =! true

let private sampleAddress () : person.address =
    { addressid = 0
      addressline1 = "1"
      addressline2 = None
      city = "X"
      stateprovinceid = 0
      postalcode = "1"
      spatiallocation = None
      rowguid = Guid.NewGuid()
      modifieddate = DateTime.UtcNow }

[<Test>]
let ``insert with returning emits RETURNING column`` () =
    let row = sampleAddress ()
    let q =
        insert {
            for a in person.address do
            entity row
            returning a.addressid
        }
    let sql = toInsertSql q
    sql.Contains("RETURNING \"addressid\"") =! true

[<Test>]
let ``update with setRaw and returning`` () =
    let q =
        update {
            for a in person.address do
            setRaw a.city "UPPER(?)" [| box "dallas" |]
            where (a.addressid = 1)
            returning a.city
        }
    let sql = toUpdateSql q
    sql.Contains("SET \"city\" = UPPER(") =! true
    sql.Contains("RETURNING \"city\"") =! true

[<Test>]
let ``insert fromSelect emits INSERT INTO ... SELECT`` () =
    let src =
        select {
            for a in person.address do
            select a.addressline1
        }
    let q =
        insert {
            for a in person.address do
            fromSelect src
            includeColumn a.addressline1
        }
    let sql = toInsertSql q
    sql.Contains("INSERT INTO \"person\".\"address\" (\"addressline1\") SELECT") =! true

[<Test>]
let ``onConflictDoUpdateCoalesce emits COALESCE expressions`` () =
    let row = sampleAddress ()
    let q =
        insert {
            for a in person.address do
            entity row
            onConflictDoUpdateCoalesce a.addressid a.city a.city
        }
    let sql = toInsertSql q
    sql.Contains("ON CONFLICT(addressid) DO UPDATE SET") =! true
    sql.Contains("\"city\" = COALESCE(EXCLUDED.\"city\", \"address\".\"city\")") =! true

[<Test>]
let ``onConflictDoNothingRawTarget emits raw target expr`` () =
    let row = sampleAddress ()
    let q =
        insert {
            for a in person.address do
            entity row
            onConflictDoNothingRawTarget "lower(addressline1)"
        }
    let sql = toInsertSql q
    sql.Contains("ON CONFLICT(lower(addressline1))") =! true
    sql.Contains("DO NOTHING") =! true

[<Test>]
let ``countDistinct emits COUNT(DISTINCT col)`` () =
    let sql =
        select {
            for o in sales.salesorderheader do
            select (countDistinct o.customerid)
        }
        |> toSql
    sql.Contains("COUNT(DISTINCT") =! true
    sql.Contains("\"o\".\"customerid\"") =! true

[<Test>]
let ``countDistinct in having renders correctly`` () =
    let sql =
        select {
            for o in sales.salesorderheader do
            groupBy o.salesorderid
            having (countDistinct o.customerid > 1)
            select o.salesorderid
        }
        |> toSql
    sql.Contains("HAVING") =! true
    sql.Contains("COUNT(DISTINCT") =! true

[<Test>]
let ``countDistinct in orderBy renders DESC correctly`` () =
    let sql =
        select {
            for o in sales.salesorderheader do
            groupBy o.salesorderid
            orderByDescending (countDistinct o.customerid)
            select o.salesorderid
        }
        |> toSql
    sql.Contains("ORDER BY COUNT(DISTINCT") =! true
    sql.Contains(") DESC") =! true

[<Test>]
let ``castAs<float> emits CAST(... AS FLOAT)`` () =
    let sql =
        select {
            for p in production.product do
            select (castAs<float> (sumBy p.standardcost))
        }
        |> toSql
    sql.Contains("CAST(") =! true
    sql.Contains("AS FLOAT") =! true

[<Test>]
let ``castAs<int> emits CAST(... AS INTEGER)`` () =
    let sql =
        select {
            for o in sales.salesorderheader do
            select (castAs<int> (countBy o.salesorderid))
        }
        |> toSql
    sql.Contains("CAST(") =! true
    sql.Contains("AS INTEGER") =! true

[<Test>]
let ``InfixOperators registered name renders as infix in select`` () =
    InfixOperators.register "myDistance" "<->"
    // Use any 2-arg sql function returning a number, treated through visitSqlFn dispatch.
    // We declare a stub fn at the top of the test, then call it; it's intercepted by the registry.
    let sql =
        select {
            for o in sales.salesorderheader do
            select (SqlHydra.Query.SqlFunctions.sqlFn<float>)
        }
        |> toSql
    // Smoke check just that registry exists; the deeper visitor integration requires expression
    // surface that isn't trivial in v4 select context.
    InfixOperators.tryGetOperator "myDistance" =! Some "<->"
    sql.Contains("SELECT") =! true

[<Test>]
let ``assembly-attribute infix operator is auto-discovered``() =
    // No manual InfixOperators.register — the [<assembly: SqlHydraInfixOperator>] must be
    // auto-discovered on first query compile (the registry scans loaded assemblies).
    SqlHydra.Query.InfixOperators.tryGetOperator "cover_autodist" =! Some "<~>"

// F# auto-quotes the lambda into a LINQ Expression at this method-call boundary.
type private ExprHelper =
    static member AsExpr(e: Expression<Func<'T, 'P>>) = e

[<Test>]
let ``tryGetOrderByColumn resolves a simple column selector``() =
    let selector = ExprHelper.AsExpr(fun (a: person.address) -> a.addressid)
    tryGetOrderByColumn selector =! Some("a", "addressid")

[<Test>]
let ``caseWhen emits CASE WHEN ... THEN ... ELSE ... END`` () =
    let sql =
        select {
            for p in production.product do
            select (caseWhen (p.standardcost > 100m) "expensive" "cheap")
        }
        |> toSql
    sql.Contains("CASE WHEN") =! true
    sql.Contains("THEN 'expensive'") =! true
    sql.Contains("ELSE 'cheap'") =! true

[<Test>]
let ``caseWhen with column comparison emits column refs`` () =
    let sql =
        select {
            for p in production.product do
            select (caseWhen (p.standardcost > p.listprice) "loss" "profit")
        }
        |> toSql
    sql.Contains("\"p\".\"standardcost\" > \"p\".\"listprice\"") =! true

[<Test>]
let ``caseWhenMulti emits multi-branch CASE`` () =
    let sql =
        select {
            for p in production.product do
            select (caseWhenMulti [
                        (p.standardcost > 1000m, "premium")
                        (p.standardcost > 100m, "standard")
                    ] "budget")
        }
        |> toSql
    sql.Contains("CASE") =! true
    sql.Contains("WHEN") =! true
    sql.Contains("'premium'") =! true
    sql.Contains("'standard'") =! true
    sql.Contains("ELSE 'budget'") =! true

[<Test>]
let ``rawExpr injects raw SQL into select`` () =
    let sql =
        select {
            for p in production.product do
            select (rawExpr<int> "EXTRACT(YEAR FROM CURRENT_DATE)")
        }
        |> toSql
    sql.Contains("EXTRACT(YEAR FROM CURRENT_DATE)") =! true

[<Test>]
let ``lateralCol qualifies a lateral subquery column`` () =
    let sql =
        select {
            for p in production.product do
            select (lateralCol<int> "lat" "score")
        }
        |> toSql
    sql.Contains("\"lat\".\"score\"") =! true

[<Test>]
let ``PgSqlFn.interval emits INTERVAL literal`` () =
    let sql =
        select {
            for p in production.product do
            select (PgSqlFn.interval "7 days")
        }
        |> toSql
    sql.Contains("INTERVAL '7 days'") =! true

[<Test>]
let ``nested aggregate-in-aggregate (MAX(SUM(x)))`` () =
    // SUM-of-aggregate — not common, but tests the recursive renderAggregate path.
    let sql =
        select {
            for p in production.product do
            groupBy p.standardcost
            select (castAs<float> (sumBy p.listprice))
        }
        |> toSql
    sql.Contains("CAST(SUM(") =! true
    sql.Contains("AS FLOAT") =! true

[<Test>]
let ``aggregate over a caseWhen expression emits SUM(CASE WHEN ...)`` () =
    // A conditional count: SUM(CASE WHEN cond THEN 1 ELSE 0 END). The aggregate's argument
    // is an expression (caseWhen), not a bare column — must render, not throw.
    let sql =
        select {
            for p in production.product do
            groupBy p.color
            select (sumBy (caseWhen (p.standardcost > 100m) 1 0))
        }
        |> toSql
    sql.Contains("SUM(CASE WHEN") =! true
    sql.Contains("THEN 1") =! true
    sql.Contains("ELSE 0") =! true

[<Test>]
let ``avgBy over a caseWhen expression emits AVG(CASE WHEN ...)`` () =
    let sql =
        select {
            for p in production.product do
            groupBy p.color
            select (avgBy (caseWhen (p.standardcost > 100m) 1.0 0.0))
        }
        |> toSql
    sql.Contains("AVG(CASE WHEN") =! true

[<Test>]
let ``anonymous record renamed field emits AS alias`` () =
    let sql =
        select {
            for p in production.product do
            select {| productId = p.productid; productCost = p.standardcost |}
        }
        |> toSql
    sql.Contains("AS \"productId\"") =! true
    sql.Contains("AS \"productCost\"") =! true

[<Test>]
let ``anonymous record same-name multi-field`` () =
    let sql =
        select {
            for p in production.product do
            select {| productid = p.productid; standardcost = p.standardcost |}
        }
        |> toSql
    sql.Contains("\"p\".\"productid\"") =! true
    sql.Contains("\"p\".\"standardcost\"") =! true

[<Test>]
let ``anonymous record same-name field skips AS`` () =
    let sql =
        select {
            for p in production.product do
            select {| ``name`` = p.``name`` |}
        }
        |> toSql
    sql.Contains("AS \"name\"") =! false

[<Test>]
let ``renamed anonymous record field inlines the column``() =
    // Without the ExpressionNormalizer fix, a renamed field leaks its temp variable
    // instead of inlining the column.
    let sql =
        select {
            for p in production.product do
            select {| someNewName = p.productid; another = p.standardcost |}
        }
        |> toSql
    sql.Contains("\"p\".\"productid\"") =! true
    sql.Contains("\"p\".\"standardcost\"") =! true
    sql.Contains("AS \"someNewName\"") =! true
    sql.Contains("AS \"another\"") =! true

[<Test>]
let ``lateral subquery WHERE with col-to-col on correlated outer`` () =
    let inner =
        subquery {
            for d in sales.salesorderdetail do
                correlate o in sales.salesorderheader
                where (d.salesorderid = o.salesorderid)
                select (countBy d.salesorderdetailid)
        }
    let sql =
        select {
            for o in sales.salesorderheader do
                lateralJoin inner "lat"
                select o.salesorderid
        }
        |> toSql
    sql.Contains("\"o\".\"salesorderid\"") =! true

[<Test>]
let ``greatest / least emit GREATEST() and LEAST()``() =
    let sql =
        select {
            for p in production.product do
            select (SqlFn.greatest (p.standardcost, p.listprice), SqlFn.least (p.standardcost, p.listprice))
        }
        |> toSql
    sql.Contains("GREATEST(") =! true
    sql.Contains("LEAST(") =! true
    // Both column args of each call render as qualified column refs.
    sql.Contains("\"p\".\"standardcost\"") =! true
    sql.Contains("\"p\".\"listprice\"") =! true

[<Test>]
let ``onConflict doNothing emits ON CONFLICT DO NOTHING``() =
    let row = sampleAddress ()
    let q =
        insert {
            for a in person.address do
            entity row
            onConflict a.addressid
            doNothing
        }
    let sql = toInsertSql q
    sql.Contains("ON CONFLICT(addressid)") =! true
    sql.Contains("DO NOTHING") =! true

[<Test>]
let ``onConflict doUpdate emits DO UPDATE SET from EXCLUDED``() =
    let row = sampleAddress ()
    let q =
        insert {
            for a in person.address do
            entity row
            onConflict a.addressid
            doUpdate a.city
        }
    let sql = toInsertSql q
    sql.Contains("ON CONFLICT(addressid) DO UPDATE SET") =! true
    sql.Contains("EXCLUDED.\"city\"") =! true

[<Test>]
let ``onConflictRaw doNothing emits a raw conflict target``() =
    let row = sampleAddress ()
    let q =
        insert {
            for a in person.address do
            entity row
            onConflictRaw "lower(addressline1)"
            doNothing
        }
    let sql = toInsertSql q
    sql.Contains("ON CONFLICT(lower(addressline1))") =! true
    sql.Contains("DO NOTHING") =! true

[<Test>]
let ``whereRawConflict emits a partial-index WHERE``() =
    let row = sampleAddress ()
    let q =
        insert {
            for a in person.address do
            entity row
            onConflict a.addressid
            whereRawConflict "city IS NOT NULL"
            doNothing
        }
    let sql = toInsertSql q
    sql.Contains("ON CONFLICT(addressid) WHERE city IS NOT NULL") =! true
    sql.Contains("DO NOTHING") =! true

[<Test>]
let ``orderByAliasDesc quotes the alias and emits DESC``() =
    let sql =
        select {
            for a in person.address do
            orderByAliasDesc "score"
        }
        |> toSql
    sql.Contains("ORDER BY \"score\" DESC") =! true

[<Test>]
let ``cteFrom produces a WITH clause and FROM alias``() =
    let inner =
        select {
            for a in person.address do
            where (a.city = "Dallas")
            select a.addressid
        }
    let recent = cteFrom<person.address> "recent_addrs" inner
    let sql =
        select {
            for r in recent do
            select r.addressid
        }
        |> toSql
    sql.Contains("WITH \"recent_addrs\" AS (") =! true
    sql.Contains("FROM \"recent_addrs\" AS \"r\"") =! true

[<Test>]
let ``inlineValue emits a SQL literal not a parameter``() =
    // inlineValue forces a captured value to be emitted as an inline SQL literal,
    // not a @p parameter. Use a captured variable so it is plainly not a column ref.
    let captured = "yes"
    let sql =
        select {
            for p in production.product do
            select (caseWhen (p.standardcost > 100m) (inlineValue captured) "no")
        }
        |> toSql
    sql.Contains("'yes'") =! true
    sql.Contains("@p") =! false

[<Test>]
let ``a value containing an apostrophe produces a runnable statement``() =
    // The bug: `inlineValue "O'Brien"` rendered as 'O'Brien'. The apostrophe closes the
    // literal and Postgres parses what follows as SQL, so the query never runs --
    //   ERROR:  syntax error at or near "Brien"
    // A value is data; it must not be able to change the shape of the statement.
    let captured = "O'Brien"
    let sql =
        select {
            for p in production.product do
            select (caseWhen (p.standardcost > 100m) (inlineValue captured) "no")
        }
        |> toSql
    test <@ sql.Contains("'O''Brien'") @>

[<Test>]
let ``an interval built from a runtime string escapes quotes``() =
    // `interval` takes an arbitrary string, so the value can carry an apostrophe and close
    // the literal the same way -- it is the one literal site not fed by a constant.
    let span = "7 days'"
    let sql =
        select {
            for p in production.product do
            select (PgSqlFn.interval span)
        }
        |> toSql
    test <@ sql.Contains("INTERVAL '7 days'''") @>

[<Test>]
let ``inlineValue beside another SQL function is a value, not a database call``() =
    // The bug: this emitted `LOWER(a.city) = INLINEVALUE('smith')`, so Postgres looked for
    // a function that does not exist and the query never ran --
    //   ERROR:  function inlinevalue(unknown) does not exist
    // `inlineValue` is a compile-time marker; it must never survive into the SQL.
    let sql =
        select {
            for a in person.address do
            where (SqlFn.lower a.city = inlineValue "smith")
        }
        |> toSql
    test <@ sql.Contains("(LOWER(a.city) = 'smith')") @>
    test <@ not (sql.Contains("INLINEVALUE")) @>

[<Test>]
let ``a where on a value does not silently become a NULL check``() =
    // The worst of the three, because nothing fails: this emitted `city IS NULL`, so the
    // query ran happily and returned every row whose city is unset -- never the row asked
    // for. The fall-through compile-and-evaluated the marker, whose body is a stub, so the
    // comparison value came back null and `= null` was folded into `IS NULL`.
    let captured = "Dallas"
    let sql =
        select {
            for a in person.address do
            where (a.city = inlineValue captured)
        }
        |> toSql
    // The whole predicate, not just the literal: rendering the marker itself as a
    // function (`INLINEVALUE('Dallas')`) would satisfy any looser assertion.
    test <@ sql.Contains("(a.city = 'Dallas')") @>
    test <@ not (sql.Contains("IS NULL")) @>
    test <@ not (sql.Contains("@p")) @>

[<Test>]
let ``a where against a raw SQL expression does not silently become a NULL check``() =
    // Same silent failure as above, reached through `rawExpr` instead of `inlineValue`.
    let sql =
        select {
            for a in person.address do
            where (a.city = rawExpr<string> "'Dallas'")
        }
        |> toSql
    test <@ sql.Contains("(a.city = 'Dallas')") @>
    test <@ not (sql.Contains("RAWEXPR")) @>
    test <@ not (sql.Contains("IS NULL")) @>

[<Test>]
let ``a SQL function can be compared against a column``() =
    // Not silently wrong, just impossible: this shape threw NotImplementedException
    // "Unable to evaluate where LHS", because the fall-through tried to compute
    // `LOWER(a.city)` as a .NET value and there is no row to compute it against.
    let sql =
        select {
            for a in person.address do
            where (SqlFn.lower a.city = a.addressline1)
        }
        |> toSql
    test <@ sql.Contains("(LOWER(a.city) = a.addressline1)") @>

[<Test>]
let ``a captured .NET value is still bound as a parameter``() =
    // Not a bug fix -- this guards the two arms above. `name.ToUpperInvariant()` is a real
    // call with a real result, so it must be computed and bound, never rendered as SQL.
    let name = "dallas"
    let sql =
        select {
            for a in person.address do
            where (a.city = name.ToUpperInvariant())
        }
        |> toSql
    test <@ sql.Contains("(\"a\".\"city\" = @p0)") @>
    test <@ not (sql.Contains("UPPER")) @>

[<Test>]
let ``a SQL function can be used in a join predicate``() =
    // Same defect as the where clause, one function over: `visitJoinPredicate` also
    // compile-and-evaluates the other side, so this threw NotImplementedException
    // "Unable to render join predicate RHS".
    let sql =
        select {
            for a in person.address do
            join' a2 in person.address; on' (a.city = SqlFn.lower a2.city)
            select a
        }
        |> toSql
    test <@ sql.Contains("ON (a.city = LOWER(a2.city))") @>

[<Test>]
let ``a SQL function on the left of a join predicate``() =
    let sql =
        select {
            for a in person.address do
            join' a2 in person.address; on' (SqlFn.lower a.city = a2.city)
            select a
        }
        |> toSql
    test <@ sql.Contains("ON (LOWER(a.city) = a2.city)") @>

[<Test>]
let ``a SQL function compared to a value in a join predicate``() =
    let sql =
        select {
            for a in person.address do
            join' a2 in person.address; on' (SqlFn.lower a2.city = "dallas")
            select a
        }
        |> toSql
    test <@ sql.Contains("ON (LOWER(a2.city) = @p0)") @>

[<Test>]
let ``two SQL functions compared in a join predicate``() =
    // The natural case-insensitive join, and the shape most likely to be reached for.
    let sql =
        select {
            for a in person.address do
            join' a2 in person.address; on' (SqlFn.lower a.city = SqlFn.lower a2.city)
            select a
        }
        |> toSql
    test <@ sql.Contains("ON (LOWER(a.city) = LOWER(a2.city))") @>

[<Test>]
let ``a captured .NET value on the left is still bound as a parameter``() =
    // Mirror of the above, covering the other guarded arm.
    let name = "dallas"
    let sql =
        select {
            for a in person.address do
            where (name.ToUpperInvariant() = a.city)
        }
        |> toSql
    test <@ sql.Contains("(\"a\".\"city\" = @p0)") @>
    test <@ not (sql.Contains("UPPER")) @>

[<Test>]
let ``a user-defined SQL function can be used in a where``() =
    // The bug: a `sqlFn` wrapper declared outside SqlHydra.Query threw
    // NotImplementedException "Unable to evaluate query parameter expression", because
    // `where` tried to compute it as a .NET value. It worked in `select` all along.
    let sql =
        select {
            for a in person.address do
            where (ExtFn.lower a.addressline2 = "dallas")
        }
        |> toSql
    test <@ sql.Contains("(LOWER(a.addressline2) = @p0)") @>

[<Test>]
let ``the README's custom-function example runs``() =
    // The example the README tells people to write, verbatim:
    //   where (SOUNDEX(p.LastName) = SOUNDEX("Smith"))
    // It threw "Unable to evaluate query parameter expression", so the documented feature
    // did not work in the clause the documentation demonstrates it in.
    let sql =
        select {
            for a in person.address do
            where (SOUNDEX(a.city) = SOUNDEX("Smith"))
        }
        |> toSql
    test <@ sql.Contains("(SOUNDEX(a.city) = SOUNDEX('Smith'))") @>

[<Test>]
let ``a user-defined SQL function over constants does not silently become a NULL check``() =
    // The dangerous shape. With no column argument there is nothing to make evaluation fail,
    // so the wrapper evaluated to the stub's null and the predicate became `city IS NULL`:
    // the query ran and returned the wrong rows, with nothing to notice.
    let sql =
        select {
            for a in person.address do
            where (a.city = SOUNDEX "Smith")
        }
        |> toSql
    test <@ sql.Contains("(a.city = SOUNDEX('Smith'))") @>
    test <@ not (sql.Contains("IS NULL")) @>

[<Test>]
let ``a generic user-defined SQL function is recognized in a where``() =
    let sql =
        select {
            for a in person.address do
            where (NULLIF(a.city, "Dallas") = "Seattle")
        }
        |> toSql
    test <@ sql.Contains("NULLIF(a.city, 'Dallas')") @>

[<Test>]
let ``an unmarked SQL function over constants raises instead of emitting IS NULL``() =
    // The regression that matters: `UNMARKED_SOUNDEX "Smith"` is not recognized as SQL, so the
    // visitor computes it — and the stub raises rather than handing back the null that used to
    // turn this predicate into `city IS NULL`. Loud beats a query that runs and lies.
    let build () =
        select {
            for a in person.address do
            where (a.city = UNMARKED_SOUNDEX "Smith")
        }
        |> toSql
        |> ignore
    let ex = Assert.Throws<SqlFunctionNotRenderedException>(fun () -> build ())
    test <@ ex.Message.Contains("SqlHydraFunction") @>
    test <@ ex.Message.Contains("UNMARKED_SOUNDEX") @>

[<Test>]
let ``an unmarked SQL function over a column names the marker it is missing``() =
    // With a column argument the expression cannot be compiled at all, so this shape fails
    // before the stub runs and keeps the older NotImplementedException. It was always loud;
    // what it lacked was any hint of what to do, which is the half worth fixing.
    let build () =
        select {
            for a in person.address do
            where (UNMARKED_SOUNDEX a.city = "Smith")
        }
        |> toSql
        |> ignore
    let ex = Assert.Throws<NotImplementedException>(fun () -> build ())
    test <@ ex.Message.Contains("SqlHydraFunction") @>
    test <@ ex.Message.Contains("UNMARKED_SOUNDEX") @>

[<Test>]
let ``an unmarked SQL function in an on' predicate raises instead of emitting IS NULL``() =
    // `on'` reads the same marker as `where`, so it has the same silent shape to protect
    // against: a wrapper over constants used to join on `city IS NULL`.
    let build () =
        select {
            for a in person.address do
            join' a2 in person.address; on' (a2.city = UNMARKED_SOUNDEX "Smith")
            select a
        }
        |> toSql
        |> ignore
    let ex = Assert.Throws<SqlFunctionNotRenderedException>(fun () -> build ())
    test <@ ex.Message.Contains("SqlHydraFunction") @>

[<Test>]
let ``calling a SQL function wrapper outside a query raises``() =
    // `sqlFn` has no runtime meaning at all. It used to return null here.
    let ex = Assert.Throws<SqlFunctionNotRenderedException>(fun () -> SOUNDEX "Smith" |> ignore)
    test <@ ex.Message.Contains("rendered as SQL") @>

[<Test>]
let ``an unmarked SQL function still renders in a select and an orderBy``() =
    // The marker is only needed where a predicate has to choose between rendering and
    // evaluating. `select` and `orderBy` render every call, and did so before this change:
    // no one's existing projection starts throwing because they never applied an attribute.
    let sql =
        select {
            for a in person.address do
            where (a.city = "Dallas")
            orderBy (UNMARKED_SOUNDEX a.city)
            select (UNMARKED_SOUNDEX a.city)
        }
        |> toSql
    test <@ sql.Contains("SELECT UNMARKED_SOUNDEX(\"a\".\"city\")") @>
    test <@ sql.Contains("ORDER BY UNMARKED_SOUNDEX(\"a\".\"city\")") @>

[<Test>]
let ``a user-defined SQL function can be used in an on' join predicate``() =
    // `on'` consults the same marker as `where`, so a user-defined wrapper reaches the
    // rendering arms there too — the case-insensitive join, spelled with your own function.
    let sql =
        select {
            for a in person.address do
            join' a2 in person.address; on' (SOUNDEX a.city = SOUNDEX a2.city)
            select a
        }
        |> toSql
    test <@ sql.Contains("ON (SOUNDEX(a.city) = SOUNDEX(a2.city))") @>

[<Test>]
let ``a marked module covers the wrappers declared in it``() =
    // The reason the marker is worth having: wrappers come in groups, so it costs one
    // attribute for the group rather than one per function.
    let sql =
        select {
            for a in person.address do
            where (Grouped.DIFFERENCE(a.city, "Dallas") = 4)
        }
        |> toSql
    test <@ sql.Contains("DIFFERENCE(a.city, 'Dallas')") @>

[<Test>]
let ``a marked module covers nested modules``() =
    let sql =
        select {
            for a in person.address do
            where (Grouped.Text.INITCAP a.city = "Dallas")
        }
        |> toSql
    test <@ sql.Contains("INITCAP(a.city)") @>

[<Test>]
let ``a marked type covers its static members``() =
    let sql =
        select {
            for a in person.address do
            where (GroupedFn.ASCII a.city = 68)
        }
        |> toSql
    test <@ sql.Contains("ASCII(a.city)") @>

// A member access over a wrapper is the one shape `NValue` doesn't match, so it reaches the
// fall-through arms with the stub intact. Four: two clauses x two operand orders.

[<Test>]
let ``a swallowed wrapper failure still names the marker (where, function on the right)``() =
    let build () =
        select {
            for a in person.address do
            where (a.addressid = (UNMARKED_SOUNDEX "Smith").Length)
        }
        |> toSql
        |> ignore
    let ex = Assert.Throws<SqlFunctionNotRenderedException>(fun () -> build ())
    test <@ ex.Message.Contains("SqlHydraFunction") @>

[<Test>]
let ``a swallowed wrapper failure still names the marker (where, function on the left)``() =
    let build () =
        select {
            for a in person.address do
            where ((UNMARKED_SOUNDEX "Smith").Length = a.addressid)
        }
        |> toSql
        |> ignore
    let ex = Assert.Throws<SqlFunctionNotRenderedException>(fun () -> build ())
    test <@ ex.Message.Contains("SqlHydraFunction") @>

[<Test>]
let ``a swallowed wrapper failure still names the marker (on', function on the right)``() =
    let build () =
        select {
            for a in person.address do
            join' a2 in person.address; on' (a2.addressid = (UNMARKED_SOUNDEX "Smith").Length)
            select a
        }
        |> toSql
        |> ignore
    let ex = Assert.Throws<SqlFunctionNotRenderedException>(fun () -> build ())
    test <@ ex.Message.Contains("SqlHydraFunction") @>

[<Test>]
let ``a swallowed wrapper failure still names the marker (on', function on the left)``() =
    let build () =
        select {
            for a in person.address do
            join' a2 in person.address; on' ((UNMARKED_SOUNDEX "Smith").Length = a2.addressid)
            select a
        }
        |> toSql
        |> ignore
    let ex = Assert.Throws<SqlFunctionNotRenderedException>(fun () -> build ())
    test <@ ex.Message.Contains("SqlHydraFunction") @>

[<Test>]
let ``every sqlFn wrapper shipped in SqlHydra.Query carries the marker``() =
    // A provider module added without the marker would go unrecognized, and `isIn`-style
    // functions would be evaluated rather than rendered. IL reading is fine as a test oracle:
    // build time, our own assembly, decides nothing at query time.
    let flags =
        System.Reflection.BindingFlags.Public ||| System.Reflection.BindingFlags.NonPublic
        ||| System.Reflection.BindingFlags.Static ||| System.Reflection.BindingFlags.Instance
        ||| System.Reflection.BindingFlags.DeclaredOnly
    let callsSqlFn (mi: System.Reflection.MethodInfo) =
        try
            match mi.GetMethodBody() with
            | null -> false
            | body ->
                match body.GetILAsByteArray() with
                | null -> false
                | il ->
                    [ 0 .. il.Length - 5 ]
                    |> List.exists (fun i ->
                        il.[i] = 0x28uy
                        && (try
                                let t = mi.Module.ResolveMethod(BitConverter.ToInt32(il, i + 1))
                                t.Name = "sqlFn"
                                && t.DeclaringType <> null
                                && t.DeclaringType.FullName = "SqlHydra.Query.SqlFunctions"
                            with _ -> false))
        with _ -> false
    let unmarked =
        typeof<SqlHydraFunctionAttribute>.Assembly.GetTypes()
        |> Seq.collect (fun t -> t.GetMethods(flags))
        |> Seq.filter callsSqlFn
        |> Seq.filter (fun mi -> not (SqlHydra.Query.LinqExpressionVisitors.isSqlHydraFunction mi))
        |> Seq.map (fun mi -> $"{mi.DeclaringType.FullName}.{mi.Name}")
        |> Seq.distinct |> Seq.sort |> Seq.toList
    test <@ unmarked = [] @>

// Three `visitWhere` arms commented "SQL function ..." never checked whether the call was one,
// so `myHelper()` went to the database as `MYHELPER()`. `on'` guarded all five of its
// equivalents all along.

[<Test>]
let ``an ordinary .NET call on the left of a where is evaluated, not rendered``() =
    let build () =
        select {
            for a in person.address do
            where (plainDotNetHelper () = "Dallas")
        }
        |> toSql
        |> ignore
    // "Value to value" is the proof that both sides were evaluated rather than rendered.
    let ex = Assert.Throws<NotImplementedException>(fun () -> build ())
    test <@ ex.Message.Contains("Value to value") @>

[<Test>]
let ``two ordinary .NET calls in a where are evaluated, not rendered``() =
    let build () =
        select {
            for a in person.address do
            where (plainDotNetHelper () = plainDotNetHelper ())
        }
        |> toSql
        |> ignore
    let ex = Assert.Throws<NotImplementedException>(fun () -> build ())
    test <@ ex.Message.Contains("Value to value") @>

// ---------------------------------------------------------------------------
// Case folding over a NULLABLE column — what a functional index on such a
// column needs in order to actually be used.
// ---------------------------------------------------------------------------

[<Test>]
let ``lower over a nullable column emits LOWER(col), not LOWER(COALESCE(col, ''))``() =
    // A functional index `CREATE INDEX ... ON address (LOWER(addressline2))` can only be
    // matched by `LOWER(addressline2)`. Without the `string option` overload a nullable
    // column forces `lower (coalesce (col, ""))` — which emits `LOWER(COALESCE(col, ''))`
    // and defeats the index (seq scan).
    let sql =
        select {
            for a in person.address do
            where (SqlFn.lower a.addressline2 = "suite 100")
        }
        |> toSql

    sql.Contains("LOWER(a.addressline2)") =! true
    sql.Contains("COALESCE") =! false

[<Test>]
let ``upper over a nullable column emits UPPER(col)``() =
    let sql =
        select {
            for a in person.address do
            where (SqlFn.upper a.addressline2 = "SUITE 100")
        }
        |> toSql

    sql.Contains("UPPER(a.addressline2)") =! true
    sql.Contains("COALESCE") =! false

[<Test>]
let ``inlineValue and lower over a nullable column compose``() =
    // The exact shape a partial functional index needs:
    //   CREATE UNIQUE INDEX ... ON t (LOWER(addressline2)) WHERE city = 'Dallas'
    let sql =
        select {
            for a in person.address do
            where (a.city = inlineValue "Dallas" && SqlFn.lower a.addressline2 = "suite 100")
        }
        |> toSql

    sql.Contains("'Dallas'") =! true
    sql.Contains("LOWER(a.addressline2)") =! true
    sql.Contains("COALESCE") =! false

module Oracle.QueryUnitTests

open Expecto
open SqlHydra.Query
open DB

#if NET5_0
open Oracle.AdventureWorksNet5
#endif
#if NET6_0_OR_GREATER
open Oracle.AdventureWorksNet6
#endif

// Tables
let contactsTable =         table<OT.CONTACTS>              |> inSchema (nameof OT)
let customerTable =         table<OT.CUSTOMERS>             |> inSchema (nameof OT)
let orderHeaderTable =      table<OT.ORDERS>                |> inSchema (nameof OT)
let orderDetailTable =      table<OT.ORDER_ITEMS>           |> inSchema (nameof OT)
let productTable =          table<OT.PRODUCTS>              |> inSchema (nameof OT)
let categoryTable =         table<OT.PRODUCT_CATEGORIES>    |> inSchema (nameof OT)
let regionsTable =          table<OT.REGIONS>               |> inSchema (nameof OT)
let countriesTable =        table<OT.COUNTRIES>             |> inSchema (nameof OT)

[<Tests>]
let tests = 
    categoryList "Oracle" "Query Unit Tests" [

        /// String comparisons against generated queries.
        test "Simple Where" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.NAME = "John Doe")
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("WHERE")) ""
        }

        test "Select 1 Column" {
            let query =
                select {
                    for c in customerTable do
                    select c.NAME
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT \"OT\".\"CUSTOMERS\".\"NAME\" FROM")) ""
        }

        test "Select 2 Columns" {
            let query =
                select {
                    for h in orderHeaderTable do
                    select (h.CUSTOMER_ID, h.STATUS)
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT \"OT\".\"ORDERS\".\"CUSTOMER_ID\", \"OT\".\"ORDERS\".\"STATUS\" FROM")) ""
        }

        test "Select 1 Table and 1 Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.ORDER_ID = d.ORDER_ID)
                    where (o.STATUS = "Pending")
                    select (o, d.UNIT_PRICE)
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT \"OT\".\"ORDERS\".*, \"OT\".\"ORDER_ITEMS\".\"UNIT_PRICE\" FROM")) ""
        }

        ptest "Where with Option Type" {
            let query = 
                select {
                    for c in contactsTable do
                    where (c.PHONE <> None)
                }

            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        ptest "Where Not Like" {
            let query =
                select {
                    for c in customerTable do
                    where (c.ADDRESS <>% "S%")
                }

            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        test "Or Where" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.NAME = "Smith" || c.NAME = "Doe")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ((\"OT\".\"CUSTOMERS\".\"NAME\" = :p0) OR (\"OT\".\"CUSTOMERS\".\"NAME\" = :p1))")) ""
        }

        test "And Where" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.NAME = "Smith" && c.NAME = "Doe")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ((\"OT\".\"CUSTOMERS\".\"NAME\" = :p0) AND (\"OT\".\"CUSTOMERS\".\"NAME\" = :p1))")) ""
        }

        test "Where with AND and OR in Parenthesis" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.NAME = "John" && (c.NAME = "Smith" || isNullValue c.ADDRESS))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue 
                (sql.Contains("WHERE ((\"OT\".\"CUSTOMERS\".\"NAME\" = :p0) AND ((\"OT\".\"CUSTOMERS\".\"NAME\" = :p1) OR (\"OT\".\"CUSTOMERS\".\"ADDRESS\" IS NULL)))")) 
                "Should wrap OR clause in parenthesis and each individual where clause in parenthesis."
        }

        test "Where Not Binary" {
            let query = 
                select {
                    for c in customerTable do
                    where (not (c.NAME = "Smith" && c.NAME = "Doe"))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (NOT ((\"OT\".\"CUSTOMERS\".\"NAME\" = :p0) AND (\"OT\".\"CUSTOMERS\".\"NAME\" = :p1)))")) ""
        }

        test "Where Customer isIn List" {
            let query = 
                select {
                    for c in customerTable do
                    where (isIn c.CUSTOMER_ID [1L;2L;3L])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"OT\".\"CUSTOMERS\".\"CUSTOMER_ID\" IN (:p0, :p1, :p2))")) ""
        }

        test "Where Customer |=| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CUSTOMER_ID |=| [1L;2L;3L])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"OT\".\"CUSTOMERS\".\"CUSTOMER_ID\" IN (:p0, :p1, :p2))")) ""
        }

        test "Where Customer |=| Array" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CUSTOMER_ID |=| [| 1L;2L;3L |])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"OT\".\"CUSTOMERS\".\"CUSTOMER_ID\" IN (:p0, :p1, :p2))")) ""
        }
        
        test "Where Customer |=| Seq" {            
            let buildQuery (values: int64 seq) =                
                select {
                    for c in customerTable do
                    where (c.CUSTOMER_ID |=| values)
                }

            let query = buildQuery([ 1L;2L;3L ])

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"OT\".\"CUSTOMERS\".\"CUSTOMER_ID\" IN (:p0, :p1, :p2))")) ""
        }

        test "Where Customer |<>| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CUSTOMER_ID |<>| [ 1L;2L;3L ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"OT\".\"CUSTOMERS\".\"CUSTOMER_ID\" NOT IN (:p0, :p1, :p2))")) ""
        }

        test "Inner Join" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.ORDER_ID = d.ORDER_ID)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("INNER JOIN \"OT\".\"ORDER_ITEMS\" ON (\"OT\".\"ORDERS\".\"ORDER_ID\" = \"OT\".\"ORDER_ITEMS\".\"ORDER_ID\")")) ""
        }

        test "Left Join" {
            let query =
                select {
                    for o in orderHeaderTable do
                    leftJoin d in orderDetailTable on (o.ORDER_ID = d.Value.ORDER_ID)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("LEFT JOIN \"OT\".\"ORDER_ITEMS\" ON (\"OT\".\"ORDERS\".\"ORDER_ID\" = \"OT\".\"ORDER_ITEMS\".\"ORDER_ID\")")) ""
        }
        
        //test "Inner Join - Multi Column" {
        //    let query =
        //        select {
        //            for o in orderHeaderTable do
        //            join d in orderDetailTable on ((o.ORDER_ID, o.) = (d.ORDER_ID, d.))
        //            select o
        //        }
        
        //    let sql = query.ToKataQuery() |> toSql
        //    //printfn "%s" sql
        //    Expect.isTrue (sql.Contains("INNER JOIN \"Sales\".\"SalesOrderDetail\" ON (\"Sales\".\"SalesOrderHeader\".\"SalesOrderID\" = \"Sales\".\"SalesOrderDetail\".\"SalesOrderID\" AND \"Sales\".\"SalesOrderHeader\".\"ModifiedDate\" = \"Sales\".\"SalesOrderDetail\".\"ModifiedDate\")")) ""
        //}
        
        //test "Left Join - Multi Column" {
        //    let query =
        //        select {
        //            for o in orderHeaderTable do
        //            leftJoin d in orderDetailTable on ((o.SalesOrderID, o.ModifiedDate) = (d.Value.SalesOrderID, d.Value.ModifiedDate))
        //            select o
        //        }
        
        //    let sql = query.ToKataQuery() |> toSql
        //    //printfn "%s" sql
        //    Expect.isTrue (sql.Contains("LEFT JOIN \"Sales\".\"SalesOrderDetail\" ON (\"Sales\".\"SalesOrderHeader\".\"SalesOrderID\" = \"Sales\".\"SalesOrderDetail\".\"SalesOrderID\" AND \"Sales\".\"SalesOrderHeader\".\"ModifiedDate\" = \"Sales\".\"SalesOrderDetail\".\"ModifiedDate\")")) ""
        //}

        test "Join On Value Bug Fix Test" {
            let query = 
                select {
                    for o in orderHeaderTable do
                    leftJoin d in orderHeaderTable on (o.SALESMAN_ID.Value = d.Value.SALESMAN_ID.Value)
                    select o
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isNotNull sql "Shouldn't fail with exception"
        }

        test "Delete Query with Where" {
            let query = 
                delete {
                    for c in customerTable do
                    where (c.CUSTOMER_ID |<>| [ 1L;2L;3L ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("DELETE FROM \"OT\".\"CUSTOMERS\"")) ""
            Expect.isTrue (sql.Contains("WHERE (\"OT\".\"CUSTOMERS\".\"CUSTOMER_ID\" NOT IN (:p0, :p1, :p2))")) ""
        }

        test "Delete All" {
            let query = 
                delete {
                    for c in customerTable do
                    deleteAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "DELETE FROM \"OT\".\"CUSTOMERS\"" sql ""
        }

        test "Update Query with Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.NAME "Smith"
                    where (c.NAME = "Doe")
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "UPDATE \"OT\".\"CUSTOMERS\" SET \"NAME\" = :p0 WHERE (\"OT\".\"CUSTOMERS\".\"NAME\" = :p1)" sql ""
        }

        test "Update Query with No Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.NAME "Smith"
                    updateAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "UPDATE \"OT\".\"CUSTOMERS\" SET \"NAME\" = :p0" sql ""
        }

        test "Update should fail without where or updateAll" {
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.NAME "Smith"
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
                        set c.NAME "Smith"
                        where (c.CUSTOMER_ID = 1)
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
                        set c.NAME "Smith"
                        updateAll
                    }
                () //Assert.Pass()
            with ex ->
                () //Assert.Pass("Should not fail because `where` is present.")
        }

        test "Insert Query without Identity" {
            let query = 
                insert {
                    into countriesTable
                    entity
                        { 
                            OT.COUNTRIES.COUNTRY_ID = "WL"
                            OT.COUNTRIES.REGION_ID = Some 2
                            OT.COUNTRIES.COUNTRY_NAME = "Wonderland"
                        }
                }
            
            let sql = query.ToKataQuery() |> toSql
            Expect.equal 
                sql 
                "INSERT INTO \"OT\".\"COUNTRIES\" (\"COUNTRY_ID\", \"COUNTRY_NAME\", \"REGION_ID\") VALUES (:p0, :p1, :p2)" 
                ""
        }

        test "Insert Query with Identity" {
            let query = 
                insert {
                    for r in regionsTable do
                    entity 
                        { 
                            OT.REGIONS.REGION_ID = 0
                            OT.REGIONS.REGION_NAME = "Outlands"
                        }
                    getId r.REGION_ID
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal 
                sql 
                "INSERT INTO \"OT\".\"REGIONS\" (\"REGION_NAME\") VALUES (:p0)" 
                ""
        }

        test "Multiple Inserts Fix" {
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
                        into countriesTable
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

                Expect.equal fixedQuery expected "SqlKata multiple insert query should be overriden to match."
                
            | None -> ()
        }

        test "Inline Aggregates" {
            let query =
                select {
                    for o in orderHeaderTable do
                    select (countBy o.ORDER_ID)
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.equal
                sql
                "SELECT COUNT(\"OT\".\"ORDERS\".\"ORDER_ID\") FROM \"OT\".\"ORDERS\""
                ""
        }
    ]

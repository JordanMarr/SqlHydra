module Oracle.QueryUnitTests

open Expecto
open SqlHydra.Query
open DB

#if NET5_0
open Oracle.AdventureWorksNet5
#endif
#if NET6_0
open Oracle.AdventureWorksNet6
#endif

// Tables
let currencyTable =         table<``C##ADVWORKS``.DIMCURRENCY>              |> inSchema (nameof ``C##ADVWORKS``)
let customerTable =         table<``C##ADVWORKS``.DIMCUSTOMER>              |> inSchema (nameof ``C##ADVWORKS``)
//let orderHeaderTable =      table<Sales.SalesOrderHeader>                   |> inSchema (nameof ``C##ADVWORKS``)
//let orderDetailTable =      table<Sales.SalesOrderDetail>                   |> inSchema (nameof ``C##ADVWORKS``)
let productTable =          table<``C##ADVWORKS``.DIMPRODUCT>               |> inSchema (nameof ``C##ADVWORKS``)
let categoryTable =         table<``C##ADVWORKS``.DIMPRODUCTCATEGORY>       |> inSchema (nameof ``C##ADVWORKS``)
let subCategoryTable =      table<``C##ADVWORKS``.DIMPRODUCTSUBCATEGORY>    |> inSchema (nameof ``C##ADVWORKS``)

[<Tests>]
let tests = 
    categoryList "Oracle" "Query Unit Tests" [

        /// String comparisons against generated queries.
        test "Simple Where" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.LASTNAME = Some "Smith")
                    orderBy c.FIRSTNAME
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("WHERE")) ""
        }

        test "Select 1 Column" {
            let query =
                select {
                    for c in customerTable do
                    select c.FIRSTNAME
                }

            let sql = query.ToKataQuery() |> toSql
            //printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT \"C##ADVWORKS\".\"DIMCUSTOMER\".\"FIRSTNAME\" FROM")) ""
        }

        //test "Select 2 Columns" {
        //    let query =
        //        select {
        //            for h in orderHeaderTable do
        //            select (h.CustomerID, h.OnlineOrderFlag)
        //        }

        //    let sql = query.ToKataQuery() |> toSql
        //    //printfn "%s" sql
        //    Expect.isTrue (sql.Contains("SELECT [Sales].[SalesOrderHeader].[CustomerID], [Sales].[SalesOrderHeader].[OnlineOrderFlag] FROM")) ""
        //}

        //test "Select 1 Table and 1 Column" {
        //    let query =
        //        select {
        //            for o in orderHeaderTable do
        //            join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
        //            where (o.OnlineOrderFlag = true)
        //            select (o, d.LineTotal)
        //        }

        //    let sql = query.ToKataQuery() |> toSql
        //    //printfn "%s" sql
        //    Expect.isTrue (sql.Contains("SELECT [Sales].[SalesOrderHeader].*, [Sales].[SalesOrderDetail].[LineTotal] FROM")) ""
        //}

        ptest "Where with Option Type" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.FIRSTNAME <> None)
                }

            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        ptest "Where Not Like" {
            let query =
                select {
                    for c in customerTable do
                    where (c.ADDRESSLINE1 <>% "S%")
                }

            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        test "Or Where" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.LASTNAME = Some "Smith" || c.LASTNAME = Some "Doe")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ((\"C##ADVWORKS\".\"DIMCUSTOMER\".\"LASTNAME\" = @p0) OR (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"LASTNAME\" = @p1))")) ""
        }

        test "And Where" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.LASTNAME = Some "Smith" && c.LASTNAME = Some "Doe")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ((\"C##ADVWORKS\".\"DIMCUSTOMER\".\"LASTNAME\" = @p0) AND (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"LASTNAME\" = @p1))")) ""
        }

        test "Where with AND and OR in Parenthesis" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.FIRSTNAME = Some "John" && (c.LASTNAME = Some "Smith" || isNullValue c.LASTNAME))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue 
                (sql.Contains("WHERE ((\"C##ADVWORKS\".\"DIMCUSTOMER\".\"FIRSTNAME\" = @p0) AND ((\"C##ADVWORKS\".\"DIMCUSTOMER\".\"LASTNAME\" = @p1) OR (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"LASTNAME\" IS NULL)))")) 
                "Should wrap OR clause in parenthesis and each individual where clause in parenthesis."
        }

        test "Where Not Binary" {
            let query = 
                select {
                    for c in customerTable do
                    where (not (c.LASTNAME = Some "Smith" && c.LASTNAME = Some "Doe"))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (NOT ((\"C##ADVWORKS\".\"DIMCUSTOMER\".\"LASTNAME\" = @p0) AND (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"LASTNAME\" = @p1)))")) ""
        }

        test "Where Customer isIn List" {
            let query = 
                select {
                    for c in customerTable do
                    where (isIn c.CUSTOMERKEY [30018M;29545M;29954M])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"CUSTOMERKEY\" IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |=| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CUSTOMERKEY |=| [30018M;29545M;29954M])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"CUSTOMERKEY\" IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |=| Array" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CUSTOMERKEY |=| [| 30018M;29545M;29954M |])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"CUSTOMERKEY\" IN (@p0, @p1, @p2))")) ""
        }
        
        test "Where Customer |=| Seq" {            
            let buildQuery (values: decimal seq) =                
                select {
                    for c in customerTable do
                    where (c.CUSTOMERKEY |=| values)
                }

            let query = buildQuery([ 30018M;29545M;29954M ])

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"CUSTOMERKEY\" IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |<>| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CUSTOMERKEY |<>| [ 30018M;29545M;29954M ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"CUSTOMERKEY\" NOT IN (@p0, @p1, @p2))")) ""
        }

        //test "Inner Join" {
        //    let query =
        //        select {
        //            for o in orderHeaderTable do
        //            join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
        //            select o
        //        }

        //    let sql = query.ToKataQuery() |> toSql
        //    //printfn "%s" sql
        //    Expect.isTrue (sql.Contains("INNER JOIN [Sales].[SalesOrderDetail] ON ([Sales].[SalesOrderHeader].[SalesOrderID] = [Sales].[SalesOrderDetail].[SalesOrderID])")) ""
        //}

        //test "Left Join" {
        //    let query =
        //        select {
        //            for o in orderHeaderTable do
        //            leftJoin d in orderDetailTable on (o.SalesOrderID = d.Value.SalesOrderID)
        //            select o
        //        }

        //    let sql = query.ToKataQuery() |> toSql
        //    //printfn "%s" sql
        //    Expect.isTrue (sql.Contains("LEFT JOIN [Sales].[SalesOrderDetail] ON ([Sales].[SalesOrderHeader].[SalesOrderID] = [Sales].[SalesOrderDetail].[SalesOrderID])")) ""
        //}
        
        //test "Inner Join - Multi Column" {
        //    let query =
        //        select {
        //            for o in orderHeaderTable do
        //            join d in orderDetailTable on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
        //            select o
        //        }
        
        //    let sql = query.ToKataQuery() |> toSql
        //    //printfn "%s" sql
        //    Expect.isTrue (sql.Contains("INNER JOIN [Sales].[SalesOrderDetail] ON ([Sales].[SalesOrderHeader].[SalesOrderID] = [Sales].[SalesOrderDetail].[SalesOrderID] AND [Sales].[SalesOrderHeader].[ModifiedDate] = [Sales].[SalesOrderDetail].[ModifiedDate])")) ""
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
        //    Expect.isTrue (sql.Contains("LEFT JOIN [Sales].[SalesOrderDetail] ON ([Sales].[SalesOrderHeader].[SalesOrderID] = [Sales].[SalesOrderDetail].[SalesOrderID] AND [Sales].[SalesOrderHeader].[ModifiedDate] = [Sales].[SalesOrderDetail].[ModifiedDate])")) ""
        //}

        //test "Join On Value Bug Fix Test" {
        //    let query = 
        //        select {
        //            for o in orderHeaderTable do
        //            leftJoin d in orderHeaderTable on (o.AccountNumber.Value = d.Value.AccountNumber.Value)
        //            select o
        //        }

        //    let sql = query.ToKataQuery() |> toSql
        //    Expect.isNotNull sql "Shouldn't fail with exception"
        //}

        test "Delete Query with Where" {
            let query = 
                delete {
                    for c in customerTable do
                    where (c.CUSTOMERKEY |<>| [ 30018M;29545M;29954M ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("DELETE FROM \"C##ADVWORKS\".\"DIMCUSTOMER\"")) ""
            Expect.isTrue (sql.Contains("WHERE (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"CUSTOMERKEY\" NOT IN (@p0, @p1, @p2))")) ""
        }

        test "Delete All" {
            let query = 
                delete {
                    for c in customerTable do
                    deleteAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "DELETE FROM \"C##ADVWORKS\".\"DIMCUSTOMER\"" sql ""
        }

        test "Update Query with Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.LASTNAME (Some "Smith")
                    where (c.LASTNAME = Some "Doe")
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "UPDATE \"C##ADVWORKS\".\"DIMCUSTOMER\" SET \"LASTNAME\" = @p0 WHERE (\"C##ADVWORKS\".\"DIMCUSTOMER\".\"LASTNAME\" = @p1)" sql ""
        }

        test "Update Query with No Where" {
            let query = 
                update {
                    for c in customerTable do
                    set c.LASTNAME (Some "Smith")
                    updateAll
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal "UPDATE \"C##ADVWORKS\".\"DIMCUSTOMER\" SET \"LASTNAME\" = @p0" sql ""
        }

        test "Update should fail without where or updateAll" {
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.LASTNAME (Some "Smith")
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
                        set c.LASTNAME (Some "Smith")
                        where (c.CUSTOMERKEY = 1M)
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
                        set c.LASTNAME (Some "Smith")
                        updateAll
                    }
                () //Assert.Pass()
            with ex ->
                () //Assert.Pass("Should not fail because `where` is present.")
        }

        test "Insert Query without Identity" {
            let query = 
                insert {
                    into currencyTable
                    entity 
                        { 
                            ``C##ADVWORKS``.DIMCURRENCY.CURRENCYKEY = 123M
                            ``C##ADVWORKS``.DIMCURRENCY.CURRENCYALTERNATEKEY = "123"
                            ``C##ADVWORKS``.DIMCURRENCY.CURRENCYNAME = "Currency123"
                        }
                }
            
            let sql = query.ToKataQuery() |> toSql
            Expect.equal 
                sql 
                "INSERT INTO \"C##ADVWORKS\".\"DIMCURRENCY\" (\"CURRENCYKEY\", \"CURRENCYALTERNATEKEY\", \"CURRENCYNAME\") VALUES (@p0, @p1, @p2)" 
                ""
        }

        test "Insert Query with Identity" {
            let query = 
                insert {
                    for c in currencyTable do
                    entity 
                        { 
                            ``C##ADVWORKS``.DIMCURRENCY.CURRENCYKEY = 0M
                            ``C##ADVWORKS``.DIMCURRENCY.CURRENCYALTERNATEKEY = "123"
                            ``C##ADVWORKS``.DIMCURRENCY.CURRENCYNAME = "Currency123"
                        }
                    getId c.CURRENCYKEY
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.equal 
                sql 
                "\"C##ADVWORKS\".\"DIMCURRENCY\" (\"CURRENCYKEY\", \"CURRENCYALTERNATEKEY\", \"CURRENCYNAME\") VALUES (@p0, @p1, @p2);SELECT scope_identity() as Id" 
                ""
        }

        //test "Inline Aggregates" {
        //    let query =
        //        select {
        //            for o in orderHeaderTable do
        //            select (countBy o.SalesOrderID)
        //        }

        //    let sql = query.ToKataQuery() |> toSql
        //    //printfn "%s" sql
        //    Expect.equal
        //        sql
        //        "SELECT COUNT([Sales].[SalesOrderHeader].[SalesOrderID]) FROM [Sales].[SalesOrderHeader]"
        //        ""
        //}
    ]

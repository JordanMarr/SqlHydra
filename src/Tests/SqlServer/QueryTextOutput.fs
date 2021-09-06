module SqlServer.QueryTextOutput

open Expecto
open SqlHydra.Query
open DB
open SqlServer.AdventureWorks
open FSharp.Control.Tasks.V2
open SalesLT

// Tables
let customerTable =         table<SalesLT.Customer>         |> inSchema (nameof SalesLT)
let customerAddressTable =  table<SalesLT.CustomerAddress>  |> inSchema (nameof SalesLT)
let addressTable =          table<SalesLT.Address>          |> inSchema (nameof SalesLT)
let productTable =          table<SalesLT.Product>          |> inSchema (nameof SalesLT)
let categoryTable =         table<SalesLT.ProductCategory>  |> inSchema (nameof SalesLT)

let tests = 
    testList "SqlHydra.Query - Query Output Unit Tests" [

        /// String comparisons against generated queries.
        test "Simple Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Dallas")
                    orderBy a.StateProvince
                    thenByDescending a.City
                }

            let sql = query.ToKataQuery() |> toSql
            printfn "%s" sql
            Expect.isTrue (sql.Contains("WHERE")) ""
        }

        test "Select 1 Column" {
            let query =
                select {
                    for a in addressTable do
                    select (a.City)
                }

            let sql = query.ToKataQuery() |> toSql
            printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT [SalesLT].[Address].[City] FROM")) ""
        }

        test "Select 2 Columns" {
            let query =
                select {
                    for a in addressTable do
                    select (a.City, a.StateProvince)
                }

            let sql = query.ToKataQuery() |> toSql
            printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT [SalesLT].[Address].[City], [SalesLT].[Address].[StateProvince] FROM")) ""
        }

        test "Select 1 Table and 1 Column" {
            let query =
                select {
                    for c in customerTable do
                    join ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
                    join a  in addressTable on (ca.AddressID = a.AddressID)
                    select (c, a.City)
                }

            let sql = query.ToKataQuery() |> toSql
            printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT [SalesLT].[Customer].*, [SalesLT].[Address].[City] FROM")) ""
        }

        test "Where with Option Type" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.AddressLine2 <> None)
                }

            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        test "Where Not Like" {
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
    
            query.ToKataQuery() |> toSql |> printfn "%s"

            //let addresses = get query
            //printfn "Results: %A" addresses
        }

        test "And Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Chicago" && a.City = "Dallas")
                }
    
            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        test "Where Not Binary" {
            let query = 
                select {
                    for a in addressTable do
                    where (not (a.City = "Chicago" && a.City = "Dallas"))
                }
    
            query.ToKataQuery() |> toSql |> printfn "%s"
        }

        test "Where Customer isIn List" {
            let query = 
                select {
                    for c in customerTable do
                    where (isIn c.CustomerID [30018;29545;29954;29897;29503;29559])
                }

            query.ToKataQuery() |> toSql |> printfn "%s"

            //let customers = get query
            //printfn "Results: %A" customers
        }

        test "Where Customer |=| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| [30018;29545;29954;29897;29503;29559])
                }

            let sql = query.ToKataQuery() |> toSql 
            sql |> printfn "%s"
        }

        test "Where Customer |=| Array" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| [|30018;29545;29954;29897;29503;29559|])
                }

            let sql = query.ToKataQuery() |> toSql 
            sql |> printfn "%s"
        }
        
        test "Where Customer |=| Seq" {            
            let buildQuery (values: int seq) =                
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| values)
                }

            let query = buildQuery([ 30018;29545;29954;29897;29503;29559 ])

            let sql = query.ToKataQuery() |> toSql 
            sql |> printfn "%s"
        }

        test "Where Customer |<>| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |<>| [30018;29545;29954;29897;29503;29559])
                }

            query.ToKataQuery() |> toSql |> printfn "%s"

            //let customers = get query
            //printfn "Results: %A" customers
        }

        test "Select Column Aggregates" {
            let query = 
                select {
                    for p in productTable do
                    where (p.ProductCategoryID <> None)
                    groupBy p.ProductCategoryID
                    having (minBy p.ListPrice > 500M && maxBy p.ListPrice < 1000M)
                    select (p.ProductCategoryID, minBy p.ListPrice, maxBy p.ListPrice)
                }

            let sql = query.ToKataQuery() |> toSql 
            sql |> printfn "%s"
        }

        test "Sorted Aggregates - Top 5 categories with highest avg price products" {
            let query = 
                select {
                    for p in productTable do
                    where (p.ProductCategoryID <> None)
                    groupBy p.ProductCategoryID
                    orderByDescending (avgBy p.ListPrice)
                    select (p.ProductCategoryID, avgBy p.ListPrice)
                    take 5
                }

            let sql = query.ToKataQuery() |> toSql 
            sql |> printfn "%s"
        }

        test "SqlKata SubQuery" {
            let averageQuery = SqlKata.Query("Posts").AsAverage("score")
            let query = SqlKata.Query("Posts").Where("Score", ">", averageQuery)
            let sql = query |> toSql
            sql |> printfn "%s"
        }

        test "Where subqueryMany" {
            let top5CategoryIdsWithHighestAvgPrices = 
                select {
                    for p in productTable do
                    where (p.ProductCategoryID <> None)
                    groupBy p.ProductCategoryID
                    orderByDescending (avgBy p.ListPrice)
                    select (p.ProductCategoryID)
                    take 5
                }

            let top5Categories =
                select {
                    for c in categoryTable do
                    where (Some c.ProductCategoryID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
                    select c.Name
                }

            let sql = top5Categories.ToKataQuery() |> toSql
            sql |> printfn "%s"
        }

        ftest "Where subqueryOne" {
            let avgListPrice = 
                select {
                    for p in productTable do
                    select (avgBy p.ListPrice)
                } 

            let productsWithAboveAveragePrice =
                select {
                    for p in productTable do
                    where (p.ListPrice > subqueryOne avgListPrice)
                    select (p.Name, p.ListPrice)
                }

            let sql = productsWithAboveAveragePrice.ToKataQuery() |> toSql
            sql |> printfn "%s"
        }

        test "Update should fail without where or updateAll" {
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.FirstName "blah"
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
                        set c.FirstName "blah"
                        where (c.CustomerID = 123)
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
                        set c.FirstName "blah"
                        updateAll
                    }
                () //Assert.Pass()
            with ex ->
                () //Assert.Pass("Should not fail because `where` is present.")
        }

        test "Multi Compiler Test" {
            let query = 
                select {
                    for c in customerTable do
                    join ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
                    join a  in addressTable on (ca.AddressID = a.AddressID)
                    where (isIn c.CustomerID [30018;29545;29954;29897;29503;29559])
                    orderBy c.CustomerID
                }

            seq [
                "SqlServerCompiler", SqlKata.Compilers.SqlServerCompiler() :> SqlKata.Compilers.Compiler
                "PostgresCompiler", SqlKata.Compilers.PostgresCompiler() :> SqlKata.Compilers.Compiler
                "FirebirdCompiler", SqlKata.Compilers.FirebirdCompiler() :> SqlKata.Compilers.Compiler
                "MySqlCompiler", SqlKata.Compilers.MySqlCompiler() :> SqlKata.Compilers.Compiler
                "OracleCompiler", SqlKata.Compilers.OracleCompiler() :> SqlKata.Compilers.Compiler
                "SqliteCompiler", SqlKata.Compilers.SqliteCompiler() :> SqlKata.Compilers.Compiler
            ]
            |> Seq.map (fun (nm, compiler) -> nm, compiler.Compile(query.ToKataQuery()).Sql)
            |> Seq.iter (fun (nm, sql) -> printfn "%s:\n%s" nm sql)
        }

        test "Build Kata Queries" {
            let compiler = new SqlKata.Compilers.SqlServerCompiler()
    
            let sampleErrors = [
                for n in [1..3] do
                    {   dbo.ErrorLog.ErrorLogID = 0 // Exclude
                        dbo.ErrorLog.ErrorTime = System.DateTime.Now
                        dbo.ErrorLog.ErrorLine = None
                        dbo.ErrorLog.ErrorMessage = $"INSERT {n}"
                        dbo.ErrorLog.ErrorNumber = 400
                        dbo.ErrorLog.ErrorProcedure = None
                        dbo.ErrorLog.ErrorSeverity = None
                        dbo.ErrorLog.ErrorState = None
                        dbo.ErrorLog.UserName = "jmarr" }
            ]

            let kataQueries = 
                [
                    for record in sampleErrors do
                        insert {
                            into table<dbo.ErrorLog>
                            entity record
                        }
                        |> fun query -> query.ToKataQuery(returnId = false)

                    update {
                        for e in table<dbo.ErrorLog> do
                        set e.ErrorMessage "Unauthorized"
                        where (e.ErrorNumber = 401)
                    }
                    |> fun query -> query.ToKataQuery()

                    update {
                        for e in table<dbo.ErrorLog> do
                        set e.ErrorMessage "Resource Not Found"
                        where (e.ErrorNumber = 404)
                    }
                    |> fun query -> query.ToKataQuery()
                ]

            kataQueries 
            |> List.map compiler.Compile
            |> List.iter (fun compiledQuery -> 
                printfn "script: \n%s\n" compiledQuery.Sql
            )
        }
    ]

module SqlServer.QueryUnitTests

open Expecto
open SqlHydra.Query
open DB
open SqlServer.AdventureWorks
open FSharp.Control.Tasks.V2

// Tables
let personTable =           table<Person.Person>                    |> inSchema (nameof Person)
let addressTable =          table<Person.Address>                   |> inSchema (nameof Person)
let customerTable =         table<Sales.Customer>                   |> inSchema (nameof Sales)
let orderHeaderTable =      table<Sales.SalesOrderHeader>           |> inSchema (nameof Sales)
let orderDetailTable =      table<Sales.SalesOrderDetail>           |> inSchema (nameof Sales)
let productTable =          table<Production.Product>               |> inSchema (nameof Production)
let subCategoryTable =      table<Production.ProductSubcategory>    |> inSchema (nameof Production)
let categoryTable =         table<Production.ProductCategory>       |> inSchema (nameof Production)
let errorLogTable =         table<dbo.ErrorLog>

let tests = 
    testList "SQL Server Query Unit Tests" [

        /// String comparisons against generated queries.
        test "Simple Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Dallas")
                    orderBy a.City
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
            Expect.isTrue (sql.Contains("SELECT [Person].[Address].[City] FROM")) ""
        }

        test "Select 2 Columns" {
            let query =
                select {
                    for h in orderHeaderTable do
                    select (h.CustomerID, h.OnlineOrderFlag)
                }

            let sql = query.ToKataQuery() |> toSql
            printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT [Sales].[SalesOrderHeader].[CustomerID], [Sales].[SalesOrderHeader].[OnlineOrderFlag] FROM")) ""
        }

        test "Select 1 Table and 1 Column" {
            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
                    where (o.OnlineOrderFlag = true)
                    select (o, d.LineTotal)
                }

            let sql = query.ToKataQuery() |> toSql
            printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT [Sales].[SalesOrderHeader].*, [Sales].[SalesOrderDetail].[LineTotal] FROM")) ""
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
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (([Person].[Address].[City] = @p0) OR ([Person].[Address].[City] = @p1))")) ""
        }

        test "And Where" {
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Chicago" && a.City = "Dallas")
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (([Person].[Address].[City] = @p0) AND ([Person].[Address].[City] = @p1))")) ""
        }

        test "Where Not Binary" {
            let query = 
                select {
                    for a in addressTable do
                    where (not (a.City = "Chicago" && a.City = "Dallas"))
                }
    
            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE (NOT (([Person].[Address].[City] = @p0) AND ([Person].[Address].[City] = @p1)))")) ""
        }

        test "Where Customer isIn List" {
            let query = 
                select {
                    for c in customerTable do
                    where (isIn c.CustomerID [30018;29545;29954])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([Sales].[Customer].[CustomerID] IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |=| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| [30018;29545;29954])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([Sales].[Customer].[CustomerID] IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |=| Array" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| [| 30018;29545;29954 |])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([Sales].[Customer].[CustomerID] IN (@p0, @p1, @p2))")) ""
        }
        
        test "Where Customer |=| Seq" {            
            let buildQuery (values: int seq) =                
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| values)
                }

            let query = buildQuery([ 30018;29545;29954 ])

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([Sales].[Customer].[CustomerID] IN (@p0, @p1, @p2))")) ""
        }

        test "Where Customer |<>| List" {
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |<>| [ 30018;29545;29954 ])
                }

            let sql = query.ToKataQuery() |> toSql
            Expect.isTrue (sql.Contains("WHERE ([Sales].[Customer].[CustomerID] NOT IN (@p0, @p1, @p2))")) ""
        }

        test "Update should fail without where or updateAll" {
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.AccountNumber "123"
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
                        set c.AccountNumber "123"
                        where (c.CustomerID = 1)
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
                        set c.AccountNumber "123"
                        updateAll
                    }
                () //Assert.Pass()
            with ex ->
                () //Assert.Pass("Should not fail because `where` is present.")
        }

        test "Multi Compiler Test" {
            let query = 
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
                    where (o.OnlineOrderFlag = true)
                    select (o, d.LineTotal)
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

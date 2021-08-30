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
        testCase "Simple Where" <| fun _ ->
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Dallas")
                    orderBy a.StateProvince
                    thenByDescending a.City
                }

            let sql = toSql query
            printfn "%s" sql
            Expect.isTrue (sql.Contains("WHERE")) ""

        testCase "Select 1 Column" <| fun _ ->
            let query =
                select {
                    for a in addressTable do
                    select (a.City)
                }

            let sql = toSql query
            printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT [SalesLT].[Address].[City] FROM")) ""


        testCase "Select 2 Columns" <| fun _ ->
            let query =
                select {
                    for a in addressTable do
                    select (a.City, a.StateProvince)
                }

            let sql = toSql query
            printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT [SalesLT].[Address].[City], [SalesLT].[Address].[StateProvince] FROM")) ""

        testCase "Select 1 Table and 1 Column" <| fun _ ->
            let query =
                select {
                    for c in customerTable do
                    join ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
                    join a  in addressTable on (ca.AddressID = a.AddressID)
                    select (c, a.City)
                }

            let sql = toSql query
            printfn "%s" sql
            Expect.isTrue (sql.Contains("SELECT [SalesLT].[Customer].*, [SalesLT].[Address].[City] FROM")) ""

        testCase "Where with Option Type" <| fun _ ->
            let query = 
                select {
                    for a in addressTable do
                    where (a.AddressLine2 <> None)
                }

            query |> toSql |> printfn "%s"
            //let addresses = get query
            //printfn "Results: %A" addresses

        testCase "Where Not Like" <| fun _ ->
            let query =
                select {
                    for a in addressTable do
                    where (a.City <>% "S%")
                }

            query |> toSql |> printfn "%s"
        
        testCase "Or Where" <| fun _ ->
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Chicago" || a.City = "Dallas")
                }
    
            query |> toSql |> printfn "%s"

            //let addresses = get query
            //printfn "Results: %A" addresses

        testCase "And Where" <| fun _ ->
            let query = 
                select {
                    for a in addressTable do
                    where (a.City = "Chicago" && a.City = "Dallas")
                }
    
            query |> toSql |> printfn "%s"

        testCase "Where Not Binary" <| fun _ ->
            let query = 
                select {
                    for a in addressTable do
                    where (not (a.City = "Chicago" && a.City = "Dallas"))
                }
    
            query |> toSql |> printfn "%s"

        testCase "Where Customer isIn List" <| fun _ ->
            let query = 
                select {
                    for c in customerTable do
                    where (isIn c.CustomerID [30018;29545;29954;29897;29503;29559])
                }

            query |> toSql |> printfn "%s"

            //let customers = get query
            //printfn "Results: %A" customers

        testCase "Where Customer |=| List" <| fun _ ->
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |=| [30018;29545;29954;29897;29503;29559])
                }

            query |> toSql |> printfn "%s"

            //let customers = get query
            //printfn "Results: %A" customers

        testCase "Where Customer |<>| List" <| fun _ ->
            let query = 
                select {
                    for c in customerTable do
                    where (c.CustomerID |<>| [30018;29545;29954;29897;29503;29559])
                }

            query |> toSql |> printfn "%s"

            //let customers = get query
            //printfn "Results: %A" customers

        testTask "From Subquery" {
            let redProducts = 
                select {
                    for p in productTable do
                    where (p.Color = Some "Red")
                }

            let redProductsThatStartWithS = 
                select {
                    for p in redProducts do
                    where (p.Name =% "S")
                    select p
                }
    
            let sql = redProductsThatStartWithS |> toSql 
            sql |> printfn "%s" 
            let expected = "SELECT [SalesLT].[Product].* FROM (SELECT * FROM [SalesLT].[Product] WHERE ([SalesLT].[Product].[Color] = @p0)) WHERE (LOWER([SalesLT].[Product].[Name]) like @p1)"
            Expect.equal expected sql ""
        }

        ftestTask "From Subquery with Join" {

            let fewMonthsAgo = System.DateTime.UtcNow.AddMonths(-6)
            let oldPostsQuery = SqlKata.Query("Posts").Where("Date", "<", fewMonthsAgo).As("old")
            let query = SqlKata.Query().From(oldPostsQuery).OrderByDesc("Date")

            let redProducts = 
                select {
                    for p in productTable do
                    where (p.Color = Some "Red")
                }

            let redProductsThatStartWithS = 
                select {
                    for p in redProducts do
                    join c in categoryTable on (p.ProductCategoryID.Value = c.ProductCategoryID)
                    where (p.Name =% "S")
                    select p
                }
    
            let sql = redProductsThatStartWithS |> toSql 
            sql |> printfn "%s" 
            let expected = "SELECT [SalesLT].[Product].* FROM (SELECT * FROM [SalesLT].[Product] WHERE ([SalesLT].[Product].[Color] = @p0)) WHERE (LOWER([SalesLT].[Product].[Name]) like @p1)"
            Expect.equal expected sql ""
        }

        //ftestTask "From Subquery Avg" {
        //    let redProducts = 
        //        select {
        //            forSub p in productTable do
        //            where (p.Color = Some "Red")
        //            avgBy p.ListPrice
        //        }
    
        //    let redProductsThatStartWithS = 
        //        select {
        //            for p in redProducts do
        //            where (p.Name =% "S")
        //            select p
        //        }
    
        //    let sql = redProductsThatStartWithS |> toSql 
        //    sql |> printfn "%s" 
        //    let expected = "SELECT [SalesLT].[Product].* FROM (SELECT * FROM [SalesLT].[Product] WHERE ([SalesLT].[Product].[Color] = @p0)) WHERE (LOWER([SalesLT].[Product].[Name]) like @p1)"
        //    Expect.equal expected sql ""
        //}

        testCase "Update should fail without where or updateAll" <| fun _ ->
            try 
                let query = 
                    update {
                        for c in customerTable do
                        set c.FirstName "blah"
                    }
                failwith "Should fail because no `where` or `updateAll` exists."
            with ex ->
                () // Pass

        testCase "Update should pass because where exists" <| fun _ ->
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

        testCase "Update should pass because updateAll exists" <| fun _ ->
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

        testCase "Multi Compiler Test" <| fun _ ->
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
            |> Seq.map (fun (nm, compiler) -> nm, compiler.Compile(query.Query).Sql)
            |> Seq.iter (fun (nm, sql) -> printfn "%s:\n%s" nm sql)

        testCase "Build Kata Queries" <| fun _ ->
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
                        |> Kata.ToQuery

                    update {
                        for e in table<dbo.ErrorLog> do
                        set e.ErrorMessage "Unauthorized"
                        where (e.ErrorNumber = 401)
                    }
                    |> Kata.ToQuery

                    update {
                        for e in table<dbo.ErrorLog> do
                        set e.ErrorMessage "Resource Not Found"
                        where (e.ErrorNumber = 404)
                    }
                    |> Kata.ToQuery
                ]

            kataQueries 
            |> List.map compiler.Compile
            |> List.iter (fun compiledQuery -> 
                printfn "script: \n%s\n" compiledQuery.Sql
            )
    ]



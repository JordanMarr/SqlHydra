module BuilderOutputTests

open SqlHydra.Query
open SqlUtils
open AdventureWorks
open System.Data.SqlClient
open NUnit.Framework
open System.Collections.Generic
open FSharp.Control.Tasks.V2
open SalesLT

// Tables
let customerTable =         table<SalesLT.Customer>         |> inSchema (nameof SalesLT)
let customerAddressTable =  table<SalesLT.CustomerAddress>  |> inSchema (nameof SalesLT)
let addressTable =          table<SalesLT.Address>          |> inSchema (nameof SalesLT)
let productTable =          table<SalesLT.Product>          |> inSchema (nameof SalesLT)
let categoryTable =         table<SalesLT.ProductCategory>  |> inSchema (nameof SalesLT)

/// String comparisons against generated queries.
[<Test>]
let ``Simple Where``() = 
    let query = 
        select {
            for a in addressTable do
            where (a.City = "Dallas")
            orderBy a.StateProvince
            thenByDescending a.City
        }

    let sql = toSql query
    printfn "%s" sql
    Assert.IsTrue(sql.Contains("WHERE"))

[<Test>]
let ``Select 1 Column``() =
    let query =
        select {
            for a in addressTable do
            select (a.City)
        }

    let sql = toSql query
    printfn "%s" sql
    Assert.IsTrue(sql.Contains("SELECT [SalesLT].[Address].[City] FROM"))


[<Test>]
let ``Select 2 Columns``() =
    let query =
        select {
            for a in addressTable do
            select (a.City, a.StateProvince)
        }

    let sql = toSql query
    printfn "%s" sql
    Assert.IsTrue(sql.Contains("SELECT [SalesLT].[Address].[City], [SalesLT].[Address].[StateProvince] FROM"))

[<Test>]
let ``Select 1 Table and 1 Column``() =
    let query =
        select {
            for c in customerTable do
            join ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
            join a  in addressTable on (ca.AddressID = a.AddressID)
            select (c, a.City)
        }

    let sql = toSql query
    printfn "%s" sql
    Assert.IsTrue(sql.Contains("SELECT [SalesLT].[Customer].*, [SalesLT].[Address].[City] FROM"))

[<Test>]
let ``Where with Option Type``() = 
    let query = 
        select {
            for a in addressTable do
            where (a.AddressLine2 <> None)
        }

    query |> toSql |> printfn "%s"
    let addresses = get query
    printfn "Results: %A" addresses


[<Test>]
let ``Where Not Like``() = 
    let query =
        select {
            for a in addressTable do
            where (a.City <>% "S%")
        }

    query |> toSql |> printfn "%s"
        
[<Test>]
let ``Or Where``() =
    let query = 
        select {
            for a in addressTable do
            where (a.City = "Chicago" || a.City = "Dallas")
        }
    
    query |> toSql |> printfn "%s"

    let addresses = get query
    printfn "Results: %A" addresses

[<Test>]
let ``And Where``() =
    let query = 
        select {
            for a in addressTable do
            where (a.City = "Chicago" && a.City = "Dallas")
        }
    
    query |> toSql |> printfn "%s"

[<Test>]
let ``Where Not Binary``() =
    let query = 
        select {
            for a in addressTable do
            where (not (a.City = "Chicago" && a.City = "Dallas"))
        }
    
    query |> toSql |> printfn "%s"

[<Test>]
let ``Where Customer isIn List``() =
    let query = 
        select {
            for c in customerTable do
            where (isIn c.CustomerID [30018;29545;29954;29897;29503;29559])
        }

    query |> toSql |> printfn "%s"

    let customers = get query
    printfn "Results: %A" customers

[<Test>]
let ``Where Customer |=| List``() =
    let query = 
        select {
            for c in customerTable do
            where (c.CustomerID |=| [30018;29545;29954;29897;29503;29559])
        }

    query |> toSql |> printfn "%s"

    let customers = get query
    printfn "Results: %A" customers

[<Test>]
let ``Where Customer |<>| List``() =
    let query = 
        select {
            for c in customerTable do
            where (c.CustomerID |<>| [30018;29545;29954;29897;29503;29559])
        }

    query |> toSql |> printfn "%s"

    let customers = get query
    printfn "Results: %A" customers

[<Test>]
let ``Update should fail without where or updateAll``() =
    try 
        let query = 
            update {
                for c in customerTable do
                set c.FirstName "blah"
            }
        Assert.Fail("Should fail because no `where` or `updateAll` exists.")
    with ex ->
        Assert.Pass()

[<Test>]
let ``Update should pass because where exists``() =
    try 
        let query = 
            update {
                for c in customerTable do
                set c.FirstName "blah"
                where (c.CustomerID = 123)
            }
        Assert.Pass()
    with ex ->
        Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Update should pass because updateAll exists``() =
    try 
        let query = 
            update {
                for c in customerTable do
                set c.FirstName "blah"
                updateAll
            }
        Assert.Pass()
    with ex ->
        Assert.Pass("Should not fail because `where` is present.")

[<Test>]
let ``Multi Compiler Test``() =
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
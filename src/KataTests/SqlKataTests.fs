module SqlKataTests

open NUnit.Framework
open AdventureWorks
open SqlKata
open SqlKata.Execution
open System.Data.SqlClient
open System.Collections.Generic

[<Test>]
let executeQuery () =
    use connection = new SqlConnection(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;")
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    use db = new QueryFactory(connection, compiler)
    
    let results = 
        db.Query("SalesLT.Customer")
            .WhereIn("SalesLT.Customer.CustomerID", [30018;29545;29954;29897;29503;29559])
            .LeftJoin("SalesLT.CustomerAddress", "SalesLT.CustomerAddress.CustomerID", "SalesLT.Customer.CustomerID")
            .LeftJoin("SalesLT.Address", "SalesLT.Address.AddressID", "SalesLT.CustomerAddress.AddressID")
            .Select(
                "SalesLT.Customer.{FirstName, LastName}"
                //,"SalesLT.Address.{City, StateProvince}"
            )
            .OrderBy("LastName", "FirstName")
            .Get<AdventureWorks.SalesLT.Customer>()
            |> Seq.toArray
    
    printfn "Result: %A" results
    Assert.Pass()

[<Test>]
let executeDynamicQuery () =
    use connection = new SqlConnection(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;")
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    use db = new QueryFactory(connection, compiler)
    
    let results = 
        db.Query("SalesLT.Customer")
            .WhereIn("SalesLT.Customer.CustomerID", [30018;29545;29954;29897;29503;29559])
            .LeftJoin("SalesLT.CustomerAddress", "SalesLT.CustomerAddress.CustomerID", "SalesLT.Customer.CustomerID")
            .LeftJoin("SalesLT.Address", "SalesLT.Address.AddressID", "SalesLT.CustomerAddress.AddressID")
            .Select(
                "SalesLT.Customer.{FirstName, LastName}",
                "SalesLT.Address.{City, StateProvince}"
            )
            .OrderBy("LastName", "FirstName")
            .Get()
            |> Seq.cast<IDictionary<string,obj>>
            |> Seq.toArray
    
    //let firstName = results.[0].["FirstName"]

    printfn "Result: %A" results
    Assert.Pass()


[<Test>]
let generateSql() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let query = 
        Query("SalesLT.Customer")
            .WhereIn("SalesLT.Customer.CustomerID", [30018;29545;29954;29897;29503;29559])
            .LeftJoin("SalesLT.CustomerAddress", "SalesLT.CustomerAddress.CustomerID", "SalesLT.Customer.CustomerID")
            .LeftJoin("SalesLT.Address", "SalesLT.Address.AddressID", "SalesLT.CustomerAddress.AddressID")
            .Select(
                "Customer.{FirstName, LastName}", 
                "Address.{City, StateProvince}"
            )
            .OrderBy("LastName", "FirstName")

    let queryResult = compiler.Compile(query)
    printfn "Query: %s "queryResult.Sql

[<Test>]
let updateTest() =
    let compiler = SqlKata.Compilers.SqlServerCompiler()

    let customerAddress = 
        {
            SalesLT.CustomerAddress.CustomerID = 1
            SalesLT.CustomerAddress.AddressID = 1
            SalesLT.CustomerAddress.AddressType = ""
            SalesLT.CustomerAddress.rowguid = System.Guid.NewGuid()
            SalesLT.CustomerAddress.ModifiedDate = System.DateTime.Today
        }

    let query = Query("SalesLT.CustomerAddress").AsUpdate(customerAddress)
    let queryResult = compiler.Compile(query)
    printfn "Query: %s "queryResult.Sql
    printfn "Params: %A" queryResult.NamedBindings

[<Test>]
let insertTest() =
    let compiler = SqlKata.Compilers.SqlServerCompiler()

    let customerAddress = 
        {
            SalesLT.CustomerAddress.CustomerID = 1
            SalesLT.CustomerAddress.AddressID = 1
            SalesLT.CustomerAddress.AddressType = ""
            SalesLT.CustomerAddress.rowguid = System.Guid.NewGuid()
            SalesLT.CustomerAddress.ModifiedDate = System.DateTime.Today
        }

    let query = Query("SalesLT.CustomerAddress").AsInsert(customerAddress)
    let queryResult = compiler.Compile(query)
    printfn "Query: %s "queryResult.Sql
    printfn "Params: %A" queryResult.NamedBindings
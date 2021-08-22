module SqlServerTests

open System
open NUnit.Framework
open SqlHydra
open SqlHydra.SqlServer
open SqlHydra.Domain

//let connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
let connectionString = @"Server=localhost,1433;Database=master;User=sa;Password=password123;"

let cfg = 
    {
        // Docker "mssql":
        ConnectionString = connectionString
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        Readers = Some { ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader" } 
    }

[<Test>]
let ``Print Schema``() =
    let schema = SqlServerSchemaProvider.getSchema cfg
    printfn "Schema: %A" schema

let getCode cfg = 
    SqlServerSchemaProvider.getSchema cfg
    |> SchemaGenerator.generateModule cfg SqlHydra.SqlServer.Program.app
    |> SchemaGenerator.toFormattedCode cfg SqlHydra.SqlServer.Program.app

let inCode (str: string) cfg = 
    let code = getCode cfg
    Assert.IsTrue(code.Contains str)

let notInCode (str: string) cfg = 
    let code = getCode cfg
    Assert.IsFalse(code.Contains str)

[<Test>]
let ``Print Code``() = 
    getCode cfg |> printfn "%s"
    
[<Test>]
let ``Code Should Have Reader``() =
    cfg |> inCode "type HydraReader"
    
[<Test>]
let ``Code Should Not Have Reader``() = 
    { cfg with Readers = None } |> notInCode "type HydraReader"

[<Test>]
let ``Code Should Have CLIMutable``() = 
    { cfg with IsCLIMutable = true } |> inCode "[<CLIMutable>]"

[<Test>]
let ``Code Should Not Have CLIMutable``() = 
    { cfg with IsCLIMutable = false } |> notInCode "[<CLIMutable>]"

[<Test>]
let ``Code Should Have Namespace``() =
    cfg |> inCode "namespace TestNS"
module SqliteTests

open NUnit.Framework
open SqlHydra.Sqlite
open SqlHydra
open SqlHydra.Schema
open SqlHydra.SchemaGenerator

let cfg = 
    {
        ConnectionString = @"Data Source=C:\_github\SqlHydra\src\Tests\TestData\AdventureWorksLT.db"
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        Readers = Some { ReadersConfig.ReaderType = "System.Data.IDataReader" } 
    }

[<Test>]
let ``Print Schema``() =
    let schema = SqliteSchemaProvider.getSchema cfg
    printfn "Schema: %A" schema

let getCode cfg = 
    SqliteSchemaProvider.getSchema cfg
    |> SchemaGenerator.generateModule cfg
    |> SchemaGenerator.toFormattedCode cfg "SqlHydra.Sqlite"

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
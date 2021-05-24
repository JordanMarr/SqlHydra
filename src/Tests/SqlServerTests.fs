module SqlServerTests

open NUnit.Framework
open SqlHydra.SqlServer

[<Test>]
let getSchema() =
    let connStr = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
    let schema = SqlServerSchemaProvider.getSchema connStr
    printfn "Schema: %A" schema
    Assert.Pass()



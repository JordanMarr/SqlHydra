module SqliteTests

open NUnit.Framework
open SqlHydra.Sqlite

[<Test>]
let getSchema() =
    let connStr = @"Data Source=C:\_github\SqlHydra\src\Tests\TestData\AdventureWorksLT.db"
    let schema = SqliteSchemaProvider.getSchema connStr
    printfn "Schema: %A" schema
    Assert.Pass()


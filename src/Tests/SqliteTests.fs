module SqliteTests

open NUnit.Framework
open SqlHydra.Sqlite
open SqlHydra.Schema

[<Test>]
let getSchema() =
    let cfg = 
        {
            ConnectionString = @"Data Source=C:\_github\SqlHydra\src\Tests\TestData\AdventureWorksLT.db"
            OutputFile = ""
            Namespace = "TestNS"
            IsCLIMutable = true
            Readers = 
                {
                    ReadersConfig.IsEnabled = true
                    ReadersConfig.ReaderType = "System.Data.IDataReader"
                } 
        }
    let schema = SqliteSchemaProvider.getSchema cfg
    printfn "Schema: %A" schema
    Assert.Pass()


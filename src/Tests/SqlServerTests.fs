module SqlServerTests

open NUnit.Framework
open SqlHydra.SqlServer
open SqlHydra.Schema

[<Test>]
let getSchema() =
    
    let cfg = 
        {
            ConnectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
            OutputFile = ""
            Namespace = "TestNS"
            IsCLIMutable = true
            Readers = 
                {
                    ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader"
                } 
                |> Some
        }
    let schema = SqlServerSchemaProvider.getSchema cfg
    printfn "Schema: %A" schema
    Assert.Pass()



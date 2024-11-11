module UnitTests.SchemaTemplateTests

open NUnit.Framework
open SqlHydra.Domain
open SqlHydra

[<Test>]
let ``Schema Template Test - Npgsql`` () = 
    let cfg = 
        { Npgsql.Generation.cfg with 
            TableDeclarations = true
        }
    let info = Npgsql.AppInfo.info
    let schema = Npgsql.NpgsqlSchemaProvider.getSchema cfg
    let output = SchemaTemplate.generate cfg info schema "1.0.0"
    printfn $"Output:\n{output}"

[<Test>]
let ``Schema Template Test - SqlServer`` () = 
    let cfg = 
        { SqlServer.Generation.cfg with
            TableDeclarations = true
            Filters = 
                { 
                    Includes = [ "*" ]
                    Excludes = [ "*/v*" ]
                    Restrictions = Map.empty                
                }
        }
    let info = SqlServer.AppInfo.info
    let schema = SqlServer.SqlServerSchemaProvider.getSchema cfg
    let output = SchemaTemplate.generate cfg info schema "1.0.0"
    printfn $"Output:\n{output}"
    // Write output to sqlserver.fs
    System.IO.File.WriteAllText("_sqlserver.fs", output)

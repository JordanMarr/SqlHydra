module UnitTests.SchemaTemplateTests

open NUnit.Framework
open SqlHydra.Domain
open SqlHydra

[<Test>]
let ``Schema Template Test`` () = 
    let cfg = 
        { Npgsql.Generation.cfg with 
            TableDeclarations = true
            //IsMutableProperties = true 
        }
    let info = SqlHydra.Npgsql.AppInfo.info
    let schema = Npgsql.NpgsqlSchemaProvider.getSchema cfg
    let output = SchemaTemplate.generate cfg info schema "1.0.0"
    printfn $"Output:\n{output}"
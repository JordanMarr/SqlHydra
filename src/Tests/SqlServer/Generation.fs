module SqlServer.Generation

open Expecto
open SqlHydra
open SqlHydra.SqlServer
open SqlHydra.Domain

let cfg = 
    {
        ConnectionString = DB.connectionString
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        Readers = Some { ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader" } 
        Filters = Filters.Empty
    }

[<Tests>]
let tests = 
    categoryList "SqlServer" "Generation Integration Tests" [

        test "Print Schema" {
            let schema = SqlServerSchemaProvider.getSchema cfg
            printfn "Schema: %A" schema
        }

        let lazySchema = lazy SqlServerSchemaProvider.getSchema cfg

        let getCode cfg = 
            lazySchema.Value
            |> SchemaGenerator.generateModule cfg SqlHydra.SqlServer.Program.app
            |> SchemaGenerator.toFormattedCode cfg SqlHydra.SqlServer.Program.app

        let inCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isTrue (code.Contains str) ""

        let notInCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isFalse (code.Contains str) ""

        test "Print Code"  {
            getCode cfg |> printfn "%s"
        }
    
        test "Code Should Have Reader"  {
            cfg |> inCode "type HydraReader"
        }
    
        test "Code Should Not Have Reader"  {
            { cfg with Readers = None } |> notInCode "type HydraReader"
        }

        test "Code Should Have CLIMutable"  {
            { cfg with IsCLIMutable = true } |> inCode "[<CLIMutable>]"
        }

        test "Code Should Not Have CLIMutable"  {
            { cfg with IsCLIMutable = false } |> notInCode "[<CLIMutable>]"
        }

        test "Code Should Have Namespace" {
            cfg |> inCode "namespace TestNS"
        }

    ]
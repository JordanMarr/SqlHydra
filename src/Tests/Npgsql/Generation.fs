module Npgsql.Generation

open Expecto
open SqlHydra.Npgsql
open SqlHydra
open SqlHydra.Domain
open SqlHydra.SchemaGenerator

let cfg = 
    {
        // Docker "mssql":
        ConnectionString = DB.connectionString
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        Readers = Some { ReadersConfig.ReaderType = Program.app.DefaultReaderType } 
        Filters = Filters.Empty
    }

[<Tests>]
let tests = 
    categoryList "Npgsql" "Generation Integration Tests" [

        test "Print Schema" {
            let schema = NpgsqlSchemaProvider.getSchema cfg
            printfn "Schema: %A" schema
        }

        let lazySchema = lazy NpgsqlSchemaProvider.getSchema cfg

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

        test "Print Code" {
            getCode cfg |> printfn "%s"
        }
    
        test "Code Should Have Reader" {
            cfg |> inCode "type HydraReader"
        }
    
        test "Code Should Not Have Reader" {
            { cfg with Readers = None } |> notInCode "type HydraReader"
        }

        test "Code Should Have CLIMutable" {
            { cfg with IsCLIMutable = true } |> inCode "[<CLIMutable>]"
        }

        test "Code Should Not Have CLIMutable" {
            { cfg with IsCLIMutable = false } |> notInCode "[<CLIMutable>]"
        }

        test "Code Should Have Namespace" {
            cfg |> inCode "namespace TestNS"
        }

        test "Should have Tables and PKs" {
            let schema = lazySchema.Value
            
            let allColumns = 
                schema.Tables 
                |> List.collect (fun t -> t.Columns)

            let pks = allColumns |> List.filter (fun c -> c.IsPK)
            
            Expect.equal schema.Tables.Length 68 ""
            Expect.isTrue (pks.Length > schema.Tables.Length) "Expected at least one pk per table"
            Expect.isTrue (pks.Length < allColumns.Length) "Every column should not be a PK"
        }
        
        test "Code Should Have ProviderDbTypeAttribute With Json" {
            cfg |> inCode "[<SqlHydra.ProviderDbTypeAttribute(\"Json\")>]"
        }
        
        test "Code Should Have ProviderDbTypeAttribute With Jsonb" {
            cfg |> inCode "[<SqlHydra.ProviderDbTypeAttribute(\"Jsonb\")>]"
        }
    ]
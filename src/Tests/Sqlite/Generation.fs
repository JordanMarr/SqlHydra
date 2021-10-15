module Sqlite.Generation

open Expecto
open SqlHydra.Sqlite
open SqlHydra
open SqlHydra.Domain
open SqlHydra.SchemaGenerator

let connectionString = 
    let assembly = System.Reflection.Assembly.GetExecutingAssembly().Location |> System.IO.FileInfo
    let thisDir = assembly.Directory.Parent.Parent.Parent.FullName
    let relativeDbPath = System.IO.Path.Combine(thisDir, "TestData", "AdventureWorksLT.db")
    $"Data Source={relativeDbPath}"

let cfg = 
    {
        ConnectionString = connectionString
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        Readers = Some { ReadersConfig.ReaderType = "System.Data.IDataReader" } 
        Filters = Filters.Empty
    }

[<Tests>]
let tests = 
    categoryList "Sqlite" "Generation Integration Tests" [

        let lazySchema = lazy SqliteSchemaProvider.getSchema cfg

        let getCode cfg = 
            lazySchema.Value
            |> SchemaGenerator.generateModule cfg SqlHydra.Sqlite.Program.app
            |> SchemaGenerator.toFormattedCode cfg SqlHydra.Sqlite.Program.app

        let inCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isTrue (code.Contains str) ""

        let notInCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isFalse (code.Contains str) ""

        ptest "Print Code" {
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

    ]
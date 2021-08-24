module SqliteTests

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
    }

let tests = 
    testList "SqlHydra.Sqlite Integration Tests" [

        //testCase "Print Schema" <| fun _ ->
        //    let schema = SqliteSchemaProvider.getSchema cfg
        //    printfn "Schema: %A" schema

        let getCode cfg = 
            SqliteSchemaProvider.getSchema cfg
            |> SchemaGenerator.generateModule cfg SqlHydra.Sqlite.Program.app
            |> SchemaGenerator.toFormattedCode cfg SqlHydra.Sqlite.Program.app

        let inCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isTrue (code.Contains str) ""

        let notInCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isFalse (code.Contains str) ""

        testCase "Print Code" <| fun _ ->
            getCode cfg |> printfn "%s"
    
        testCase "Code Should Have Reader" <| fun _ ->
            cfg |> inCode "type HydraReader"
    
        testCase "Code Should Not Have Reader" <| fun _ ->
            { cfg with Readers = None } |> notInCode "type HydraReader"

        testCase "Code Should Have CLIMutable" <| fun _ ->
            { cfg with IsCLIMutable = true } |> inCode "[<CLIMutable>]"

        testCase "Code Should Not Have CLIMutable" <| fun _ ->
            { cfg with IsCLIMutable = false } |> notInCode "[<CLIMutable>]"

        testCase "Code Should Have Namespace" <| fun _ ->
            cfg |> inCode "namespace TestNS"

    ]
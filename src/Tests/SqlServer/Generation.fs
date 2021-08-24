module SqlServer.Generation

open Expecto
open SqlHydra
open SqlHydra.SqlServer
open SqlHydra.Domain

let cfg = 
    {
        // Docker "mssql":
        ConnectionString = DB.connectionString
        OutputFile = ""
        Namespace = "TestNS"
        IsCLIMutable = true
        Readers = Some { ReadersConfig.ReaderType = "Microsoft.Data.SqlClient.SqlDataReader" } 
    }

let tests = 
    testList "SqlHydra.SqlServer Integration Tests" [

        testCase "Print Schema" <| fun _ ->
            let schema = SqlServerSchemaProvider.getSchema cfg
            printfn "Schema: %A" schema

        let getCode cfg = 
            SqlServerSchemaProvider.getSchema cfg
            |> SchemaGenerator.generateModule cfg SqlHydra.SqlServer.Program.app
            |> SchemaGenerator.toFormattedCode cfg SqlHydra.SqlServer.Program.app

        let inCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isTrue (code.Contains str) ""

        let notInCode (str: string) cfg = 
            let code = getCode cfg
            Expect.isFalse (code.Contains str) ""

        testCase "Print Code"  <| fun _ ->
            getCode cfg |> printfn "%s"
    
        testCase "Code Should Have Reader"  <| fun _ ->
            cfg |> inCode "type HydraReader"
    
        testCase "Code Should Not Have Reader"  <| fun _ ->
            { cfg with Readers = None } |> notInCode "type HydraReader"

        testCase "Code Should Have CLIMutable"  <| fun _ ->
            { cfg with IsCLIMutable = true } |> inCode "[<CLIMutable>]"

        testCase "Code Should Not Have CLIMutable"  <| fun _ ->
            { cfg with IsCLIMutable = false } |> notInCode "[<CLIMutable>]"

        testCase "Code Should Have Namespace" <| fun _ ->
            cfg |> inCode "namespace TestNS"

    ]
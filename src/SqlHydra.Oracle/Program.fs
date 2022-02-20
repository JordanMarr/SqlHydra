module SqlHydra.Oracle.Program

open SqlHydra.Oracle
open SqlHydra
open System.IO
open Domain

type private SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let app = 
    {
        AppInfo.Name = "SqlHydra.Oracle"
        AppInfo.Command = "sqlhydra-oracle"
        AppInfo.DefaultReaderType = "Oracle.ManagedDataAccess.Client.OracleDataReader"
        AppInfo.Version = version
    }

[<EntryPoint>]
let main argv =

    let cfg = Console.getConfig(app, argv)

    let formattedCode = 
        OracleSchemaProvider.getSchema cfg
        |> SchemaGenerator.generateModule cfg app
        |> SchemaGenerator.toFormattedCode cfg app

    File.WriteAllText(cfg.OutputFile, formattedCode)
    Fsproj.addFileToProject(cfg)
    0

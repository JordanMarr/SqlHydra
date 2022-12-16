module SqlHydra.SqlServer.Program

open SqlHydra.SqlServer
open SqlHydra
open SqlHydra.Domain
open System.IO
open Spectre.Console

type private SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let app = 
    {
        AppInfo.Name = "SqlHydra.SqlServer"
        AppInfo.Command = "sqlhydra-mssql"
        AppInfo.DefaultReaderType = "Microsoft.Data.SqlClient.SqlDataReader"
        AppInfo.DefaultProvider = "Microsoft.Data.SqlClient"
        AppInfo.Version = version
    }

[<EntryPoint>]
let main argv =

    let cfg = Console.getConfig(app, argv)

    let formattedCode = 
        SqlServerSchemaProvider.getSchema cfg
        |> SchemaGenerator.generateModule cfg app
        |> SchemaGenerator.toFormattedCode cfg app

    File.WriteAllText(cfg.OutputFile, formattedCode)
    Fsproj.addFileToProject(cfg)
    AnsiConsole.MarkupLine($"[green]-[/] `{cfg.OutputFile}` has been generated!")
    0

module SqlHydra.Npgsql.Program

open SqlHydra.Npgsql
open SqlHydra
open System.IO
open Domain

let app = 
    {
        AppInfo.Name = "SqlHydra.Npgsql"
        AppInfo.Command = "sqlhydra-npgsql"
        AppInfo.DefaultReaderType = "Npgsql.NpgsqlDataReader"
        AppInfo.Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
    }

[<EntryPoint>]
let main argv =

    let cfg = Console.getConfig(app, argv)

    let formattedCode = 
        NpgsqlSchemaProvider.getSchema cfg
        |> SchemaGenerator.generateModule cfg app
        |> SchemaGenerator.toFormattedCode cfg app

    File.WriteAllText(cfg.OutputFile, formattedCode)
    Fsproj.addFileToProject(cfg)
    0

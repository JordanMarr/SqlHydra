module SqlHydra.Sqlite.Program

open SqlHydra.Sqlite
open SqlHydra
open System.IO
open Domain

let app = 
    {
        AppInfo.Name = "SqlHydra.Sqlite"
        AppInfo.Command = "sqlhydra-sqlite"
        AppInfo.DefaultReaderType = "System.Data.IDataReader"
        AppInfo.Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
    }

[<EntryPoint>]
let main argv =

    let cfg = Console.getConfig(app, argv)

    let formattedCode = 
        SqliteSchemaProvider.getSchema cfg
        |> SchemaGenerator.generateModule cfg app
        |> SchemaGenerator.toFormattedCode cfg app

    File.WriteAllText(cfg.OutputFile, formattedCode)
    Fsproj.addFileToProject(cfg)
    0

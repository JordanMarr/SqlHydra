module SqlHydra.Sqlite.Program

open SqlHydra
open SqlHydra.Sqlite
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
        |> SchemaGenerator.generateModule cfg
        |> SchemaGenerator.toFormattedCode cfg app

    System.IO.File.WriteAllText(cfg.OutputFile, formattedCode)
    0

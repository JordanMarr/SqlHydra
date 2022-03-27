module SqlHydra.Sqlite.Program

open SqlHydra.Sqlite
open SqlHydra
open SqlHydra.Domain
open System.IO
open Spectre.Console

type private SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let app = 
    {
        AppInfo.Name = "SqlHydra.Sqlite"
        AppInfo.Command = "sqlhydra-sqlite"        
        // BREAKING: .NET 6 requires `DbDataReader` for access to `GetFieldValue` for `DateOnly`/`TimeOnly`.
        // Users upgrading existing to .NET 6 will need to update the `reader_type` in the `sqlhydra-sqlite.toml`.
        AppInfo.DefaultReaderType = "System.Data.Common.DbDataReader" // "System.Data.IDataReader" 
        AppInfo.DefaultProvider = "System.Data.SQLite"
        AppInfo.Version = version
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
    AnsiConsole.MarkupLine($"`{cfg.OutputFile}` has been generated.")
    0

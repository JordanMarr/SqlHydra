module SqlHydra.SqlServer.Program

open SqlHydra
open SqlHydra.SqlServer
open Domain

let app = 
    {
        AppInfo.Name = "SqlHydra.SqlServer"
        AppInfo.Command = "sqlhydra-mssql"
        AppInfo.DefaultReaderType = "Microsoft.Data.SqlClient.SqlDataReader"
        AppInfo.Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
    }

[<EntryPoint>]
let main argv =

    let cfg = Console.getConfig(app, argv)

    let formattedCode = 
        SqlServerSchemaProvider.getSchema cfg
        |> SchemaGenerator.generateModule cfg
        |> SchemaGenerator.toFormattedCode cfg app

    System.IO.File.WriteAllText(cfg.OutputFile, formattedCode)
    0

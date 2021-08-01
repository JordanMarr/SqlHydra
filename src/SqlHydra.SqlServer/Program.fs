open SqlHydra
open SqlHydra.SqlServer
open Schema

let app = 
    {
        Console.AppInfo.Name = "SqlHydra.SqlServer"
        Console.AppInfo.Command = "sqlhydra-mssql"
        Console.AppInfo.DefaultReaderType = "Microsoft.Data.SqlClient.SqlDataReader"
        Console.AppInfo.Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
    }

[<EntryPoint>]
let main argv =

    let cfg = Console.getConfig(app, argv)

    let formattedCode = 
        SqlServerSchemaProvider.getSchema cfg
        |> SchemaGenerator.generateModule cfg
        |> SchemaGenerator.toFormattedCode cfg app.Name

    System.IO.File.WriteAllText(cfg.OutputFile, formattedCode)
    0

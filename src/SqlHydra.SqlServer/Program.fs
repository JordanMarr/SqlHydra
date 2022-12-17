module SqlHydra.SqlServer.Program

open SqlHydra.SqlServer
open SqlHydra
open SqlHydra.Domain

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
    Console.run (app, argv, SqlServerSchemaProvider.getSchema)

module SqlHydra.Npgsql.Program

open SqlHydra.Npgsql
open SqlHydra
open SqlHydra.Domain

type private SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let app = 
    {
        AppInfo.Name = "SqlHydra.Npgsql"
        AppInfo.Command = "sqlhydra-npgsql"
        AppInfo.DefaultReaderType = "Npgsql.NpgsqlDataReader"
        AppInfo.DefaultProvider = "Npgsql"
        AppInfo.Version = version
    }

[<EntryPoint>]
let main argv =
    Console.run (app, argv, NpgsqlSchemaProvider.getSchema)

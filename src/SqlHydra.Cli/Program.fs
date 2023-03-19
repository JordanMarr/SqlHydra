module SqlHydra.Program

open FSharp.SystemCommandLine

type private SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let handler (providerName: string, tomlFile: string option) = 
    let app, getSchema = 
        match providerName with
        | "mssql" -> SqlServer.AppInfo.app, SqlServer.SqlServerSchemaProvider.getSchema
        | "npgsql" -> Npgsql.AppInfo.app, Npgsql.NpgsqlSchemaProvider.getSchema
        | "sqlite" -> Sqlite.AppInfo.app, Sqlite.SqliteSchemaProvider.getSchema
        | "oracle" -> Oracle.AppInfo.app, Oracle.OracleSchemaProvider.getSchema
        | _ -> failwith "Unsupported db provider. Valid options are: 'mssql', 'npgsql', 'sqlite', or 'oracle'."

    let args = 
        {
            Console.Args.App = app
            Console.Args.GetSchema = getSchema
            Console.Args.TomlFile = tomlFile
            Console.Args.Version = version
        }

    Console.run (args)

[<EntryPoint>]
let main argv =
    rootCommand argv {
        description "SqlHydra"
        inputs (
            Input.Argument<string>("Provider", "The database provider name: 'mssql', 'npgsql', 'sqlite', or 'oracle'"), 
            Input.OptionMaybe<string>(["-t"; "--toml-output"], "The name of the toml file.")
        )
        setHandler handler
    }

module SqlHydra.Program

open FSharp.SystemCommandLine

type private SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let handler (providerName: string, tomlOutput: string option) = 
    let app, getSchema = 
        match providerName with
        | "mssql" -> SqlServer.AppInfo.get version, SqlServer.SqlServerSchemaProvider.getSchema
        | "npgsql" -> Npgsql.AppInfo.get version, Npgsql.NpgsqlSchemaProvider.getSchema
        | "sqlite" -> Sqlite.AppInfo.get version, Sqlite.SqliteSchemaProvider.getSchema
        | "oracle" -> Oracle.AppInfo.get version, Oracle.OracleSchemaProvider.getSchema
        | _ -> failwith "Unsupported db provider. Valid options are: 'mssql', 'npgsql', 'sqlite', or 'oracle'."

    Console.run (app, tomlOutput, getSchema)

[<EntryPoint>]
let main argv =
    rootCommand argv {
        description "SqlHydra"
        inputs (
            Input.Argument<string>("Provider", "The database provider name: 'mssql', 'npgsql', 'sqlite', or 'oracle'"), 
            Input.OptionMaybe<string>(["-o"; "--toml-output"], "The name of the toml file.")
        )
        setHandler handler
    }

module SqlHydra.Program

open System
open FSharp.SystemCommandLine

let handler (provider: string, tomlFile: IO.FileInfo option) = 

    let info, getSchema = 
        match provider with
        | "mssql" -> SqlServer.AppInfo.info, SqlServer.SqlServerSchemaProvider.getSchema
        | "npgsql" -> Npgsql.AppInfo.info, Npgsql.NpgsqlSchemaProvider.getSchema
        | "sqlite" -> Sqlite.AppInfo.info, Sqlite.SqliteSchemaProvider.getSchema
        | "oracle" -> Oracle.AppInfo.info, Oracle.OracleSchemaProvider.getSchema
        | _ -> failwith "Unsupported db provider. Valid options are: 'mssql', 'npgsql', 'sqlite', or 'oracle'."

    let args : Console.Args = 
        {
            Provider = provider
            AppInfo = info
            GetSchema = getSchema
            TomlFile = tomlFile |> Option.defaultWith (fun () -> IO.FileInfo($"sqlhydra-{provider}.toml"))                    
            Version = Reflection.Assembly.GetAssembly(typeof<Console.Args>).GetName().Version |> string
        }

    Console.run args

[<EntryPoint>]
let main argv =
    rootCommand argv {
        description "SqlHydra"
        inputs (
            Input.Argument<string>("provider", "The database provider name: 'mssql', 'npgsql', 'sqlite', or 'oracle'"), 
            Input.OptionMaybe<IO.FileInfo>(["-t"; "--toml-file"], "The toml configuration filename. Default: 'sqlhydra-{provider}.toml'")
        )
        setHandler handler
    }

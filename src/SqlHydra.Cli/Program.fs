module SqlHydra.Program

open System.IO
open FSharp.SystemCommandLine

type SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let handler (provider: string, tomlFile: FileInfo option) = 

    let info, getSchema = 
        match provider with
        | "mssql" -> SqlServer.AppInfo.info, SqlServer.SqlServerSchemaProvider.getSchema
        | "npgsql" -> Npgsql.AppInfo.info, Npgsql.NpgsqlSchemaProvider.getSchema
        | "sqlite" -> Sqlite.AppInfo.info, Sqlite.SqliteSchemaProvider.getSchema
        | "oracle" -> Oracle.AppInfo.info, Oracle.OracleSchemaProvider.getSchema
        | _ -> failwith "Unsupported db provider. Valid options are: 'mssql', 'npgsql', 'sqlite', or 'oracle'."

    let args = 
        {
            Console.Args.Provider = provider
            Console.Args.AppInfo = info
            Console.Args.GetSchema = getSchema
            Console.Args.TomlFile = tomlFile |> Option.defaultWith (fun () -> FileInfo($"sqlhydra-{provider}.toml"))                    
            Console.Args.Version = version
        }

    Console.run args

[<EntryPoint>]
let main argv =
    rootCommand argv {
        description "SqlHydra"
        inputs (
            Input.Argument<string>("provider", "The database provider name: 'mssql', 'npgsql', 'sqlite', or 'oracle'"), 
            Input.OptionMaybe<FileInfo>(["-t"; "--toml-file"], "The toml configuration filename. Default: 'sqlhydra-{provider}.toml'")
        )
        setHandler handler
    }

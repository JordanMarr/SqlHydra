module SqlHydra.Program

open System.IO
open FSharp.SystemCommandLine

type SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let handler (provider: string, tomlFileMaybe: FileInfo option) = 

    let info, getSchema = 
        match provider with
        | "mssql" -> SqlServer.AppInfo.info, SqlServer.SqlServerSchemaProvider.getSchema
        | "npgsql" -> Npgsql.AppInfo.info, Npgsql.NpgsqlSchemaProvider.getSchema
        | "sqlite" -> Sqlite.AppInfo.info, Sqlite.SqliteSchemaProvider.getSchema
        | "oracle" -> Oracle.AppInfo.info, Oracle.OracleSchemaProvider.getSchema
        | _ -> failwith "Unsupported db provider. Valid options are: 'mssql', 'npgsql', 'sqlite', or 'oracle'."

    let args = 
        {
            Console.Args.ProviderArg = provider
            Console.Args.AppInfo = info
            Console.Args.GetSchema = getSchema
            Console.Args.TomlFile = 
                match tomlFileMaybe with
                | Some tomlFile -> 
                    tomlFile
                | None -> 
                    FileInfo($"sqlhydra-{provider}.toml")
                    //FileInfo(Path.Combine(System.Environment.CurrentDirectory, $"sqlhydra-{provider}.toml"))
            Console.Args.Version = version
        }

    Console.run args

[<EntryPoint>]
let main argv =
    rootCommand argv {
        description "SqlHydra"
        inputs (
            Input.Argument<string>("Provider", "The database provider name: 'mssql', 'npgsql', 'sqlite', or 'oracle'"), 
            Input.OptionMaybe<FileInfo>(["-t"; "--toml-file"], "The name of the toml configuration file.")
        )
        setHandler handler
    }

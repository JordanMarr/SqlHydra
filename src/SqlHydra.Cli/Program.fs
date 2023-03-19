module SqlHydra.Program

type private SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

[<EntryPoint>]
let main argv =
    let app, getSchema = 
        match argv with
        | [| "mssql" |] -> SqlServer.AppInfo.get version, SqlServer.SqlServerSchemaProvider.getSchema
        | [| "npgsql "|] -> Npgsql.AppInfo.get version, Npgsql.NpgsqlSchemaProvider.getSchema
        | [| "sqlite" |] -> Sqlite.AppInfo.get version, Sqlite.SqliteSchemaProvider.getSchema
        | [| "oracle "|] -> Oracle.AppInfo.get version, Oracle.OracleSchemaProvider.getSchema
        | _ -> failwith "Unsupported db provider. Valid options are: 'mssql', 'npgsql', 'sqlite', or 'oracle'."

    Console.run (app, argv, getSchema)

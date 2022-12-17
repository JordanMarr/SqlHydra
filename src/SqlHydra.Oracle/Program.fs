module SqlHydra.Oracle.Program

open SqlHydra.Oracle
open SqlHydra
open SqlHydra.Domain

type private SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let app = 
    {
        AppInfo.Name = "SqlHydra.Oracle"
        AppInfo.Command = "sqlhydra-oracle"
        AppInfo.DefaultReaderType = "Oracle.ManagedDataAccess.Client.OracleDataReader"
        AppInfo.DefaultProvider = "Oracle.ManagedDataAccess.Core"
        AppInfo.Version = version
    }

[<EntryPoint>]
let main argv =
    Console.run (app, argv, OracleSchemaProvider.getSchema)

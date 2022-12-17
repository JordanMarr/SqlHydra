module SqlHydra.Sqlite.Program

open SqlHydra.Sqlite
open SqlHydra
open SqlHydra.Domain

type private SelfRef = class end
let version = System.Reflection.Assembly.GetAssembly(typeof<SelfRef>).GetName().Version |> string

let app = 
    {
        AppInfo.Name = "SqlHydra.Sqlite"
        AppInfo.Command = "sqlhydra-sqlite"        
        // BREAKING: .NET 6 requires `DbDataReader` for access to `GetFieldValue` for `DateOnly`/`TimeOnly`.
        // Users upgrading existing to .NET 6 will need to update the `reader_type` in the `sqlhydra-sqlite.toml`.
        AppInfo.DefaultReaderType = "System.Data.Common.DbDataReader" // "System.Data.IDataReader" 
        AppInfo.DefaultProvider = "System.Data.SQLite"
        AppInfo.Version = version
    }

[<EntryPoint>]
let main argv =
    Console.run (app, argv, SqliteSchemaProvider.getSchema)

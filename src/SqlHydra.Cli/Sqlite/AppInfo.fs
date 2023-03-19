module SqlHydra.Sqlite.AppInfo

open SqlHydra.Domain

let get version = 
    {
        AppInfo.Name = "SqlHydra.Sqlite"
        AppInfo.Command = "sqlhydra-sqlite"        
        // BREAKING: .NET 6 requires `DbDataReader` for access to `GetFieldValue` for `DateOnly`/`TimeOnly`.
        // Users upgrading existing to .NET 6 will need to update the `reader_type` in the `sqlhydra-sqlite.toml`.
        AppInfo.DefaultReaderType = "System.Data.Common.DbDataReader" // "System.Data.IDataReader" 
        AppInfo.DefaultProvider = "System.Data.SQLite"
        AppInfo.Version = version
    }

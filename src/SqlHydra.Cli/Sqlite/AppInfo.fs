module SqlHydra.Sqlite.AppInfo

open SqlHydra.Domain

let info = 
    {
        AppInfo.Name = "SqlHydra.Sqlite"
        AppInfo.Command = "sqlhydra-sqlite"        
        AppInfo.DefaultReaderType = "System.Data.Common.DbDataReader" // "System.Data.IDataReader" 
        AppInfo.DefaultProvider = "System.Data.SQLite"
    }

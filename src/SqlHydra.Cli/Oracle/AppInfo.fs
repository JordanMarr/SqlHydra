module SqlHydra.Oracle.AppInfo

open SqlHydra.Domain

let get version = 
    {
        AppInfo.Name = "SqlHydra.Oracle"
        AppInfo.Command = "sqlhydra-oracle"
        AppInfo.DefaultReaderType = "Oracle.ManagedDataAccess.Client.OracleDataReader"
        AppInfo.DefaultProvider = "Oracle.ManagedDataAccess.Core"
        AppInfo.Version = version
    }

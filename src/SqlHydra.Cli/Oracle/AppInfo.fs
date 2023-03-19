module SqlHydra.Oracle.AppInfo

open SqlHydra.Domain

let app = 
    {
        AppInfo.Name = "SqlHydra.Oracle"
        AppInfo.Command = "sqlhydra-oracle"
        AppInfo.DefaultReaderType = "Oracle.ManagedDataAccess.Client.OracleDataReader"
        AppInfo.DefaultProvider = "Oracle.ManagedDataAccess.Core"
    }

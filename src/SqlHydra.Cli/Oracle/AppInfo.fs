module SqlHydra.Oracle.AppInfo

open SqlHydra.Domain

let info = 
    {
        AppInfo.Name = "SqlHydra.Oracle"
        AppInfo.DefaultReaderType = "Oracle.ManagedDataAccess.Client.OracleDataReader"
        AppInfo.DefaultProvider = "Oracle.ManagedDataAccess.Core"
    }

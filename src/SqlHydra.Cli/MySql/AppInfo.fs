module SqlHydra.MySql.AppInfo

open SqlHydra.Domain

let info =
    {
        AppInfo.Name = "SqlHydra.MySql"
        AppInfo.DefaultReaderType = "System.Data.Common.DbDataReader" // "System.Data.IDataReader"
        AppInfo.DefaultProvider = "MySql.Data"
    }

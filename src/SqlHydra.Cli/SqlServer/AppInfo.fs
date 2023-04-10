module SqlHydra.SqlServer.AppInfo

open SqlHydra.Domain

let info = 
    {
        AppInfo.Name = "SqlHydra.SqlServer"
        AppInfo.DefaultReaderType = "Microsoft.Data.SqlClient.SqlDataReader"
        AppInfo.DefaultProvider = "Microsoft.Data.SqlClient"
    }
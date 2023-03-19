module SqlHydra.SqlServer.AppInfo

open SqlHydra.Domain

let app = 
    {
        AppInfo.Name = "SqlHydra.SqlServer"
        AppInfo.Command = "sqlhydra-mssql"
        AppInfo.DefaultReaderType = "Microsoft.Data.SqlClient.SqlDataReader"
        AppInfo.DefaultProvider = "Microsoft.Data.SqlClient"
    }
module SqlHydra.Npgsql.AppInfo

open SqlHydra.Domain

let info = 
    {
        AppInfo.Name = "SqlHydra.Npgsql"
        AppInfo.Command = "sqlhydra-npgsql"
        AppInfo.DefaultReaderType = "Npgsql.NpgsqlDataReader"
        AppInfo.DefaultProvider = "Npgsql"
    }

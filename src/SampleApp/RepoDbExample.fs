module RepoDbExample
open Microsoft.Data.SqlClient
open RepoDb
open AdventureWorks // Generated Types

SqlServerBootstrap.Initialize()

FluentMapper
    .Entity<SalesLT.Address>().Table("[SalesLT].[Address]") // Must manually register non- "dbo" schemas
    |> ignore

let connect() = 
    let conn = new SqlConnection("Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;")
    conn.Open()
    conn

let runQueries() =    
    use conn = connect()

    conn.Query(fun (a: SalesLT.Address) -> a.City = "Dallas")
    |> printfn "Addresses: %A"

    conn.QueryAll<dbo.BuildVersion>()
    |> printfn "Build Versions: %A"

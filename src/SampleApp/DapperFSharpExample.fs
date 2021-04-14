module DapperFSharpExample
open System.Data
open System.Data.SqlClient
open Dapper.FSharp
open Dapper.FSharp.MSSQL
open AdventureWorks // Generated Types

Dapper.FSharp.OptionTypes.register()

let connect() = 
    let cs = "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
    let conn = new SqlConnection(cs) :> IDbConnection
    conn.Open()
    conn

let getAddressesForCity(conn: IDbConnection) (city: string) = 
    select {
        table "SalesLT.Address"
        where (eq "City" city)
    } 
    |> conn.SelectAsync<SalesLT.Address>
    |> Async.AwaitTask 

let runQueries() = 
    
    use conn = connect()
    
    let addresses = getAddressesForCity conn "Dallas" |> Async.RunSynchronously

    printfn "Dallas Addresses: %A" addresses

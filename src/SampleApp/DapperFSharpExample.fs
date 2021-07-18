module SampleApp.DapperFSharpExample
open System.Data
open Microsoft.Data.SqlClient
open Dapper
open Dapper.FSharp.LinqBuilders
open Dapper.FSharp.MSSQL
open SampleApp.AdventureWorks // Generated Types
open FSharp.Control.Tasks.V2

Dapper.FSharp.OptionTypes.register()

let connect() = 
    let cs = "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
    let conn = new SqlConnection(cs)
    conn.Open()
    conn

// Tables
let customerTable =         table<SalesLT.Customer>         |> inSchema (nameof SalesLT)
let customerAddressTable =  table<SalesLT.CustomerAddress>  |> inSchema (nameof SalesLT)
let addressTable =          table<SalesLT.Address>          |> inSchema (nameof SalesLT)
let productTable =          table<SalesLT.Product>          |> inSchema (nameof SalesLT)

let getAddressesForCity(conn: IDbConnection) (city: string) = 
    select {
        for a in addressTable do
        where (a.City = city)
    } |> conn.SelectAsync<SalesLT.Address>
    
let getCustomersWithAddresses(conn: SqlConnection) =
    select {
        for c in customerTable do
        leftJoin ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
        leftJoin a  in addressTable on (ca.AddressID = a.AddressID)
        where (isIn c.CustomerID [30018;29545;29954;29897;29503;29559])
        orderBy c.CustomerID
    } |> conn.SelectAsyncOption<SalesLT.Customer, SalesLT.CustomerAddress, SalesLT.Address>

let getProductsWithThumbnail(conn: SqlConnection) = task {
    let sql = "SELECT TOP 2 * FROM SalesLT.Product p WHERE ThumbNailPhoto IS NOT NULL"
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let productDataReader = SalesLT.ProductDataReader(reader)
    return [
        while reader.Read() do
            productDataReader.ToRecord()
    ]
}

let getProductNamesNumbers(conn: SqlConnection) = task {
    let sql = "SELECT TOP 10 [Name], [ProductNumber] FROM SalesLT.Product p WHERE ThumbNailPhoto IS NOT NULL"
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    return [
        let productDataReader = SalesLT.ProductDataReader(reader)
        while reader.Read() do
            productDataReader.Name(), productDataReader.ProductNumber()
    ]
}

let runQueries() = task {
    use conn = connect()    
    let! addresses = getAddressesForCity conn "Dallas"
    printfn "Dallas Addresses: %A" addresses

    let! product = getProductsWithThumbnail conn
    printfn "Products with Thumbnails Count: %i" (addresses |> Seq.length)

    let! productNamesNumbers = getProductNamesNumbers conn
    printfn "Product Names and Numbers: %A" productNamesNumbers
}
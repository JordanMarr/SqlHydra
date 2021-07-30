module SampleApp.DapperFSharpExample
open System.Data
open Microsoft.Data.SqlClient
open Dapper.FSharp.LinqBuilders
open Dapper.FSharp.MSSQL
open SampleApp.AdventureWorks // Generated Types
open FSharp.Control.Tasks.V2

Dapper.FSharp.OptionTypes.register()

let connect() = 
    let cs = "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
    let conn = new SqlConnection(cs) :> IDbConnection
    conn.Open()
    conn

// Tables
let customerTable =         table<SalesLT.Customer>         |> inSchema (nameof SalesLT)
let customerAddressTable =  table<SalesLT.CustomerAddress>  |> inSchema (nameof SalesLT)
let addressTable =          table<SalesLT.Address>          |> inSchema (nameof SalesLT)
let productTable =          table<SalesLT.Product>          |> inSchema (nameof SalesLT)
let categoryTable =         table<SalesLT.ProductCategory>  |> inSchema (nameof SalesLT)

let getAddressesForCity(conn: IDbConnection) (city: string) = 
    select {
        for a in addressTable do
        where (a.City = city)
    } 
    |> conn.SelectAsync<SalesLT.Address>
    
let getCustomersWithAddresses(conn: IDbConnection) =
    let query = 
        select {
            for c in customerTable do
            join ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
            join a  in addressTable on (ca.AddressID = a.AddressID)
            where (isIn c.CustomerID [30018;29545;29954;29897;29503;29559])
            orderBy c.CustomerID
        } 

    let (sql, p, _) = query |> Deconstructor.select<SalesLT.Customer, SalesLT.CustomerAddress, SalesLT.Address>
    
    query |> conn.SelectAsyncOption<SalesLT.Customer, SalesLT.CustomerAddress, SalesLT.Address>

let getProductsCategories(conn: IDbConnection) =
    select {
        for p in productTable do
        join c in categoryTable on (p.ProductCategoryID = Some c.ProductCategoryID)
        selectAll
    }
    |> conn.SelectAsyncOption<SalesLT.Product, SalesLT.ProductCategory>

let runQueries() = task {
    use conn = connect()

    let! addresses = getAddressesForCity conn "Dallas"
    printfn "Dallas Addresses: %A" addresses
    
    let! customers = getCustomersWithAddresses conn
    printfn "Customer Addresses: %A" customers

    // Dapper.FSharp is unable to download the product Thumbnail `byte[] option`
    //let! productsCategories = getProductsCategories conn
    //printfn "Products-Categories: %A" productsCategories
}
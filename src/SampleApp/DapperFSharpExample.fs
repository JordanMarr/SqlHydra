module SampleApp.DapperFSharpExample
open System.Data
open Microsoft.Data.SqlClient
open Dapper.FSharp.MSSQL
open SampleApp.AdventureWorks // Generated Types
open FSharp.Control.Tasks.V2
open System

Dapper.FSharp.OptionTypes.register()

let connect() = 
    let cs = "Server=localhost,12019;Database=AdventureWorks;User=sa;Password=Password#123;Connect Timeout=3;TrustServerCertificate=True"
    let conn = new SqlConnection(cs) :> IDbConnection
    conn.Open()
    conn

// Tables
let customerTable =         table<Sales.Customer>         |> inSchema (nameof Sales)
let orderHeaderTable =      table<Sales.SalesOrderHeader> |> inSchema (nameof Sales)
let orderDetailTable =      table<Sales.SalesOrderDetail> |> inSchema (nameof Sales)

let getTop10Customers(conn: IDbConnection) = 
    select {
        for c in customerTable do
        where (c.CustomerID < 200)
        selectAll
    } 
    |> conn.SelectAsync<Sales.Customer>
    
let getOrders(conn: IDbConnection) =
    select {
        for o in orderHeaderTable do
        leftJoin d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
        where (o.SalesOrderID > 43600 && o.SalesOrderID < 43700)
        selectAll
    } 
    |> conn.SelectAsyncOption<Sales.SalesOrderHeader, Sales.SalesOrderDetail>

let runQueries() = task {
    use conn = connect()

    let! customers = getTop10Customers(conn)
    printfn "Customers: %A" customers
    
    let! orders = getOrders conn
    printfn "Orders: %A" orders
}
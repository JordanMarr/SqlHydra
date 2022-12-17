module SampleApp.DonaldExample

open System.Data
open Microsoft.Data.SqlClient
open SampleApp.AdventureWorks // Generated Types
open FSharp.Control.Tasks.V2
open Donald

let connect() = 
    let cs = "Server=localhost,12019;Database=AdventureWorks;User=sa;Password=Password#123;Connect Timeout=3;TrustServerCertificate=True"
    let conn = new SqlConnection(cs)
    conn.Open()
    conn

let getTop10Products(conn: SqlConnection) = task {
    use! reader = 
        conn
        |> Db.newCommand "SELECT TOP 10 * FROM Production.Product p"
        |> Db.Async.read

    let hydra = HydraReader(reader :?> SqlDataReader)
    return [ 
        while reader.Read() do
            hydra.``Production.Product``.Read()
    ]
}

let getOrderHeadersAndDetails(conn: SqlConnection) = task {
    let sql = 
        """
        SELECT TOP 10 *
        FROM Purchasing.PurchaseOrderHeader h
        LEFT JOIN Purchasing.PurchaseOrderDetail d ON h.PurchaseOrderId = d.PurchaseOrderId
        """
    use! reader = 
        conn
        |> Db.newCommand sql
        |> Db.Async.read
    
    let hydra = HydraReader(reader :?> SqlDataReader)

    return [
        while reader.Read() do
            hydra.``Purchasing.PurchaseOrderHeader``.Read(), 
            hydra.``Purchasing.PurchaseOrderDetail``.ReadIfNotNull()
    ]
}

let runQueries() = task {
    use conn = connect()    
    
    let! products = getTop10Products conn
    printfn "Products with Thumbnails Count: %i" (products |> Seq.length)

    let! orders = getOrderHeadersAndDetails(conn)
    printfn "Orders: %A" orders
}
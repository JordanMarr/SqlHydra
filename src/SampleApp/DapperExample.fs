module SampleApp.DapperExample

open Microsoft.Data.SqlClient
open SampleApp.AdventureWorks // Generated Types
open FSharp.Control.Tasks.V2
open Dapper
open System.Data

let connect() = 
    let cs = "Server=localhost,12019;Database=AdventureWorks;User=sa;Password=Password#123;Connect Timeout=3;TrustServerCertificate=True"
    let conn = new SqlConnection(cs)
    conn.Open()
    conn

/// Gets the SqlDataReader from the Dapper WrappedReader.
let unwrapIDataReader (dapperReader: IDataReader) = 
    (dapperReader :?> IWrappedDataReader).Reader :?> SqlDataReader

/// Gets the SqlDataReader from the Dapper DbWrappedReader
let unwrapDbDataReader (dapperReader: Common.DbDataReader) = 
    (unbox<IWrappedDataReader> dapperReader).Reader :?> SqlDataReader

let getTop10Products(conn: SqlConnection) = task {
    use reader = conn.ExecuteReader("SELECT TOP 10 * FROM Production.Product p")
    let hydra = HydraReader(unwrapIDataReader reader)
    return [ 
        while reader.Read() do
            hydra.``Production.Product``.Read()
    ]
}

let getOrderHeadersAndDetails(conn: SqlConnection) = task {
    use! reader = conn.ExecuteReaderAsync(
        """
        SELECT TOP 10 *
        FROM Purchasing.PurchaseOrderHeader h
        LEFT JOIN Purchasing.PurchaseOrderDetail d ON h.PurchaseOrderId = d.PurchaseOrderId
        """
    )
    let hydra = HydraReader(unwrapDbDataReader reader)
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
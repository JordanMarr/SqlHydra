module SampleApp.ReaderExample
open System.Data
open Microsoft.Data.SqlClient
open SampleApp.AdventureWorks // Generated Types
open FSharp.Control.Tasks.V2

let connect() = 
    let cs = "Server=localhost,12019;Database=AdventureWorks;User=sa;Password=Password#123;Connect Timeout=3;TrustServerCertificate=True"
    let conn = new SqlConnection(cs)
    conn.Open()
    conn

let getTop10Products(conn: SqlConnection) = task {
    let sql = "SELECT TOP 10 * FROM Production.Product p"
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let hydra = HydraReader(reader)

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
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let hydra = HydraReader(reader)

    return [
        while reader.Read() do
            hydra.``Purchasing.PurchaseOrderHeader``.Read(), 
            hydra.``Purchasing.PurchaseOrderDetail``.ReadIfNotNull()
    ]
}

let runQueries() = task {
    use conn = connect()    
    
    let! products = getTop10Products conn
    printfn "getTop10Products: %i" (products |> Seq.length)

    let! orders = getOrderHeadersAndDetails(conn)
    printfn "getOrderHeadersAndDetails: %A" orders

}
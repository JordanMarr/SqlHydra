module SampleApp.ReaderExample
open System.Data
open Microsoft.Data.SqlClient
open SampleApp.AdventureWorks // Generated Types
open FSharp.Control.Tasks.V2

let connect() = 
    let cs = "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
    let conn = new SqlConnection(cs)
    conn.Open()
    conn

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

let getCustomersWithAddresses_Readers(conn: SqlConnection) = task {
    let sql = 
        """
        SELECT * FROM SalesLT.Customer c
        JOIN SalesLT.CustomerAddress ca ON c.CustomerID = ca.CustomerID
        JOIN SalesLT.Address a on ca.AddressID = a.AddressID
        WHERE c.CustomerID IN (30018,29545,29954,29897,29503,29559)
        ORDER BY c.CustomerID
        """
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let customer = SalesLT.CustomerDataReader(reader)
    //let customerAddress = SalesLT.CustomerAddressDataReader(reader)
    let address = SalesLT.AddressDataReader(reader)

    return [
        while reader.Read() do
            customer.ToRecord(), address.ToRecord()
    ]
}

let runQueries() = task {
    use conn = connect()    
    
    let! products = getProductsWithThumbnail conn
    printfn "Products with Thumbnails Count: %i" (products |> Seq.length)

    let! productNamesNumbers = getProductNamesNumbers conn
    printfn "Product Names and Numbers: %A" productNamesNumbers

    let! customersAddresses = getCustomersWithAddresses_Readers conn
    printfn "Customers-Addresses: %A" customersAddresses
}
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
    let hydra = HydraReader(reader)

    return [
        while reader.Read() do
            hydra.``SalesLT.Product``.Read()
    ]
}

let getProductNamesNumbers(conn: SqlConnection) = task {
    let sql = "SELECT TOP 10 [Name], [ProductNumber] AS ProductNo FROM SalesLT.Product p WHERE ThumbNailPhoto IS NOT NULL"
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let hydra = HydraReader(reader)

    return [
        while reader.Read() do
            hydra.``SalesLT.Product``.Name.Read(), hydra.``SalesLT.Product``.ProductNumber.Read("ProductNo")
    ]
}

let getCustomersJoinAddresses(conn: SqlConnection) = task {
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
    let hydra = HydraReader(reader)

    return [
        while reader.Read() do
            hydra.``SalesLT.Customer``.Read(), hydra.``SalesLT.Address``.Read()
    ]
}

let getCustomersLeftJoinAddresses(conn: SqlConnection) = task {
    let sql = 
        """
        SELECT TOP 20 * FROM SalesLT.Customer c
        LEFT JOIN SalesLT.CustomerAddress ca ON c.CustomerID = ca.CustomerID
        LEFT JOIN SalesLT.Address a on ca.AddressID = a.AddressID
        WHERE c.CustomerID IN (
            29485,29486, 29489, -- these have an a.AddressID, so LEFT JOIN should yield "Some"
            1,2)                -- these do not have have an a.AddressID, so LEFT JOIN should yield "None"
        ORDER BY c.CustomerID
        """
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let hydra = HydraReader(reader)

    return [
        while reader.Read() do
            hydra.``SalesLT.Customer``.Read(), hydra.``SalesLT.Address``.ReadIfNotNull()
    ]
}

let getProductsAndCategories(conn: SqlConnection) = task {
    let sql = 
        """
        SELECT *, c.Name as Category
        FROM SalesLT.Product p
        LEFT JOIN SalesLT.ProductCategory c ON p.ProductCategoryID = c.ProductCategoryID
        """
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let hydra = HydraReader(reader)

    return [
        while reader.Read() do
            hydra.``SalesLT.Product``.Read(), 
            hydra.``SalesLT.ProductCategory``.ReadIfNotNull()
    ]
}

let runQueries() = task {
    use conn = connect()    
    
    let! products = getProductsWithThumbnail conn
    printfn "Products with Thumbnails Count: %i" (products |> Seq.length)

    let! productNamesNumbers = getProductNamesNumbers conn
    printfn "Product Names and Numbers: %A" productNamesNumbers

    let! customersAddresses = getCustomersJoinAddresses conn
    printfn "Customers-Join-Addresses: %A" customersAddresses

    let! customerLeftJoinAddresses = getCustomersLeftJoinAddresses conn
    printfn "Customer-LeftJoin-Addresses: %A" customerLeftJoinAddresses

    let! productsCategories = getProductsAndCategories conn
    printfn "Products-Categories: %A" productsCategories
}
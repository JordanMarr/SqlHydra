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
    let p = SalesLT.ProductReader(reader)
    return [
        while reader.Read() do
            p.Read()
    ]
}

let getProductNamesNumbers(conn: SqlConnection) = task {
    let sql = "SELECT TOP 10 [Name], [ProductNumber] AS ProductNo FROM SalesLT.Product p WHERE ThumbNailPhoto IS NOT NULL"
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    return [
        let productDataReader = SalesLT.ProductReader(reader)
        while reader.Read() do
            productDataReader.Name.Read(), productDataReader.ProductNumber.Read("ProductNo")
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
    let c = SalesLT.CustomerReader(reader)
    let a = SalesLT.AddressReader(reader)

    return [
        while reader.Read() do
            c.Read(), a.Read()
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
    let c = SalesLT.CustomerReader(reader)
    let a = SalesLT.AddressReader(reader)

    return [
        while reader.Read() do
            c.Read(), a.ReadIf(a.AddressID)
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
    let p = SalesLT.ProductReader(reader)
    let c = SalesLT.ProductCategoryReader(reader)
    c.Name.Alias "Category"

    return [
        while reader.Read() do
            p.Read(), c.ReadIf(c.ProductCategoryID)
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
module SampleApp.DapperExample

open Microsoft.Data.SqlClient
open SampleApp.AdventureWorks // Generated Types
open FSharp.Control.Tasks.V2
open Dapper
open System.Data

let connect() = 
    let cs = "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI;"
    let conn = new SqlConnection(cs)
    conn.Open()
    conn

/// Gets the SqlDataReader from the Dapper WrappedReader.
let unwrapIDataReader (dapperReader: IDataReader) = 
    (dapperReader :?> IWrappedDataReader).Reader :?> SqlDataReader

/// Gets the SqlDataReader from the Dapper DbWrappedReader
let unwrapDbDataReader (dapperReader: Common.DbDataReader) = 
    (unbox<IWrappedDataReader> dapperReader).Reader :?> SqlDataReader

let getProductsWithThumbnail(conn: SqlConnection) = task {
    use reader = conn.ExecuteReader("SELECT TOP 2 * FROM SalesLT.Product p WHERE ThumbNailPhoto IS NOT NULL")
    let hydra = HydraReader(unwrapIDataReader reader)
    return [ 
        while reader.Read() do
            hydra.``SalesLT.Product``.Read() 
    ]
}

let getProductsWithThumbnailAsync(conn: SqlConnection) = task {
    use! reader = conn.ExecuteReaderAsync("SELECT TOP 2 * FROM SalesLT.Product p WHERE ThumbNailPhoto IS NOT NULL")
    let hydra = HydraReader(unwrapDbDataReader reader)
    return [ 
        while reader.Read() do
            hydra.``SalesLT.Product``.Read() 
    ]
}

let runQueries() = task {
    use conn = connect()    
    
    let! products = getProductsWithThumbnail conn
    printfn "Products with Thumbnails Count: %i" (products |> Seq.length)

    let! products2 = getProductsWithThumbnailAsync conn
    printfn "Products with Thumbnails Count: %i" (products |> Seq.length)
}
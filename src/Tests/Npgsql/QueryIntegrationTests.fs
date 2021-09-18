module Npgsql.QueryIntegrationTests

open Expecto
open SqlHydra.Query
open DB
open Npgsql.AdventureWorks

let openContext() = 
    let compiler = SqlKata.Compilers.PostgresCompiler()
    let conn = new Npgsql.NpgsqlConnection(connectionString)
    conn.Open()
    new QueryContext(conn, compiler)

// Tables
let personTable =           table<person.person>                    |> inSchema (nameof person)
let addressTable =          table<person.address>                   |> inSchema (nameof person)
let customerTable =         table<sales.customer>                   |> inSchema (nameof sales)
let orderHeaderTable =      table<sales.salesorderheader>           |> inSchema (nameof sales)
let orderDetailTable =      table<sales.salesorderdetail>           |> inSchema (nameof sales)
let productTable =          table<production.product>               |> inSchema (nameof production)
let subCategoryTable =      table<production.productsubcategory>    |> inSchema (nameof production)
let categoryTable =         table<production.productcategory>       |> inSchema (nameof production)

/// Sequence length is > 0.
let gt0 (items: 'Item seq) =
    Expect.isTrue (items |> Seq.length > 0) "Expected more than 0."

[<Tests>]
let tests = 
    categoryList "Npgsql" "Query Integration Tests" [

        testTask "Where City Starts With S" {
            use ctx = openContext()
            
            let addresses =
                select {
                    for a in addressTable do
                    where (a.city |=| [ "Seattle"; "Santa Cruz" ])
                }
                |> ctx.Read HydraReader.Read

            gt0 addresses
            Expect.isTrue (addresses |> Seq.forall (fun a -> a.city = "Seattle" || a.city = "Santa Cruz")) "Expected only 'Seattle' or 'Santa Cruz'."
        }
    ]
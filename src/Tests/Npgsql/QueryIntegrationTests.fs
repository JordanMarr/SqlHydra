module Npgsql.QueryIntegrationTests

open Expecto
open SqlHydra.Query
open DB
open Npgsql.AdventureWorks
open System.Threading.Tasks

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
let currencyTable =         table<sales.currency>                   |> inSchema (nameof sales)
let productReviewTable =    table<production.productreview>         |> inSchema (nameof production)

/// Sequence length is > 0.
let gt0 (items: 'Item seq) =
    Expect.isTrue (items |> Seq.length > 0) "Expected more than 0."

[<Tests>]
let tests = 
    categoryList "Npgsql" "Query Integration Tests" [

        testTask "Where city Starts With S" {
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

        testTask "Select city Column Where city Starts with S" {
            use ctx = openContext()

            let cities =
                select {
                    for a in addressTable do
                    where (a.city =% "S%")
                    select a.city
                }
                |> ctx.Read HydraReader.Read

            gt0 cities
            Expect.isTrue (cities |> Seq.forall (fun city -> city.StartsWith "S")) "Expected all cities to start with 'S'."
        }

        testTask "Inner Join Orders-Details" {
            use ctx = openContext()

            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.salesorderid = d.salesorderid)
                    where (o.onlineorderflag = true)
                    select (o, d)
                }

            query.ToKataQuery() |> toSql |> printfn "%s"

            let! results = query |> ctx.ReadAsync HydraReader.Read
            gt0 results
        }

        testTask "Product with Category name" {
            use ctx = openContext()

            let query = 
                select {
                    for p in productTable do
                    join sc in subCategoryTable on (p.productsubcategoryid = Some sc.productsubcategoryid)
                    join c in categoryTable on (sc.productcategoryid = c.productcategoryid)
                    select (c.name, p)
                    take 5
                }

            let! rows = query |> ctx.ReadAsync HydraReader.Read
            printfn "Results: %A" rows
            query.ToKataQuery() |> toSql |> printfn "%s"
            gt0 rows
        }

        testTask "Select Column Aggregates From Product IDs 1-3" {
            use ctx = openContext()

            let query =
                select {
                    for p in productTable do
                    where (p.productsubcategoryid <> None)
                    groupBy p.productsubcategoryid
                    where (p.productsubcategoryid.Value |=| [ 1; 2; 3 ])
                    select (p.productsubcategoryid, minBy p.listprice, maxBy p.listprice, avgBy p.listprice, countBy p.listprice, sumBy p.listprice)
                }

            let! aggregates = query |> ctx.ReadAsync HydraReader.Read
            let sql = query.ToKataQuery() |> toSql 
            sql |> printfn "%s"

            gt0 aggregates
    
            let aggByCatID = 
                aggregates 
                |> Seq.map (fun (catId, minPrice, maxPrice, avgPrice, priceCount, sumPrice) -> catId, (minPrice, maxPrice, avgPrice, priceCount, sumPrice)) 
                |> Map.ofSeq
            
            let dc (actual: decimal) (expected: decimal) = Expect.floatClose Accuracy.medium (float actual) (float expected) "Expected values to be close"

            let verifyAggregateValuesFor (catId: int) (xMinPrice, xMaxPrice, xAvgPrice, xPriceCount, xSumPrice) =
                let aMinPrice, aMaxPrice, aAvgPrice, aPriceCount, aSumPrice = aggByCatID.[Some catId]
                dc aMinPrice xMinPrice; dc aMaxPrice xMaxPrice; dc aAvgPrice xAvgPrice; Expect.equal aPriceCount xPriceCount ""; dc aSumPrice xSumPrice
            
            verifyAggregateValuesFor 1 (539.99M, 3399.99M, 1683.365M, 32, 53867.6800M)
            verifyAggregateValuesFor 2 (539.99M, 3578.2700M, 1597.4500M, 43, 68690.3500M)
            verifyAggregateValuesFor 3 (742.3500M, 2384.0700M, 1425.2481M, 22, 31355.4600M)
        }

        testTask "Aggregate Subquery One" {
            use ctx = openContext()

            let avgListPrice = 
                select {
                    for p in productTable do
                    select (avgBy p.listprice)
                }

            let! productsWithHigherThanAvgPrice = 
                select {
                    for p in productTable do
                    where (p.listprice > subqueryOne avgListPrice)
                    orderByDescending p.listprice
                    select (p.name, p.listprice)
                }
                |> ctx.ReadAsync HydraReader.Read

            let avgListPrice = 438.6662M
    
            gt0 productsWithHigherThanAvgPrice
            Expect.isTrue (productsWithHigherThanAvgPrice |> Seq.forall (fun (nm, price) -> price > avgListPrice)) "Expected all prices to be > than avg price of $438.67."
        }

        testTask "Select Column Aggregates" {
            use ctx = openContext()

            let! aggregates = 
                select {
                    for p in productTable do
                    where (p.productsubcategoryid <> None)
                    groupBy p.productsubcategoryid
                    having (minBy p.listprice > 50M && maxBy p.listprice < 1000M)
                    select (p.productsubcategoryid, minBy p.listprice, maxBy p.listprice)
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 aggregates
        }

        testTask "Sorted Aggregates - Top 5 categories with highest avg price products" {
            use ctx = openContext()

            let! aggregates = 
                select {
                    for p in productTable do
                    where (p.productsubcategoryid <> None)
                    groupBy p.productsubcategoryid
                    orderByDescending (avgBy p.listprice)
                    select (p.productsubcategoryid, avgBy p.listprice)
                    take 5
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 aggregates
        }

        testTask "Where subqueryMany" {
            use ctx = openContext()

            let top5CategoryIdsWithHighestAvgPrices = 
                select {
                    for p in productTable do
                    where (p.productsubcategoryid <> None)
                    groupBy p.productsubcategoryid
                    orderByDescending (avgBy p.listprice)
                    select (p.productsubcategoryid)
                    take 5
                }

            let! top5Categories =
                select {
                    for c in categoryTable do
                    where (Some c.productcategoryid |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
                    select c.name
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 top5Categories
        }

        testTask "Where subqueryOne" {
            use ctx = openContext()

            let avgListPrice = 
                select {
                    for p in productTable do
                    select (avgBy p.listprice)
                } 

            let! productsWithAboveAveragePrice =
                select {
                    for p in productTable do
                    where (p.listprice > subqueryOne avgListPrice)
                    select (p.name, p.listprice)
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 productsWithAboveAveragePrice
        }

        testTask "Select Columns with Option" {
            use ctx = openContext()

            let! values = 
                select {
                    for p in productTable do
                    where (p.productsubcategoryid <> None)
                    select (p.productsubcategoryid, p.listprice)
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 values
            Expect.isTrue (values |> Seq.forall (fun (catId, price) -> catId <> None)) "Expected subcategories to all have a value."
        }

        testTask "Insert Currency" {
            use ctx = openContext()

            let! results = 
                insert {
                    into currencyTable
                    entity 
                        {
                            sales.currency.currencycode = "BTC"
                            sales.currency.name = "BitCoin"
                            sales.currency.modifieddate = System.DateTime.Now
                        }
                }
                |> ctx.InsertAsync

            Expect.isTrue (results = 1) ""

            let! btc = 
                select {
                    for c in currencyTable do
                    where (c.currencycode = "BTC")
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 btc
        }

        testTask "Update Currency" {
            use ctx = openContext()

            let! results = 
                update {
                    for c in currencyTable do
                    set c.name "BitCoinzz"
                    where (c.currencycode = "BTC")
                }
                |> ctx.UpdateAsync

            Expect.isTrue (results > 0) ""

            let! btc = 
                select {
                    for c in currencyTable do
                    where (c.name = "BitCoinzz")
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 btc
        }

        testTask "Delete Currency" {
            use ctx = openContext()

            let! _ = 
                delete {
                    for c in currencyTable do
                    where (c.currencycode = "BTC")
                }
                |> ctx.DeleteAsync

            let! btc = 
                select {
                    for c in currencyTable do
                    where (c.currencycode = "BTC")
                }
                |> ctx.ReadAsync HydraReader.Read

            Expect.isTrue (btc |> Seq.length = 0) "Should be deleted"
        }

        testTask "Insert and Get Id" {
            use ctx = openContext()
            ctx.BeginTransaction()

            let! prodReviewId = 
                insert {
                    for r in productReviewTable do
                    entity 
                        {
                            production.productreview.productreviewid = 0 // PK
                            production.productreview.comments = Some "The ML Fork makes for a plush ride."
                            production.productreview.emailaddress = "gfisher@askjeeves.com"
                            production.productreview.modifieddate = System.DateTime.Today
                            production.productreview.productid = 803 //ML Fork
                            production.productreview.rating = 5
                            production.productreview.reviewdate = System.DateTime.Today
                            production.productreview.reviewername = "Gary Fisher"
                        }
                    getId r.productreviewid
                }
                |> ctx.InsertAsync

            let! review = 
                select {
                    for r in productReviewTable do
                    where (r.reviewername = "Gary Fisher")
                }
                |> ctx.ReadOneAsync HydraReader.Read
            
            match review with
            | Some (rev : production.productreview) -> 
                Expect.isTrue (prodReviewId > 0) "Expected productreviewid to be greater than 0"
            | None -> 
                failwith "Expected to query a review row."

            let! deletedCount = 
                delete {
                    for r in productReviewTable do
                    where (r.productreviewid = prodReviewId)
                }
                |> ctx.DeleteAsync

            Expect.equal deletedCount 1 "Expected exactly one review to be deleted"

            let! reviews = 
                select {
                    for r in productReviewTable do
                    where (r.reviewername = "Gary Fisher")
                }
                |> ctx.ReadAsync HydraReader.Read

            Expect.equal (reviews |> Seq.length) 0 "Expected no reviews to be queryable"

            ctx.CommitTransaction()
        }
    ]
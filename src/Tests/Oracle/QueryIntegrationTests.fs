module Oracle.QueryIntegrationTests

open Expecto
open SqlHydra.Query
open DB
open Oracle.ManagedDataAccess.Client
#if NET5_0
open Oracle.AdventureWorksNet5
#endif
#if NET6_0
open Oracle.AdventureWorksNet6
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.OracleCompiler()
    let conn = new OracleConnection(connectionString)
    conn.Open()
    new QueryContext(conn, compiler)

// Tables
let regionsTable =          table<OT.REGIONS>               |> inSchema (nameof OT)
let countriesTable =        table<OT.COUNTRIES>             |> inSchema (nameof OT)
let customersTable =        table<OT.CUSTOMERS>             |> inSchema (nameof OT)
let orderHeaderTable =      table<OT.ORDERS>                |> inSchema (nameof OT)
let orderDetailTable =      table<OT.ORDER_ITEMS>           |> inSchema (nameof OT)
let productTable =          table<OT.PRODUCTS>              |> inSchema (nameof OT)
let categoryTable =         table<OT.PRODUCT_CATEGORIES>    |> inSchema (nameof OT)

[<Tests>]
let tests = 
    categoryList "Oracle" "Query Integration Tests" [

        testTask "Where Name Contains" {
            use ctx = openContext()
            
            let addresses =
                select {
                    for c in customersTable do
                    where (c.NAME |=| [ "Staples"; "Aflac" ])
                }
                |> ctx.Read HydraReader.Read

            gt0 addresses
            Expect.isTrue (addresses |> Seq.forall (fun a -> a.NAME = "Staples" || a.NAME = "Aflac")) "Expected only 'Staples' or 'Aflac'."
        }

        testTask "Select Address Column Where Address Contains Detroit" {
            use ctx = openContext()

            let cities =
                select {
                    for c in customersTable do
                    where (c.ADDRESS =% "%Detroit%")
                    select c.ADDRESS
                }
                |> ctx.Read HydraReader.Read

            gt0 cities
            Expect.isTrue (cities |> Seq.choose id |> Seq.forall (fun city -> city.Contains "Detroit")) "Expected all cities to contain 'Detroit'."
        }

        testTask "Inner Join Orders-Details" {
            use ctx = openContext()

            let query =
                select {
                    for o in orderHeaderTable do
                    join d in orderDetailTable on (o.ORDER_ID = d.ORDER_ID)
                    where (o.STATUS = "PENDING")
                    select (o, d)
                }

            //query.ToKataQuery() |> toSql |> printfn "%s"

            let! results = query |> ctx.ReadAsync HydraReader.Read
            gt0 results
        }

        testTask "Product with Category name" {
            use ctx = openContext()

            let query = 
                select {
                    for p in productTable do
                    join c in categoryTable on (p.CATEGORY_ID = c.CATEGORY_ID)
                    select (c.CATEGORY_NAME, p)
                    take 5
                }

            let! rows = query |> ctx.ReadAsync HydraReader.Read
            printfn "Results: %A" rows
            //query.ToKataQuery() |> toSql |> printfn "%s"
            gt0 rows
        }

        testTask "Select Column Aggregates From Product IDs 1-3" {
            use ctx = openContext()

            let query =
                select {
                    for p in productTable do
                    join c in categoryTable on (p.CATEGORY_ID = c.CATEGORY_ID)
                    where (p.LIST_PRICE <> None)
                    groupBy p.CATEGORY_ID
                    select (p.CATEGORY_ID, minBy p.LIST_PRICE.Value, maxBy p.LIST_PRICE.Value, avgBy p.LIST_PRICE.Value, countBy p.LIST_PRICE.Value, sumBy p.LIST_PRICE.Value)
                }

            let! aggregates = query |> ctx.ReadAsync HydraReader.Read
            //let sql = query.ToKataQuery() |> toSql 
            //sql |> printfn "%s"

            gt0 aggregates
    
            let aggByCatID = 
                aggregates 
                |> Seq.map (fun (catId, minPrice, maxPrice, avgPrice, priceCount, sumPrice) -> catId, (minPrice, maxPrice, avgPrice, priceCount, sumPrice)) 
                |> Map.ofSeq
            
            let dc (actual: decimal) (expected: decimal) = Expect.floatClose Accuracy.medium (float actual) (float expected) "Expected values to be close"

            let verifyAggregateValuesFor (catId: int64) (xMinPrice, xMaxPrice, xAvgPrice, xPriceCount, xSumPrice) =
                let aMinPrice, aMaxPrice, aAvgPrice, aPriceCount, aSumPrice = aggByCatID.[catId]
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
                    where (p.LIST_PRICE <> None)
                    select (avgBy p.LIST_PRICE.Value)
                }

            let! productsWithHigherThanAvgPrice = 
                select {
                    for p in productTable do
                    where (p.LIST_PRICE.Value > subqueryOne avgListPrice)
                    orderByDescending p.LIST_PRICE
                    select (p.PRODUCT_NAME, p.LIST_PRICE.Value)
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
                    where (p.LIST_PRICE <> None)
                    groupBy p.CATEGORY_ID
                    having (minBy p.LIST_PRICE.Value > 50M && maxBy p.LIST_PRICE.Value < 1000M)
                    select (p.CATEGORY_ID, minBy p.LIST_PRICE.Value, maxBy p.LIST_PRICE.Value)
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 aggregates
        }

        testTask "Sorted Aggregates - Top 5 categories with highest avg price products" {
            use ctx = openContext()

            //let! aggregates = 
            let reader : OracleDataReader = 
                select {
                    for p in productTable do
                    where (p.LIST_PRICE <> None)
                    groupBy p.CATEGORY_ID
                    orderByDescending (avgBy p.LIST_PRICE.Value)
                    select (p.CATEGORY_ID, avgBy p.LIST_PRICE.Value)
                    take 5
                }
                |> ctx.GetReader
                //|> ctx.ReadAsync HydraReader.Read

            let aggregates = 
                reader.SuppressGetDecimalInvalidCastException <- true
                [ while reader.Read() do
                    reader.GetInt64(0), reader.GetDecimal(1)
                ]

            gt0 aggregates
        }

        testTask "Where subqueryMany" {
            use ctx = openContext()

            let top5CategoryIdsWithHighestAvgPrices = 
                select {
                    for p in productTable do
                    where (p.LIST_PRICE <> None)
                    groupBy p.CATEGORY_ID
                    orderByDescending (avgBy p.LIST_PRICE.Value)
                    select (p.CATEGORY_ID)
                    take 5
                }

            let! top5Categories =
                select {
                    for c in categoryTable do
                    where (c.CATEGORY_ID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
                    select c.CATEGORY_NAME
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 top5Categories
        }

        testTask "Where subqueryOne" {
            use ctx = openContext()

            let avgListPrice = 
                select {
                    for p in productTable do
                    select (avgBy p.LIST_PRICE.Value)
                } 

            let! productsWithAboveAveragePrice =
                select {
                    for p in productTable do
                    where (p.LIST_PRICE <> None && p.LIST_PRICE.Value > subqueryOne avgListPrice)
                    select (p.PRODUCT_NAME, p.LIST_PRICE.Value)
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 productsWithAboveAveragePrice
        }

        testTask "Select Columns with Option" {
            use ctx = openContext()

            let! values = 
                select {
                    for p in productTable do
                    where (p.LIST_PRICE <> None)
                    select (p.CATEGORY_ID, p.LIST_PRICE)
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 values
            Expect.isTrue (values |> Seq.forall (fun (catId, price) -> price <> None)) "Expected subcategories to all have a value."
        }

        testTask "Insert Country" {
            use ctx = openContext()

            let! results = 
                insert {
                    into countriesTable
                    entity 
                        {
                            OT.COUNTRIES.COUNTRY_ID = "WL"
                            OT.COUNTRIES.COUNTRY_NAME = "Wonderland"
                            OT.COUNTRIES.REGION_ID = Some 2
                        }
                }
                |> ctx.InsertAsync

            Expect.isTrue (results = 1) ""

            let! wl = 
                select {
                    for c in countriesTable do
                    where (c.COUNTRY_ID = "WL")
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 wl
        }

        testTask "Update Country" {
            use ctx = openContext()

            let! results = 
                update {
                    for c in countriesTable do
                    set c.COUNTRY_NAME "Wonder Land"
                    where (c.COUNTRY_ID = "WL")
                }
                |> ctx.UpdateAsync

            Expect.isTrue (results > 0) ""

            let! wl = 
                select {
                    for c in countriesTable do
                        where (c.COUNTRY_NAME = "Wonder Land")
                }
                |> ctx.ReadAsync HydraReader.Read

            gt0 wl
        }

        testTask "Delete Country" {
            use ctx = openContext()

            let! _ = 
                delete {
                    for c in countriesTable do
                    where (c.COUNTRY_ID = "WL")
                }
                |> ctx.DeleteAsync

            let! wl = 
                select {
                    for c in countriesTable do
                    where (c.COUNTRY_ID = "WL")
                }
                |> ctx.ReadAsync HydraReader.Read

            Expect.isTrue (wl |> Seq.length = 0) "Should be deleted"
        }
        
        testTask "Insert and Get Id" {
            use ctx = openContext()
            ctx.BeginTransaction()

            let! regionId = 
                insert {
                    for r in regionsTable do
                    entity 
                        {
                            OT.REGIONS.REGION_ID = 0 // PK
                            OT.REGIONS.REGION_NAME = "Test Region"
                        }
                    getId r.REGION_ID
                }
                |> ctx.InsertAsync

            let! region = 
                select {
                    for r in regionsTable do
                    where (r.REGION_ID = regionId)
                }
                |> ctx.ReadOneAsync HydraReader.Read
            
            match region with
            | Some (r: OT.REGIONS) -> 
                Expect.isTrue (r.REGION_ID > 0) "Expected REGION_ID to be greater than 0"
            | None -> 
                failwith "Expected to query a region row."
        }
                
        testTask "Multiple Inserts" {
            use ctx = openContext()

            ctx.BeginTransaction()

            let countriesAL1 = 
                [ 0 .. 2 ] 
                |> List.map (fun i -> 
                    {
                        OT.COUNTRIES.COUNTRY_ID = $"X{i}"
                        OT.COUNTRIES.COUNTRY_NAME = $"Country-{i}"
                        OT.COUNTRIES.REGION_ID = Some 2
                    }
                )
                |> AtLeastOne.tryCreate
    
            match countriesAL1 with
            | Some countries ->
                let! rowsInserted = 
                    insert {
                        into countriesTable
                        entities countries
                    }
                    |> ctx.InsertAsync

                Expect.equal rowsInserted 3 "Expected 3 rows to be inserted"

                let! results =
                    select {
                        for c in countriesTable do
                        where (c.COUNTRY_ID =% "X%")
                        orderBy c.COUNTRY_ID
                        select c.COUNTRY_ID
                    }
                    |> ctx.ReadAsync HydraReader.Read

                let codes = results |> Seq.toList
    
                Expect.equal codes [ "X0"; "X1"; "X2" ] ""
            | None -> ()

            ctx.RollbackTransaction()
        }

        testTask "Distinct Test" {
            use ctx = openContext()

            ctx.BeginTransaction()

            let countriesAL1 = 
                [ 0 .. 2 ] 
                |> List.map (fun i -> 
                    {
                        OT.COUNTRIES.COUNTRY_ID = $"X{i}"
                        OT.COUNTRIES.COUNTRY_NAME = $"Country-{i}"
                        OT.COUNTRIES.REGION_ID = Some 2
                    }
                )
                |> AtLeastOne.tryCreate
    
            match countriesAL1 with
            | Some countries ->
                let! rowsInserted = 
                    insert {
                        for e in countriesTable do
                        entities countries
                    }
                    |> ctx.InsertAsync

                Expect.equal rowsInserted 3 "Expected 3 rows to be inserted"

                let! results =
                    select {
                        for c in countriesTable do
                        where (c.COUNTRY_ID =% "X%")
                        select c.COUNTRY_NAME
                    }
                    |> ctx.ReadAsync HydraReader.Read

                let! distinctResults =
                    select {
                        for c in countriesTable do
                        where (c.COUNTRY_ID =% "X%")
                        select c.REGION_ID
                        distinct
                    }
                    |> ctx.ReadAsync HydraReader.Read

                Expect.equal (results |> Seq.length) 3 ""
                Expect.equal (distinctResults |> Seq.length) 1 ""
            | None -> ()

            ctx.RollbackTransaction()
        }
        
    ]
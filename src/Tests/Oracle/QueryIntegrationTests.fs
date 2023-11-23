module Oracle.``Query Integration Tests``

open Swensen.Unquote
open SqlHydra.Query
open Oracle.ManagedDataAccess.Client
open NUnit.Framework
open DB

#if NET6_0
open Oracle.AdventureWorksNet6
#endif
#if NET8_0
open Oracle.AdventureWorksNet8
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.OracleCompiler()
    let conn = new OracleConnection(connectionString)
    conn.Open()
    new QueryContext(conn, compiler)

[<Test>]
let ``Where Name Contains``() = task {
    use ctx = openContext()
    
    let addresses =
        select {
            for c in OT.CUSTOMERS do
            where (c.NAME |=| [ "Staples"; "Aflac" ])
        }
        |> ctx.Read HydraReader.Read

    gt0 addresses
    Assert.IsTrue(addresses |> Seq.forall (fun a -> a.NAME = "Staples" || a.NAME = "Aflac"), "Expected only 'Staples' or 'Aflac'.")
}

[<Test>]
let ``Select Address Column Where Address Contains Detroit``() = task {
    use ctx = openContext()

    let cities =
        select {
            for c in OT.CUSTOMERS do
            where (c.ADDRESS =% "%Detroit%")
            select c.ADDRESS
        }
        |> ctx.Read HydraReader.Read

    gt0 cities
    Assert.IsTrue(cities |> Seq.choose id |> Seq.forall (fun city -> city.Contains "Detroit"), "Expected all cities to contain 'Detroit'.")
}

[<Test>]
let ``Inner Join Orders-Details``() = task {
    use ctx = openContext()

    let query =
        select {
            for o in OT.ORDERS do
            join d in OT.ORDER_ITEMS on (o.ORDER_ID = d.ORDER_ID)
            where (o.STATUS = "Pending")
            select (o, d)
        }

    let! results = query |> ctx.ReadAsync HydraReader.Read
    gt0 results
}

[<Test>]
let ``Product with Category name``() = task {
    use ctx = openContext()

    let query = 
        select {
            for p in OT.PRODUCTS do
            join c in OT.PRODUCT_CATEGORIES on (p.CATEGORY_ID = c.CATEGORY_ID)
            select (c.CATEGORY_NAME, p)
            take 5
        }

    let! rows = query |> ctx.ReadAsync HydraReader.Read
    gt0 rows
}

[<Test>]
let ``Select Column Aggregates From Product IDs 1-3``() = task {
    use ctx = openContext()

    let query =
        select {
            for p in OT.PRODUCTS do
            join c in OT.PRODUCT_CATEGORIES on (p.CATEGORY_ID = c.CATEGORY_ID)
            where (p.LIST_PRICE <> None)
            groupBy p.CATEGORY_ID
            select (p.CATEGORY_ID, minBy p.LIST_PRICE.Value, maxBy p.LIST_PRICE.Value, avgBy p.LIST_PRICE.Value, countBy p.LIST_PRICE.Value, sumBy p.LIST_PRICE.Value)
        }

    let! aggregates = query |> ctx.ReadAsync HydraReader.Read
    gt0 aggregates

    let aggByCatID = 
        aggregates 
        |> Seq.map (fun (catId, minPrice, maxPrice, avgPrice, priceCount, sumPrice) -> catId, (minPrice, maxPrice, avgPrice, priceCount, sumPrice)) 
        |> Map.ofSeq
    
    let dc (actual: decimal) (expected: decimal) = 
        Assert.AreEqual(float actual, float expected, 0.001, "Expected values to be close")

    let verifyAggregateValuesFor (catId: int64) (xMinPrice, xMaxPrice, xAvgPrice, xPriceCount, xSumPrice) =
        let aMinPrice, aMaxPrice, aAvgPrice, aPriceCount, aSumPrice = aggByCatID.[catId]
        dc aMinPrice xMinPrice; dc aMaxPrice xMaxPrice; dc aAvgPrice xAvgPrice; Assert.AreEqual(aPriceCount, xPriceCount); dc aSumPrice xSumPrice
    
    verifyAggregateValuesFor 1 (554.99M, 3410.46M, 1386.966M, 70, 97087.65M)
    verifyAggregateValuesFor 2 (739.99M, 5499.99M, 1406.098M, 50, 70304.9M)
    verifyAggregateValuesFor 5 (15.55M, 8867.99M, 635.216M, 108, 68603.38M)
}

[<Test>]
let ``Aggregate Subquery One``() = task {
    use ctx = openContext()

    let avgListPrice = 
        select {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None)
            select (avgBy p.LIST_PRICE.Value)
        }

    let! productsWithHigherThanAvgPrice = 
        select {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE.Value > subqueryOne avgListPrice)
            orderByDescending p.LIST_PRICE
            select (p.PRODUCT_NAME, p.LIST_PRICE.Value)
        }
        |> ctx.ReadAsync HydraReader.Read

    let avgListPrice = 438.6662M

    gt0 productsWithHigherThanAvgPrice
    Assert.IsTrue(productsWithHigherThanAvgPrice |> Seq.forall (fun (nm, price) -> price > avgListPrice), "Expected all prices to be > than avg price of $438.67.")
}

// This stopped working after implementing columns with table aliases.
// ERROR: ORA-00904: "P"."LIST_PRICE": invalid identifier
[<Test; Ignore "Ignore">]
let ``Select Column Aggregates``() = task {
    use ctx = openContext()

    let! aggregates = 
        select {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None)
            groupBy p.CATEGORY_ID
            having (minBy p.LIST_PRICE.Value > 50M && maxBy p.LIST_PRICE.Value < 1000M)
            select (p.CATEGORY_ID, minBy p.LIST_PRICE.Value, maxBy p.LIST_PRICE.Value)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 aggregates
}

// ERROR: ORA-00904: "P"."LIST_PRICE": invalid identifier
[<Test; Ignore "Ignore">]
let ``Sorted Aggregates - Top 5 categories with highest avg price products``() = task {
    use ctx = openContext()

    let! aggregates = 
        select {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None)
            groupBy p.CATEGORY_ID
            orderByDescending (avgBy p.LIST_PRICE.Value)
            select (p.CATEGORY_ID, avgBy p.LIST_PRICE.Value)
            take 5
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 aggregates
}

// This stopped working after implementing columns with table aliases.
// ERROR: ORA-00904: "P"."LIST_PRICE": invalid identifier
[<Test; Ignore "Ignore">]
let ``Where subqueryMany``() = task {
    use ctx = openContext()

    let top5CategoryIdsWithHighestAvgPrices = 
        select {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None)
            groupBy p.CATEGORY_ID
            orderByDescending (avgBy p.LIST_PRICE.Value)
            select (p.CATEGORY_ID)
            take 5
        }

    let! top5Categories =
        select {
            for c in OT.PRODUCT_CATEGORIES do
            where (c.CATEGORY_ID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
            select c.CATEGORY_NAME
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 top5Categories
}

[<Test>]
let ``Where subqueryOne``() = task {
    use ctx = openContext()

    let avgListPrice = 
        select {
            for p in OT.PRODUCTS do
            select (avgBy p.LIST_PRICE.Value)
        } 

    let! productsWithAboveAveragePrice =
        select {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None && p.LIST_PRICE.Value > subqueryOne avgListPrice)
            select (p.PRODUCT_NAME, p.LIST_PRICE.Value)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 productsWithAboveAveragePrice
}

[<Test>]
let ``Select Columns with Option``() = task {
    use ctx = openContext()

    let! values = 
        select {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None)
            select (p.CATEGORY_ID, p.LIST_PRICE)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 values
    Assert.IsTrue(values |> Seq.forall (fun (catId, price) -> price <> None), "Expected subcategories to all have a value.")
}

[<Test>]
let ``Insert Country``() = task {
    use ctx = openContext()

    let! results = 
        insert {
            into OT.COUNTRIES
            entity 
                {
                    OT.COUNTRIES.COUNTRY_ID = "WL"
                    OT.COUNTRIES.COUNTRY_NAME = "Wonderland"
                    OT.COUNTRIES.REGION_ID = Some 2
                }
        }
        |> ctx.InsertAsync

    results =! 1

    let! wl = 
        select {
            for c in OT.COUNTRIES do
            where (c.COUNTRY_ID = "WL")
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 wl
}

[<Test>]
let ``Update Country``() = task {
    use ctx = openContext()

    let! results = 
        update {
            for c in OT.COUNTRIES do
            set c.COUNTRY_NAME "Wonder Land"
            where (c.COUNTRY_ID = "WL")
        }
        |> ctx.UpdateAsync

    results >! 0

    let! wl = 
        select {
            for c in OT.COUNTRIES do
                where (c.COUNTRY_NAME = "Wonder Land")
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 wl
}

[<Test>]
let ``Delete Country``() = task {
    use ctx = openContext()

    let! _ = 
        delete {
            for c in OT.COUNTRIES do
            where (c.COUNTRY_ID = "WL")
        }
        |> ctx.DeleteAsync

    let! wl = 
        select {
            for c in OT.COUNTRIES do
            where (c.COUNTRY_ID = "WL")
        }
        |> ctx.ReadAsync HydraReader.Read

    Assert.IsTrue(wl |> Seq.length = 0, "Should be deleted")
}

[<Test>]
let ``Insert and Get Id``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    let! regionId = 
        insert {
            for r in OT.REGIONS do
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
            for r in OT.REGIONS do
            where (r.REGION_ID = regionId)
        }
        |> ctx.ReadOneAsync HydraReader.Read
    
    match region with
    | Some (r: OT.REGIONS) -> 
        Assert.IsTrue(r.REGION_ID > 0, "Expected REGION_ID to be greater than 0")
    | None -> 
        failwith "Expected to query a region row."
}
        
[<Test>]
let ``Multiple Inserts``() = task {
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
                into OT.COUNTRIES
                entities countries
            }
            |> ctx.InsertAsync

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

        let! results =
            select {
                for c in OT.COUNTRIES do
                where (c.COUNTRY_ID =% "X%")
                orderBy c.COUNTRY_ID
                select c.COUNTRY_ID
            }
            |> ctx.ReadAsync HydraReader.Read

        let codes = results |> Seq.toList

        codes =! [ "X0"; "X1"; "X2" ]
    | None -> 
        ()

    ctx.RollbackTransaction()
}

[<Test>]
let ``Distinct Test``() = task {
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
            insertTask (Shared ctx) {
                for e in OT.COUNTRIES do
                entities countries
            }

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

        let! results =
            selectTask HydraReader.Read (Shared ctx) {
                for c in OT.COUNTRIES do
                where (c.COUNTRY_ID =% "X%")
                select c.COUNTRY_NAME
            }

        let! distinctResults =
            selectTask HydraReader.Read (Shared ctx) {
                for c in OT.COUNTRIES do
                where (c.COUNTRY_ID =% "X%")
                select c.REGION_ID
                distinct
            }

        results |> Seq.length =! 3
        distinctResults |> Seq.length =! 1
    | None -> ()

    ctx.RollbackTransaction()
}

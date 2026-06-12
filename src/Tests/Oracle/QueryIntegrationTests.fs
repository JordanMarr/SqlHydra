module Oracle.``Query Integration Tests``

open Swensen.Unquote
open SqlHydra.Query
open SqlHydra.Query.OracleExtensions
open type SqlFn
open Oracle.ManagedDataAccess.Client
open NUnit.Framework
open DB

#if NET8_0
open Oracle.AdventureWorksNet8
#endif
#if NET9_0
open Oracle.AdventureWorksNet9
#endif
#if NET10_0
open Oracle.AdventureWorksNet10
#endif

[<Test>]
let ``Where Name Contains``() = task {
    let! addresses =
        selectTask db {
            for c in OT.CUSTOMERS do
            where (c.NAME |=| [ "ABC Corp"; "XYZ Ltd" ])
        }

    gt0 addresses
    Assert.IsTrue(addresses |> Seq.forall (fun a -> a.NAME = "ABC Corp" || a.NAME = "XYZ Ltd"), "Expected only 'ABC Corp' or 'XYZ Ltd'.")
}

[<Test>]
let ``Select with Timeout``() = task {
    use! ctx = db.OpenContextAsync()
    let q =
        select {
            for c in OT.CUSTOMERS do
            where (c.CUSTOMER_ID = 1L)
            timeout (System.TimeSpan.FromSeconds 45.0)
        }
    use cmd = ctx.BuildCommand(q.IR)
    cmd.CommandTimeout =! 45
}

[<Test>]
let ``Select Address Column Where Address Contains USA``() = task {
    let! cities =
        selectTask db {
            for c in OT.CUSTOMERS do
            where (c.ADDRESS =% "%USA")
            select c.ADDRESS
        }

    gt0 cities
    Assert.IsTrue(cities |> Seq.choose id |> Seq.forall (fun city -> city.Contains "USA"), "Expected all cities to contain 'USA'.")
}

[<Test>]
let ``Inner Join Orders-Details``() = task {
    let! results =
        selectAsync db {
            for o in OT.ORDERS do
            join d in OT.ORDER_ITEMS on (o.ORDER_ID = d.ORDER_ID)
            where (o.STATUS = "Shipped")
            select (o, d)
        }
    gt0 results
}

[<Test>]
let ``Product with Category name``() = task {
    let! rows = 
        selectAsync db {
            for p in OT.PRODUCTS do
            join c in OT.PRODUCT_CATEGORIES on (p.CATEGORY_ID = c.CATEGORY_ID)
            select (c.CATEGORY_NAME, p)
            take 5
        }
    gt0 rows
}

[<Test>]
let ``Select Column Aggregates From Product IDs 1-3``() = task {
    let! aggregates =
        selectAsync db {
            for p in OT.PRODUCTS do
            join c in OT.PRODUCT_CATEGORIES on (p.CATEGORY_ID = c.CATEGORY_ID)
            where (p.LIST_PRICE <> None)
            groupBy p.CATEGORY_ID
            select (p.CATEGORY_ID, minBy p.LIST_PRICE.Value, maxBy p.LIST_PRICE.Value, avgBy p.LIST_PRICE.Value, countBy p.LIST_PRICE.Value, sumBy p.LIST_PRICE.Value)
        }

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
    
    verifyAggregateValuesFor 1 (150.0M, 350.0M, 250.0M, 2, 500.0M)
    verifyAggregateValuesFor 2 (250.0M, 750.0M, 500.0M, 2, 1000.0M)
    verifyAggregateValuesFor 3 (200.0M, 200.0M, 200.0M, 1, 200.0M)
}

[<Test>]
let ``Aggregate Subquery One``() = task {
    let avgListPrice = 
        select {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None)
            select (avgBy p.LIST_PRICE.Value)
        }

    let! productsWithHigherThanAvgPrice = 
        selectAsync db {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE.Value > subqueryOne avgListPrice)
            orderByDescending p.LIST_PRICE
            select (p.PRODUCT_NAME, p.LIST_PRICE.Value)
        }

    let actualAvgListPrice = 340.0M // verified from running the avg query manually

    gt0 productsWithHigherThanAvgPrice
    Assert.IsTrue(productsWithHigherThanAvgPrice |> Seq.forall (fun (nm, price) -> price > actualAvgListPrice), "Expected all prices to be > than avg price of $340.00.")
}

// This stopped working after implementing columns with table aliases.
// ERROR: ORA-00904: "P"."LIST_PRICE": invalid identifier
[<Test; Ignore "Ignore">]
let ``Select Column Aggregates``() = task {
    let! aggregates = 
        selectAsync db {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None)
            groupBy p.CATEGORY_ID
            having (minBy p.LIST_PRICE.Value > 50M && maxBy p.LIST_PRICE.Value < 1000M)
            select (p.CATEGORY_ID, minBy p.LIST_PRICE.Value, maxBy p.LIST_PRICE.Value)
        }
    gt0 aggregates
}

// ERROR: ORA-00904: "P"."LIST_PRICE": invalid identifier
[<Test; Ignore "Ignore">]
let ``Sorted Aggregates - Top 5 categories with highest avg price products``() = task {
    let! aggregates =
        selectTask db {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None)
            groupBy p.CATEGORY_ID
            orderByDescending (avgBy p.LIST_PRICE.Value)
            select (p.CATEGORY_ID, avgBy p.LIST_PRICE.Value)
            take 5
        }

    gt0 aggregates
}

// This stopped working after implementing columns with table aliases.
// ERROR: ORA-00904: "P"."LIST_PRICE": invalid identifier
[<Test; Ignore "Ignore">]
let ``Where subqueryMany``() = task {
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
        selectTask db {
            for c in OT.PRODUCT_CATEGORIES do
            where (c.CATEGORY_ID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
            select c.CATEGORY_NAME
        }

    gt0 top5Categories
}

[<Test>]
let ``Where subqueryOne``() = task {
    let avgListPrice = 
        select {
            for p in OT.PRODUCTS do
            select (avgBy p.LIST_PRICE.Value)
        } 

    let! productsWithAboveAveragePrice =
        selectAsync db {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None && p.LIST_PRICE.Value > subqueryOne avgListPrice)
            select (p.PRODUCT_NAME, p.LIST_PRICE.Value)
        }

    gt0 productsWithAboveAveragePrice
}

[<Test>]
let ``Select Columns with Option``() = task {
    let! values =
        selectAsync db {
            for p in OT.PRODUCTS do
            where (p.LIST_PRICE <> None)
            select (p.CATEGORY_ID, p.LIST_PRICE)
        }

    gt0 values
    Assert.IsTrue(values |> Seq.forall (fun (catId, price) -> price <> None), "Expected subcategories to all have a value.")
}

[<Test>]
let ``Insert Country``() = task {
    use! shared = db.OpenContextAsync()

    let! results = 
        insertTask shared {
            into OT.COUNTRIES
            entity 
                {
                    OT.COUNTRIES.COUNTRY_ID = "WL"
                    OT.COUNTRIES.COUNTRY_NAME = "Wonderland"
                    OT.COUNTRIES.REGION_ID = Some 2
                }
        }

    results =! 1

    let! wl =
        selectTask shared {
            for c in OT.COUNTRIES do
            where (c.COUNTRY_ID = "WL")
        }

    gt0 wl
}

[<Test>]
let ``Update Country``() = task {
    use! shared = db.OpenContextAsync()

    let! results = 
        updateTask db {
            for c in OT.COUNTRIES do
            set c.COUNTRY_NAME "Wonder Land"
            where (c.COUNTRY_ID = "WL")
        }

    results >! 0

    let! wl =
        selectTask shared {
            for c in OT.COUNTRIES do
                where (c.COUNTRY_NAME = "Wonder Land")
        }

    gt0 wl
}

[<Test>]
let ``Delete Country``() = task {
    use! shared = db.OpenContextAsync()

    let! _ = 
        deleteTask db {
            for c in OT.COUNTRIES do
            where (c.COUNTRY_ID = "WL")
        }

    let! wl =
        selectTask db {
            for c in OT.COUNTRIES do
            where (c.COUNTRY_ID = "WL")
        }

    Assert.IsTrue(wl |> Seq.length = 0, "Should be deleted")
}

[<Test>]
let ``Insert and Get Id``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

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
        |> shared.InsertAsync

    let! region =
        selectTask shared {
            for r in OT.REGIONS do
            where (r.REGION_ID = regionId)
            tryHead
        }
    
    match region with
    | Some (r: OT.REGIONS) -> 
        Assert.IsTrue(r.REGION_ID > 0, "Expected REGION_ID to be greater than 0")
    | None -> 
        failwith "Expected to query a region row."
}
        
[<Test>]
let ``Multiple Inserts``() = task {
    use! shared = db.OpenContextAsync()

    shared.BeginTransaction()

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
            |> shared.InsertAsync

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

        let! results =
            selectTask shared {
                for c in OT.COUNTRIES do
                where (c.COUNTRY_ID =% "X%")
                orderBy c.COUNTRY_ID
                select c.COUNTRY_ID
            }

        let codes = results |> Seq.toList

        codes =! [ "X0"; "X1"; "X2" ]
    | None -> 
        ()

    shared.RollbackTransaction()
}

[<Test>]
let ``Distinct Test``() = task {
    use! shared = db.OpenContextAsync()

    shared.BeginTransaction()

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
            insertTask shared {
                for e in OT.COUNTRIES do
                entities countries
            }

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

        let! results =
            selectTask shared {
                for c in OT.COUNTRIES do
                where (c.COUNTRY_ID =% "X%")
                select c.COUNTRY_NAME
            }

        let! distinctResults =
            selectTask shared {
                for c in OT.COUNTRIES do
                where (c.COUNTRY_ID =% "X%")
                select c.REGION_ID
                distinct
            }

        results |> Seq.length =! 3
        distinctResults |> Seq.length =! 1
    | None -> ()

    shared.RollbackTransaction()
}

[<Test>]
let ``SqlFn - Oracle functions smoke test``() = task {
    let! results =
        selectTask db {
            for c in OT.CUSTOMERS do
            select (c.NAME, LENGTH c.NAME, UPPER c.NAME, NVL(c.WEBSITE, "N/A"))
            take 1
        }

    let name, len, upperName, website = results |> Seq.head
    Assert.That(len, Is.GreaterThan(0))
    Assert.That(upperName, Is.EqualTo(name.ToUpper()))
    Assert.That(website, Is.Not.Null)
}

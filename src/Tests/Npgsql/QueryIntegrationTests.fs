module Npgsql.``Query Integration Tests``

open Swensen.Unquote
open SqlHydra.Query
open SqlHydra.Query.NpgsqlExtensions
open NUnit.Framework
open System.Threading.Tasks
open DB
#if NET6_0
open Npgsql.AdventureWorksNet6
#endif
#if NET8_0
open Npgsql.AdventureWorksNet8
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.PostgresCompiler()
    let conn = new Npgsql.NpgsqlConnection(connectionString)
    conn.Open()
    new QueryContext(conn, compiler)

[<Test>]
let ``Where City Contains``() = task {
    use ctx = openContext()
            
    let addresses =
        select {
            for a in person.address do
            where (a.city |=| [ "Seattle"; "Santa Cruz" ])
        }
        |> ctx.Read HydraReader.Read

    gt0 addresses
    Assert.IsTrue(addresses |> Seq.forall (fun a -> a.city = "Seattle" || a.city = "Santa Cruz"), "Expected only 'Seattle' or 'Santa Cruz'.")
}

[<Test>]
let ``Select city Column Where city Starts with S``() = task {
    use ctx = openContext()

    let cities =
        select {
            for a in person.address do
            where (a.city =% "S%")
            select a.city
        }
        |> ctx.Read HydraReader.Read

    gt0 cities
    Assert.IsTrue(cities |> Seq.forall (fun city -> city.StartsWith "S"), "Expected all cities to start with 'S'.")
}

[<Test>]
let ``Inner Join Orders-Details``() = task {
    use ctx = openContext()

    let query =
        select {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            where o.onlineorderflag
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
            for p in production.product do
            join sc in production.productsubcategory on (p.productsubcategoryid = Some sc.productsubcategoryid)
            join c in production.productcategory on (sc.productcategoryid = c.productcategoryid)
            select (c.name, p)
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
            for p in production.product do
            where (p.productsubcategoryid <> None)
            groupBy p.productsubcategoryid
            where (p.productsubcategoryid.Value |=| [ 1; 2; 3 ])
            select (p.productsubcategoryid, minBy p.listprice, maxBy p.listprice, avgBy p.listprice, countBy p.listprice, sumBy p.listprice)
        }

    let! aggregates = query |> ctx.ReadAsync HydraReader.Read

    gt0 aggregates

    let aggByCatID = 
        aggregates 
        |> Seq.map (fun (catId, minPrice, maxPrice, avgPrice, priceCount, sumPrice) -> catId, (minPrice, maxPrice, avgPrice, priceCount, sumPrice)) 
        |> Map.ofSeq
    
    let dc (actual: decimal) (expected: decimal) = 
        Assert.AreEqual(float actual, float expected, 0.0001, "Expected values to be close")

    let verifyAggregateValuesFor (catId: int) (xMinPrice, xMaxPrice, xAvgPrice, xPriceCount, xSumPrice) =
        let aMinPrice, aMaxPrice, aAvgPrice, aPriceCount, aSumPrice = aggByCatID.[Some catId]
        dc aMinPrice xMinPrice; dc aMaxPrice xMaxPrice; dc aAvgPrice xAvgPrice; Assert.AreEqual(aPriceCount, xPriceCount); dc aSumPrice xSumPrice
    
    verifyAggregateValuesFor 1 (539.99M, 3399.99M, 1683.365M, 32, 53867.6800M)
    verifyAggregateValuesFor 2 (539.99M, 3578.2700M, 1597.4500M, 43, 68690.3500M)
    verifyAggregateValuesFor 3 (742.3500M, 2384.0700M, 1425.2481M, 22, 31355.4600M)
}

[<Test>]
let ``Aggregate Subquery One``() = task {
    use ctx = openContext()

    let avgListPrice = 
        select {
            for p in production.product do
            select (avgBy p.listprice)
        }

    let! productsWithHigherThanAvgPrice = 
        select {
            for p in production.product do
            where (p.listprice > subqueryOne avgListPrice)
            orderByDescending p.listprice
            select (p.name, p.listprice)
        }
        |> ctx.ReadAsync HydraReader.Read

    let avgListPrice = 438.6662M

    gt0 productsWithHigherThanAvgPrice
    Assert.IsTrue(productsWithHigherThanAvgPrice |> Seq.forall (fun (nm, price) -> price > avgListPrice), "Expected all prices to be > than avg price of $438.67.")
}

[<Test>]
let ``Select Column Aggregates``() = task {
    use ctx = openContext()

    let! aggregates = 
        select {
            for p in production.product do
            where (p.productsubcategoryid <> None)
            groupBy p.productsubcategoryid
            where (p.productsubcategoryid.Value |=| [ 1; 2; 3 ])
            select (p.productsubcategoryid, minBy p.listprice, maxBy p.listprice)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 aggregates
}

[<Test>]
let ``Sorted Aggregates - Top 5 categories with highest avg price products``() = task {
    use ctx = openContext()

    let! aggregates = 
        select {
            for p in production.product do
            where (p.productsubcategoryid <> None)
            groupBy p.productsubcategoryid
            orderByDescending (avgBy p.listprice)
            select (p.productsubcategoryid, avgBy p.listprice)
            take 5
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 aggregates
}

[<Test>]
let ``Where subqueryMany``() = task {
    use ctx = openContext()

    let top5CategoryIdsWithHighestAvgPrices = 
        select {
            for p in production.product do
            where (p.productsubcategoryid <> None)
            groupBy p.productsubcategoryid
            orderByDescending (avgBy p.listprice)
            select (p.productsubcategoryid)
            take 5
        }

    let! top5Categories =
        select {
            for c in production.productcategory do
            where (Some c.productcategoryid |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
            select c.name
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 top5Categories
}

[<Test>]
let ``Where subqueryOne``() = task {
    use ctx = openContext()

    let avgListPrice = 
        select {
            for p in production.product do
            select (avgBy p.listprice)
        } 

    let! productsWithAboveAveragePrice =
        select {
            for p in production.product do
            where (p.listprice > subqueryOne avgListPrice)
            select (p.name, p.listprice)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 productsWithAboveAveragePrice
}

[<Test>]
let ``Select Columns with Option``() = task {
    use ctx = openContext()

    let! values = 
        select {
            for p in production.product do
            where (p.productsubcategoryid <> None)
            select (p.productsubcategoryid, p.listprice)
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 values
    Assert.IsTrue(values |> Seq.forall (fun (catId, price) -> catId <> None), "Expected subcategories to all have a value.")
}

[<Test>]
let ``Insert Currency``() = task {
    use ctx = openContext()

    let! results = 
        insert {
            into sales.currency
            entity 
                {
                    sales.currency.currencycode = "BTC"
                    sales.currency.name = "BitCoin"
                    sales.currency.modifieddate = System.DateTime.Today
                }
        }
        |> ctx.InsertAsync

    results =! 1

    let! btc = 
        select {
            for c in sales.currency do
            where (c.currencycode = "BTC")
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 btc
}

[<Test>]
let ``Update Currency``() = task {
    use ctx = openContext()

    let! results = 
        update {
            for c in sales.currency do
            set c.name "BitCoinzz"
            where (c.currencycode = "BTC")
        }
        |> ctx.UpdateAsync

    results >! 0

    let! btc = 
        select {
            for c in sales.currency do
            where (c.name = "BitCoinzz")
        }
        |> ctx.ReadAsync HydraReader.Read

    gt0 btc
}

[<Test>]
let ``Delete Currency``() = task {
    use ctx = openContext()

    let! _ = 
        delete {
            for c in sales.currency do
            where (c.currencycode = "BTC")
        }
        |> ctx.DeleteAsync

    let! btc = 
        select {
            for c in sales.currency do
            where (c.currencycode = "BTC")
        }
        |> ctx.ReadAsync HydraReader.Read

    Assert.IsTrue(btc |> Seq.length = 0, "Should be deleted")
}

[<Test; Ignore "Ignore">]
let ``Insert and Get Id``() = task {
    use ctx = openContext()
            
    ctx.BeginTransaction()
    let! deletedCount =
        delete {
            for r in production.productreview do
            where (r.emailaddress = "gfisher@askjeeves.com")
        }
        |> ctx.DeleteAsync
    ctx.CommitTransaction()

    ctx.BeginTransaction()

    let! prodReviewId = 
        insertTask ctx {
            for r in production.productreview do
            entity 
                {
                    production.productreview.productreviewid = 9999 // PK
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

    let! review = 
        select {
            for r in production.productreview do
            where (r.reviewername = "Gary Fisher")
        }
        |> ctx.ReadOneAsync HydraReader.Read
            
    match review with
    | Some (rev : production.productreview) -> 
        Assert.IsTrue(prodReviewId > 0, "Expected productreviewid to be greater than 0")
    | None -> 
        failwith "Expected to query a review row."

    let! deletedCount = 
        delete {
            for r in production.productreview do
            where (r.productreviewid = prodReviewId)
        }
        |> ctx.DeleteAsync

    Assert.AreEqual(deletedCount, 1, "Expected exactly one review to be deleted")

    let! reviews = 
        select {
            for r in production.productreview do
            where (r.reviewername = "Gary Fisher")
        }
        |> ctx.ReadAsync HydraReader.Read

    Assert.AreEqual(reviews |> Seq.length, 0, "Expected no reviews to be queryable")
    ctx.CommitTransaction()
}

[<Test>]
let ``Multiple Inserts``() = task {
    use ctx = openContext()

    ctx.BeginTransaction()

    let currencies = 
        [ 0 .. 2 ] 
        |> List.map (fun i -> 
            {
                sales.currency.currencycode = $"BC{i}"
                sales.currency.name = "BitCoin"
                sales.currency.modifieddate = System.DateTime.Now
            }
        )
        |> AtLeastOne.tryCreate
    
    match currencies with
    | Some currencies ->
        let! rowsInserted = 
            insert {
                into sales.currency
                entities currencies
            }
            |> ctx.InsertAsync

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

        let! results =
            select {
                for c in sales.currency do
                where (c.currencycode =% "BC%")
                orderBy c.currencycode
                select c.currencycode
            }
            |> ctx.ReadAsync HydraReader.Read

        let codes = results |> Seq.toList
    
        codes =! [ "BC0"; "BC1"; "BC2" ]
    | None -> ()

    ctx.RollbackTransaction()
}

[<Test>]
let ``Distinct Test``() = task {
    use ctx = openContext()

    ctx.BeginTransaction()

    let currencies = 
        [ 0 .. 2 ] 
        |> List.map (fun i -> 
            {
                sales.currency.currencycode = $"BC{i}"
                sales.currency.name = "BitCoin"
                sales.currency.modifieddate = System.DateTime.Today
            }
        )
        |> AtLeastOne.tryCreate
    
    match currencies with
    | Some currencies ->
        let! rowsInserted = 
            insertTask ctx {
                for e in sales.currency do
                entities currencies
            }

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

        let! results =
            selectTask HydraReader.Read ctx {
                for c in sales.currency do
                where (c.currencycode =% "BC%")
                select c.name
            }

        let! distinctResults =
            selectTask HydraReader.Read ctx {
                for c in sales.currency do
                where (c.currencycode =% "BC%")
                select c.name
                distinct
            }

        results |> Seq.length =! 3
        distinctResults |> Seq.length =! 1
    | None -> 
        ()

    ctx.RollbackTransaction()
}

[<Test>]
let ``Insert, Update and Read npgsql provider specific db fields``() = task {
    use ctx = openContext ()
            
    let expectJsonEqual (dbValue: string) (jsonValue: string) err = 
        Assert.AreEqual(dbValue.Replace(" ", ""), jsonValue, err)
                
    let getRowById id =
        select {
            for e in ext.jsonsupport do
                select e
                where (e.id = id)
        } |> ctx.ReadAsync HydraReader.Read
                
    // Simple insert of one entity
    let jsonValue = """{"name":"test"}"""
    let entity': ext.jsonsupport =
        {
            id = 0
            json_field = jsonValue
            jsonb_field = jsonValue
        }
                
    let! insertedRowId = 
        insert {
            for e in ext.jsonsupport do
            entity entity'
            getId e.id
        }
        |> ctx.InsertAsync
                  
    let! selectedRows = getRowById insertedRowId
    match selectedRows |> Seq.tryHead with
    | Some row ->
        expectJsonEqual row.json_field jsonValue "Json field after insert doesn't match"
        expectJsonEqual row.jsonb_field jsonValue "Jsonb field after insert doesn't match"
    | None ->
        failwith "Expected Some"
     
    // Simple update of one entity
    let updatedJsonValue = """{"name":"test_2"}"""
    let! updatedRows =
        update {
                for e in ext.jsonsupport do
                set e.json_field updatedJsonValue
                set e.jsonb_field updatedJsonValue
                where (e.id = insertedRowId)
            }
            |> ctx.UpdateAsync
        
    Assert.AreEqual(updatedRows, 1, "Expected 1 row to be updated")
            
    let! selectedRowsAfterUpdate = getRowById insertedRowId
    match selectedRowsAfterUpdate |> Seq.tryHead with
    | Some row ->
        expectJsonEqual row.json_field  updatedJsonValue "Json field after update doesn't match"
        expectJsonEqual row.jsonb_field updatedJsonValue "Jsonb field after update doesn't match"
    | None -> 
        failwith "Expected Some"
                   
    let entities = [entity'; entity'] |> AtLeastOne.tryCreate

    match entities with
    | Some entities' ->
        // Insert of multiple entities
        let! insertedNumberOfRows = 
            insert {
                for e in ext.jsonsupport do
                entities entities'
            }
            |> ctx.InsertAsync
            
        Assert.AreEqual(insertedNumberOfRows, 2, "Failed insert multiple entities")
    | None -> 
        ()
}

[<Test; Ignore("This test works with npgsql v7, but fails with v8.")>]
let ``Enum Tests``() = task {
    //Npgsql.NpgsqlConnection.GlobalTypeMapper.MapEnum<experiments.mood>("experiments.mood") |> ignore

    use ctx = openContext ()

#if NET6_0
    (ctx.Connection :?> Npgsql.NpgsqlConnection)
        .TypeMapper.MapEnum<ext.mood>("ext.mood") |> ignore
#else
    // https://www.npgsql.org/doc/release-notes/7.0.html#managing-type-mappings-at-the-connection-level-is-no-longer-supported
    // https://www.npgsql.org/doc/release-notes/7.0.html#global-type-mappings-must-now-be-done-before-any-usage
    let dataSourceBuilder = NpgsqlDataSourceBuilder(DB.connectionString)
    //dataSourceBuilder.MapEnum<ext.mood>("ext.mood") |> ignore    
    dataSourceBuilder.MapEnum<ext.mood>() |> ignore    
#endif

    let! deleteResults =
        deleteTask ctx {
            for p in ext.person do
            deleteAll
        }

    let! insertResults = 
        insertTask ctx {
            into ext.person
            entity (
                { 
                    ext.person.name = "john doe"
                    ext.person.currentmood = ext.mood.ok
                }
            )
        }

    Assert.IsTrue(insertResults > 0, "Expected insert results > 0")

    let! query1Results = 
        selectTask HydraReader.Read ctx {
            for p in ext.person do
            select p
            toList
        } 

    let! updateResults = 
        updateTask ctx {
            for p in ext.person do
            set p.currentmood ext.mood.happy
            where (p.currentmood = ext.mood.ok)
        }

    Assert.IsTrue(updateResults > 0, "Expected update results > 0")

    let! query2Results = 
        selectTask HydraReader.Read ctx {
            for p in ext.person do
            select p
            toList
        } 

    query2Results |> List.forall (fun (p: ext.person) -> p.currentmood = ext.mood.happy) =! true
}

[<Test>]
let ``OnConflictDoUpdate``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    let upsertCurrency currency = 
        insertTask ctx {
            for c in sales.currency do
            entity currency
            onConflictDoUpdate c.currencycode (c.name, c.modifieddate)
        } :> Task

    let queryCurrency code = 
        select {
            for c in sales.currency do
            where (c.currencycode = code)
        }
        |> ctx.Read HydraReader.Read
        |> Seq.head

    let newCurrency = 
        { sales.currency.currencycode = "NEW"
        ; sales.currency.name = "New Currency"
        ; sales.currency.modifieddate = System.DateTime.Today }

    do! upsertCurrency newCurrency
    let query1 = queryCurrency "NEW"
    query1 =! newCurrency

    let editedCurrency = { query1 with name = "Edited Currency" }
            
    do! upsertCurrency editedCurrency
    let query2 = queryCurrency "NEW"
    query2 =! editedCurrency

    ctx.RollbackTransaction()
}

[<Test>]
let ``OnConflictDoNothing``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    let tryInsertCurrency currency = 
        insert {
            for c in sales.currency do
            entity currency
            onConflictDoNothing c.currencycode
        }   
        |> ctx.Insert
        |> ignore
            
    let queryCurrency code = 
        select {
            for c in sales.currency do
            where (c.currencycode = code)
        }
        |> ctx.Read HydraReader.Read
        |> Seq.head

    let newCurrency = 
        { sales.currency.currencycode = "NEW"
        ; sales.currency.name = "New Currency"
        ; sales.currency.modifieddate = System.DateTime.Today }

    tryInsertCurrency newCurrency
    let query1 = queryCurrency "NEW"
    query1 =! newCurrency

    let editedCurrency = { query1 with name = "Edited Currency" }
    tryInsertCurrency editedCurrency
    let query2 = queryCurrency "NEW"
    query2 =! newCurrency

    ctx.RollbackTransaction()
}

[<Test>]
let ``Query Employee Record with DateOnly``() = task {
    use ctx = openContext()
            
    let! employees =
        selectTask HydraReader.Read ctx {
            for e in humanresources.employee do
            select e
        }

    gt0 employees
}

[<Test>]
let ``Query Employee Column with DateOnly``() = task {
    use ctx = openContext()
            
    let! employeeBirthDates =
        selectTask HydraReader.Read ctx {
            for e in humanresources.employee do
            select e.birthdate
        }

    gt0 employeeBirthDates
}

[<Test>]
let ``Test Array Columns``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()

    let row = 
        { 
            ext.arrays.id = "Test Array Columns"
            ext.arrays.text_array = [| "one"; "two"; "three" |]
            ext.arrays.integer_array = [| 1; 2; 3 |]
        }

    let! insertResults = 
        insertTask ctx {
            into ext.arrays
            entity row
        }

    Assert.IsTrue(insertResults > 0, "Expected insert results > 0")

            
    let! query1Result = 
        selectTask HydraReader.Read ctx {
            for r in ext.arrays do
            select r
            tryHead
        } 
                            
    Assert.AreEqual(query1Result, Some row, "Expected query result to match inserted row.")

    let! query2Result = 
        selectTask HydraReader.Read ctx {
            for r in ext.arrays do
            select (r.integer_array, r.text_array)
            tryHead
        } 

    Assert.AreEqual(query2Result, Some (row.integer_array, row.text_array), "Expected to query individually selected array columns.")

    ctx.RollbackTransaction()
}

[<Test>]
let ``Update Employee DateOnly``() = task {
    use ctx = openContext()
    ctx.BeginTransaction()
            
    let! employees =
        selectTask HydraReader.Read ctx {
            for e in humanresources.employee do
            select e
        }

    gt0 employees

    let emp : humanresources.employee = employees |> Seq.head
    let birthDate = System.DateOnly(1980, 1, 1)

    let! result = 
        updateTask ctx {
            for e in humanresources.employee do
            set e.birthdate birthDate
            where (e.businessentityid = emp.businessentityid)
        }

    result =! 1

    let! refreshedEmp = 
        selectTask HydraReader.Read ctx {
            for e in humanresources.employee do
            where (e.businessentityid = emp.businessentityid)                    
            tryHead
        }

    let actualBirthDate = 
        (refreshedEmp : humanresources.employee option)
        |> Option.map (fun e -> e.birthdate)
            
    actualBirthDate =! Some birthDate
            
    ctx.RollbackTransaction()
}

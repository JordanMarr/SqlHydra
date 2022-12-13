module Npgsql.QueryIntegrationTests

open Expecto
open SqlHydra.Query
open DB
open SqlHydra.Query.NpgsqlExtensions
open Swensen.Unquote
#if NET5_0
open Npgsql.AdventureWorksNet5
#endif
#if NET6_0_OR_GREATER
open Npgsql.AdventureWorksNet6
#endif

let openContext() = 
    let compiler = SqlKata.Compilers.PostgresCompiler()
    let conn = new Npgsql.NpgsqlConnection(connectionString)
    conn.Open()
    new QueryContext(conn, compiler)

// Tables
let personTable =           table<person.person>
let addressTable =          table<person.address>
let customerTable =         table<sales.customer>
let orderHeaderTable =      table<sales.salesorderheader>
let orderDetailTable =      table<sales.salesorderdetail>
let productTable =          table<production.product>
let subCategoryTable =      table<production.productsubcategory>
let categoryTable =         table<production.productcategory>
let currencyTable =         table<sales.currency>
let productReviewTable =    table<production.productreview>
let employeeTable =         table<humanresources.employee>
let jsonSupportTable =      table<ext.jsonsupport>
let arraysTable =           table<ext.arrays>

[<Tests>]
let tests = 
    categoryList "Npgsql" "Query Integration Tests" [

        testTask "Where City Contains" {
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

            //query.ToKataQuery() |> toSql |> printfn "%s"

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
            //printfn "Results: %A" rows
            //query.ToKataQuery() |> toSql |> printfn "%s"
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
            //let sql = query.ToKataQuery() |> toSql 
            //sql |> printfn "%s"

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
                            sales.currency.modifieddate = System.DateTime.Today
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
            let! deletedCount =
                delete {
                    for r in productReviewTable do
                    where (r.emailaddress = "gfisher@askjeeves.com")
                }
                |> ctx.DeleteAsync
            ctx.CommitTransaction()

            ctx.BeginTransaction()

            let! prodReviewId = 
                insertTask (Shared ctx) {
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

        testTask "Multiple Inserts" {
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
                        into currencyTable
                        entities currencies
                    }
                    |> ctx.InsertAsync

                Expect.equal rowsInserted 3 "Expected 3 rows to be inserted"

                let! results =
                    select {
                        for c in currencyTable do
                        where (c.currencycode =% "BC%")
                        orderBy c.currencycode
                        select c.currencycode
                    }
                    |> ctx.ReadAsync HydraReader.Read

                let codes = results |> Seq.toList
    
                Expect.equal codes [ "BC0"; "BC1"; "BC2" ] ""
            | None -> ()

            ctx.RollbackTransaction()
        }

        testTask "Distinct Test" {
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
                    insertTask (Shared ctx) {
                        for e in currencyTable do
                        entities currencies
                    }

                Expect.equal rowsInserted 3 "Expected 3 rows to be inserted"

                let! results =
                    selectTask HydraReader.Read (Shared ctx) {
                        for c in currencyTable do
                        where (c.currencycode =% "BC%")
                        select c.name
                    }

                let! distinctResults =
                    selectTask HydraReader.Read (Shared ctx) {
                        for c in currencyTable do
                        where (c.currencycode =% "BC%")
                        select c.name
                        distinct
                    }

                Expect.equal (results |> Seq.length) 3 ""
                Expect.equal (distinctResults |> Seq.length) 1 ""
            | None -> ()

            ctx.RollbackTransaction()
        }
        
        testTask "Insert, Update and Read npgsql provider specific db fields" {
            use ctx = openContext ()
            
            let expectJsonEqual (dbValue: string) = Expect.equal (dbValue.Replace(" ", ""))
                
            let getRowById id =
                select {
                    for e in jsonSupportTable do
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
                    for e in jsonSupportTable do
                    entity entity'
                    getId e.id
                }
                |> ctx.InsertAsync
                  
            let! selectedRows = getRowById insertedRowId

            Expect.wantSome (selectedRows |> Seq.tryHead) "Select returned empty set"
            |> fun (row: ext.jsonsupport) ->
                 expectJsonEqual row.json_field jsonValue "Json field after insert doesn't match"
                 expectJsonEqual row.jsonb_field jsonValue "Jsonb field after insert doesn't match"
     
            // Simple update of one entity
            let updatedJsonValue = """{"name":"test_2"}"""
            let! updatedRows =
                update {
                        for e in jsonSupportTable do
                        set e.json_field updatedJsonValue
                        set e.jsonb_field updatedJsonValue
                        where (e.id = insertedRowId)
                    }
                    |> ctx.UpdateAsync
        
            Expect.equal updatedRows 1 "Expected 1 row to be updated"
            
            let! selectedRowsAfterUpdate = getRowById insertedRowId

            Expect.wantSome (selectedRowsAfterUpdate |> Seq.tryHead) "Select returned empty set"
            |> fun (row: ext.jsonsupport) ->
                 expectJsonEqual row.json_field  updatedJsonValue "Json field after update doesn't match"
                 expectJsonEqual row.jsonb_field updatedJsonValue "Jsonb field after update doesn't match"
                   
            let entities = [entity'; entity'] |> AtLeastOne.tryCreate

            match entities with
            | Some entities' ->
                // Insert of multiple entities
                let! insertedNumberOfRows = 
                    insert {
                        for e in jsonSupportTable do
                        entities entities'
                    }
                    |> ctx.InsertAsync
            
                Expect.equal insertedNumberOfRows 2 "Failed insert multiple entities"
            | None -> ()
        }

        testTask "Enum Tests" {
            let expPerson = table<ext.person> |> inSchema (nameof ext)

            //Npgsql.NpgsqlConnection.GlobalTypeMapper.MapEnum<experiments.mood>("experiments.mood") |> ignore

            use ctx = openContext ()
#if NET7_0
            failwith "TODO: NpgSql for .NET 7 no longer supports this."
#else
            (ctx.Connection :?> Npgsql.NpgsqlConnection)
                .TypeMapper.MapEnum<ext.mood>("ext.mood") |> ignore
#endif

            let! deleteResults =
                deleteTask (Shared ctx) {
                    for p in expPerson do
                    deleteAll
                }

            let! insertResults = 
                insertTask (Shared ctx) {
                    into expPerson
                    entity (
                        { 
                            ext.person.name = "john doe"
                            ext.person.currentmood = ext.mood.ok
                        }
                    )
                }

            Expect.isTrue (insertResults > 0) "Expected insert results > 0"

            let! query1Results = 
                selectTask HydraReader.Read (Shared ctx) {
                    for p in expPerson do
                    select p
                    toList
                } 

            let! updateResults = 
                updateTask (Shared ctx) {
                    for p in expPerson do
                    set p.currentmood ext.mood.happy
                    where (p.currentmood = ext.mood.ok)
                }

            Expect.isTrue (updateResults > 0) "Expected update results > 0"

            let! query2Results = 
                selectTask HydraReader.Read (Shared ctx) {
                    for p in expPerson do
                    select p
                    toList
                } 

            Expect.isTrue (
                query2Results 
                |> List.forall (fun (p: ext.person) -> 
                    p.currentmood = ext.mood.happy
                )
            ) ""
        }
        
        testTask "OnConflictDoUpdate" {
            use ctx = openContext()
            ctx.BeginTransaction()

            let upsertCurrency currency = 
                insertTask (Shared ctx) {
                    for c in currencyTable do
                    entity currency
                    onConflictDoUpdate c.currencycode (c.name, c.modifieddate)
                }

            let queryCurrency code = 
                select {
                    for c in currencyTable do
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

        testTask "OnConflictDoNothing" {
            use ctx = openContext()
            ctx.BeginTransaction()

            let tryInsertCurrency currency = 
                insert {
                    for c in currencyTable do
                    entity currency
                    onConflictDoNothing c.currencycode
                }   
                |> ctx.Insert
                |> ignore
            
            let queryCurrency code = 
                select {
                    for c in currencyTable do
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


        testTask "Query Employee Record with DateOnly" {
            use ctx = openContext()
            
            let! employees =
                selectTask HydraReader.Read (Shared ctx) {
                    for e in employeeTable do
                    select e
                }

            gt0 employees
        }
        
        testTask "Query Employee Column with DateOnly" {
            use ctx = openContext()
            
            let! employeeBirthDates =
                selectTask HydraReader.Read (Shared ctx) {
                    for e in employeeTable do
                    select e.birthdate
                }

            gt0 employeeBirthDates
        }

        testTask "Test Array Columns" {
            use ctx = openContext()
            ctx.BeginTransaction()

            let row = 
                { 
                    ext.arrays.id = "Test Array Columns"
                    ext.arrays.text_array = [| "one"; "two"; "three" |]
                    ext.arrays.integer_array = [| 1; 2; 3 |]
                }

            let! insertResults = 
                insertTask (Shared ctx) {
                    into arraysTable
                    entity row
                }

            Expect.isTrue (insertResults > 0) "Expected insert results > 0"

            
            let! query1Result = 
                selectTask HydraReader.Read (Shared ctx) {
                    for r in arraysTable do
                    select r
                    tryHead
                } 
                            
            Expect.equal query1Result (Some row) "Expected query result to match inserted row."

            let! query2Result = 
                selectTask HydraReader.Read (Shared ctx) {
                    for r in arraysTable do
                    select (r.integer_array, r.text_array)
                    tryHead
                } 

            Expect.equal query2Result (Some (row.integer_array, row.text_array)) "Expected to query individually selected array columns."

            ctx.RollbackTransaction()
        }

#if NET6_0_OR_GREATER
        testTask "Update Employee DateOnly" {
            use ctx = openContext()
            ctx.BeginTransaction()
            
            let! employees =
                selectTask HydraReader.Read (Shared ctx) {
                    for e in employeeTable do
                    select e
                }

            gt0 employees

            let emp : humanresources.employee = employees |> Seq.head
            let birthDate = System.DateOnly(1980, 1, 1)

            let! result = 
                updateTask (Shared ctx) {
                    for e in employeeTable do
                    set e.birthdate birthDate
                    where (e.businessentityid = emp.businessentityid)
                }

            Expect.isTrue (result = 1) "Should update exactly one record."

            let! refreshedEmp = 
                selectTask HydraReader.Read (Shared ctx) {
                    for e in employeeTable do
                    where (e.businessentityid = emp.businessentityid)                    
                    tryHead
                }

            let actualBirthDate = 
                (refreshedEmp : humanresources.employee option)
                |> Option.map (fun e -> e.birthdate)
            
            Expect.isTrue (actualBirthDate = Some birthDate) ""
            
            ctx.RollbackTransaction()
        }
#endif

    ]
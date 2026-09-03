module Npgsql.``Query Integration Tests``

open Swensen.Unquote
open SqlHydra.Query
open SqlHydra.Query.NpgsqlExtensions
open type SqlFn
open NUnit.Framework
open System.Threading.Tasks
open Npgsql.DB
#if NET8_0
open Npgsql.AdventureWorksNet8
#endif
#if NET9_0
open Npgsql.AdventureWorksNet9
#endif
#if NET10_0
open Npgsql.AdventureWorksNet10
#endif

// The connection-string overload auto-registers generated enums via Enums.register,
// so the enum tests below exercise the zero-config path end-to-end.
let db = QueryContextFactory.Create(connectionString, sqlLogger = printf "SQL: %O")

[<Test>]
let ``Where City Contains``() = task {
    let! addresses =
        selectTask db {
            for a in person.address do
            where (a.city |=| [ "Seattle"; "Santa Cruz" ])
        }

    gt0 addresses
    Assert.IsTrue(addresses |> Seq.forall (fun a -> a.city = "Seattle" || a.city = "Santa Cruz"), "Expected only 'Seattle' or 'Santa Cruz'.")
}

[<Test>]
let ``Select with Timeout``() = task {
    use! ctx = db.OpenContextAsync()
    let q =
        select {
            for a in person.address do
            where (a.addressid = 1)
            timeout (System.TimeSpan.FromSeconds 45.0)
        }
    use cmd = ctx.BuildCommand(q.IR)
    cmd.CommandTimeout =! 45
}

[<Test>]
let ``Select city Column Where city Starts with S``() = task {
    let! cities =
        selectTask db {
            for a in person.address do
            where (a.city =% "S%")
            select a.city
        }

    gt0 cities
    Assert.IsTrue(cities |> Seq.forall (fun city -> city.StartsWith "S"), "Expected all cities to start with 'S'.")
}

[<Test>]
let ``Inner Join Orders-Details``() = task {
    let! results =
        selectTask db {
            for o in sales.salesorderheader do
            join d in sales.salesorderdetail on (o.salesorderid = d.salesorderid)
            where o.onlineorderflag
            select (o, d)
        }

    gt0 results
}

[<Test>]
let ``Product with Category name``() = task {
    let! rows =
        selectTask db {
            for p in production.product do
            join sc in production.productsubcategory on (p.productsubcategoryid = Some sc.productsubcategoryid)
            join c in production.productcategory on (sc.productcategoryid = c.productcategoryid)
            select (c.name, p)
            take 5
        }

    gt0 rows
}

[<Test>]
let ``Select Column Aggregates From Product IDs 1-3``() = task {
    let! aggregates =
        selectTask db {
            for p in production.product do
            where (p.productsubcategoryid <> None)
            groupBy p.productsubcategoryid
            where (p.productsubcategoryid.Value |=| [ 1; 2; 3 ])
            select (p.productsubcategoryid, minBy p.listprice, maxBy p.listprice, avgBy p.listprice, countBy p.listprice, sumBy p.listprice)
        }

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
    let avgListPrice =
        select {
            for p in production.product do
            select (avgBy p.listprice)
        }

    let! productsWithHigherThanAvgPrice =
        selectTask db {
            for p in production.product do
            where (p.listprice > subqueryOne avgListPrice)
            orderByDescending p.listprice
            select (p.name, p.listprice)
        }

    let avgListPrice = 438.6662M

    gt0 productsWithHigherThanAvgPrice
    Assert.IsTrue(productsWithHigherThanAvgPrice |> Seq.forall (fun (nm, price) -> price > avgListPrice), "Expected all prices to be > than avg price of $438.67.")
}

[<Test>]
let ``Select Column Aggregates``() = task {
    let! aggregates =
        selectTask db {
            for p in production.product do
            where (p.productsubcategoryid <> None)
            groupBy p.productsubcategoryid
            where (p.productsubcategoryid.Value |=| [ 1; 2; 3 ])
            select (p.productsubcategoryid, minBy p.listprice, maxBy p.listprice)
        }

    gt0 aggregates
}

[<Test>]
let ``Sorted Aggregates - Top 5 categories with highest avg price products``() = task {
    let! aggregates =
        selectTask db {
            for p in production.product do
            where (p.productsubcategoryid <> None)
            groupBy p.productsubcategoryid
            orderByDescending (avgBy p.listprice)
            select (p.productsubcategoryid, avgBy p.listprice)
            take 5
        }

    gt0 aggregates
}

[<Test>]
let ``Where subqueryMany``() = task {
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
        selectTask db {
            for c in production.productcategory do
            where (Some c.productcategoryid |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
            select c.name
        }

    gt0 top5Categories
}

[<Test>]
let ``Where subqueryOne``() = task {
    let avgListPrice =
        select {
            for p in production.product do
            select (avgBy p.listprice)
        }

    let! productsWithAboveAveragePrice =
        selectTask db {
            for p in production.product do
            where (p.listprice > subqueryOne avgListPrice)
            select (p.name, p.listprice)
        }

    gt0 productsWithAboveAveragePrice
}

[<Test>]
let ``Select Columns with Option``() = task {
    let! values =
        selectTask db {
            for p in production.product do
            where (p.productsubcategoryid <> None)
            select (p.productsubcategoryid, p.listprice)
        }

    gt0 values
    Assert.IsTrue(values |> Seq.forall (fun (catId, price) -> catId <> None), "Expected subcategories to all have a value.")
}

[<Test>]
let ``Insert Currency``() = task {
    use! shared = db.OpenContextAsync()

    let! results =
        insertTask shared {
            for c in sales.currency do
            entity 
                {
                    sales.currency.currencycode = "BTC"
                    sales.currency.name = "BitCoin"
                    sales.currency.modifieddate = System.DateTime.Today
                }
        }

    results =! 1

    let! btc =
        selectTask shared {
            for c in sales.currency do
            where (c.currencycode = "BTC")
        }

    gt0 btc
}

[<Test>]
let ``Update Currency``() = task {
    use! shared = db.OpenContextAsync()

    let! results = 
        updateTask shared {
            for c in sales.currency do
            set c.name "BitCoinzz"
            where (c.currencycode = "BTC")
        }

    results >! 0

    let! btc =
        selectTask shared {
            for c in sales.currency do
            where (c.name = "BitCoinzz")
        }

    gt0 btc
}

[<Test>]
let ``Delete Currency``() = task {
    use! shared = db.OpenContextAsync()

    let! _ = 
        deleteAsync shared {
            for c in sales.currency do
            where (c.currencycode = "BTC")
        }

    let! btc =
        selectTask shared {
            for c in sales.currency do
            where (c.currencycode = "BTC")
        }

    Assert.IsTrue(btc |> Seq.length = 0, "Should be deleted")
}

[<Test>]
let ``Insert Network``() = task {
    use! shared = db.OpenContextAsync()

    let! results = 
        insertAsync shared {
            for c in network_sample.network_addresses do
            entity 
                {
                    network_sample.network_addresses.id = 0
                    network_sample.network_addresses.net_cidr = System.Net.IPNetwork.Parse("::ffff:1.2.3.0/120")
                    network_sample.network_addresses.net_inet = System.Net.IPAddress.Parse("127.0.0.2")
                    network_sample.network_addresses.net_macaddr = System.Net.NetworkInformation.PhysicalAddress.Parse("00-11-22-33-44-55")
                    network_sample.network_addresses.net_macaddr8 = System.Net.NetworkInformation.PhysicalAddress.Parse("00-11-22-33-44-55")
                }
            excludeColumn c.id
        }

    results =! 1

    let! ipAddr =
        selectTask shared {
            for c in network_sample.network_addresses do
            where (c.net_inet = System.Net.IPAddress.Parse "127.0.0.2")
        }

    gt0 ipAddr
}

[<Test; Ignore "Ignore">]
let ``Insert and Get Id``() = task {
    use! shared = db.OpenContextAsync()
            
    shared.BeginTransaction()
    let! deletedCount =
        deleteAsync shared {
            for r in production.productreview do
            where (r.emailaddress = "gfisher@askjeeves.com")
        }

    shared.CommitTransaction()

    shared.BeginTransaction()

    let! prodReviewId = 
        insertTask shared {
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
        selectTask shared {
            for r in production.productreview do
            where (r.reviewername = "Gary Fisher")
            tryHead
        }
            
    match review with
    | Some (rev : production.productreview) -> 
        Assert.IsTrue(prodReviewId > 0, "Expected productreviewid to be greater than 0")
    | None -> 
        failwith "Expected to query a review row."

    let! deletedCount = 
        deleteAsync shared {
            for r in production.productreview do
            where (r.productreviewid = prodReviewId)
        }

    Assert.AreEqual(deletedCount, 1, "Expected exactly one review to be deleted")

    let! reviews =
        selectTask shared {
            for r in production.productreview do
            where (r.reviewername = "Gary Fisher")
        }

    Assert.AreEqual(reviews |> Seq.length, 0, "Expected no reviews to be queryable")
    shared.CommitTransaction()
}

[<Test>]
let ``Multiple Inserts``() = task {
    use! shared = db.OpenContextAsync()

    shared.BeginTransaction()

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
            insertAsync shared {
                into sales.currency
                entities currencies
            }

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

        let! results =
            selectTask shared {
                for c in sales.currency do
                where (c.currencycode =% "BC%")
                orderBy c.currencycode
                select c.currencycode
            }

        let codes = results |> Seq.toList
    
        codes =! [ "BC0"; "BC1"; "BC2" ]
    | None -> ()

    shared.RollbackTransaction()
}

[<Test>]
let ``Distinct Test``() = task {
    use! shared = db.OpenContextAsync()

    shared.BeginTransaction()

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
            insertTask shared {
                for e in sales.currency do
                entities currencies
            }

        Assert.AreEqual(rowsInserted, 3, "Expected 3 rows to be inserted")

        let! results =
            selectTask shared {
                for c in sales.currency do
                where (c.currencycode =% "BC%")
                select c.name
            }

        let! distinctResults =
            selectTask shared {
                for c in sales.currency do
                where (c.currencycode =% "BC%")
                select c.name
                distinct
            }

        results |> Seq.length =! 3
        distinctResults |> Seq.length =! 1
    | None -> 
        ()

    shared.RollbackTransaction()
}

[<Test>]
let ``Insert, Update and Read npgsql provider specific db fields``() = task {
    use! shared = db.OpenContextAsync()
            
    let expectJsonEqual (dbValue: string) (jsonValue: string) err = 
        Assert.AreEqual(dbValue.Replace(" ", ""), jsonValue, err)
                
    let getRowById id =
        selectTask shared {
            for e in ext.jsonsupport do
            select e
            where (e.id = id)
        }
                
    // Simple insert of one entity
    let jsonValue = """{"name":"test"}"""
    let entity': ext.jsonsupport =
        {
            id = 0
            json_field = jsonValue
            jsonb_field = jsonValue
        }
                
    let! insertedRowId = 
        insertAsync shared {
            for e in ext.jsonsupport do
            entity entity'
            getId e.id
        }
                  
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
        updateTask shared {
            for e in ext.jsonsupport do
            set e.json_field updatedJsonValue
            set e.jsonb_field updatedJsonValue
            where (e.id = insertedRowId)
        }
        
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
            insertAsync shared {
                for e in ext.jsonsupport do
                entities entities'
            }
            
        Assert.AreEqual(insertedNumberOfRows, 2, "Failed insert multiple entities")
    | None -> 
        ()
}

[<Test>]
let ``Enum Tests``() = task {
    use! shared = db.OpenContextAsync()

    let! deleteResults =
        deleteTask shared {
            for p in ext.person do
            deleteAll
        }

    let! insertResults = 
        insertTask shared {
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
        selectTask shared {
            for p in ext.person do
            select p
            toList
        } 

    let! updateResults = 
        updateTask shared {
            for p in ext.person do
            set p.currentmood ext.mood.happy
            where (p.currentmood = ext.mood.ok)
        }

    Assert.IsTrue(updateResults > 0, "Expected update results > 0")

    let! query2Results = 
        selectTask shared {
            for p in ext.person do
            select p
            toList
        } 

    query2Results |> List.forall (fun (p: ext.person) -> p.currentmood = ext.mood.happy) =! true
}

[<Test>]
let ``OnConflictDoUpdate``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

    let upsertCurrency currency = 
        insertTask shared {
            for c in sales.currency do
            entity currency
            onConflictDoUpdate c.currencycode (c.name, c.modifieddate)
        } :> Task

    let queryCurrency code = task {
        let! results =
            selectTask shared {
                for c in sales.currency do
                where (c.currencycode = code)
            }
        return results |> Seq.head
    }

    let newCurrency =
        { sales.currency.currencycode = "NEW"
        ; sales.currency.name = "New Currency"
        ; sales.currency.modifieddate = System.DateTime.Today }

    do! upsertCurrency newCurrency
    let! query1 = queryCurrency "NEW"
    query1 =! newCurrency

    let editedCurrency = { query1 with name = "Edited Currency" }

    do! upsertCurrency editedCurrency
    let! query2 = queryCurrency "NEW"
    query2 =! editedCurrency

    shared.RollbackTransaction()
}

[<Test>]
let ``OnConflictDoNothing``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

    let tryInsertCurrency currency = 
        insertTask shared {
            for c in sales.currency do
            entity currency
            onConflictDoNothing c.currencycode
        } : Task
        
            
    let queryCurrency code = task {
        let! results =
            selectTask shared {
                for c in sales.currency do
                where (c.currencycode = code)
            }
        return results |> Seq.head
    }

    let newCurrency =
        { sales.currency.currencycode = "NEW"
        ; sales.currency.name = "New Currency"
        ; sales.currency.modifieddate = System.DateTime.Today }

    do! tryInsertCurrency newCurrency
    let! query1 = queryCurrency "NEW"
    query1 =! newCurrency

    let editedCurrency = { query1 with name = "Edited Currency" }
    do! tryInsertCurrency editedCurrency
    let! query2 = queryCurrency "NEW"
    query2 =! newCurrency

    shared.RollbackTransaction()
}

[<Test>]
let ``Query Employee Record with DateOnly``() = task {
    let! employees =
        selectTask db {
            for e in humanresources.employee do
            select e
        }

    gt0 employees
}

[<Test>]
let ``Query Employee Column with DateOnly``() = task {
    let! employeeBirthDates =
        selectTask db {
            for e in humanresources.employee do
            select e.birthdate
        }

    gt0 employeeBirthDates
}

[<Test>]
let ``Test Array Columns``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()

    let row = 
        { 
            ext.arrays.id = "Test Array Columns"
            ext.arrays.text_array = [| "one"; "two"; "three" |]
            ext.arrays.integer_array = [| 1; 2; 3 |]
        }

    let! insertResults = 
        insertTask shared {
            into ext.arrays
            entity row
        }

    Assert.IsTrue(insertResults > 0, "Expected insert results > 0")

            
    let! query1Result = 
        selectTask shared {
            for r in ext.arrays do
            select r
            tryHead
        } 
                            
    Assert.AreEqual(query1Result, Some row, "Expected query result to match inserted row.")

    let! query2Result = 
        selectTask shared {
            for r in ext.arrays do
            select (r.integer_array, r.text_array)
            tryHead
        } 

    Assert.AreEqual(query2Result, Some (row.integer_array, row.text_array), "Expected to query individually selected array columns.")

    shared.RollbackTransaction()
}

[<Test>]
let ``Update Employee DateOnly``() = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()
            
    let! employees =
        selectTask shared {
            for e in humanresources.employee do
            select e
        }

    gt0 employees

    let emp : humanresources.employee = employees |> Seq.head
    let birthDate = System.DateOnly(1980, 1, 1)

    let! result = 
        updateTask shared {
            for e in humanresources.employee do
            set e.birthdate birthDate
            where (e.businessentityid = emp.businessentityid)
        }

    result =! 1

    let! refreshedEmp = 
        selectTask shared {
            for e in humanresources.employee do
            where (e.businessentityid = emp.businessentityid)                    
            tryHead
        }

    let actualBirthDate = 
        (refreshedEmp : humanresources.employee option)
        |> Option.map (fun e -> e.birthdate)
            
    actualBirthDate =! Some birthDate

    shared.RollbackTransaction()
}

[<Test>]
let ``SqlFn - PostgreSQL functions smoke test``() = task {
    let! results =
        selectTask db {
            for p in person.person do
            where (p.firstname = "Ken")
            select (p.firstname, char_length p.firstname, upper p.firstname, coalesce(p.middlename, "N/A"))
            take 1
        }

    let firstName, len, upperName, middleName = results |> Seq.head
    firstName =! "Ken"
    len =! 3
    upperName =! "KEN"
    Assert.That(middleName, Is.Not.Null)
}

// The write record end to end: what the generator emits for a table with read-only columns,
// written through `writeEntity` and read back as the read record.

module WriteRecordFixture =
    module sales =
        /// Mirrors the DDL below. `disc` is NULL at price 20: a generated column that is also nullable.
        [<CLIMutable>]
        type sqlhydra_write_record =
            { id: int
              code: string
              price: decimal
              tax: decimal
              disc: Option<decimal> }
            interface SqlHydra.IHasWrite<sqlhydra_write_record_write> with
                member this.ToWrite() =
                    { code = this.code; price = this.price }
            interface SqlHydra.IWriteColumns with
                member this.WriteColumns =
                    [
                      { SqlHydra.WriteColumn.Name = "code"; Value = box this.code; ProviderDbType = None }
                      { SqlHydra.WriteColumn.Name = "price"; Value = box this.price; ProviderDbType = None }
                    ]

        and [<CLIMutable>] sqlhydra_write_record_write =
            { code: string
              price: decimal }
            interface SqlHydra.IWriteOf<sqlhydra_write_record> with
                member this.WriteColumns =
                    [
                      { SqlHydra.WriteColumn.Name = "code"; Value = box this.code; ProviderDbType = None }
                      { SqlHydra.WriteColumn.Name = "price"; Value = box this.price; ProviderDbType = None }
                    ]

    let rows = table<sales.sqlhydra_write_record>

    let row : sales.sqlhydra_write_record_write = { code = "a"; price = 10m }

    let ddl =
        """
        DROP TABLE IF EXISTS sales.sqlhydra_write_record;
        CREATE TABLE sales.sqlhydra_write_record (
            id    int GENERATED ALWAYS AS IDENTITY,
            code  text NOT NULL,
            price numeric NOT NULL,
            tax   numeric GENERATED ALWAYS AS (price * 0.1) STORED,
            disc  numeric GENERATED ALWAYS AS (nullif(price, 20) * 0.05) STORED
        );
        """

    let dropDdl = "DROP TABLE sales.sqlhydra_write_record;"

    let exec (ctx: QueryContext) (sql: string) = task {
        use cmd = ctx.Connection.CreateCommand()
        cmd.CommandText <- sql
        let! _ = cmd.ExecuteNonQueryAsync()
        ()
    }

    /// A context on a freshly created table, appending each statement's SQL to `sqlLog`.
    let openFresh (sqlLog: ResizeArray<string>) = task {
        let! ctx = db.OpenContextAsync()
        ctx.Logger <- fun compiled -> sqlLog.Add compiled.Sql
        do! exec ctx ddl
        return ctx
    }

    /// Seeds a row without going through the insert builder, so an update test stands on its own.
    let seed ctx (price: decimal) =
        exec ctx $"INSERT INTO sales.sqlhydra_write_record (code, price) VALUES ('seeded', {price});"

    let read ctx =
        selectTask ctx {
            for r in rows do
            orderBy r.id
            select r
        }

[<Test>]
let ``writeEntity: an insert names only the write record's fields, and the row reads back as the read record with the generated values``() = task {
    let sqlLog = ResizeArray<string>()
    use! ctx = WriteRecordFixture.openFresh sqlLog

    let! inserted =
        insertTask ctx {
            into WriteRecordFixture.rows
            writeEntity WriteRecordFixture.row
        }
    inserted =! 1
    (sqlLog |> Seq.exactlyOne) =! """INSERT INTO "sales"."sqlhydra_write_record" ("code", "price") VALUES (@p0, @p1)"""

    let! rows = WriteRecordFixture.read ctx
    let row = rows |> Seq.exactlyOne
    row.id =! 1
    row.code =! "a"
    row.price =! 10m
    row.tax =! 1.0m

    do! WriteRecordFixture.exec ctx WriteRecordFixture.dropDdl
}

[<Test>]
let ``writeEntity: an update sets only the write record's fields, with a where over the read record``() = task {
    let sqlLog = ResizeArray<string>()
    use! ctx = WriteRecordFixture.openFresh sqlLog
    do! WriteRecordFixture.seed ctx 10m

    // `r` is the read record: `id` has no field on the write record, and is still there to filter on.
    let! updated =
        updateTask ctx {
            for r in WriteRecordFixture.rows do
            writeEntity { WriteRecordFixture.row with price = 30m }
            where (r.id = 1)
        }
    updated =! 1
    (sqlLog |> Seq.exactlyOne)
        =! """UPDATE "sales"."sqlhydra_write_record" SET "code" = @p0, "price" = @p1 WHERE ("sales"."sqlhydra_write_record"."id" = @p2)"""

    let! rows = WriteRecordFixture.read ctx
    let row = rows |> Seq.exactlyOne
    row.price =! 30m
    row.tax =! 3.0m

    do! WriteRecordFixture.exec ctx WriteRecordFixture.dropDdl
}

[<Test>]
let ``entity: a row read back round-trips with one field changed, the database-owned columns dropped``() = task {
    use! ctx = WriteRecordFixture.openFresh (ResizeArray())
    do! WriteRecordFixture.seed ctx 10m
    let! seeded = WriteRecordFixture.read ctx
    let readRow = seeded |> Seq.exactlyOne

    let! updated =
        updateTask ctx {
            for r in WriteRecordFixture.rows do
            entity { readRow with price = 30m }
            where (r.id = readRow.id)
        }
    updated =! 1

    let! rows = WriteRecordFixture.read ctx
    (rows |> Seq.exactlyOne) =! { readRow with price = 30m; tax = 3.0m; disc = Some 1.5m }

    do! WriteRecordFixture.exec ctx WriteRecordFixture.dropDdl
}

[<Test>]
let ``toWrite: a row read back round-trips through writeEntity with one field changed``() = task {
    // The read record carries `id` and `tax`; `toWrite` drops them, so nothing is left to exclude.
    use! ctx = WriteRecordFixture.openFresh (ResizeArray())
    do! WriteRecordFixture.seed ctx 10m
    let! seeded = WriteRecordFixture.read ctx
    let readRow = seeded |> Seq.exactlyOne

    let! updated =
        updateTask ctx {
            for r in WriteRecordFixture.rows do
            writeEntity { toWrite readRow with price = 30m }
            where (r.id = readRow.id)
        }
    updated =! 1

    let! rows = WriteRecordFixture.read ctx
    (rows |> Seq.exactlyOne) =! { readRow with price = 30m; tax = 3.0m; disc = Some 1.5m }

    do! WriteRecordFixture.exec ctx WriteRecordFixture.dropDdl
}

[<Test>]
let ``writeEntity: getId returns the generated identity``() = task {
    // `getId` narrows the field list through `excludeColumn`, a second path to the write record's fields.
    use! ctx = WriteRecordFixture.openFresh (ResizeArray())

    let! newId =
        insertTask ctx {
            for r in WriteRecordFixture.rows do
            writeEntity WriteRecordFixture.row
            getId r.id
        }
    newId >! 0

    let! rows = WriteRecordFixture.read ctx
    (rows |> Seq.exactlyOne).id =! newId

    do! WriteRecordFixture.exec ctx WriteRecordFixture.dropDdl
}

[<Test>]
let ``writeEntities: inserts one row per write record``() = task {
    let sqlLog = ResizeArray<string>()
    use! ctx = WriteRecordFixture.openFresh sqlLog

    let! inserted =
        insertTask ctx {
            into WriteRecordFixture.rows
            writeEntities [ { WriteRecordFixture.row with code = "a" }; { WriteRecordFixture.row with code = "b" } ]
        }
    inserted =! 2
    (sqlLog |> Seq.exactlyOne) =! """INSERT INTO "sales"."sqlhydra_write_record" ("code", "price") VALUES (@p0, @p1), (@p2, @p3)"""

    let! rows = WriteRecordFixture.read ctx
    (rows |> Seq.map (fun r -> r.code) |> List.ofSeq) =! [ "a"; "b" ]

    do! WriteRecordFixture.exec ctx WriteRecordFixture.dropDdl
}

[<Test>]
let ``writeEntity: a nullable generated column reads back as Some where the database computed a value and None where it did not``() = task {
    // The write record has no field for `disc` at all; the read record has to carry both cases.
    use! ctx = WriteRecordFixture.openFresh (ResizeArray())

    let! _ = insertTask ctx { into WriteRecordFixture.rows; writeEntity { WriteRecordFixture.row with price = 10m } }
    let! _ = insertTask ctx { into WriteRecordFixture.rows; writeEntity { WriteRecordFixture.row with price = 20m } }

    let! rows = WriteRecordFixture.read ctx
    (rows |> Seq.map (fun r -> r.disc) |> List.ofSeq) =! [ Some 0.5m; None ]

    do! WriteRecordFixture.exec ctx WriteRecordFixture.dropDdl
}

// The typed DO UPDATE members end to end, on a table whose conflict column is unique.

module DoUpdateWriteFixture =
    module sales =
        /// Mirrors the DDL below. `note` is nullable so that a coalesce has something to keep.
        [<CLIMutable>]
        type sqlhydra_do_update_write =
            { id: int
              code: string
              price: decimal
              note: Option<string>
              tax: decimal }

        and [<CLIMutable>] sqlhydra_do_update_write_write =
            { code: string
              price: decimal
              note: Option<string> }
            interface SqlHydra.IWriteOf<sqlhydra_do_update_write> with
                member this.WriteColumns =
                    [
                      { SqlHydra.WriteColumn.Name = "code"; Value = box this.code; ProviderDbType = None }
                      { SqlHydra.WriteColumn.Name = "price"; Value = box this.price; ProviderDbType = None }
                      { SqlHydra.WriteColumn.Name = "note"; Value = box this.note; ProviderDbType = None }
                    ]

    let rows = table<sales.sqlhydra_do_update_write>

    let row : sales.sqlhydra_do_update_write_write = { code = "a"; price = 10m; note = None }

    let ddl =
        """
        DROP TABLE IF EXISTS sales.sqlhydra_do_update_write;
        CREATE TABLE sales.sqlhydra_do_update_write (
            id    int GENERATED ALWAYS AS IDENTITY,
            code  text NOT NULL UNIQUE,
            price numeric NOT NULL,
            note  text,
            tax   numeric GENERATED ALWAYS AS (price * 0.1) STORED
        );
        """

    let dropDdl = "DROP TABLE sales.sqlhydra_do_update_write;"

    /// A context on a freshly created table, appending each statement's SQL to `sqlLog`.
    let openFresh (sqlLog: ResizeArray<string>) = task {
        let! ctx = db.OpenContextAsync()
        ctx.Logger <- fun compiled -> sqlLog.Add compiled.Sql
        do! WriteRecordFixture.exec ctx ddl
        return ctx
    }

    /// The one statement the upserts so far have emitted, whitespace collapsed.
    let upsertSql (sqlLog: ResizeArray<string>) =
        System.Text.RegularExpressions.Regex.Replace(sqlLog |> Seq.distinct |> Seq.exactlyOne, @"\s+", " ").Trim()

    let readOne ctx = task {
        let! rows = selectTask ctx { for r in rows do select r }
        return rows |> Seq.exactlyOne
    }

[<Test>]
let ``doUpdateWrite: the upsert names only the write record's fields, and the conflicting row is updated``() = task {
    let sqlLog = ResizeArray<string>()
    use! ctx = DoUpdateWriteFixture.openFresh sqlLog

    let upsert (price: decimal) =
        insertTask ctx {
            for r in DoUpdateWriteFixture.rows do
            writeEntity { DoUpdateWriteFixture.row with price = price }
            onConflict r.code
            doUpdateWrite (fun w -> w.price)
        }
    let! inserted = upsert 10m
    let! updated = upsert 25m
    inserted =! 1
    updated =! 1
    DoUpdateWriteFixture.upsertSql sqlLog =! """INSERT INTO "sales"."sqlhydra_do_update_write" ("code", "price", "note") VALUES (@p0, @p1, @p2) ON CONFLICT(code) DO UPDATE SET price=EXCLUDED."price" ;"""

    let! row = DoUpdateWriteFixture.readOne ctx
    row.id =! 1
    row.price =! 25m
    row.tax =! 2.5m

    do! WriteRecordFixture.exec ctx DoUpdateWriteFixture.dropDdl
}

[<Test>]
let ``doUpdateCoalesceWrite: a null in the new row keeps the existing value where coalesced, and the rest is updated``() = task {
    let sqlLog = ResizeArray<string>()
    use! ctx = DoUpdateWriteFixture.openFresh sqlLog

    let upsert (price: decimal) (note: string option) =
        insertTask ctx {
            for r in DoUpdateWriteFixture.rows do
            writeEntity { DoUpdateWriteFixture.row with price = price; note = note }
            onConflict r.code
            doUpdateCoalesceWrite (fun w -> (w.price, w.note)) (fun w -> w.note)
        }
    let! inserted = upsert 10m (Some "kept")
    let! updated = upsert 25m None
    inserted =! 1
    updated =! 1
    DoUpdateWriteFixture.upsertSql sqlLog =! """INSERT INTO "sales"."sqlhydra_do_update_write" ("code", "price", "note") VALUES (@p0, @p1, @p2) ON CONFLICT(code) DO UPDATE SET "price" = EXCLUDED."price" ,"note" = COALESCE(EXCLUDED."note", "sqlhydra_do_update_write"."note") ;"""

    let! row = DoUpdateWriteFixture.readOne ctx
    row.price =! 25m
    row.note =! Some "kept"
    row.tax =! 2.5m

    do! WriteRecordFixture.exec ctx DoUpdateWriteFixture.dropDdl
}

[<Test>]
let ``onConflictDoUpdateWrite: the upsert names only the write record's fields, and the conflicting row is updated``() = task {
    let sqlLog = ResizeArray<string>()
    use! ctx = DoUpdateWriteFixture.openFresh sqlLog

    let upsert (price: decimal) =
        insertTask ctx {
            for r in DoUpdateWriteFixture.rows do
            writeEntity { DoUpdateWriteFixture.row with price = price }
            onConflictDoUpdateWrite r.code (fun w -> w.price)
        }
    let! inserted = upsert 10m
    let! updated = upsert 25m
    inserted =! 1
    updated =! 1
    DoUpdateWriteFixture.upsertSql sqlLog =! """INSERT INTO "sales"."sqlhydra_do_update_write" ("code", "price", "note") VALUES (@p0, @p1, @p2) ON CONFLICT(code) DO UPDATE SET price=EXCLUDED."price" ;"""

    let! row = DoUpdateWriteFixture.readOne ctx
    row.id =! 1
    row.price =! 25m
    row.tax =! 2.5m

    do! WriteRecordFixture.exec ctx DoUpdateWriteFixture.dropDdl
}

[<Test>]
let ``onConflictDoUpdateCoalesceWrite: a null in the new row keeps the existing value where coalesced, and the rest is updated``() = task {
    let sqlLog = ResizeArray<string>()
    use! ctx = DoUpdateWriteFixture.openFresh sqlLog

    let upsert (price: decimal) (note: string option) =
        insertTask ctx {
            for r in DoUpdateWriteFixture.rows do
            writeEntity { DoUpdateWriteFixture.row with price = price; note = note }
            onConflictDoUpdateCoalesceWrite r.code (fun w -> (w.price, w.note)) (fun w -> w.note)
        }
    let! inserted = upsert 10m (Some "kept")
    let! updated = upsert 25m None
    inserted =! 1
    updated =! 1
    DoUpdateWriteFixture.upsertSql sqlLog =! """INSERT INTO "sales"."sqlhydra_do_update_write" ("code", "price", "note") VALUES (@p0, @p1, @p2) ON CONFLICT(code) DO UPDATE SET "price" = EXCLUDED."price" ,"note" = COALESCE(EXCLUDED."note", "sqlhydra_do_update_write"."note") ;"""

    let! row = DoUpdateWriteFixture.readOne ctx
    row.price =! 25m
    row.note =! Some "kept"
    row.tax =! 2.5m

    do! WriteRecordFixture.exec ctx DoUpdateWriteFixture.dropDdl
}

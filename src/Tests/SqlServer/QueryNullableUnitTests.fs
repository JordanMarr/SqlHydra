module SqlServer.``Query Nullable Unit Tests``

open Swensen.Unquote
open SqlHydra.Query
open DB
open NUnit.Framework
open System

#if NET6_0
open SqlServer.AdventureWorksNullableNet6
#endif
#if NET8_0
open SqlServer.AdventureWorksNullableNet8
#endif

type OptionalBoolEntity = 
    {
        QuestionAnswered: bool Nullable
    }

[<Test>]
let ``Where bool option is true``() = 
    let sql = 
        select {
            for o in table<OptionalBoolEntity> do
            where o.QuestionAnswered.Value
        }
        |> toSql

    sql.Contains("WHERE ([o].[QuestionAnswered] = cast(1 as bit))") =! true

[<Test>]
let ``Where bool option is false``() = 
    let sql = 
        select {
            for o in table<OptionalBoolEntity> do
            where (not o.QuestionAnswered.Value)
        }
        |> toSql

    sql.Contains("WHERE ([o].[QuestionAnswered] = cast(0 as bit))") =! true

[<Test>]
let ``Where bool option is false or null``() = 
    let sql = 
        select {
            for o in table<OptionalBoolEntity> do
            where (not o.QuestionAnswered.Value || not o.QuestionAnswered.HasValue)
        }
        |> toSql

    sql.Contains("WHERE (([o].[QuestionAnswered] = cast(0 as bit)) OR ([o].[QuestionAnswered] IS NULL))") =! true

[<Test>]
let ``Where with Nullable Object Type``() = 
    let sql =  
        select {
            for a in Person.Address do
            where (a.AddressLine2 <> null)
        }
        |> toSql

    sql.Contains("IS NOT NULL") =! true

[<Test>]
let ``Where Customer |<>| List``() = 
    let sql =  
        select {
            for c in Sales.Customer do
            where (c.PersonID.Value |<>| [ 30018;29545;29954 ]) // should work with option values
        }
        |> toSql

    sql.Contains("WHERE ([c].[PersonID] NOT IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Nullable Property Value in Where``() = 
    let date = System.DateTime(2023,1,1)

    let sql =  
        select {
            for wo in Production.WorkOrder do
            where (wo.EndDate.HasValue = false || wo.EndDate.Value >= date)
        }
        |> toSql

    sql =! """SELECT * FROM [Production].[WorkOrder] AS [wo] WHERE (([wo].[EndDate] IS NULL) OR ([wo].[EndDate] >= @p0))"""

[<Test>]
let ``Nullable Property HasValue Not Null Check``() = 
    let date = System.DateTime(2023,1,1)

    let sql =  
        select {
            for wo in Production.WorkOrder do
            where (wo.EndDate.HasValue)
        }
        |> toSql

    sql =! """SELECT * FROM [Production].[WorkOrder] AS [wo] WHERE ([wo].[EndDate] IS NOT NULL)"""

[<Test>]
let ``Nullable Property HasValue Null Check``() = 
    let date = System.DateTime(2023,1,1)

    let sql =  
        select {
            for wo in Production.WorkOrder do
            where (not wo.EndDate.HasValue)
        }
        |> toSql

    sql =! """SELECT * FROM [Production].[WorkOrder] AS [wo] WHERE ([wo].[EndDate] IS NULL)"""

[<Test>]
let ``Where Static Property``() = 
    let sql = 
        select {
            for o in Sales.SalesOrderHeader do
            where (o.SalesOrderID = System.Int32.MaxValue)
        }
        |> toSql

    sql.Contains("WHERE ([o].[SalesOrderID] = @p0)") =! true

[<Test>]
let ``Delete Query with Where``() = 
    let sql =  
        delete {
            for c in Sales.Customer do
            where (c.CustomerID |<>| [ 30018;29545;29954 ])
        }
        |> toSql

    sql.Contains("DELETE FROM [Sales].[Customer]") =! true
    sql.Contains("WHERE ([Sales].[Customer].[CustomerID] NOT IN (@p0, @p1, @p2))") =! true

[<Test>]
let ``Delete All``() = 
    let sql =  
        delete {
            for c in Sales.Customer do
            deleteAll
        }
        |> toSql

    sql =! "DELETE FROM [Sales].[Customer]"

[<Test>]
let ``Update Query with Where``() = 
    let sql =  
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.AccountNumber = "000")
        }
        |> toSql

    sql =! "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0 WHERE ([Sales].[Customer].[AccountNumber] = @p1)"

[<Test>]
let ``Update Query with multiple Wheres``() = 
    let sql =  
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.AccountNumber = "000")
            where (c.CustomerID = 123)
        }
        |> toSql

    sql =! "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0 WHERE ([Sales].[Customer].[AccountNumber] = @p1 AND ([Sales].[Customer].[CustomerID] = @p2))"

[<Test>]
let ``Update Query with No Where``() = 
    let sql =  
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            updateAll
        }
        |> toSql

    sql =! "UPDATE [Sales].[Customer] SET [AccountNumber] = @p0"

[<Test>]
let ``Update should fail without where or updateAll``() = 
    try 
        let _ =  
            update {
                for c in Sales.Customer do
                set c.AccountNumber "123"
            }
        Assert.Fail("Should fail because no `where` or `updateAll` exists.")
    with ex ->
        Assert.Pass()

[<Test>]
let ``Update should pass because where exists``() = 
    try 
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.CustomerID = 1)
        }
        |> ignore
    with ex ->
        Assert.Fail()

[<Test>]
let ``Update should pass because updateAll exists``() = 
    try 
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            updateAll
        }
        |> ignore
    with ex ->
        Assert.Fail()

[<Test>]
let ``Update with where followed by updateAll should fail``() = 
    try
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            where (c.CustomerID = 1)
            updateAll
        }
        |> ignore
        Assert.Fail()
    with ex ->
        ()

[<Test>]
let ``Update with updateAll followed by where should fail``() = 
    try
        update {
            for c in Sales.Customer do
            set c.AccountNumber "123"
            updateAll
            where (c.CustomerID = 1)
        }
        |> ignore
        Assert.Fail()
    with ex ->
        Assert.Pass()

[<Test>]
let ``Insert Query without Identity``() = 
    let sql =  
        insert {
            into Sales.Customer
            entity 
                { 
                    Sales.Customer.AccountNumber = "123"
                    Sales.Customer.rowguid = System.Guid.NewGuid()
                    Sales.Customer.ModifiedDate = System.DateTime.Now
                    Sales.Customer.PersonID = Nullable()
                    Sales.Customer.StoreID = Nullable()
                    Sales.Customer.TerritoryID = Nullable()
                    Sales.Customer.CustomerID = 0
                }
        }
        |> toSql

    sql =! "INSERT INTO [Sales].[Customer] ([CustomerID], [PersonID], [StoreID], [TerritoryID], [AccountNumber], [rowguid], [ModifiedDate]) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)" 

[<Test>]
let ``Insert Query with Identity``() = 
    let sql =  
        insert {
            for c in Sales.Customer do
            entity 
                { 
                    Sales.Customer.AccountNumber = "123"
                    Sales.Customer.rowguid = System.Guid.NewGuid()
                    Sales.Customer.ModifiedDate = System.DateTime.Now
                    Sales.Customer.PersonID = Nullable()
                    Sales.Customer.StoreID = Nullable()
                    Sales.Customer.TerritoryID = Nullable()
                    Sales.Customer.CustomerID = 0
                }
            getId c.CustomerID
        }
        |> toSql

    sql =! "INSERT INTO [Sales].[Customer] ([PersonID], [StoreID], [TerritoryID], [AccountNumber], [rowguid], [ModifiedDate]) VALUES (@p0, @p1, @p2, @p3, @p4, @p5);SELECT scope_identity() as Id" 

# SqlHydra
SqlHydra is a suite of NuGet packages for working with databases in F# with an emphasis on type safety and convenience.

### Generation Tools
- [SqlHydra.SqlServer](#sqlhydrasqlserver-) is a dotnet tool that generates F# records for a SQL Server database.
- [SqlHydra.Npgsql](#sqlhydranpgsql-) is a dotnet tool that generates F# records for a PostgreSQL database
- [SqlHydra.Sqlite](#sqlhydrasqlite-) is a dotnet tool that generates F# records for a SQLite database.

### Query Library
- [SqlHydra.Query](#sqlhydraquery-) provides strongly typed Linq queries against generated types. 
        
#### Notes
- The generation tools can be used with any query library for creating strongly typed table records and data readers.
- SqlHydra.Query is designed to be used with SqlHydra generated types. (If you would prefer to create your own types over using generated types, then I would recommend checking out [Dapper.FSharp](https://github.com/Dzoukr/Dapper.FSharp) instead.)
- SqlHydra.Query uses [SqlKata](https://sqlkata.com/) to generate provider-specific SQL queries. SqlKata officially supports SQL Server, SQLite, PostgreSql, MySql, Oracle and Firebird; however, SqlHydra.Query does not yet have generators for MySql, Oracle and Firebird. Please submit an issue if you are interested in contributing a generator for one of these!
- _All SqlHydra NuGet packages will be released with matching major and minor version numbers._

## Contributing
* This project uses the vs-code Remote-Containers extension to spin up a dev environment that includes databases for running the Tests project.
* [Contributing Wiki](https://github.com/JordanMarr/SqlHydra/wiki/Contributing)

## SqlHydra.SqlServer [![NuGet version (SqlHydra.SqlServer)](https://img.shields.io/nuget/v/SqlHydra.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.SqlServer/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.SqlServer`

### Configure and Run

Run the tool from the command line (or add to a .bat|.cmd|.sh file):

```bat
dotnet sqlhydra-mssql
```

* The configuration wizard will ask you some questions, create a new [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration) file for you, and then run your new config.
* If a .toml configuration file already exists, it will run.
* The generated .fs file will automatically be added to your .fsproj as `Visible="false"`.

![hydra-console](https://user-images.githubusercontent.com/1030435/127790303-a69ca6ea-f0a7-4216-aa5d-c292b0dc3229.gif)

### Build Event (optional)

To regenerate after a Rebuild, you can run SqlHydra from an fsproj build event:

```bat
  <Target Name="SqlHydra" BeforeTargets="Clean">
    <Exec Command="dotnet sqlhydra-mssql" />
  </Target>
```

## SqlHydra.Npgsql [![NuGet version (SqlHydra.Npgsql)](https://img.shields.io/nuget/v/SqlHydra.Npgsql.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Npgsql/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.Npgsql`

### Configure / Run

Run the tool from the command line (or add to a .bat|.cmd|.sh file):

```bat
dotnet sqlhydra-npgsql
```

* The configuration wizard will ask you some questions, create a new [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration) file for you, and then run your new config.
* If a .toml configuration file already exists, it will run.
* The generated .fs file will automatically be added to your .fsproj as `Visible="false"`.

### Build Event (optional)
To regenerate after a Rebuild, you can run SqlHydra from an fsproj build event:

```bat
  <Target Name="SqlHydra" BeforeTargets="Clean">
    <Exec Command="dotnet sqlhydra-npgsql" />
  </Target>
```

## SqlHydra.Sqlite [![NuGet version (SqlHydra.Sqlite)](https://img.shields.io/nuget/v/SqlHydra.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Sqlite/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.Sqlite`

### Configure / Run

Run the tool from the command line (or add to a .bat|.cmd|.sh file):

```bat
dotnet sqlhydra-sqlite
```

* The configuration wizard will ask you some questions, create a new [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration) file for you, and then run your new config.
* If a .toml configuration file already exists, it will run.
* The generated .fs file will automatically be added to your .fsproj as `Visible="false"`.

### Build Event (optional)
To regenerate after a Rebuild, you can run SqlHydra from an fsproj build event:

```bat
  <Target Name="SqlHydra" BeforeTargets="Clean">
    <Exec Command="dotnet sqlhydra-sqlite" />
  </Target>
```

### Upgrading to .NET 6
If you are upgrading a previous version to a version that supports .NET 6 (SqlHydra.Sqlite v0.630.0 or above), you will need to manually update your `sqlhydra-sqlite.toml` configuration file. 

Change your `reader_type` from:
```
reader_type = "System.Data.IDataReader"
```
to:
```
reader_type = "System.Data.Common.DbDataReader"
```

This change is necessary to support the new .NET 6 `System.DateOnly` and `System.TimeOnly` types.
(Note that v0.630.0 and above will now use `System.Data.Common.DbDataReader` by default when generating a new .toml configuration file.)

## Example Output for AdventureWorks
```F#
// This code was generated by SqlHydra.SqlServer.
namespace SampleApp.AdventureWorks

module dbo =
    type ErrorLog =
        { ErrorLogID: int
          ErrorTime: System.DateTime
          UserName: string
          ErrorNumber: int
          ErrorMessage: string
          ErrorSeverity: Option<int>
          ErrorState: Option<int>
          ErrorProcedure: Option<string>
          ErrorLine: Option<int> }

    type BuildVersion =
        { SystemInformationID: byte
          ``Database Version``: string
          VersionDate: System.DateTime
          ModifiedDate: System.DateTime }

module SalesLT =
    type Address =
        { City: string
          StateProvince: string
          CountryRegion: string
          PostalCode: string
          rowguid: System.Guid
          ModifiedDate: System.DateTime
          AddressID: int
          AddressLine1: string
          AddressLine2: Option<string> }

    type Customer =
        { LastName: string
          PasswordHash: string
          PasswordSalt: string
          rowguid: System.Guid
          ModifiedDate: System.DateTime
          CustomerID: int
          NameStyle: bool
          FirstName: string
          MiddleName: Option<string>
          Title: Option<string>
          Suffix: Option<string>
          CompanyName: Option<string>
          SalesPerson: Option<string>
          EmailAddress: Option<string>
          Phone: Option<string> }
    
    // etc...
```


## Strongly Type Data Readers
The generated `HydraReader` class works in tandem with SqlHydra.Query for reading queried entities, but it can also be used on its own with any query library that returns an IDataReader.

* [Using HydraReader automatically with SqlHydra.Query](#sqlhydraquery-)
* [Using HydraReader manually with other query libraries](https://github.com/JordanMarr/SqlHydra/wiki/DataReaders)

## TOML Configuration Reference
* [View TOML Configuration Reference](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration)

## SqlHydra.Query [![NuGet version (SqlHydra.Query)](https://img.shields.io/nuget/v/SqlHydra.Query.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Query/)
SqlHydra.Query wraps the powerful [SqlKata](https://sqlkata.com/) query generator with F# computation expression builders for strongly typed query generation.
It can create queries for the following databases: SQL Server, SQLite, PostgreSql, MySql, Oracle, Firebird.
SqlHydra.Query can be used with any library that accepts a data reader; however, is designed pair well with SqlHydra generated records and readers! 

### Setup

```F#
open SqlHydra.Query

// Tables
let customerTable =         table<SalesLT.Customer>         |> inSchema (nameof SalesLT)
let customerAddressTable =  table<SalesLT.CustomerAddress>  |> inSchema (nameof SalesLT)
let addressTable =          table<SalesLT.Address>          |> inSchema (nameof SalesLT)
let productTable =          table<SalesLT.Product>          |> inSchema (nameof SalesLT)
let categoryTable =         table<SalesLT.ProductCategory>  |> inSchema (nameof SalesLT)
let errorLogTable =         table<dbo.ErrorLog>
```

```F#
/// Opens a connection and creates a QueryContext that will generate SQL Server dialect queries
let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)
```

### Select Builder

The following select queries will use the `HydraReader.Read` method generated by `SqlHydra.*` when the [Readers](#data-readers) option is selected.
`HydraReader.Read` infers the type generated by the query and uses the generated reader to hydrate the queried entities.

Selecting city and state columns only:
```F#
use ctx = openContext()

let cities =
    select {
        for a in addressTable do
        where (a.City = "Seattle")
        select (a.City, a.StateProvince)
    }
    |> ctx.Read HydraReader.Read
    |> List.map (fun (city, state) -> $"City, State: %s{city}, %s{state}")
```

_Special `where` filter operators:_
- `isIn` or `|=|`
- `isNotIn` or `|<>|`
- `like` or `=%`
- `notLike` or `<>%`
- `isNullValue` or `= None`
- `isNotNullValue` or `<> None`
- `subqueryMany`
- `subqueryOne`


Select `Address` entities where City starts with `S`:
```F#
let addresses =
    select {
        for a in addressTable do
        where (a.City =% "S%")
    }
    |> ctx.Read HydraReader.Read
```

#### Joins

Select top 10 `Product` entities with inner joined category name:
```F#
let! productsWithCategory = 
    select {
        for p in productTable do
        join c in categoryTable on (p.ProductCategoryID.Value = c.ProductCategoryID)
        select (p, c.Name)
        take 10
    }
    |> ctx.ReadAsync HydraReader.Read
```

Select `Customer` with left joined `Address` where `CustomerID` is in a list of values:
(Note that left joined tables will be of type `'T option`, so you will need to use the `.Value` property to access join columns.)

```F#
let! customerAddresses =
    select {
        for c in customerTable do
        leftJoin ca in customerAddressTable on (c.CustomerID = ca.Value.CustomerID)
        leftJoin a  in addressTable on (ca.Value.AddressID = a.Value.AddressID)
        where (c.CustomerID |=| [1;2;30018;29545]) // two without address, two with address
        orderBy c.CustomerID
        select (c, a)
    }
    |> ctx.ReadAsync HydraReader.Read
```

To perform a join with multi-columns, use tuples:

```F#
select {
    for o in orderHeaderTable do
    join d in orderDetailTable on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
    select o
}
```

#### Aggregates

_Aggregate functions (can be used in `select`, `having` and `orderBy` clauses):_
- `countBy`
- `sumBy`
- `minBy`
- `maxBy`
- `avgBy`

```F#
// Select categories with an avg product price > 500 and < 1000
select {
    for p in productTable do
    where (p.ProductCategoryID <> None)
    groupBy p.ProductCategoryID
    having (minBy p.ListPrice > 500M && maxBy p.ListPrice < 1000M)
    select (p.ProductCategoryID, minBy p.ListPrice, maxBy p.ListPrice)
}
|> ctx.Read HydraReader.Read
|> Seq.map (fun (catId, minPrice, maxPrice) -> $"CatID: {catId}, MinPrice: {minPrice}, MaxPrice: {maxPrice}")
|> Seq.iter (printfn "%s")
```

Alternative Row Count Query:
```F#
let! customersWithNoSalesPersonCount =
    select {
        for c in customerTable do
        where (c.SalesPerson = None)
        count
    }
    |> ctx.CountAsync
```

#### WHERE Subqueries

_Use the `subqueryMany` function for subqueries that return multiple rows for comparison:_

```F#
// Create a subquery that gets top 5 avg prices by category ID:
let top5CategoryIdsWithHighestAvgPrices = 
    select {
        for p in productTable do
        where (p.ProductCategoryID <> None)
        groupBy p.ProductCategoryID
        orderByDescending (avgBy p.ListPrice)
        select p.ProductCategoryID
        take 5
    }

// Get category names where the category ID is "IN" the subquery:
let top5Categories =
    select {
        for c in categoryTable do
        where (Some c.ProductCategoryID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
        select c.Name
    }
    |> ctx.ReadAsync HydraReader.Read
```

_Use the `subqueryOne` function for subqueries that return a single value for comparison:_

```F#
// Create a subquery that gets the avg list price (a single value):
let avgListPrice = 
    select {
        for p in productTable do
        select (avgBy p.ListPrice)
    } 

// Get products with a price > the average price
let productsWithAboveAveragePrice =
    select {
        for p in productTable do
        where (p.ListPrice > subqueryOne avgListPrice)
        select (p.Name, p.ListPrice)
    }
    |> ctx.ReadAsync HydraReader.Read
```

Distinct Query:
```F#
let! distinctCustomerNames = 
    select {
        for c in customerTable do
        select (c.FirstName, c.LastName)
        distinct
    }
    |> ctx.ReadAsync HydraReader.Read
```

### Dos and Don'ts

:boom: The `select` clause currently only supports tables and fields for the sake of modifying the generated SQL query and the returned query type `'T`.
Transformations (i.e. `.ToString()` or calling any functions is _not supported_ and will throw an exception.

:boom: The `where` clause will automatically parameterize your input values. _However_, similar to the `select` clause, the `where` clause does not support calling an transformations (i.e. `.ToString()`). So you must prepare any parameter transformations before the builder. 

✔️ CORRECT:
```F#
let city = getCity() // DO prepare where parameters above and then pass into the where clause

let cities =
    select {
        for a in addressTable do
        where (a.City = city)
        select (a.City, a.StateProvince)
    }
    |> ctx.Read HydraReader.Read
    |> List.map (fun (city, state) -> $"City: %s{city}, State: %s{state}") // DO transforms after data is queried
```

❌ INCORRECT:
```F#
let cities =
    select {
        for a in addressTable do
        where (a.City = getCity()) // DO NOT perform calculations or translations within the builder
        select ("City: " + a.City, "State: " + a.StateProvince) // DO NOT perform translations within the builder 
    }
    |> ctx.Read HydraReader.Read
    |> List.map (fun (city, state) -> $"%s{city}, %s{state}")
```

### Insert Builder

#### Simple Inserts
For simple inserts with no identity column and no included/excluded columns, use the `into _` syntax:

```F#
let rowsInserted = 
    insert {
        into personTable
        entity 
            {
                dbo.Person.ID = Guid.NewGuid()
                dbo.Person.FirstName = "Bojack"
                dbo.Person.LastName = "Horseman"
                dbo.Person.LastUpdated = DateTime.Now
            }
    }
    |> ctx.Insert

printfn "Rows inserted: %i" rowsInserted
```

#### Insert with an Identity Field
If you have an Identity column or if you want to specify columns to include/exclude, use the `for _ in _ do` syntax.
By default, all record fields will be included as insert values, so when using an identity column, you must handle it in one of two ways:
1) Mark it with `getId`. This will prevent it from being added as an insert value, and it will also select and return the identity field.
2) Mark it with `excludeColumn` to prevent it from being added as an insert value.

```F#

let errorLogID =
    insert {
        for e in errorLogTable do
        entity 
            {
                dbo.ErrorLog.ErrorLogID = 0 // Adding `getId` below will ignore this value.
                dbo.ErrorLog.ErrorTime = System.DateTime.Now
                dbo.ErrorLog.ErrorLine = None
                dbo.ErrorLog.ErrorMessage = "TEST"
                dbo.ErrorLog.ErrorNumber = 400
                dbo.ErrorLog.ErrorProcedure = (Some "Procedure 400")
                dbo.ErrorLog.ErrorSeverity = None
                dbo.ErrorLog.ErrorState = None
                dbo.ErrorLog.UserName = "jmarr"
            }
        getId e.ErrorLogID
    }
    |> ctx.Insert

printfn "ErrorLogID Identity: %i" errorLogID
```

#### Multiple Inserts
To insert multiple entities in one query, use the `entities` operation in conjunction with the `AtLeastOne` type to ensure that at least one item exists in the collection. (The `AtLeastOne` forces you to handle the case where an empty collection is passed to `entities` which would throw a runtime exception.)

NOTE: `getId` is not supported for multiple inserts with `entities`! So if you are inserting multiple entities that have an identity field, you must use `excludeColumn` on the identity column.

```F#
let currenciesMaybe = 
    [ 0..2 ] 
    |> List.map (fun i -> 
        {
            Sales.Currency.CurrencyCode = $"BC{i}"
            Sales.Currency.Name = "BitCoin"
            Sales.Currency.ModifiedDate = System.DateTime.Now
        }
    )
    |> AtLeastOne.tryCreate

match currenciesMaybe with
| Some currencies ->
    let! rowsInserted = 
        insert {
            into currencyTable
            entities currencies
        }
        |> ctx.InsertAsync
| None ->
    printfn "Skipping insert because entities seq was empty."
```

### Update Builder

#### Update Individual Fields
To update individual columns, use the `set` operation.

```F#
let rowsUpdated = 
    update {
        for e in errorLogTable do
        set e.ErrorNumber 123
        set e.ErrorMessage "ERROR #123"
        set e.ErrorLine (Some 999)
        set e.ErrorProcedure None
        where (e.ErrorLogID = 1)
    }
    |> ctx.Update
```

#### Update Entire Record
To update an entire record, use the `entity` operation.
You may optionally use `includeColumn` to specify an allow list of one or more columns on the record to include in the update.
You may optionally use `excludeColum` to specify a deny list of one or more columns on the record to exclude from the update.
NOTE: You may use `includeColumn` or `excludeColumn` multiple times - once for each column to include/exclude.

```F#
let rowsUpdated = 
    update {
        for e in errorLogTable do
        entity 
            {
                dbo.ErrorLog.ErrorLogID = 0 // Add `excludeColumn` below to ignore an identity column
                dbo.ErrorLog.ErrorTime = System.DateTime.Now
                dbo.ErrorLog.ErrorLine = None
                dbo.ErrorLog.ErrorMessage = "TEST"
                dbo.ErrorLog.ErrorNumber = 400
                dbo.ErrorLog.ErrorProcedure = (Some "Procedure 400")
                dbo.ErrorLog.ErrorSeverity = None
                dbo.ErrorLog.ErrorState = None
                dbo.ErrorLog.UserName = "jmarr"
            }
        excludeColumn e.ErrorLogID // Exclude the identity column
        where (e.ErrorLogID = errorLog.ErrorLogID)
    }
    |> ctx.Update
```

If you want to apply an update to all records in a table, you must use the `updateAll` keyword or else it will throw an exception (it's a safety precaution that may save you some trouble. 😊):
```F#
update {
    for c in customerTable do
    set c.AccountNumber "123"
    updateAll
}
```

### Delete Builder

```F#
let rowsDeleted = 
    delete {
        for e in errorLogTable do
        where (e.ErrorLogID = 5)
    }
    |> ctx.Delete

printfn "Rows deleted: %i" rowsDeleted
```

If you want to delete all records in a table, you must use the `deleteAll` keyword in lieu of a `where` statement or else it will not compile:
```F#
delete {
    for c in customerTable do
    deleteAll
}
```

## .NET 5 and .NET 6
Both .NET 5 and .NET 6 are now supported!

### .NET 6
All generators now support the new .NET 6 `System.DateOnly` and `System.TimeOnly` fields.
(Note that if you are upgrading SqlHydra.Sqlite from .NET 5 to .NET 6, please refer to the [SqlHydra.Sqlite](#sqlhydrasqlite-) section for special instructions.)

### .NET 5
If you have .NET 5 and .NET 6 installed side-by-side but you want to continue generating using .NET 5 (meaning you don't want your generated code to utilize the new `System.DateOnly` and `System.TimeOnly` types, you can add a `global.json` file to your project folder with the following:

```json
{
  "sdk": {
    "version": "5.0.0",
    "rollForward": "latestFeature"
  }
}
```




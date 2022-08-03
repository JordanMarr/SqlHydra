# SqlHydra
SqlHydra is a suite of NuGet packages for working with databases in F# with an emphasis on type safety and convenience.

### Generation Tools
- [SqlHydra.SqlServer](#sqlhydrasqlserver-) is a dotnet tool that generates F# records for a SQL Server database.
- [SqlHydra.Npgsql](#sqlhydranpgsql-) is a dotnet tool that generates F# records for a PostgreSQL database.
- [SqlHydra.Oracle](#sqlhydraoracle-) is a dotnet tool that generates F# records for an Oracle database.
- [SqlHydra.Sqlite](#sqlhydrasqlite-) is a dotnet tool that generates F# records for a SQLite database.

### Query Library
- [SqlHydra.Query](#sqlhydraquery-) provides strongly typed Linq queries against generated types. 
        
#### Notes
- The generation tools can be used alone or with any query library for creating strongly typed table records and data readers.
- SqlHydra.Query is designed to be used with SqlHydra generated types. (If you would prefer to create your own types over using generated types, then I would recommend checking out [Dapper.FSharp](https://github.com/Dzoukr/Dapper.FSharp).)
- SqlHydra.Query uses [SqlKata](https://sqlkata.com/) internally to generate provider-specific SQL queries.
- _All SqlHydra NuGet packages will be released with matching major and minor version numbers._

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tr>
    <td align="center"><a href="https://github.com/MargaretKrutikova"><img src="https://avatars.githubusercontent.com/u/5932274?v=4?s=100" width="100px;" alt=""/>
        <br /><sub><b>MargaretKrutikova</b></sub></a><br /><a href="https://github.com/JordanMarr/SqlHydra/pull/10" title="Code">💻</a>
    </td>
    <td align="center"><a href="https://github.com/Jmaharman"><img src="https://avatars.githubusercontent.com/u/215359?v=4?s=100" width="100px;" alt=""/>
        <br /><sub><b>Jmaharman</b></sub></a><br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=Jmaharman" title="Code">💻</a>
    </td>
    <td align="center"><a href="https://github.com/ntwilson"><img src="https://avatars.githubusercontent.com/u/15835006?v=4?s=100" width="100px;" alt=""/>
        <br /><sub><b>ntwilson</b></sub></a><br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=ntwilson" title="Code">💻</a>
    </td>
  </tr>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!


## Contributing
* This project uses the vs-code Remote-Containers extension to spin up a dev environment that includes databases for running the Tests project.
* Alternatively, you can manually run the docker-compose file to load the development databases along with your IDE of choice.
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
* You can filter the generated schemas by manually editing the generated [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration) file.

![hydra-console](https://user-images.githubusercontent.com/1030435/127790303-a69ca6ea-f0a7-4216-aa5d-c292b0dc3229.gif)

### Build Event (optional)

To regenerate after a Rebuild, you can run SqlHydra from an fsproj build event:

```bat
  <Target Name="SqlHydra" BeforeTargets="Clean">
    <Exec Command="dotnet sqlhydra-mssql" />
  </Target>
```

### Troubleshooting

The following exception may occur with the latest version of `Microsoft.Data.SqlClient`:
```
Microsoft.Data.SqlClient.SqlException (0x80131904): 
A connection was successfully established with the server, but then an error occurred during the login process. 
(provider: SSL Provider, error: 0 - The certificate chain was issued by an authority that is not trusted.)
```

The most simple way to resolve this is to append `;TrustServerCertificate=True` to the connection string in your .toml configuration file.

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
* You can filter the generated schemas by manually editing the generated [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration) file.

### Build Event (optional)
To regenerate after a Rebuild, you can run SqlHydra from an fsproj build event:

```bat
  <Target Name="SqlHydra" BeforeTargets="Clean">
    <Exec Command="dotnet sqlhydra-npgsql" />
  </Target>
```

### Support for Postgres Enums
Postgres enum types are generated as CLR enums!
You will, however, still need to manually "register" your custom enums via `NpgsqlConnection.GlobalTypeMapper.MapEnum` method.
See Npgsql docs on mapping enums [here](https://www.npgsql.org/doc/types/enums_and_composites.html).

Npgsql Mapping Example:
```F#
// Global mapping should occur only once at startup:
// `experiments.mood` is the generated enum, and "experiments.mood" is the "{schema}.{enum}".
Npgsql.NpgsqlConnection.GlobalTypeMapper.MapEnum<experiments.mood>(nameof experiments.mood) |> ignore
```

💥 SqlHydra.Npgsql 1.0.0 uses Npgsql v6.0.3 which has a bug that throws an exception during SqlHydra.Npgsql code generation if an enum exists an a schema other than `public`.
This bug has been fixed and is slated for v6.0.4. The workaround until then is to ensure that enums in non `public` schemas are also added to `public`.

✔️ SqlHydra.Npgsql 1.0.1 uses Npgsql v6.0.4 which fixes the bug that causes generation to fail if an enum exists in a schema other than `public`.

## SqlHydra.Oracle [![NuGet version (SqlHydra.Oracle)](https://img.shields.io/nuget/v/SqlHydra.Oracle.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Oracle/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.Oracle`

### Configure / Run

Run the tool from the command line (or add to a .bat|.cmd|.sh file):

```bat
dotnet sqlhydra-oracle
```

* The configuration wizard will ask you some questions, create a new [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration) file for you, and then run your new config.
* If a .toml configuration file already exists, it will run.
* The generated .fs file will automatically be added to your .fsproj as `Visible="false"`.
* You can filter the generated schemas by manually editing the generated [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration) file.

### Build Event (optional)
To regenerate after a Rebuild, you can run SqlHydra from an fsproj build event:

```bat
  <Target Name="SqlHydra" BeforeTargets="Clean">
    <Exec Command="dotnet sqlhydra-oracle" />
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
* You can filter the generated schemas by manually editing the generated [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration) file.

### Build Event (optional)
To regenerate after a Rebuild, you can run SqlHydra from an fsproj build event:

```bat
  <Target Name="SqlHydra" BeforeTargets="Clean">
    <Exec Command="dotnet sqlhydra-sqlite" />
  </Target>
```

### Sqlite Data Type Aliases
Sqlite stores all data as either an `INTEGER`, `REAL`, `TEXT` or `BLOB` type.
Fortunately, you can also use aliases for data types more commonly used in other databases in your table definitions and Sqlite will translate them to the appropriate type.
Using these type aliases also allows `SqlHydra.Sqlite` to generate the desired .NET CLR property type.

Here is a list of valid data type aliases (or "affinity names"):
https://www.sqlite.org/datatype3.html#affinity_name_examples

### Upgrading to .NET 6
If you are upgrading SqlHydra.Sqlite to a version that supports .NET 6 (SqlHydra.Sqlite v0.630.0 or above), you will need to manually update your `sqlhydra-sqlite.toml` configuration file. 

Change `reader_type` from:
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

## Generating Multiple TOML Files

It is also possible to have more than one .toml file in the same project. By default, SqlHydra will create a .toml file named after the version of SqlHydra used.
For example, running `dotnet sqlhydra-sqlite` will generate `sqlhydra-sqlite.toml`. 

However, you can also specify a name for your .toml file: `dotnet sqlhydra-sqlite "Shared.toml"`
This can be useful for various use cases, such as:
* data migrations where you want to generate types for a source and a target database.
* generating record types with different schema/table filters in separate files.


## Supported Frameworks
Both .NET 5 and .NET 6 are now supported.
(.NET 5 will be supported until Microsoft ends official support.)

### .NET 6
The new .NET 6 `System.DateOnly` and `System.TimeOnly` types are now supported by all generators.
(Note that if you are upgrading SqlHydra.Sqlite from .NET 5 to .NET 6, please refer to the [SqlHydra.Sqlite](#sqlhydrasqlite-) section for special instructions.)

### .NET 5
If you have .NET 5 and .NET 6 installed side-by-side but you want to continue generating types using .NET 5 (meaning you do not want your generated types to utilize the new `System.DateOnly` and `System.TimeOnly` types), you can add a `global.json` file to your project folder with the following:

```json
{
  "sdk": {
    "version": "5.0.0",
    "rollForward": "latestFeature"
  }
}
```

## SqlHydra.Query [![NuGet version (SqlHydra.Query)](https://img.shields.io/nuget/v/SqlHydra.Query.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Query/)
SqlHydra.Query wraps the powerful [SqlKata](https://sqlkata.com/) query generator with F# computation expression builders for strongly typed query generation.
SqlHydra.Query can be used with any library that accepts a data reader; however, is designed pair well with SqlHydra generated records and readers! 

### Setup Tables

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

There are three select builders:
* `selectTask` - creates a self-executing query that returns a Task<'T> of query results
* `selectAsync` - creates a self-executing query that returns an Async<'T> of query results
* `select` - creates a query (this is mostly used for creating subqueries)

All three select query builders must be passed the generated `HydraReader.Read` static method (which is generated by `SqlHydra.*` when the ["Generate HydraReader?"](#data-readers) option is selected).

The `selectTask` and `selectAsync` builders must also be passed a `QueryType` which is a discriminated union that allows the user to specify the scope of the `QueryContext` (which manages the `DbConnection` and executes the various types of queries). `QueryType` allows for the following options:
* `QueryType.Create of unit -> QueryContext` - this takes a function that returns a new `QueryContext`. This option will create its own `QueryContext` and `DbConnection` automatically, execute the query and then dispose them. This is very useful because it allows you to create a simple data function that executes a query without the need of manually instantiating the `QueryContext`, executing the query and then disposing (which also necessitates wrapping everything in a `task` or `async` block to ensure that the connection isn't prematurely disposed). The end result is a much cleaner data function that doesn't need to be wrapped in a `task` or `async` block!
* `QueryType.Shared of QueryContext` - this takes an already instantiated `QueryContext` and uses it to execute the query. In this case, the builder will ensure that the connection is open before executing the query, but it will not try to close or dispose when it is done. This is useful for when you need to call multiple queries within a `task` or `async` block with a single shared `QueryContext`.


Selecting city and state columns only:
```F#
let getCities (cityFilter: string) = 
    selectTask HydraReader.Read (Create openContext) {
        for a in addressTable do                                // Specifies a FROM table in the query
        where (a.City = cityFilter)                             // Specifies a WHERE clause in the query
        select (a.City, a.StateProvince) into selected          // Specifies which entities and/or columns to SELECT in the query
        mapList (                                               // Transforms the query results
            let city, state = selected
            $"City, State: %s{city}, %s{state}"
        )
    }
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
let getAddressesInCitiesStartingWithS () = 
        selectAsync HydraReader.Read (Create openContext) {
            for a in addressTable do
            where (a.City =% "S%")
        }
```

#### Joins

Select top 10 `Product` entities with inner joined category name:
```F#
let getProductsWithCategory () = 
    selectTask HydraReader.Read (Create openContext) {
        for p in productTable do
        join c in categoryTable on (p.ProductCategoryID.Value = c.ProductCategoryID)
        select (p, c.Name)
        take 10
    }
```

Select `Customer` with left joined `Address` where `CustomerID` is in a list of values:
(Note that left joined tables will be of type `'T option`, so you will need to use the `.Value` property to access join columns.)

```F#
let getCustomerAddressesInIds (customerIds: int list) =
    selectAsync HydraReader.Read (Create openContext) {
        for c in customerTable do
        leftJoin ca in customerAddressTable on (c.CustomerID = ca.Value.CustomerID)
        leftJoin a  in addressTable on (ca.Value.AddressID = a.Value.AddressID)
        where (c.CustomerID |=| customerIds)
        orderBy c.CustomerID
        select (c, a)
    }
```

To create a join query with multi-columns, use tuples:

```F#
select {
    for o in orderHeaderTable do
    join d in orderDetailTable on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
    select (o, d)
}
```

#### Transforming Query Results

To transform the query results use the `mapSeq`, `mapArray` or `mapList` operations. 

```F#
    let! lineTotals =
        selectTask HydraReader.Read (Create openContext) {
            for o in orderHeaderTable do
            join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
            where (o.OnlineOrderFlag = true)
            mapList (
                {| 
                    ShipDate = 
                        match o.ShipDate with
                        | Some d -> d.ToShortDateString()
                        | None -> "No Order Number"
                    LineTotal = (decimal qty) * unitPrice
                |}
            )
        }
```

If a custom subset of entities and/or columns has been selected in the query, you will need to project them into a new binding using the `into` operation:

```F#
    let! lineTotals =
        selectTask HydraReader.Read (Create openContext) {
            for o in orderHeaderTable do
            join d in orderDetailTable on (o.SalesOrderID = d.SalesOrderID)
            where (o.OnlineOrderFlag = true)
            select (o, d.OrderQty, d.UnitPrice) into selected  // project selected values so they can be mapped
            mapList (
                let o, qty, unitPrice = selected               // unpack the selected values for use in transform
                {| 
                    ShipDate = 
                        match o.ShipDate with
                        | Some d -> d.ToShortDateString()
                        | None -> "No Order Number"
                    LineTotal = (decimal qty) * unitPrice
                |}
            )
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
/// Select categories with an avg product price > 500 and < 1000
let getCategoriesWithHighAvgPrice () = 
    selectTask HydraReader.Read (Create openContext) {
        for p in productTable do
        where (p.ProductCategoryID <> None)
        groupBy p.ProductCategoryID
        having (minBy p.ListPrice > 500M && maxBy p.ListPrice < 1000M)
        select (p.ProductCategoryID, minBy p.ListPrice, maxBy p.ListPrice) into selected
        mapList (
            let catId, minPrice, maxPrice = selected
            $"CatID: {catId}, MinPrice: {minPrice}, MaxPrice: {maxPrice}"
        )
    }
```

Alternative Row Count Query:
```F#
let! customersWithNoSalesPersonCount =
    selectTask HydraReader.Read (Create openContext) {
        for c in customerTable do
        where (c.SalesPerson = None)
        count
    }
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
let! top5Categories =
    selectTask HydraReader.Read (Create openContext) {
        for c in categoryTable do
        where (Some c.ProductCategoryID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
        select c.Name
    }
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
let! productsWithAboveAveragePrice =
    selectTask HydraReader.Read (Create openContext) {
        for p in productTable do
        where (p.ListPrice > subqueryOne avgListPrice)
        select (p.Name, p.ListPrice)
    }
```

Distinct Query:
```F#
let! distinctCustomerNames = 
    selectTask HydraReader.Read (Create openContext) {
        for c in customerTable do
        select (c.FirstName, c.LastName)
        distinct
    }
```

### Dos and Don'ts

:boom: The `select` clause only supports tables and fields for the sake of modifying the generated SQL query and the returned query type `'T`.
Transformations (i.e. `.ToString()` or calling any functions is _not supported_ and will throw an exception.

:boom: The `where` clause will automatically parameterize your input values. _However_, similar to the `select` clause, the `where` clause does not support calling an transformations (i.e. `.ToString()`). So you must prepare any parameter transformations before the builder. 

✅ CORRECT:
```F#
let getCities () =
    let city = getCity() // DO prepare where parameters above and then pass into the where clause
    selectTask HydraReader.Read (Create openContext) {
        for a in addressTable do
        where (a.City = city)
        select (a.City, a.StateProvince) into (city, state)
        mapList $"City: %s{city}, State: %s{state}"   // DO transforms using the `mapSeq`, `mapArray` or `mapList` operations
    }
```

❌ INCORRECT:
```F#
let getCities () =
    selectTask HydraReader.Read (Create openContext) {
        for a in addressTable do
        where (a.City = getCity()) // DO NOT perform calculations or translations within the builder
        select ($"City: %s{city}, State: %s{state}")   // DO NOT transform results within the builder 
    }
```

### Using the `selectAsync` and `selectTask` Builders
The new `selectAsync` and `selectTask` builders should generally be prefered over the older `select` builder (with the exception of creating subqueries, which must be done using the `select` builder) because they provide several advantages:

#### They are self-executing
The new `selectAsync` and `selectTask` builders will execute the query automatically, whereas the old `select` builder creates a query that must be manually passed into a `QueryContext` execution method. 

#### They offer more explicit control over the `QueryContext` and connection handling
The new `selectAsync` and `selectTask` builders must be initialized with with a `ContextType` discriminated union value that can either be `Shared` or `Create`.
Passing in a `Shared` context will run the query with an already existing `QueryContext`, whereas `Create` will create a new context and dispose it automatically after executing the query. 

#### They make it possible to create a query function that is not wrapped in an `async` or `task` block.
One problem with the `select` builder is that the `QueryContext` generally had to be initialized within the `task` block to ensure that it was not disposed while the task was running asynchronously. Having the ability to initilize  a `selectAsync` or `selectTask` builder with `Create` makes it a completely self-contained query which can exist by itself in a function without being wrapped in a `task` block.
**The new `selectAsync` and `selectTask` builders have the following new custom operations that are applied to the queried results:**

  * `toArray`
  * `toList`
  * `mapArray`
  * `mapList`
These new operations are designed to make the new select builders completely self-contained by removing the need to pipe the results.

#### They are Cleaner
Removing the need to pipeline the query builder into a `QueryContext` makes the code a bit more tidy.

### Creating a Custom `selectAsync` or `selectTask` Builder
If the redundancy of passing the generated `HydraReader.Read` static method into the `selectAsync` and `selectTask` builders bothers you, you can easily create your builder that has it baked-in:

```F#
let selectTask' ct = selectTask HydraReader.Read ct

// Usage:

let! distinctCustomerNames = 
    selectTask' (Create openContext) {
        for c in customerTable do
        select (c.FirstName, c.LastName)
        distinct
    }
```

### Insert Builder

#### Simple Inserts
For simple inserts with no identity column and no included/excluded columns, use the `into _` syntax:

```F#
let! rowsInserted = 
    insertTask (Create openContext) {
        into personTable
        entity 
            {
                dbo.Person.ID = Guid.NewGuid()
                dbo.Person.FirstName = "Bojack"
                dbo.Person.LastName = "Horseman"
                dbo.Person.LastUpdated = DateTime.Now
            }
    }

printfn "Rows inserted: %i" rowsInserted
```

#### Insert with an Identity Field
If you have an Identity column or if you want to specify columns to include/exclude, use the `for _ in _ do` syntax.
By default, all record fields will be included as insert values, so when using an identity column, you must handle it in one of two ways:
1) Mark it with `getId`. This will prevent it from being added as an insert value, and it will also select and return the identity field.
2) Mark it with `excludeColumn` to prevent it from being added as an insert value.

```F#

let! errorLogID =
    insertTask (Create openContext) {
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
    do! insertTask (Create openContext) {
            into currencyTable
            entities currencies
        } :> Task // upcast to Task if you want to ignore the resulting value
| None ->
    printfn "Skipping insert because entities seq was empty."
```

#### Upsert
Upsert support has been added for Postgres and Sqlite only because they support `ON CONFLICT DO ___` which provides atomic upsert capabilities.
(Unfortunately, SQL Server and Oracle only have MERGE which can suffer from concurrency issues.)

**Postgres:**
`open SqlHydra.Query.NpgsqlExtensions`

**Sqlite:**
`open SqlHydra.Query.SqliteExtensions`

**Example Usage:**

```F#
    /// Inserts an address or updates it if it already exists.
    let upsertAddress address = 
        insertTask (Create openContext) {
            for a in addressTable do
            entity address
            onConflictDoUpdate a.AddressID (
                a.AddressLine1,
                a.AddressLine2,
                a.City,
                a.StateProvince,
                a.CountryRegion,
                a.PostalCode,
                a.ModifiedDate
            )
        }
```


```F#
    /// Tries to insert an address if it doesn't already exist.
    let tryInsertAddress address = 
        insertTask (Create openContext) {
            for a in addressTable do
            entity address
            onConflictDoNothing a.AddressID
        }
```


### Update Builder

#### Update Individual Fields
To update individual columns, use the `set` operation.

```F#
do! updateAsync (Create openContext) {
        for e in errorLogTable do
        set e.ErrorNumber 123
        set e.ErrorMessage "ERROR #123"
        set e.ErrorLine (Some 999)
        set e.ErrorProcedure None
        where (e.ErrorLogID = 1)
    } :> Task // upcast to Task if you want to ignore the resulting value
```

#### Update Entire Record
To update an entire record, use the `entity` operation.
You may optionally use `includeColumn` to specify an allow list of one or more columns on the record to include in the update.
You may optionally use `excludeColum` to specify a deny list of one or more columns on the record to exclude from the update.
NOTE: You may use `includeColumn` or `excludeColumn` multiple times - once for each column to include/exclude.

```F#
let! rowsUpdated = 
    updateTask (Create openContext) {
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
do! deleteTask (Create openContext) {
        for e in errorLogTable do
        where (e.ErrorLogID = 5)
    } :> Task // upcast to Task if you want to ignore the resulting value

printfn "Rows deleted: %i" rowsDeleted
```

If you want to delete all records in a table, you must use the `deleteAll` keyword in lieu of a `where` statement or else it will not compile:
```F#
let! rowsDeleted = 
    deleteTask (Create openContext) {
        for c in customerTable do
        deleteAll
    }
```

# SqlHydra
SqlHydra is a set of NuGet packages for working with databases in F# with an emphasis on type safety and convenience.


### SqlHydra.Cli
[SqlHydra.Cli](#sqlhydracli-) is a dotnet tool that generates F# types and readers for  the following databases:
* SQL Server
* PostgreSQL
* Oracle
* SQLite
* MySQL (recently contributed; exists as-is with limited support)

### SqlHydra.Query
[SqlHydra.Query](#sqlhydraquery-) provides strongly typed Linq queries against generated types. 
        
#### Notes
- The generated code can be used alone or with any query library for creating strongly typed table records and data readers.
- SqlHydra.Query is designed to be used with SqlHydra generated types. (If you would prefer to create your own types over using generated types, then I would recommend checking out [Dapper.FSharp](https://github.com/Dzoukr/Dapper.FSharp).)
- SqlHydra.Query uses [SqlKata](https://sqlkata.com/) internally to generate provider-specific SQL queries.
- _All SqlHydra NuGet packages will be released with matching major and minor version numbers._

## Contributors ✨

Thanks to these awesome F# community contributors:

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tr>
    <td align="center">
        <a href="https://github.com/MargaretKrutikova"><img src="https://avatars.githubusercontent.com/u/5932274?v=4?s=100" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/pull/10" title="Code">💻</svg></a>
    </td>
    <td align="center">
        <a href="https://github.com/Jmaharman"><img src="https://avatars.githubusercontent.com/u/215359?v=4&s=100" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=Jmaharman" title="Code">💻</a>
    </td>
    <td align="center">
        <a href="https://github.com/ntwilson"><img src="https://avatars.githubusercontent.com/u/15835006?v=4&s=100" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=ntwilson" title="Code">💻</a>
    </td>
    <td align="center">
        <a href="https://github.com/MangelMaxime"><img src="https://avatars.githubusercontent.com/u/4760796?v=4&s=100" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=MangelMaxime" title="Code">💻</a>
    </td>
    <td align="center">
        <a href="https://github.com/aciq"><img src="https://avatars.githubusercontent.com/u/36763595?v=4&s=100" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=aciq" title="Code">💻</a>
    </td>
  <tr>
  </tr>
    <td align="center">
        <a href="https://github.com/jwosty"><img src="https://avatars.githubusercontent.com/u/4031185?v=4&s=100" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=jwosty" title="Code">💻</a>
    </td>
    <td align="center">
        <a href="https://github.com/devinlyons"><img src="https://avatars.githubusercontent.com/u/8211199?v=4&s=100" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=devinlyons" title="Code">💻</a>
    </td>
    <td align="center">
        <a href="https://github.com/EverybodyKurts"><img src="https://avatars.githubusercontent.com/u/879734?v=4&s=100" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=EverybodyKurts" title="Code">💻</a>
    </td>
   <td align="center">
        <a href="https://github.com/RJSonnenberg"><img src="https://avatars.githubusercontent.com/u/24612120?v=4" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=RJSonnenberg" title="Code">💻</a>
    </td>
   <td align="center">
        <a href="https://github.com/michelbieleveld"><img src="https://avatars.githubusercontent.com/u/4332783?v=4" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/commits?author=michelbieleveld" title="Code">💻</a>
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


## SqlHydra.Cli [![NuGet version (SqlHydra.Cli)](https://img.shields.io/nuget/v/SqlHydra.Cli.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Cli/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.Cli`

### Configure and Run

Run the tool from the command line, passing in a database provider: `mssql`, `npgsql`, `sqlite`, `oracle`

```bat
dotnet sqlhydra mssql
```

* If no .toml configuration file is detected, a configuration wizard will ask you some questions to create a new [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration) file for you, and will then generate code using the new config.
* If a .toml configuration file already exists, it will generate code.
* The generated .fs file will automatically be added to your .fsproj as `Visible="false"`.
* By default, the generated toml file will be named `sqlhydra-{provider}.toml`

### TOML Creation Wizard
The wizard will prompt you for the following input:

```
- Enter a database Connection String:
```
This is the [connection string](https://www.connectionstrings.com/) that SqlHydra can use to query table and column metadata.

```
- Enter an Output Filename (Ex: AdventureWorks.fs):
```
This is the filename that your generated types will be added to. (This file will be automatically added to your fsproj.)

```
- Enter a Namespace (Ex: MyApp.AdventureWorks):
```
This is the namespace that your generated table record types will be created in.

```
- Select a use case:

> SqlHydra.Query integration (default)
  Other data library
  Standalone      
```

Selecting a use case will set the base configuration options in your TOML file. 
* __SqlHydra.Query integration (default)__ should be chosen if you plan on using the SqlHydra.Query NuGet package to query your database using the generated types. This option will generated additional metadata that is utilized by the SqlHydra.Query package to recognize things like provider-specific parameter types. This use case will also generates a `HydraReader` class and corresponding `selectTask` and `selectAsync` computation expressions that SqlHydra.Query can use to query against the generated types.
* __Other data library__ should be chosen if you plan on using a 3rd party data library (ex: Dapper.FSharp, Donald, Npgsql.FSharp, ADO.NET, and many others). This use case only generates the table record types. No `HydraReader` class is generated.
* __Standalone__ means that you will only be using the generated read-only querying methods that will be generated. This use case creates the table record types and the `HydraReader` for reading them. (It does not create the additional metadata used by SqlHydra.Query.)

For more details, see the [.toml configuration](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration).


### Build Event (optional)

To regenerate after a Rebuild (only when in Debug mode), you can run SqlHydra from an fsproj build event:

```bat
  <!-- Regenerate entities on Rebuild in Debug mode -->
  <Target Name="SqlHydra" BeforeTargets="Clean" Condition="'$(Configuration)' == 'Debug'">
    <Exec Command="dotnet sqlhydra mssql" />
  </Target>
```

### Support for Postgres Enums
Postgres enum types are generated as CLR enums!
You will, however, need to manually register your custom enums with Npgsql.

```F#
// Global mapping should occur only once at startup:
// `ext.mood` is the generated enum, and "ext.mood" is the "{schema}.{enum}".

/// This should be created once per app
let dataSource = 
    let builder = NpgsqlDataSourceBuilder("connection string...")
    builder.MapEnum<ext.mood>("ext.mood") |> ignore    
    builder.Build()

let openContext() = 
    let compiler = SqlKata.Compilers.PostgresCompiler()
    // You must create your connection from your dataSource with enum mappings
    let conn = dataSource.OpenConnection() 
    new QueryContext(conn, compiler)
```

### Support for Postgres Arrays
SqlHydra.Cli supports `text[]` and `integer[]` column types.

### Sqlite Data Type Aliases
Sqlite stores all data as either an `INTEGER`, `REAL`, `TEXT` or `BLOB` type.
Fortunately, you can also use aliases for data types more commonly used in other databases in your table definitions and Sqlite will translate them to the appropriate type.
Using these type aliases also allows `SqlHydra.Cli` to generate the desired .NET CLR property type.

Here is a list of valid data type aliases (or "affinity names"):
https://www.sqlite.org/datatype3.html#affinity_name_examples

### SQL Server Troubleshooting

The following exception may occur with the latest version of `Microsoft.Data.SqlClient`:
```
Microsoft.Data.SqlClient.SqlException (0x80131904): 
A connection was successfully established with the server, but then an error occurred during the login process. 
(provider: SSL Provider, error: 0 - The certificate chain was issued by an authority that is not trusted.)
```

The most simple way to resolve this is to append `;TrustServerCertificate=True` to the connection string in your .toml configuration file.
UPDATE: This behavior has been fixed in `Microsoft.Data.SqlClient` v4.1.1.


## Generated Table Types for AdventureWorks

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

    let ErrorLog = table<ErrorLog>

    type BuildVersion =
        { SystemInformationID: byte
          ``Database Version``: string
          VersionDate: System.DateTime
          ModifiedDate: System.DateTime }

    let BuildVersion = table<BuildVersion>

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

    let Address = table<Address>

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

    let Customer = table<Customer>
    
    // etc...
```

## Strongly Typed Data Readers
The generated `HydraReader` class works in tandem with SqlHydra.Query for reading queried entities, but it can also be used on its own with any query library that returns an IDataReader.

* [Using HydraReader automatically with SqlHydra.Query](#sqlhydraquery-)
* [Using HydraReader manually with other query libraries](https://github.com/JordanMarr/SqlHydra/wiki/DataReaders)

## TOML Configuration Reference
* [View TOML Configuration Reference](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration)

## Generating Multiple TOML Files

It is also possible to have more than one .toml file in the same project. By default, SqlHydra will create a .toml file named after the version of SqlHydra used.
For example, running `dotnet sqlhydra sqlite` will generate `sqlhydra-sqlite.toml`. 

However, you can also specify a name for your .toml file: `dotnet sqlhydra sqlite -t "shared.toml"`
This can be useful for various use cases, such as:
* data migrations where you want to generate types for a source and a target database.
* generating record types with different schema/table filters in separate files.


## Supported Frameworks
.NET 6 - .NET 8 are currently supported.
(If you still need support for .NET 5, use the deprecated `SqlHydra.SqlServer`, `SqlHydra.Sqlite`, `SqlHydra.Npgsql` or `SqlHydra.Oracle` tools.)

### .NET 6 and Greater
The new .NET 6 `System.DateOnly` and `System.TimeOnly` types are now supported by all generators.

## SqlHydra.Query [![NuGet version (SqlHydra.Query)](https://img.shields.io/nuget/v/SqlHydra.Query.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Query/)
SqlHydra.Query wraps the powerful [SqlKata](https://sqlkata.com/) query generator with F# computation expression builders for strongly typed query generation.
SqlHydra.Query can be used with any library that accepts a data reader; however, is designed pair well with SqlHydra generated records and readers! 

### Creating a Query Context

```F#
/// Opens a connection and creates a QueryContext that will generate SQL Server dialect queries
let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = new SqlConnection("Replace with your connection string")
    conn.Open()
    let ctx = new QueryContext(conn, compiler)
    #if DEBUG
    // Writes your queries and parameters to the console
    ctx.Logger <- printfn "SQL: %O" 
    #endif
    ctx
```

### Select Builders

There are two main select builders that you can use to create queries:
* `selectTask` - creates a _self-executing_ query that returns a Task<'T> of query results.
* `selectAsync` - creates a _self-executing_ query that returns an Async<'T> of query results.

:new: As of SqlHydra.Cli v2.6.0, your generated types will provide `selectTask` and `selectAsync` functions that work with the generated `HydraReader.Read` function.
_Be sure to open the namespace to your generated types._

```F#
open SampleApp.AdventureWorks
open SampleApp.AdventureWorks.HydraBuilders // Generated builders are here

let getErrorNumbers () =
    selectTask openContext {
        for e in dbo.ErrorLog do
        select e.ErrorNumber
    }
```

Input: 
1) You must open the namespace you provided for your generated types to be able to use the corresponding `selectTask` and `selectAsync` computation expressions.
2) You must pass in either an existing `QueryContext` or a function that will create a new one.

#### Example 1: Creating a new `QueryContext`

In this case, the `selectAsync` expression will create the `QueryContext` using the given `openContext` function, and it will also dispose it for you automatically.

```F#
let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = new SqlConnection("Replace with your connection string")
    conn.Open()
    new QueryContext(conn, compiler)

let getErrorNumbers () =
    selectAsync openContext {
        for e in dbo.ErrorLog do
        select e.ErrorNumber
    }
```

#### Example 2: Sharing a `QueryContext`

In this case, you manually create a shared `QueryContext` and pass it to each `selectTask` expression. You are also responsible for disposing the `QueryContext` manually.

```F#
let getUserOrders email =
    task {
        use ctx = openContext ()

        let! userInfo = 
            selectTask ctx {
                for user in dbo.Users do
                where (user.Email = email)
            }

        let! orderHeaders = 
            selectTask ctx {
                for o in dbo.OrderHeaders do
                where (o.CustomerEmail = email)
            }

        return { User = userInfo; Orders = orderHeaders }
    }
```


#### Notes on Select Builders
* The `selectTask` and `selectAsync` computation expressions are generated alongside your datatypes as of SqlHydra.Cli v2.6.0.
* The argument to the `selectTask` and `selectAsync` expressions is a `ContextType` discriminated union. However, you can now implicitly pass a `QueryContext` or a function that returns a `QueryContext` in place of the `ContextType` DU.
* The `selectTask` and `selectAsync` computation expressions are self-executing and do not require being wrapped within an `async` or `task` computation expression.
* The `selectTask` and `selectAsync` builders also require you to pass in either an existing `QueryContext` (which manages the `DbConnection` and executes the various types of queries), or a function that returns a `QueryContext`.
  * If you pass in an existing `QueryContext`, you will be responsible for disposing it.
  * You can also pass in a function that will return a `QueryContext` which will be disposed on your behalf. The following are valid create functions:
  * `unit -> QueryContext`
  * `unit -> Async<QueryContext>`
  * `unit -> Task<QueryContext>`
* The `selectTask` and `selectAsync` computation expressions builders also provide the following custom operations that are applied to the queried results (after the query data is returned):
  * `toArray`
  * `toList`
  * `mapArray`
  * `mapList`
  * `tryHead`
  * `head`
 
Selecting city and state columns only:
```F#
let getCities (cityFilter: string) = 
    selectTask openContext {
        for a in SalesLT.Address do                             // Specifies a FROM table in the query
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
    selectAsync openContext {
        for a in SalesLT.Address do
        where (a.City =% "S%")
        select a
    }
```

NOTE: You should always explicitly `select` the table or tables. As of v2.5.0, selecting the table(s) will explicitly select all columns in the table. Otherwise, it will issue a `SELECT *` query which is slightly less performant as it will require a table scan.

Try to select a single row (this example returns a `decimal option`):
```F#
let tryGetOrderTotal (orderId: int) = 
        selectAsync openContext {
            for o in SalesLT.Order do
            where (o.Id = orderId)
            select o.Total
            tryHead
        }
```

#### Evaluation of Where Parameter Expressions

As of v3, SqlHydra.Query `where` parameters can now be expressions that are evaluated inline within the query. (In previous versions, `where` parameters had to be constant value bindings that were evaluated outside of the query builder.) 

For example, these are all valid ways to set a `where` parameter as of v3:

```F#
let getAddresses() =
    let chicago = "Chicago"
    let getCity() = "Chicago"

    selectTask openContext {
        for a in Person.Address do
        where (
            a.City = "Chicago" ||
            a.City = chicago ||
            a.City = getCity() ||
            a.City = getCity().ToUpper() ||
            a.City = $"City: {getCity()}"
        )
    }
```

#### Conditional Where

As of v3, you can conditionally include or exclude `where` conditions using the `&&` operator to short-circuit the condition.
In the example below, if `cityFilter.IsSome` evaluates to `false`, the `cityFilter` clause will not be added to the query.

```F#
let getAddresses(cityFilter: string option; zipFilter: string option) = 
    selectTask openContext {
        for a in Person.Address do
        where (
            (cityFilter.IsSome && a.City = cityFilter.Value) &&
            (zipFilter.IsSome && a.PostalCode = zipFilter.Value)
        )
    }
```

#### Conditional Order By

As of v3, you can conditionally include or exclude `orderBy` columns using the custom `^^` operator to short-circuit the sort condition.

```F#
let getAddresses(sortByCity: bool, sortByZip: bool) = 
    selectTask openContext {
        for a in Person.Address do
        orderBy (
            (sortByCity ^^ a.City) &&
            (sortByZip ^^ a.PostalCode)
        )
    }
```

#### Joins

Select top 10 `Product` entities with inner joined category name:
```F#
let getProductsWithCategory () = 
    selectTask openContext {
        for p in SalesLT.Product do
        join c in categoryTable on (p.ProductCategoryID.Value = c.ProductCategoryID)
        select (p, c.Name)
        take 10
    }
```

> [!WARNING]
> You need to write the join `on` equality condition using the known (left) table variable on the left and the new (right) one on the right; otherwise, the F# compiler will complain that `p` and `c` are not defined. (This order condition is due to the way F# [Query Expressions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/query-expressions) are defined.)

Select `Customer` with left joined `Address` where `CustomerID` is in a list of values:
(Note that left joined tables will be of type `'T option`, so you will need to use the `.Value` property to access join columns.)

```F#
let getCustomerAddressesInIds (customerIds: int list) =
    selectAsync openContext {
        for c in SalesLT.Customer do
        leftJoin ca in SalesLT.CustomerAddress on (c.CustomerID = ca.Value.CustomerID)
        leftJoin a  in SalesLT.Address on (ca.Value.AddressID = a.Value.AddressID)
        where (c.CustomerID |=| customerIds)
        orderBy c.CustomerID
        select (c, a)
    }
```

When selecting individual columns from a left joined table, you can force non-optional columns to be optional by wrapping them in `Some`:

```F#
let getCustomerZipCodes (customerId: int) =
    selectAsync openContext {
        for c in SalesLT.Customer do
        leftJoin ca in SalesLT.CustomerAddress on (c.CustomerID = ca.Value.CustomerID)
        leftJoin a  in SalesLT.Address on (ca.Value.AddressID = a.Value.AddressID)
        where (c.CustomerID = customerId)
        orderBy c.CustomerID
        select (c, Some a.Value.ZipCode)
    }
```

To create a join query with multi-columns, use tuples:

```F#
select {
    for o in SalesLT.OrderHeaders do
    join d in SalesLT.OrderDetails on ((o.SalesOrderID, o.ModifiedDate) = (d.SalesOrderID, d.ModifiedDate))
    select (o, d)
}
```

> [!WARNING] 
> The `join` `on` clause only supports simple column = column comparisons. Constant value parameters are not supported.
Any custom filters that you might normally put in the `on` clause, especially those involving input parameters, will need to be moved to the `where` clause.
This is because the F# `join` `on` syntax does not support complex filter clauses.

#### Transforming Query Results

To transform the query results use the `mapSeq`, `mapArray` or `mapList` operations. 

```F#
    let! lineTotals =
        selectTask openContext {
            for o in SalesLT.OrderHeaders do
            join d in SalesLT.OrderDetails on (o.SalesOrderID = d.SalesOrderID)
            where (o.OnlineOrderFlag = true)
            select (o, d) into selected
            mapList (
                let o,d = selected
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
        selectTask openContext {
            for o in SalesLT.OrderHeaders do
            join d in SalesLT.OrderDetails on (o.SalesOrderID = d.SalesOrderID)
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

You can also use `mapSeq` in conjunction with `tryHead` to map a single result:

```F#
        selectAsync openContext {
            for o in SalesLT.Order do
            where (o.Id = orderId)
            select o.Total
            mapSeq {| GrandTotal = o.Total |}
            tryHead
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
    selectTask openContext {
        for p in SalesLT.Product do
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
    selectTask openContext {
        for c in SalesLT.Customer do
        where (c.SalesPerson = None)
        count
    }
```
> [!WARNING]
> In some cases when selecting an aggregate of a non-NULL column, the database will still return NULL if the query result set is empty, for example if selecting the MAX of an INT column in an empty table. This is not supported and will throw an exception. If your query might return NULL for the aggregate of a non-NULL column, you may include `Some` in the aggregate to support parsing the NULL as an `Option` value:

❌ INCORRECT:
```F#
/// Select the minimum item price above a threshold
let getNextLowestPrice threshold = 
    selectTask openContext {
        for p in SalesLT.Product do
        where (p.ListPrice > threshold)
        select (minBy p.ListPrice)
    }
```

✅ CORRECT:
```F#
/// Select the minimum item price above a threshold
let getNextLowestPrice threshold = 
    selectTask openContext {
        for p in SalesLT.Product do
        where (p.ListPrice > threshold)
        select (minBy (Some p.ListPrice))
    }
```


#### WHERE Subqueries

_Use the `subqueryMany` function for subqueries that return multiple rows for comparison:_

```F#
// Create a subquery that gets top 5 avg prices by category ID:
let top5CategoryIdsWithHighestAvgPrices = 
    select {
        for p in SalesLT.Product do
        where (p.ProductCategoryID <> None)
        groupBy p.ProductCategoryID
        orderByDescending (avgBy p.ListPrice)
        select p.ProductCategoryID
        take 5
    }

// Get category names where the category ID is "IN" the subquery:
let! top5Categories =
    selectTask openContext {
        for c in SalesLT.ProductCategory do
        where (Some c.ProductCategoryID |=| subqueryMany top5CategoryIdsWithHighestAvgPrices)
        select c.Name
    }
```

_Use the `subqueryOne` function for subqueries that return a single value for comparison:_

```F#
// Create a subquery that gets the avg list price (a single value):
let avgListPrice = 
    select {
        for p in SalesLT.Product do
        select (avgBy p.ListPrice)
    } 

// Get products with a price > the average price
let! productsWithAboveAveragePrice =
    selectTask openContext {
        for p in SalesLT.Product do
        where (p.ListPrice > subqueryOne avgListPrice)
        select (p.Name, p.ListPrice)
    }
```

##### Correlated Subqueries

If the subquery is correlated with the parent query (i.e., the subquery references a row variable from the parent query), use the `correlate` keyword in the subquery to introduce the correlated variable. **Note: the variable name in the subquery must match the variable name in the parent query, because it determines the table alias in the generated SQL query.**

```F#
// Create a subquery that gets the min price for this product line,
// referencing a row variable "outer" from the parent query:
let lowestPriceByProductLine = 
    select {
        for inner in Production.Product do
        correlate outer in Production.Product
        where (inner.ProductLine = outer.ProductLine)
        select (minBy inner.ListPrice)
    }

// Get the products whose price is the lowest of all prices in its product line.
// The name "outer" needs to match the subquery.
let! cheapestByProductLine = 
    selectTask openContext {
        for outer in Production.Product do
        where (outer.ListPrice = subqueryOne lowestPriceByProductLine)
        select (outer.Name, outer.ListPrice)
    }
```


Distinct Query:
```F#
let! distinctCustomerNames = 
    selectTask openContext {
        for c in SalesLT.Customer do
        select (c.FirstName, c.LastName)
        distinct
    }
```

### Transforming Select Results

:boom: The `select` clause allows you to limit the tables and columns that will be included in the generated SQL query and the returned query type `'T`.
Transformations (i.e. `.ToString()` or calling any functions is _not supported_ in the `select` and will throw an exception. 

✅ CORRECT:
```F#
let getCity (city: string) =
    selectTask openContext {
        for a in SalesLT.Address do
        where (a.City = city)
        select (a.City, a.StateProvince) into (city, state)
        mapList $"City: %s{city}, State: %s{state}"   // DO transforms within `mapSeq`, `mapArray` or `mapList` operations
    }
```

❌ INCORRECT:
```F#
let getCity (city: string) =
    selectTask openContext {
        for a in SalesLT.Address do
        where (a.City = city) 
        select ($"City: %s{city}, State: %s{state}")   // DO NOT transform results within the `select` operation.
    }
```

### Insert Builder

#### Simple Inserts
For simple inserts with no identity column and no included/excluded columns, use the `into _` syntax:

```F#
let! rowsInserted = 
    insertTask openContext {
        into Person.Person
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
    insertTask openContext {
        for e in dbo.ErrorLog do
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

#### Insert and Output Inserted Fields (SQL Server)
`output` is a SQL Server feature that allows you to return one or more columns from an insert or update statement. 
To use it, you must open the `SqlHydra.Query.SqlServerExtensions` module.
* You may select one or more columns to return
* Selecting the entire entity to be returned is not supported and will throw an exception.
* Can not be used in conjunction with `getId`. (`getId` will take precedence.)

```F#
open SqlHydra.Query
open SqlHydra.Query.SqlServerExtensions

let insertPerson (row: Person) =
  task {
    let! createDate, updateDate =
        insertTask openContext {
            for p in dbo.Person do
            entity row
            output (e.CreateDate, e.UpdateDate)
        }

    return updateDate
  }
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
    do! insertTask openContext {
            into Sales.Currency
            entities currencies
        } :> Task // upcast to Task if you want to ignore the resulting value
| None ->
    printfn "Skipping insert because entities seq was empty."
```

#### Upsert
Upsert support has been added for Postgres and Sqlite only because they support `ON CONFLICT DO ___` which provides atomic upsert capabilities.
(Unfortunately, SQL Server and Oracle only have MERGE which can suffer from concurrency issues. For SQL Server bulk operations, please try my [SqlBulkTools.Fsharp](https://github.com/JordanMarr/SqlBulkTools.FSharp) library.)

**Postgres:**
`open SqlHydra.Query.NpgsqlExtensions`

**Sqlite:**
`open SqlHydra.Query.SqliteExtensions`

**Example Usage:**

```F#
    /// Inserts an address or updates it if it already exists.
    let upsertAddress address = 
        insertTask openContext {
            for a in Person.Address do
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

Or, if you have multiple addresses to upsert:

```F#
    /// Inserts multiple addresses or updates them if they already exist.
    let upsertAddress addresses =
        match addresses |> AtLeastOne.tryCreate with
        | Some addresses -> 
            insertTask openContext {
                for a in Person.Address do
                entities addresses
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
        | None ->
            printfn "No addresses to insert."
            0
```

```F#
    /// Tries to insert an address if it doesn't already exist.
    let tryInsertAddress address = 
        insertTask openContext {
            for a in Person.Address do
            entity address
            onConflictDoNothing a.AddressID
        }
```


### Update Builder

#### Update Individual Fields
To update individual columns, use the `set` operation.

```F#
do! updateAsync openContext {
        for e in dbo.ErrorLog do
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
    updateTask openContext {
        for e in dbo.ErrorLog do
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
    for c in Sales.Customer do
    set c.AccountNumber "123"
    updateAll
}
```

### Delete Builder

```F#
do! deleteTask openContext {
        for e in dbo.ErrorLog do
        where (e.ErrorLogID = 5)
    } :> Task // upcast to Task if you want to ignore the resulting value
```

If you want to delete all records in a table, you must use the `deleteAll` keyword in lieu of a `where` statement or else it will not compile:
```F#
let! rowsDeleted = 
    deleteTask openContext {
        for c in Sales.Customer do
        deleteAll
    }
    
printfn "Rows deleted: %i" rowsDeleted
```

## Custom SqlKata Queries

SqlKata supports a lot of custom query operations, many of which are not supported by SqlHydra query builders.
The `kata` custom operation allows you to manipulate the underlying SqlKata query directly.
For example, you could use this to conditionally add columns to the WHERE or ORDER BY clauses:

```F#
let getCustomers filters = 
  select {
      for c in main.Customer do
      where (c.FirstName = "John")
      kata (fun query -> 
          match filters.LastName with
          | Some lastName -> query.Where("c.LastName", lastName)
          | None -> query
      )
      kata (fun query -> 
          query.OrderBy(filters.SortColumns)
      )
  }
```

## Custom SQL Queries

Sometimes it is easier to just write a custom SQL query. This can be helpful when you have a very custom query, or are using SQL constructs that do not yet exist in `SqlHydra.Query`. 
You can do this while still maintaining the benefits of the strongly typed generated `HydraReader`.

This example uses the generated `HydraReader` to hydrate the generated `dbo.Product` table record.

```F#
let getTop10Products(conn: SqlConnection) = task {
    let sql = $"SELECT TOP 10 * FROM {nameof dbo.Product} p"
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let hydra = HydraReader(reader)

    return [
        while reader.Read() do
            hydra.``dbo.Product``.Read()
    ]
}
```

The next example uses a query expression that modifies the underlying `SqlKata` query (using the the `kata` custom operation) to override the `SELECT` clause, and then manually reads the results into a custom record type.
This technique can be useful if you want to select custom columns that use functions with a custom result type, but you still want to use a strongly typed query expression. 

```F#
/// A custom result type to be used for this query.
type CityRow = { City3: string; Number: int }

let getCities() = task {
    use ctx = openContext()
            
    let! reader =
        select {
            for a in Person.Address do
            where (a.City |=| [ "Seattle"; "Denver" ])
            kata (fun query -> query.SelectRaw("SUBSTRING(City, 1, 3) AS City3, 123 AS Number"))
        }
        |> ctx.GetReaderAsync
        
    return
        [
            while reader.Read() do
                {
                    City3 = reader.Get "City3"
                    Number = reader.Get "Number"
                }
        ]
}
```

See more examples of using the generated `HydraReader`:
https://github.com/JordanMarr/SqlHydra/wiki/DataReaders


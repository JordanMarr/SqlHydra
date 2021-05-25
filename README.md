# SqlHydra
SqlHydra is a collection of dotnet tools that generate F# records for a given database provider.

## SqlHydra.SqlServer [![NuGet version (SqlHydra.SqlServer)](https://img.shields.io/nuget/v/SqlHydra.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.SqlServer/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.SqlServer`

### Configure

Create a batch file or shell script (`gen.bat` or `gen.sh`) in your project directory with the following contents:

```bat
dotnet sqlhydra-mssql {connection string} {namespace} {filename.fs}
```

_Example:_
```bat
dotnet sqlhydra-mssql "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI" "SampleApp.AdventureWorks" "AdventureWorks.fs"
```

### Generate Records
1) Run your `gen.bat` (or `gen.sh`) file to generate the output .fs file.
2) Manually add the .fs file to your project.

### Regenerate Records
1) Run your `gen.bat` (or `gen.sh`) file to refresh the output .fs file.


_That's it!_



## Officially Recommended ORM: Dapper.FSharp!

After creating SqlHydra, I was trying to find the perfect ORM to complement SqlHyda's generated records.
Ideally, I wanted to find a library with 
- First-class support for F# records, option types, etc.
- LINQ queries (to take advantage of strongly typed SqlHydra generated records)

[FSharp.Dapper](https://github.com/Dzoukr/Dapper.FSharp) met the first critera with flying colors. 
As the name suggests, Dapper.FSharp was written specifically for F# with simplicity and ease-of-use as the driving design priorities.
FSharp.Dapper features custom F# Computation Expressions for selecting, inserting, updating and deleting, and support for F# Option types and records (no need for `[<CLIMutable>]` attributes!).

If only it had Linq queries, it would be the _perfect_ complement to SqlHydra...

So I submitted a [PR](https://github.com/Dzoukr/Dapper.FSharp/pull/26) to Dapper.FSharp that adds Linq query expressions (now in v2.0+)!

The result is that it is now the _perfect_ complement to SqlHydra!
Between the two, you can have strongly typed access to your database:

```fsharp
module DapperFSharpExample
open System.Data
open System.Data.SqlClient
open Dapper.FSharp.LinqBuilders
open Dapper.FSharp.MSSQL
open AdventureWorks // Generated Types

Dapper.FSharp.OptionTypes.register()
    
// Tables
let customerTable =         table<Customer>         |> inSchema (nameof SalesLT)
let customerAddressTable =  table<CustomerAddress>  |> inSchema (nameof SalesLT)
let addressTable =          table<SalesLT.Address>  |> inSchema (nameof SalesLT)

let getAddressesForCity(conn: IDbConnection) (city: string) = 
    select {
        for a in addressTable do
        where (a.City = city)
    } |> conn.SelectAsync<SalesLT.Address>
    
let getCustomersWithAddresses(conn: IDbConnection) =
    select {
        for c in customerTable do
        leftJoin ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
        leftJoin a  in addressTable on (ca.AddressID = a.AddressID)
        where (isIn c.CustomerID [30018;29545;29954;29897;29503;29559])
        orderBy c.CustomerID
    } |> conn.SelectAsyncOption<Customer, CustomerAddress, Address>

```


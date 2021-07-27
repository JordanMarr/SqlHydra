# SqlHydra
SqlHydra is a collection of dotnet tools that generate F# records for a given database provider.

Currently supported databases:
- [SQL Server](https://github.com/JordanMarr/SqlHydra#sqlhydrasqlserver-)
- [SQLite](https://github.com/JordanMarr/SqlHydra#sqlhydrasqlite-)

Features
- Generate a record for each table
- Generate [Data Readers](https://github.com/JordanMarr/SqlHydra#) for each table

## SqlHydra.SqlServer [![NuGet version (SqlHydra.SqlServer)](https://img.shields.io/nuget/v/SqlHydra.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.SqlServer/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.SqlServer`

### Configure

Create a batch file or shell script (`gen.bat` or `gen.sh`) in your project directory with the following contents:

```bat
dotnet sqlhydra-mssql -c "{connection string}" -o "{output file}.fs" -ns "{namespace}"
```

_Example:_

```bat
dotnet sqlhydra-mssql -c "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI" -o "AdventureWorks.fs" -ns "SampleApp.AdventureWorks"
```

### Generate Records
1) Run your `gen.bat` (or `gen.sh`) file to generate the output .fs file.
2) Manually add the .fs file to your project.

### Regenerate Records
1) Run your `gen.bat` (or `gen.sh`) file to refresh the output .fs file.

## SqlHydra.Sqlite [![NuGet version (SqlHydra.Sqlite)](https://img.shields.io/nuget/v/SqlHydra.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Sqlite/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.Sqlite`

### Configure

Create a batch file or shell script (`gen.bat` or `gen.sh`) in your project directory with the following contents:

```bat
dotnet sqlhydra-sqlite -c "{connection string}" -o "{output file}.fs" -ns "{namespace}"
```

_Example:_

```bat
dotnet sqlhydra-sqlite -c "Data Source=C:\MyProject\AdventureWorksLT.db" -o "AdventureWorks.fs" -ns "SampleApp.AdventureWorks"
```

### Generate Records
1) Run your `gen.bat` (or `gen.sh`) file to generate the output .fs file.
2) Manually add the .fs file to your project.

### Regenerate Records
1) Run your `gen.bat` (or `gen.sh`) file to refresh the output .fs file.

## Data Readers
In addition to generating table reocrds, you can now also generate data readers for each table using the `--readers` option.
The generated reader classes provide strongly typed access for each column, and also include helper methods for loading the entire table record.

### Reading Generated Table Records

The following example loads the generated AdventureWorks Customer and Address records using the `Read` and `ReadIfNotNull` methods.
The `getCustomersLeftJoinAddresses` function returns a  `Task<(SalesLT.Customer * SalesLT.Address option) list>`.

``` fsharp
let getCustomersLeftJoinAddresses(conn: SqlConnection) = task {
    let sql = 
        """
        SELECT TOP 20 * FROM SalesLT.Customer c
        LEFT JOIN SalesLT.CustomerAddress ca ON c.CustomerID = ca.CustomerID
        LEFT JOIN SalesLT.Address a on ca.AddressID = a.AddressID
        WHERE c.CustomerID IN (
            29485,29486, 29489, -- these have an a.AddressID, so LEFT JOIN should yield "Some"
            1,2)                -- these do not have have an a.AddressID, so LEFT JOIN should yield "None"
        ORDER BY c.CustomerID
        """
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let c = SalesLT.CustomerReader(reader)
    let a = SalesLT.AddressReader(reader)

    return [
        while reader.Read() do
            c.Read(), a.ReadIfNotNull(a.AddressID)
    ]
}
```

### Reading Individual Columns

The next example loads individual columns using the property readers. This is useful for loading your own custom domain entities or for loading a subset of fields.
The `getProductImages` function returns a `Task<(string * string * byte[] option) list>`.

```fsharp
let getProductImages(conn: SqlConnection) = task {
    let sql = "SELECT TOP 10 [Name], [ProductNumber] FROM SalesLT.Product p WHERE ThumbNailPhoto IS NOT NULL"
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    return [
        let p = SalesLT.ProductReader(reader)
        while reader.Read() do
            p.Name.Read(), 
            p.ProductNumber.Read(), 
            p.ThumbNailPhoto.Read()
    ]
}

```

### Reading Individual Columns with Aliases

When joining tables that have the same column name, it may be necessary to load an aliased column.
There are two ways to do this:

1) If you are reading individual columns, you can supply an optional `alias` argument to the `Read` method:

```fsharp
let getProductsAndCategories(conn: SqlConnection) = task {
    let sql = 
        """
        SELECT p.Name as Product, c.Name as Category
        FROM SalesLT.Product p
        LEFT JOIN SalesLT.ProductCategory c ON p.ProductCategoryID = c.ProductCategoryID
        """
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let p = SalesLT.ProductReader(reader)
    let c = SalesLT.ProductCategoryReader(reader)

    return [
        while reader.Read() do
            p.Name.Read("Product"), 
            c.Name.Read("Category")
    ]
}
```

2) If you reading entire table records that have shared columns, you can configure aliases in advance using the `As` method on an individual column property:

```fsharp
let getProductsAndCategories(conn: SqlConnection) = task {
    let sql = 
        """
        SELECT *, c.Name as Category
        FROM SalesLT.Product p
        LEFT JOIN SalesLT.ProductCategory c ON p.ProductCategoryID = c.ProductCategoryID
        """
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let p = SalesLT.ProductReader(reader)
    let c = SalesLT.ProductCategoryReader(reader)
    c.Name.As "Category"

    return [
        while reader.Read() do
            p.Read(), 
            c.ReadIfNotNull(c.ProductCategoryID)
    ]
}
```


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

## CLI Reference

### Arguments

| Name&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Alias | Default | Description |
| -------- | ----- | ------- | ------- |
| --connection | -c | *Required* | The database connection string |
| --output | -o | *Required* | A path to the generated .fs output file (relative paths are valid) |
| --namespace | -ns | *Required* | The namespace of the generated .fs output file |
| --cli-mutable |  | false | If this argument exists, a `[<CLIMutable>]` attribute will be added to each record. |
| --readers [IDataReader Type Override] |  |  | Generates data readers for each table. You can optionally override the default ADO.NET IDataReader type. Ex: `--readers "System.Data.SqlClient.SqlDataReader"`

_Example:_

```bat
dotnet sqlhydra-mssql -c "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI" -o "AdventureWorks.fs" -ns "SampleApp.AdventureWorks" --cli-mutable
```

## Recommended ORM: Dapper.FSharp

After creating SqlHydra, I was trying to find the perfect ORM to complement SqlHyda's generated records.
Ideally, I wanted to find a library with 
- First-class support for F# records, option types, etc.
- LINQ queries (to take advantage of strongly typed SqlHydra generated records)

[FSharp.Dapper](https://github.com/Dzoukr/Dapper.FSharp) met the first critera with flying colors. 
As the name suggests, Dapper.FSharp was written specifically for F# with simplicity and ease-of-use as the driving design priorities.
FSharp.Dapper features custom F# Computation Expressions for selecting, inserting, updating and deleting, and support for F# Option types and records (no need for `[<CLIMutable>]` attributes!).

If only it had Linq queries, it would be the _perfect_ complement to SqlHydra...

So I submitted a [PR](https://github.com/Dzoukr/Dapper.FSharp/pull/26) to Dapper.FSharp that adds Linq query expressions (now in v2.0+)!

Between the two, you can have strongly typed access to your database:

```fsharp
module SampleApp.DapperFSharpExample
open System.Data
open Microsoft.Data.SqlClient
open Dapper.FSharp.LinqBuilders
open Dapper.FSharp.MSSQL
open SampleApp.AdventureWorks // Generated Types

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


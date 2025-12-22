# SqlHydra

Type-safe SQL generation for F#. Generate types from your database, query with strongly-typed computation expressions.

[![SqlHydra.Cli NuGet](https://img.shields.io/nuget/v/SqlHydra.Cli.svg?style=flat-square&label=SqlHydra.Cli)](https://www.nuget.org/packages/SqlHydra.Cli/)
[![SqlHydra.Query NuGet](https://img.shields.io/nuget/v/SqlHydra.Query.svg?style=flat-square&label=SqlHydra.Query)](https://www.nuget.org/packages/SqlHydra.Query/)

**Supported Databases:** SQL Server | PostgreSQL | SQLite | Oracle | MySQL

---

## Quick Start

**1. Install the CLI tool:**
```bash
dotnet new tool-manifest
dotnet tool install SqlHydra.Cli
```

**2. Generate types from your database:**
```bash
dotnet sqlhydra mssql    # or: npgsql, sqlite, oracle, mysql
```
The wizard will prompt you for connection string, output file, and namespace.

**3. Install the query library:**
```bash
dotnet add package SqlHydra.Query
```

**4. Write your first query:**
```fsharp
open MyApp.AdventureWorks              // Your generated namespace
open MyApp.AdventureWorks.HydraBuilders

let openContext () =
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = new SqlConnection("your connection string")
    conn.Open()
    new QueryContext(conn, compiler)

// Query with full type safety
let getProducts minPrice =
    selectTask openContext {
        for p in SalesLT.Product do
        where (p.ListPrice > minPrice)
        orderBy p.Name
        select p
    }
```

> **Note:** All query builders have both `Task` and `Async` variants: `selectTask`/`selectAsync`, `insertTask`/`insertAsync`, `updateTask`/`updateAsync`, `deleteTask`/`deleteAsync`.

That's it! Your queries are now type-checked at compile time.

---

## What Gets Generated?

SqlHydra.Cli reads your database schema and generates:

- **F# record types** for each table (with Option types for nullable columns)
- **Table declarations** for use in queries
- **HydraReader** for efficiently reading query results

```fsharp
// Generated from your database schema:
module SalesLT =
    type Product =
        { ProductID: int
          Name: string
          ListPrice: decimal
          Color: string option }  // nullable columns become Option

    let Product = table<Product>  // table declaration for queries
```

---

<details>
<summary><h2>SqlHydra.Cli Reference</h2></summary>

### Installation

**Local Install (recommended):**
```bash
dotnet new tool-manifest
dotnet tool install SqlHydra.Cli
```

### Running the CLI

```bash
dotnet sqlhydra mssql     # SQL Server
dotnet sqlhydra npgsql    # PostgreSQL
dotnet sqlhydra sqlite    # SQLite
dotnet sqlhydra oracle    # Oracle
dotnet sqlhydra mysql     # MySQL
```

- If no `.toml` config exists, a wizard will guide you through setup
- If a `.toml` config exists, it regenerates code using that config
- Generated `.fs` files are automatically added to your `.fsproj` as `Visible="false"`

### Configuration Wizard

The wizard prompts for:

1. **Connection String** - Used to query your database schema
2. **Output Filename** - e.g., `AdventureWorks.fs`
3. **Namespace** - e.g., `MyApp.AdventureWorks`
4. **Use Case:**
   - **SqlHydra.Query integration** (default) - Generates everything needed for SqlHydra.Query
   - **Other data library** - Just the record types (for Dapper.FSharp, Donald, etc.)
   - **Standalone** - Record types + HydraReader (no SqlHydra.Query metadata)

For advanced configuration, see the [TOML Configuration Reference](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration).

### Auto-Regeneration (Build Event)

To regenerate on Rebuild in Debug mode:

```xml
<Target Name="SqlHydra" BeforeTargets="Clean" Condition="'$(Configuration)' == 'Debug'">
  <Exec Command="dotnet sqlhydra mssql" />
</Target>
```

### Multiple TOML Files

You can have multiple `.toml` files for different scenarios:

```bash
dotnet sqlhydra sqlite -t "shared.toml"
dotnet sqlhydra mssql -t "reporting.toml"
```

Useful for data migrations or generating types with different filters.

</details>

<details>
<summary><h2>Select Queries</h2></summary>

### Basic Select

```fsharp
let getProducts () =
    selectTask openContext {
        for p in SalesLT.Product do
        select p
    }
```

### Where Clauses

```fsharp
let getExpensiveProducts minPrice =
    selectTask openContext {
        for p in SalesLT.Product do
        where (p.ListPrice > minPrice)
        select p
    }
```

**Where operators:**
| Operator | Function | Description |
|----------|----------|-------------|
| `\|=\|` | `isIn` | Column IN list |
| `\|<>\|` | `isNotIn` | Column NOT IN list |
| `=%` | `like` | LIKE pattern |
| `<>%` | `notLike` | NOT LIKE pattern |
| `= None` | `isNullValue` | IS NULL |
| `<> None` | `isNotNullValue` | IS NOT NULL |

```fsharp
// Filter where City starts with 'S'
let getCitiesStartingWithS () =
    selectTask openContext {
        for a in SalesLT.Address do
        where (a.City =% "S%")
        select a
    }
```

### Conditional Where (v3.0+)

Use `&&` to conditionally include/exclude where clauses:

```fsharp
let getAddresses (cityFilter: string option) (zipFilter: string option) =
    selectTask openContext {
        for a in Person.Address do
        where (
            (cityFilter.IsSome && a.City = cityFilter.Value) &&
            (zipFilter.IsSome && a.PostalCode = zipFilter.Value)
        )
    }
```

If `cityFilter.IsSome` is `false`, that clause is excluded from the query.

### Joins

```fsharp
// Inner join
let getProductsWithCategory () =
    selectTask openContext {
        for p in SalesLT.Product do
        join c in SalesLT.ProductCategory on (p.ProductCategoryID.Value = c.ProductCategoryID)
        select (p, c.Name)
        take 10
    }

// Left join (joined table becomes Option)
let getCustomerAddresses () =
    selectTask openContext {
        for c in SalesLT.Customer do
        leftJoin a in SalesLT.Address on (c.AddressID = a.Value.AddressID)
        select (c, a)
    }
```

> **Note:** In join `on` clauses, put the known (left) table on the left side of the `=`.

### Selecting Columns

```fsharp
// Select specific columns
let getCityStates () =
    selectTask openContext {
        for a in SalesLT.Address do
        select (a.City, a.StateProvince)
    }

// Transform results with mapList
let getCityLabels () =
    selectTask openContext {
        for a in SalesLT.Address do
        select (a.City, a.StateProvince) into (city, state)
        mapList $"City: {city}, State: {state}"
    }
```

### Aggregates

```fsharp
let getCategoriesWithHighPrices () =
    selectTask openContext {
        for p in SalesLT.Product do
        where (p.ProductCategoryID <> None)
        groupBy p.ProductCategoryID
        having (avgBy p.ListPrice > 500M)
        select (p.ProductCategoryID, avgBy p.ListPrice)
    }

// Count
let getCustomerCount () =
    selectTask openContext {
        for c in SalesLT.Customer do
        count
    }
```

**Aggregate functions:** `countBy`, `sumBy`, `minBy`, `maxBy`, `avgBy`

> **Warning:** If an aggregate might return NULL (e.g., `minBy` on an empty result set), wrap in `Some`:
> ```fsharp
> select (minBy (Some p.ListPrice))  // Returns Option
> ```

### Subqueries

```fsharp
// Subquery returning multiple values
let top5Categories =
    select {
        for p in SalesLT.Product do
        groupBy p.ProductCategoryID
        orderByDescending (avgBy p.ListPrice)
        select p.ProductCategoryID
        take 5
    }

let getTopCategoryNames () =
    selectTask openContext {
        for c in SalesLT.ProductCategory do
        where (Some c.ProductCategoryID |=| subqueryMany top5Categories)
        select c.Name
    }

// Subquery returning single value
let avgPrice =
    select {
        for p in SalesLT.Product do
        select (avgBy p.ListPrice)
    }

let getAboveAverageProducts () =
    selectTask openContext {
        for p in SalesLT.Product do
        where (p.ListPrice > subqueryOne avgPrice)
        select p
    }
```

### Other Operations

```fsharp
// Ordering
selectTask openContext {
    for p in SalesLT.Product do
    orderBy p.Name
    thenByDescending p.ListPrice
    select p
}

// Conditional ordering with ^^
let getAddresses (sortByCity: bool) =
    selectTask openContext {
        for a in Person.Address do
        orderBy (sortByCity ^^ a.City)
        select a
    }

// Pagination
selectTask openContext {
    for p in SalesLT.Product do
    skip 10
    take 20
    select p
}

// Distinct
selectTask openContext {
    for c in SalesLT.Customer do
    select (c.FirstName, c.LastName)
    distinct
}

// Get single/optional result
selectTask openContext {
    for p in SalesLT.Product do
    where (p.ProductID = 123)
    select p
    tryHead  // Returns Option
}
```

### Transforming Results (Important!)

The `select` clause only supports selecting columns/tables - **not** transformations like `.ToString()` or string interpolation.

**Correct:** Transform in `mapList`/`mapArray`/`mapSeq`:
```fsharp
selectTask openContext {
    for a in SalesLT.Address do
    select (a.City, a.StateProvince) into (city, state)
    mapList $"City: {city}, State: {state}"
}
```

**Incorrect:** Transforming in `select` throws at runtime:
```fsharp
// DON'T DO THIS - will throw!
selectTask openContext {
    for a in SalesLT.Address do
    select ($"City: {a.City}")
}
```

</details>

<details>
<summary><h2>Insert, Update, Delete</h2></summary>

### Insert

```fsharp
// Simple insert
let! rowsInserted =
    insertTask openContext {
        into dbo.Person
        entity { ID = Guid.NewGuid(); FirstName = "John"; LastName = "Doe" }
    }

// Insert with identity column
let! newId =
    insertTask openContext {
        for e in dbo.ErrorLog do
        entity { ErrorLogID = 0; ErrorMessage = "Test"; (* ... *) }
        getId e.ErrorLogID  // Returns the generated ID
    }

// Multiple inserts
match items |> AtLeastOne.tryCreate with
| Some items ->
    insertTask openContext {
        into dbo.Product
        entities items
    }
| None ->
    printfn "Nothing to insert"
```

### Upsert (Postgres/SQLite only)

```fsharp
open SqlHydra.Query.NpgsqlExtensions  // or SqliteExtensions

insertTask openContext {
    for a in Person.Address do
    entity address
    onConflictDoUpdate a.AddressID (a.City, a.PostalCode, a.ModifiedDate)
}
```

### Update

```fsharp
// Update specific fields
updateTask openContext {
    for e in dbo.ErrorLog do
    set e.ErrorMessage "Updated message"
    set e.ErrorNumber 500
    where (e.ErrorLogID = 1)
}

// Update entire entity
updateTask openContext {
    for e in dbo.ErrorLog do
    entity errorLog
    excludeColumn e.ErrorLogID  // Don't update the ID
    where (e.ErrorLogID = errorLog.ErrorLogID)
}

// Update all rows (requires explicit opt-in)
updateTask openContext {
    for c in Sales.Customer do
    set c.AccountNumber "123"
    updateAll
}
```

### Delete

```fsharp
deleteTask openContext {
    for e in dbo.ErrorLog do
    where (e.ErrorLogID = 5)
}

// Delete all rows (requires explicit opt-in)
deleteTask openContext {
    for c in Sales.Customer do
    deleteAll
}
```

</details>

<details>
<summary><h2>Advanced Topics</h2></summary>

### Sharing a QueryContext

```fsharp
let getUserWithOrders email = task {
    use ctx = openContext()

    let! user = selectTask ctx {
        for u in dbo.Users do
        where (u.Email = email)
        tryHead
    }

    let! orders = selectTask ctx {
        for o in dbo.Orders do
        where (o.CustomerEmail = email)
        select o
    }

    return (user, orders)
}
```

### Custom SqlKata Operations

For operations not directly supported, use the `kata` operation:

```fsharp
select {
    for c in main.Customer do
    where (c.FirstName = "John")
    kata (fun query ->
        query.OrderByRaw("LastName COLLATE NOCASE")
    )
}
```

### Custom SQL with HydraReader

```fsharp
let getTop10Products (conn: SqlConnection) = task {
    let sql = "SELECT TOP 10 * FROM Product"
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    let hydra = HydraReader(reader)

    return [
        while reader.Read() do
            hydra.``dbo.Product``.Read()
    ]
}
```

### SQL Server OUTPUT Clause

```fsharp
open SqlHydra.Query.SqlServerExtensions

let! (created, updated) =
    insertTask openContext {
        for p in dbo.Person do
        entity person
        output (p.CreateDate, p.UpdateDate)
    }
```

</details>

<details>
<summary><h2>Database-Specific Notes</h2></summary>

### PostgreSQL

**Enum Types:** Postgres enums are generated as CLR enums. Register them with Npgsql:

```fsharp
let dataSource =
    let builder = NpgsqlDataSourceBuilder("connection string")
    builder.MapEnum<ext.mood>("ext.mood") |> ignore
    builder.Build()
```

**Arrays:** `text[]` and `integer[]` column types are supported.

### SQLite

SQLite uses type affinity. Use standard type aliases in your schema for proper .NET type mapping.
See: [SQLite Type Affinity](https://www.sqlite.org/datatype3.html#affinity_name_examples)

### SQL Server

If you get SSL certificate errors, append `;TrustServerCertificate=True` to your connection string.
(Fixed in `Microsoft.Data.SqlClient` v4.1.1+)

</details>

<details>
<summary><h2>Supported Frameworks</h2></summary>

- .NET 8 and .NET 9 are supported
- For .NET 5 support, use the older provider-specific tools (`SqlHydra.SqlServer`, etc.)

</details>

<details>
<summary><h2>Contributing</h2></summary>

- Uses VS Code Remote Containers for dev environment with test databases
- Or run `docker-compose` manually with your IDE
- See [Contributing Wiki](https://github.com/JordanMarr/SqlHydra/wiki/Contributing)

### Contributors

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tr>
    <td align="center">
        <a href="https://github.com/MargaretKrutikova"><img src="https://avatars.githubusercontent.com/u/5932274?v=4?s=100" style="width: 100px" alt=""/>
        <br /><a href="https://github.com/JordanMarr/SqlHydra/pull/10" title="Code">💻</a>
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

</details>

---

## Links

- [TOML Configuration Reference](https://github.com/JordanMarr/SqlHydra/wiki/TOML-Configuration)
- [Using HydraReader with other libraries](https://github.com/JordanMarr/SqlHydra/wiki/DataReaders)
- [SqlKata Documentation](https://sqlkata.com/)

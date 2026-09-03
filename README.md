# SqlHydra

Type-safe SQL generation for F#. Generate types from your database, query with strongly-typed computation expressions.

[![SqlHydra.Cli NuGet](https://img.shields.io/nuget/v/SqlHydra.Cli.svg?style=flat-square&label=SqlHydra.Cli)](https://www.nuget.org/packages/SqlHydra.Cli/)
[![SqlHydra.Query NuGet](https://img.shields.io/nuget/v/SqlHydra.Query.svg?style=flat-square&label=SqlHydra.Query)](https://www.nuget.org/packages/SqlHydra.Query/)

**Supported Databases:** SQL Server | PostgreSQL | SQLite | Oracle | MySQL

---

## Quick Start

**1. Install the CLI tool locally:**
```bash
dotnet new tool-manifest
dotnet tool install --local SqlHydra.Cli
```

**2. Generate types from your database:**
```bash
dotnet sqlhydra mssql    # or: npgsql, sqlite, oracle, mysql
```
The wizard will prompt you for **connection string**, **output file**, and **namespace**.

**3. Install the query library:**
```bash
dotnet add package SqlHydra.Query
```

**4. Configure Query Context:**

SqlHydra.Cli now generates a DB‑specific `QueryContextFactory` for each generated database (perfect for DI injection). 

Use it to create a strongly‑typed query context:
```fsharp
let db = AdventureWorks.QueryContextFactory.Create(connStr, printfn "SQL: %O") // Optional SQL output logging
```

**5. Write your first query:**

```fsharp
open SqlHydra.Query
open AdventureWorks

// Query with full type safety
let getProducts minPrice =
    selectTask db {
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

SqlHydra.Cli reads your database schema and adds a generated file to your project that contains:

- **F# record types** for each table (with `Option` types for nullable columns)
- **Table declarations** for use in queries
- **`QueryContextFactory`** with a static `Create(connectionString: string)` method.

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
let getProducts (db: QueryContextFactory)  =
    selectTask db {
        for p in SalesLT.Product do
        select p
    }
```

### Where Clauses

```fsharp
let getExpensiveProducts (db: QueryContextFactory) minPrice =
    selectTask db {
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
let getCitiesStartingWithS (db: QueryContextFactory)  =
    selectTask db {
        for a in SalesLT.Address do
        where (a.City =% "S%")
        select a
    }
```

### Conditional Where (v3.0+)

Use `&&` to conditionally include/exclude where clauses:

```fsharp
let getAddresses (db: QueryContextFactory) (cityFilter: string option) (zipFilter: string option) =
    selectTask db {
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
let getProductsWithCategory (db: QueryContextFactory)  =
    selectTask db {
        for p in SalesLT.Product do
        join c in SalesLT.ProductCategory on (p.ProductCategoryID.Value = c.ProductCategoryID)
        select (p, c.Name)
        take 10
    }

// Left join (joined table becomes Option).
// You can use `|> Option.map` to select specifc left joined columns.
let getCustomerAddresses (db: QueryContextFactory)  =
    selectTask db {
        for c in SalesLT.Customer do
        leftJoin a in SalesLT.Address on (c.AddressID = a.Value.AddressID)
        select (
            c.Email, 
            a |> Option.map _.State
        ) into selected
        mapList (
            let email, stateMaybe = selected
            let state = stateMaybe |> Option.defaultValue "N/A"
            $"Customer: {email}, State: {state}"
        )
    }


// Improved join syntax with `join'` and `leftJoin'` lets you use full predicates in `on'` clauses.
// * Makes multi-column joins much cleaner (no need for tuple comparison).
// * Allows full predicates (e.g., AND/OR) in join conditions.
// * Optional cheeky usage of `;` if you want `on'` on the same line!
selectTask db {
    for o in Sales.SalesOrderHeader do
    join' d in Sales.SalesOrderDetail; on' (o.ID = d.OrderID && o.Status = "Completed")
    select o
}

```

> **Note:** In join `on` clauses, put the known (left) table on the left side of the `=`.

### Selecting Columns

```fsharp
// Select specific columns
let getCityStates (db: QueryContextFactory)  =
    selectTask db {
        for a in SalesLT.Address do
        select (a.City, a.StateProvince)
    }

// Transform results with mapList
let getCityLabels (db: QueryContextFactory)  =
    selectTask db {
        for a in SalesLT.Address do
        select (a.City, a.StateProvince) into (city, state)
        mapList $"City: {city}, State: {state}"
    }
```

### Aggregates

```fsharp
let getCategoriesWithHighPrices (db: QueryContextFactory)  =
    selectTask db {
        for p in SalesLT.Product do
        where (p.ProductCategoryID <> None)
        groupBy p.ProductCategoryID
        having (avgBy p.ListPrice > 500M)
        select (p.ProductCategoryID, avgBy p.ListPrice)
    }

// Count
let getCustomerCount (db: QueryContextFactory)  =
    selectTask db {
        for c in SalesLT.Customer do
        count
    }
```

**Aggregate functions:** `countBy`, `sumBy`, `minBy`, `maxBy`, `avgBy`

> **Warning:** If an aggregate might return NULL (e.g., `minBy` on an empty result set), wrap in `Some`:
> ```fsharp
> select (minBy (Some p.ListPrice))  // Returns Option
> ```

### SQL Functions

SqlHydra.Query includes built-in SQL functions for each supported database provider. These can be used in both `select` and `where` clauses.

> If a query needs several functions just to take shape, consider whether plain SQL would be clearer — see [When to Reach for Plain SQL](#when-to-reach-for-plain-sql).

**Setup:**
```fsharp
// Import the extension module for your database provider:
open SqlHydra.Query.SqlServerExtensions  // SQL Server
open SqlHydra.Query.NpgsqlExtensions     // PostgreSQL
open SqlHydra.Query.SqliteExtensions     // SQLite
open SqlHydra.Query.OracleExtensions     // Oracle
open SqlHydra.Query.MySqlExtensions      // MySQL

open type SqlFn  // Optional: allows unqualified access, e.g. LEN vs SqlFn.LEN
```

**Use in select and where clauses:**
```fsharp
// String functions
selectTask db {
    for p in Person.Person do
    where (LEN(p.FirstName) > 3)
    select (p.FirstName, LEN(p.FirstName), UPPER(p.FirstName))
}
// Generates: SELECT ... WHERE LEN([p].[FirstName]) > 3

// Null handling - ISNULL accepts Option<'T> and returns unwrapped 'T
selectTask db {
    for p in Person.Person do
    select (ISNULL(p.MiddleName, "N/A"))  // Option<string> -> string
}

// Date functions
selectTask db {
    for o in Sales.SalesOrderHeader do
    where (YEAR(o.OrderDate) = 2024)
    select (o.OrderDate, YEAR(o.OrderDate), MONTH(o.OrderDate))
}

// Compare two functions
selectTask db {
    for p in Person.Person do
    where (LEN(p.FirstName) < LEN(p.LastName))
    select (p.FirstName, p.LastName)
}
```

**Built-in functions** include string functions (`LEN`, `UPPER`, `SUBSTRING`, etc.), null handling (`ISNULL`/`COALESCE` with overloads for `Option<'T>` and `Nullable<'T>`), numeric functions (`ABS`, `ROUND`, etc.), and date/time functions (`GETDATE`, `YEAR`, `MONTH`, etc.).

See the full list for each provider:
- [SQL Server](src/SqlHydra.Query/SqlServerExtensions.fs)
- [PostgreSQL](src/SqlHydra.Query/NpgsqlExtensions.fs)
- [SQLite](src/SqlHydra.Query/SqliteExtensions.fs)
- [Oracle](src/SqlHydra.Query/OracleExtensions.fs)
- [MySQL](src/SqlHydra.Query/MySqlExtensions.fs)

**Define custom functions:**

You can easily define your own SQL function wrappers using the `sqlFn` helper. Mark them with `[<SqlHydraFunction>]` - one marker on the module covers every wrapper in it:
```fsharp
// The function name becomes the SQL function name
[<SqlHydraFunction>]
module CustomFn =
    let SOUNDEX (s: string) : string = sqlFn
    let DIFFERENCE (s1: string, s2: string) : int = sqlFn

// Use in queries
selectTask db {
    for p in Person.Person do
    where (CustomFn.SOUNDEX(p.LastName) = CustomFn.SOUNDEX("Smith"))
    select p.LastName
}
```

Add `[<AutoOpen>]` to the module to call them unqualified, as `open type SqlFn` does for the built-ins.

The marker also goes on a single function, or on a type of static members:
```fsharp
[<SqlHydraFunction>]
let LEVENSHTEIN (s1: string, s2: string) : int = sqlFn

[<SqlHydraFunction>]
type TextFn =
    static member METAPHONE(s: string) : string = sqlFn
```

> **Note:** `[<SqlHydraFunction>]` is what tells `where` and `on'` to render a call as SQL rather than compute it as a .NET value. Everything declared inside a marked module or type qualifies, nesting included, so keep ordinary helpers out of one. `select` and `orderBy` render every call and work without the marker.
>
> Extension packages that ship their own `sqlFn` wrappers need the marker for the same reason — their assembly is not `SqlHydra.Query`, so a `where` would otherwise try to compute the call.
>
> A `sqlFn` body has no runtime meaning, so executing one raises `SqlFunctionNotRenderedException` - which is what a missing marker gets you, rather than a query that quietly compares your column to `NULL`. If you use a function name the database does not have, you get a database error at runtime.

**PostgreSQL functions are generated from the catalog.** The members of the Npgsql `SqlFn` between `// <generated>` and `// </generated>` come from `pg_proc`: argument and return types, and `proisstrict` (NULL in means NULL out), which gives every parameter of a strict function a `'T option` twin. `src/SqlHydra.Query/codegen/NpgsqlSqlFn.allowlist` lists one overload per line and chooses which functions appear; the catalog decides their shape. Each member is executed once at generation, so a function that cannot be called as `NAME(args)` is never emitted, and a keyword-named one such as `position` renders schema-qualified.

```
lpad s:string length:int fill:string     # the (text, integer, text) overload, with parameter names
trim=btrim s:string                      # `trim` is parser sugar; its shape lives under btrim
concat s1:string s2:string               # a variadic function takes whatever list you write
```

```bash
dotnet fsi src/SqlHydra.Query/codegen/NpgsqlSqlFn.fsx           # rewrites the region
dotnet fsi src/SqlHydra.Query/codegen/NpgsqlSqlFn.fsx --check   # CI: fails when the region is stale
```

The same script emits a type for your own database, extensions included:

```bash
dotnet fsi NpgsqlSqlFn.fsx --conn "Host=...;Database=app" --allowlist app-fns.txt \
    --schema public --map vector=Pgvector.Vector \
    --module App.Sql --type PgFn --out src/App/PgFn.fs
```

Then `open type PgFn` next to `SqlFn`. The query visitor only needs `[<SqlHydraFunction>]` and the member name, so a generated type is treated exactly like the built-in one.

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

let getTopCategoryNames (db: QueryContextFactory)  =
    selectTask db {
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

let getAboveAverageProducts (db: QueryContextFactory)  =
    selectTask db {
        for p in SalesLT.Product do
        where (p.ListPrice > subqueryOne avgPrice)
        select p
    }

// WHERE EXISTS / WHERE NOT EXISTS
// Use `correlate` in the subquery to reference a table from the outer query
// (name the correlated variable the same as the outer query's variable).
let orderDetails =
    select {
        for d in SalesLT.SalesOrderDetail do
        correlate o in SalesLT.SalesOrderHeader
        where (d.SalesOrderID = o.SalesOrderID)
        select d.SalesOrderID
    }

let getOrdersWithDetails (db: QueryContextFactory)  =
    selectTask db {
        for o in SalesLT.SalesOrderHeader do
        whereExists orderDetails
        select o
    }
```

### Common Table Expressions (CTEs)

`cte<'T>` creates a named `WITH` source from a select query. The resulting source can be queried like a table (and can be used as the inner side of `join'` / `leftJoin'`):

```fsharp
let dallasAddresses =
    select {
        for a in SalesLT.Address do
        where (a.City = "Dallas")
    }

let dallas = cte<SalesLT.Address> "dallas_addresses" dallasAddresses

let getDallasAddressIds (db: QueryContextFactory)  =
    selectTask db {
        for a in dallas do
        select a.AddressID
    }
// WITH "dallas_addresses" AS (SELECT ... WHERE ...)
// SELECT "a"."AddressID" FROM "dallas_addresses" AS "a"
```

Use `cteFrom<'T>` instead when the CTE's row shape differs from the inner select's row type (e.g. when the inner query builds computed columns with raw SELECT fragments); `'T` is the type the rows will be read back as.

### Other Operations

```fsharp
// Ordering
selectTask db {
    for p in SalesLT.Product do
    orderBy p.Name
    thenByDescending p.ListPrice
    select p
}

// Conditional ordering with ^^
let getAddresses (db: QueryContextFactory) (sortByCity: bool) =
    selectTask db {
        for a in Person.Address do
        orderBy (sortByCity ^^ a.City)
        select a
    }

// Pagination
selectTask db {
    for p in SalesLT.Product do
    skip 10
    take 20
    select p
}

// Distinct
selectTask db {
    for c in SalesLT.Customer do
    select (c.FirstName, c.LastName)
    distinct
}

// Get single/optional result
selectTask db {
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
selectTask db {
    for a in SalesLT.Address do
    select (a.City, a.StateProvince) into (city, state)
    mapList $"City: {city}, State: {state}"
}
```

**Incorrect:** Transforming in `select` throws at runtime:
```fsharp
// DON'T DO THIS - will throw!
selectTask db {
    for a in SalesLT.Address do
    select ($"City: {a.City}")
}
```

</details>

<details>
<summary><h2>Insert, Update, Upsert, Delete</h2></summary>

### Insert

```fsharp
// Simple insert
let! rowsInserted =
    insertTask db {
        into dbo.Person
        entity { ID = Guid.NewGuid(); FirstName = "John"; LastName = "Doe" }
    }

// Insert with identity column
let! newId =
    insertTask db {
        for e in dbo.ErrorLog do
        entity { ErrorLogID = 0; ErrorMessage = "Test"; (* ... *) }
        getId e.ErrorLogID  // Returns the generated ID
    }

// Multiple inserts
match items |> AtLeastOne.tryCreate with
| Some items ->
    insertTask db {
        into dbo.Product
        entities items
    }
| None ->
    printfn "Nothing to insert"
```

### Insert From Select (`fromSelect`)

Inserts the results of a select query in a single SQL command (`INSERT INTO ... SELECT ...`) — the rows never round-trip through your application:

```fsharp
let dallasAddressLines =
    select {
        for a in SalesLT.Address do
        where (a.City = "Dallas")
        select a.AddressLine1
    }

let! rowsCopied =
    insertTask db {
        for arch in dbo.AddressArchive do
        fromSelect dallasAddressLines
        includeColumn arch.AddressLine1  // Target column list, in select-column order
    }
```

### Update

```fsharp
// Update specific fields
updateTask db {
    for e in dbo.ErrorLog do
    set e.ErrorMessage "Updated message"
    set e.ErrorNumber 500
    where (e.ErrorLogID = 1)
}

// Update entire entity
updateTask db {
    for e in dbo.ErrorLog do
    entity errorLog
    excludeColumn e.ErrorLogID  // Don't update the ID
    where (e.ErrorLogID = errorLog.ErrorLogID)
}

// Update all rows (requires explicit opt-in)
updateTask db {
    for c in Sales.Customer do
    set c.AccountNumber "123"
    updateAll
}
```

### Upsert - SQL Server (`insertOrUpdateOnUnique`)

SqlHydra.Query v3.5+ supports **insert-or-update (upsert)** for SQL Server via the new `insertOrUpdateOnUnique` custom operation. This allows you to atomically insert a row or update it if a row with the same unique key already exists.

The goal was to provide a built-in upsert capability for SQL Server that is analogous to the `onConflictDoUpdate` style upsert extensions already available for SQLite and PostgreSQL queries. A key design decision was to avoid using SQL Server's `MERGE` statement in order to sidestep its [well-known footguns ](https://www.mssqltips.com/sqlservertip/3074/use-caution-with-sql-servers-merge-statement/).

#### How It Works

The generated SQL uses a `TRY/CATCH` pattern that:
1. Attempts the `INSERT`
2. If it fails with a duplicate key violation (error 2627 or 2601), falls back to an `UPDATE`
3. If the `UPDATE` affects 0 rows (due to a concurrent delete), retries the `INSERT`

```fsharp
open SqlHydra.Query.SqlServerExtensions

let saveUser (user: Domain.User) =
    let utcNow = System.DateTime.UtcNow
    
    insertTask db {
        for u in dbo.Users do
        entity {
            Id = user.Id
            Username = user.Username
            Email = user.Email
            CreatedDate = utcNow
            UpdatedDate = utcNow
        }
        insertOrUpdateOnUnique
            // Match on unique key (supports tuple for composite keys):
            u.Id
            // If unique key is matched, update columns in the tuple below:
            (
                u.Username, 
                u.Email, 
                u.UpdatedDate
            )
    }
```

### Upsert - PostgreSQL and SQLite (`onConflictDoUpdate`)

```fsharp
open SqlHydra.Query.NpgsqlExtensions
// open SqlHydra.Query.SqliteExtensions

let saveUser (user: Domain.User) =
    let utcNow = System.DateTime.UtcNow
    
    insertTask db {
        for u in dbo.Users do
        entity {
            Id = user.Id
            Username = user.Username
            Email = user.Email
            CreatedDate = utcNow
            UpdatedDate = utcNow
        }
        onConflictDoUpdate
            u.Id // If key is matched, update columns in the tuple below:
            (
                u.Username, 
                u.Email, 
                u.UpdatedDate
            )
    }
```

### Delete

```fsharp
deleteTask db {
    for e in dbo.ErrorLog do
    where (e.ErrorLogID = 5)
}

// Delete all rows (requires explicit opt-in)
deleteTask db {
    for c in Sales.Customer do
    deleteAll
}
```

### Returning Values - PostgreSQL and SQLite (`returning`)

The `insert`, `update`, and `delete` builders support a `returning` operation that emits a `RETURNING` clause. When present, the task returns the requested column value (or tuple of values) instead of the affected row count — a single round trip:

```fsharp
// Insert and get generated values back
let! (addressId, modifiedDate) =
    insertTask db {
        for a in SalesLT.Address do
        entity newAddress
        returning (a.AddressID, a.ModifiedDate)
    }

// Update ... returning
let! updatedCity =
    updateTask db {
        for a in SalesLT.Address do
        set a.City "Dallas"
        where (a.AddressID = 5)
        returning a.City
    }
```

> **Note:** When combined with `onConflictDoNothing` and no row is actually inserted, the returned value is the type's default.

</details>

<details>
<summary><h2>Advanced Topics</h2></summary>

### Sharing a QueryContext Transaction Across Multiple Operations

```fsharp
let completeOrder (db: QueryContextFactory) orderId = task {
    use! shared = db.OpenContextAsync()
    shared.BeginTransaction()        

    // Update status for order
    do! updateTask shared {
            for o in dbo.Orders do
            set o.Status "Complete"
            where (o.Id = orderId)
        } : Task

    // Write to audit log
    do! insertTask shared {
            into dbo.AuditLog
            entity { Message = $"Completed order {orderId}"; Timestamp = DateTime.UtcNow }
        } : Task

    shared.CommitTransaction()
}
```

### When to Reach for Plain SQL

SqlHydra is designed to give you type safety for the 90–95% of queries that follow a common shape: select, filter, join, aggregate, insert, update. For the last 5–10% — the genuinely bespoke reporting query, the vendor-specific trick, the statement a DBA handed you — plain SQL is usually the clearer tool, and using it is the intended path, not a failure of the library.

A few guidelines:

- **Prefer the query builder** when the query reads naturally in it. You get compile-time checking against your schema and refactoring support for free.
- **Consider using plain SQL** when expressing the query would require stacking several `sqlFn` wrappers, `rawExpr`, or `inlineValue` calls just to get the shape right. At that point the builder is no longer adding safety; it is adding a layer someone must learn before they can read the query.
- **You don't have to use a feature because it exists.** The function machinery, CTEs, lateral joins, and plugin operators are there for domains that need them everywhere (vector search, for example, is *entirely* functions and operators). If you only need them once, a SQL string is often the more maintainable choice.

The [`HydraReader`](#custom-sql-with-hydrareader) below lets you keep the generated types on the result side even when the query itself is hand-written, so dropping down to SQL only costs you the typing on the query text — never on the rows.

### Custom SQL with HydraReader

```fsharp
let getTop10Products (db: QueryContextFactory) (conn: SqlConnection) = task {
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
    insertTask db {
        for p in dbo.Person do
        entity person
        output (p.CreateDate, p.UpdateDate)
    }
```

</details>

<details>
<summary><h2>Database-Specific Notes</h2></summary>

### PostgreSQL

**Enum Types:** Postgres enums are generated as CLR enums and registered with Npgsql automatically.
When enums exist (and `provider_db_type_attributes` is enabled), the generated code includes an `Enums.register` helper, and the generated `QueryContextFactory.Create(connectionString)` applies it for you — no manual `MapEnum` calls needed:

```fsharp
let factory = QueryContextFactory.Create("connection string")
```

If you build your own `NpgsqlDataSource`, pipe the builder through `Enums.register` before `Build()`:

```fsharp
let dataSource =
    NpgsqlDataSourceBuilder("connection string")
    |> Enums.register
    |> _.Build()

let factory = QueryContextFactory.Create(dataSource)
```

**Arrays:** `text[]` and `integer[]` column types are supported.

**Lateral Joins:** `lateralJoin` (in `SqlHydra.Query.NpgsqlExtensions`) emits `LEFT JOIN LATERAL (subquery) AS "alias" ON true`. Use `correlate` inside the subquery to reference outer tables, and `lateralCol<'T> "alias" "column"` to read the subquery's columns in the outer select:

```fsharp
open SqlHydra.Query.NpgsqlExtensions

let latestDetail =
    select {
        for d in sales.salesorderdetail do
        correlate o in sales.salesorderheader
        where (d.salesorderid = o.salesorderid)
        orderByDescending d.modifieddate
        select d.orderqty
        take 1
    }

selectTask db {
    for o in sales.salesorderheader do
    lateralJoin latestDetail "latest"
    select (o.salesorderid, lateralCol<int16> "latest" "orderqty")
}
```

### SQLite

SQLite uses type affinity. Use standard type aliases in your schema for proper .NET type mapping.
See: [SQLite Type Affinity](https://www.sqlite.org/datatype3.html#affinity_name_examples)

### SQL Server

If you get SSL certificate errors, append `;TrustServerCertificate=True` to your connection string.
(Fixed in `Microsoft.Data.SqlClient` v4.1.1+)

</details>

<details>
<summary><h2>Extensibility</h2></summary>

### Creating a Custom Database Provider

SqlHydra supports 5 built-in database providers (SQL Server, PostgreSQL, SQLite, MySQL, Oracle), but you can add support for any database by implementing the `ISqlHydraDbProvider` interface from `SqlHydra.Domain`.

#### Implementing the Provider

Create a library project that references `SqlHydra.Domain` and implements `ISqlHydraDbProvider`:

```fsharp
open SqlHydra.Domain

type DuckDbProvider() =
    interface ISqlHydraDbProvider with
        member _.Id = "duckdb"
        member _.Name = "SqlHydra.DuckDB"
        member _.Type = Custom "DuckDb"
        member _.DefaultReaderType = "System.Data.Common.DbDataReader"
        member _.DefaultProvider = "DuckDB.NET.Data"
        member _.SqlEmitter = "MyApp.DuckDbEmitter()"
        member _.ProviderConnectionType = "DuckDB.NET.Data.DuckDBConnection"
        member _.GetSchema(cfg, isLegacy, extensions) =
            // Query database metadata and return a Schema
            // with Tables, Columns, and type mappings
            ...
```

The `GetSchema` method is the core of your provider -- it connects to the database using `cfg.ConnectionString`, reads schema metadata (tables, columns, types), applies any `IExtendTypeMapping` extensions, and returns a `Schema` record that SqlHydra uses to generate F# types.

The `SqlEmitter` property should be the fully-qualified constructor expression for your `ISqlEmitter` implementation (used in the generated `QueryContextFactory`).

#### Running with a Custom Provider

Add your provider project as a `ProjectReference` (or publish it as a NuGet package and add a `PackageReference`), build your project, then run:

```bash
dotnet sqlhydra custom SqlHydra.Query.DuckDB --toml-file sqlhydra-duckdb.toml
```

SqlHydra will load the named assembly from the project's build output and discover the `ISqlHydraDbProvider` implementation automatically.

### Overriding Database Type Mappings

SqlHydra supports type mapping extensions via the `IExtendTypeMapping` interface in `SqlHydra.Domain`. This lets you add custom database-to-CLR type mappings that SqlHydra doesn't handle out of the box.

#### Implementing a Type Mapping Extension

Add a class implementing `IExtendTypeMapping` in your project (or in a separate library):

```fsharp
open SqlHydra.Domain

type MyCustomMapping() =
    interface IExtendTypeMapping with
        member _.Extend(baseTryFind) =
            fun (ctx: TypeMappingContext) ->
                match ctx.Column.ProviderTypeName.ToLower() with
                | "vector" ->
                    Some {
                        TypeMapping.ColumnTypeAlias = "vector"
                        TypeMapping.ClrType = "Pgvector.Vector"
                        TypeMapping.DbType = System.Data.DbType.Object
                        // No NpgsqlDbType for vector -- Pgvector.Npgsql infers it from the value.
                        TypeMapping.ProviderDbType = None
                    }
                | _ -> baseTryFind ctx
```

Your extension wraps the built-in type mapping function, giving you a chance to handle custom types before falling back to the default behavior.

#### Registering the Extension

Type mapping extensions must be explicitly registered in your TOML configuration. The name should match your project name, `PackageReference`, or `ProjectReference`:

```toml
[extensions]
type_mappings = ["MyProject"]
```

This gives you control over which providers use which extensions. For example, if you only want a custom mapping applied to SQLite, add it to `sqlhydra-sqlite.toml` but not to `sqlhydra-mssql.toml`.

> **Note:** Make sure your project is built before running `sqlhydra` so the extension assembly can be found.

#### The TypeMappingContext

Your extension receives a `TypeMappingContext` with full schema metadata for the column being mapped:

```fsharp
type TypeMappingContext =
    {
        Table: TableSchema   // Table catalog, schema, name, type, and all columns
        Column: ColumnSchema  // Column name, type, nullability, precision, scale, etc.
    }
```

This lets you make mapping decisions based on the table name, column name, schema, or any other metadata -- not just the provider type name.

#### NuGet Extension Packages

Type mapping extensions can also be published as NuGet packages. Add it as a `PackageReference` in your project and register it in your TOML configuration:

```toml
[extensions]
type_mappings = ["SqlHydra.Query.Pgvector"]
```

SqlHydra will resolve the assembly from your project's build output and load any `IExtendTypeMapping` implementations it finds.

[**SqlHydra.Query.Pgvector**](https://github.com/michaelglass/SqlHydra.Query.Pgvector) is a worked example of such a package: it maps the PostgreSQL `vector` column type to `Pgvector.Vector` and adds pgvector distance operators (`<=>`, `<->`, `<#>`) for `SqlHydra.Query`.

#### Multiple Extensions

Multiple extensions compose in order -- each wraps the previous one. An extension should call `baseTryFind ctx` for any types it doesn't handle, allowing the next extension (or the built-in mappings) to take over.

</details>

<details>
<summary><h2>Supported Frameworks</h2></summary>

- .NET 8, .NET 9, and .NET 10 are supported
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

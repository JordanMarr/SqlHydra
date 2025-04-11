namespace SqlHydra.Query

[<AutoOpen>]
module Table = 

    /// Maps the entity 'T to a table of the exact same name.
    let table<'T> = 
        let ent = typeof<'T>
        let tables = Map [Root, { Name = ent.Name; Schema = ent.DeclaringType.Name}]
        QuerySource<'T>(tables)

    /// Maps the entity 'T to a schema of the given name.
    [<System.Obsolete("The table schema is now automatically inferred from the declaring type.")>]
    let inSchema<'T> (schemaName: string) (qs: QuerySource<'T>) =
        qs

[<AutoOpen>]
module Where = 

    /// WHERE column is IN values
    let isIn<'P> (prop: 'P) (values: 'P seq) = true
    /// WHERE column is IN values
    let inline (|=|) (prop: 'P) (values: 'P seq) = true

    /// WHERE column is NOT IN values
    let isNotIn<'P> (prop: 'P) (values: 'P seq) = true
    /// WHERE column is NOT IN values
    let inline (|<>|) (prop: 'P) (values: 'P seq) = true

    /// WHERE column like value   
    let like<'P> (prop: 'P) (pattern: string) = true
    /// WHERE column like value   
    let inline (=%) (prop: 'P) (pattern: string) = true

    /// WHERE column not like value   
    let notLike<'P> (prop: 'P) (pattern: string) = true
    /// WHERE column not like value   
    let inline (<>%) (prop: 'P) (pattern: string) = true

    /// WHERE column IS NULL
    let isNullValue<'P> (prop: 'P) = true
    /// WHERE column IS NOT NULL
    let isNotNullValue<'P> (prop: 'P) = true

    /// Creates a subquery that returns a single value to be used with column comparisons.
    let subqueryOne (query: SelectQuery<'T>) : 'T = Unchecked.defaultof<'T>

    /// Creates a subquery that returns many values to be used with "isIn", "isNotIn", "|=|" or "|<>|".
    let subqueryMany (query: SelectQuery<'T>) : 'T list = []

    /// Compares two values for equality.
    let areEqual (prop: 'P) (value: 'P) = true

    /// Compares two values for inequality.
    let notEqual (prop: 'P) (value: 'P) = true

[<AutoOpen>]
module OrderBy = 

    // infix operator ^^ that takes a boolean that determines whether .
    let inline (^^) (_: bool) (prop: 'P) =
        prop

(*
Select Aggregates:

countBy, avgBy, minBy, maxBy, sumBy

select {
    for p in productsTable do
    join c in categoryTable on (p.ProductCategoryID.Value = c.ProductCategoryID)
    groupBy p.Department
    select p.Department, minBy p.Price, maxBy p.Price
}

SELECT [SalesLT].[Product].[Department], MIN([SalesLT].[Product].[Price]) AS MinPrice, MAX([SalesLT].[Product].[Price]) AS MaxPrice
*)

[<AutoOpen>]
module Aggregates =

    /// Gets the COUNT of the given column
    let countBy (prop: 'P) = Unchecked.defaultof<int>

    /// Gets the MIN of the given column
    let minBy (prop: 'P) = Unchecked.defaultof<'P>

    /// Gets the MAX of the given column
    let maxBy (prop: 'P) = Unchecked.defaultof<'P>

    /// Gets the SUM of the given column
    let sumBy (prop: 'P when 'P : struct) = Unchecked.defaultof<'P>

    /// Gets the AVG of the given column
    let avgBy (prop: 'P when 'P : struct) = Unchecked.defaultof<'P>

    /// Gets the AVG of the given column and returns 'Result.
    let avgByAs<'P, 'Result when 'P : struct and 'Result : struct> (prop: 'P) : 'Result = Unchecked.defaultof<'Result>

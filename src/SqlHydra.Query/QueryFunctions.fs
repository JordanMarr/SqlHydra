[<AutoOpen>]
module SqlHydra.Query.QueryFunctions

/// WHERE column is IN values
let isIn<'P> (prop: 'P) (values: 'P list) = true
/// WHERE column is IN values
let inline (|=|) (prop: 'P) (values: 'P list) = true

/// WHERE column is NOT IN values
let isNotIn<'P> (prop: 'P) (values: 'P list) = true
/// WHERE column is NOT IN values
let inline (|<>|) (prop: 'P) (values: 'P list) = true

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

///// Gets the COUNT of the given column
//let countBy (prop: 'P) = int

///// Gets the MIN of the given column
//let minBy (prop: 'P) = Unchecked.defaultof<'P>

///// Gets the MAX of the given column
//let maxBy (prop: 'P) = Unchecked.defaultof<'P>

///// Gets the SUM of the given column
//let sumBy (prop: 'P when 'P : struct) = Unchecked.defaultof<'P>

///// Gets the AVG of the given column
//let avgBy (prop: 'P when 'P : struct) = Unchecked.defaultof<'P>

///// Gets the AVG of the given column and returns 'Result.
//let avgByAs<'P, 'Result when 'P : struct and 'Result : struct> (prop: 'P) : 'Result = Unchecked.defaultof<'Result>

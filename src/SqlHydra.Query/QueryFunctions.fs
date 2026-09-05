namespace SqlHydra.Query

/// Marks `sqlFn` wrappers as SQL functions, so that a `where` or `on'` predicate renders the
/// call as SQL instead of computing it as a .NET value. SqlHydra's own query functions carry it
/// too: it is the only thing that makes a call a SQL function.
/// Apply it to one function, or to the module or type holding a group of them: everything
/// declared inside a marked module or type qualifies, nesting included, so keep ordinary
/// helpers out of one.
/// Unmarked wrappers still work in `select` and `orderBy`, which render every call; in a
/// predicate they raise `SqlFunctionNotRenderedException` rather than compare against a stub.
///
/// Example:
///   [<SqlHydraFunction>]
///   module SqlFn =
///       let SOUNDEX (s: string) : string = sqlFn
///       let DIFFERENCE (s1: string, s2: string) : int = sqlFn
[<System.AttributeUsage(System.AttributeTargets.Method ||| System.AttributeTargets.Class)>]
type SqlHydraFunctionAttribute() =
    inherit System.Attribute()

/// Raised when a `sqlFn` wrapper is executed as ordinary .NET code instead of being rendered
/// as SQL: either it was called outside a query expression, or it is used in a `where`/`on'`
/// predicate without `[<SqlHydraFunction>]`, so the visitor took it for a value to compute.
type SqlFunctionNotRenderedException(message: string, inner: exn) =
    inherit System.InvalidOperationException(message, inner)
    new (message: string) = SqlFunctionNotRenderedException(message, null)

[<AutoOpen>]
[<SqlHydraFunction>]
module Table =

    /// A table row's write record, for `writeEntity` and the typed `…Write` operations.
    let toWrite (row: #SqlHydra.IHasWrite<'Write>) : 'Write = row.ToWrite()

    /// Maps the entity 'T to a table of the exact same name.
    let table<'T> =
        let ent = typeof<'T>
        let tables = Map [Root, { Name = ent.Name; Schema = ent.DeclaringType.Name}]
        QuerySource<'T>(tables)

    /// Creates a CTE source: `WITH alias AS (innerQuery) SELECT ... FROM alias`.
    /// The inner query's row type matches the outer 'T.
    let cte<'T> (alias: string) (innerQuery: SelectQuery<'T>) : QuerySource<'T> =
        let tables = Map [Root, { Name = alias; Schema = "" }]
        let ir = { SelectQueryIR.empty with WithCtes = [(alias, innerQuery.SelectIR)] }
        QuerySource<'T, SelectQueryIR>(ir, tables) :> QuerySource<'T>

    /// Creates a CTE source where the outer 'T may differ from the inner select's row type.
    /// Use when the inner query uses raw SELECT fragments for computed columns.
    let cteFrom<'T> (alias: string) (innerQuery: SelectQuery) : QuerySource<'T> =
        let tables = Map [Root, { Name = alias; Schema = "" }]
        let ir = { SelectQueryIR.empty with WithCtes = [(alias, innerQuery.SelectIR)] }
        QuerySource<'T, SelectQueryIR>(ir, tables) :> QuerySource<'T>

[<AutoOpen>]
[<SqlHydraFunction>]
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
[<SqlHydraFunction>]
module OrderBy = 

    // infix operator ^^ that takes a boolean that conditionally includes the sort property.
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
[<SqlHydraFunction>]
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

    /// Gets the COUNT(DISTINCT col) of the given column.
    let countDistinct (prop: 'P) = Unchecked.defaultof<int>

[<AutoOpen>]
[<SqlHydraFunction>]
module CastFunctions =
    /// CAST(expression AS targetType).
    /// The target SQL type is inferred from the F# return type:
    /// float/double → FLOAT, int → INTEGER, int64 → BIGINT, decimal → NUMERIC, string → TEXT, bool → BOOLEAN.
    let castAs<'Result> (_value: obj) : 'Result = Unchecked.defaultof<'Result>

[<AutoOpen>]
[<SqlHydraFunction>]
module CaseWhenFunctions =
    /// CASE WHEN condition THEN thenValue ELSE elseValue END.
    /// Note: values are rendered as SQL literals, not parameters.
    /// Column references are properly qualified. Do not pass unsanitized user input.
    let caseWhen<'T> (condition: bool) (thenValue: 'T) (elseValue: 'T) : 'T = Unchecked.defaultof<'T>

    /// Multi-branch CASE WHEN expression.
    /// CASE WHEN cond1 THEN val1 WHEN cond2 THEN val2 ... ELSE elseVal END.
    let caseWhenMulti<'T> (branches: (bool * 'T) list) (elseValue: 'T) : 'T = Unchecked.defaultof<'T>

[<AutoOpen>]
[<SqlHydraFunction>]
module RawExpressions =
    /// Reference a column from a lateral subquery by alias and column name (raw quoted).
    /// Example: lateralCol "lat" "score" → "lat"."score"
    let lateralCol<'T> (_alias: string) (_column: string) : 'T = Unchecked.defaultof<'T>

    /// Inject a raw SQL expression into a select projection. Use sparingly.
    let rawExpr<'T> (_sql: string) : 'T = Unchecked.defaultof<'T>

    /// Wrap an external value so it's emitted as a SQL parameter inside a select expression
    /// (rather than being treated as a column reference). Use for literals/captured variables
    /// inside `caseWhen`, `castAs`, infix operator args, etc.
    /// Example: caseWhen (col > 0) (inlineValue "yes") (inlineValue "no")
    let inlineValue<'T> (_value: 'T) : 'T = Unchecked.defaultof<'T>

/// Assembly-level attribute that registers a SQL function name as an infix operator.
/// Extension packages (e.g. SqlHydra.Query.Pgvector) apply this attribute on themselves and
/// SqlHydra discovers it the first time a query is compiled. No explicit registration call required.
///
/// Example (in extension package):
///   [<assembly: SqlHydra.Query.SqlHydraInfixOperator("cosine_distance", "<=>")>]
[<System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)>]
type SqlHydraInfixOperatorAttribute(fnName: string, op: string) =
    inherit System.Attribute()
    member _.FnName = fnName
    member _.Operator = op

/// Registry for SQL functions that should be emitted as infix operators.
/// Discovers `SqlHydraInfixOperatorAttribute` declarations from all loaded assemblies, and
/// subscribes to `AssemblyLoad` so plugin assemblies that load lazily are picked up automatically.
/// Manual registration is also supported for tests / dynamic scenarios.
module InfixOperators =
    let private registry = System.Collections.Concurrent.ConcurrentDictionary<string, string>()

    let private scanAssembly (asm: System.Reflection.Assembly) =
        try
            for attr in asm.GetCustomAttributes(typeof<SqlHydraInfixOperatorAttribute>, false) do
                let a = attr :?> SqlHydraInfixOperatorAttribute
                registry.[a.FnName] <- a.Operator
        with _ -> () // tolerate dynamic / reflection-only / partially-loaded assemblies

    let private initOnce =
        lazy (
            // Pick up assemblies already loaded at startup.
            for asm in System.AppDomain.CurrentDomain.GetAssemblies() do
                scanAssembly asm
            // Pick up plugin assemblies that load lazily after first use.
            System.AppDomain.CurrentDomain.AssemblyLoad.Add(fun e -> scanAssembly e.LoadedAssembly)
        )

    /// Manually register a function name. Extension packages should prefer
    /// `SqlHydraInfixOperatorAttribute` so registration is automatic.
    let register (fnName: string) (operator: string) =
        registry.[fnName] <- operator

    /// Look up whether a function should be emitted as an infix operator.
    let tryGetOperator (fnName: string) =
        initOnce.Value
        match registry.TryGetValue(fnName) with
        | true, op -> Some op
        | _ -> None

[<AutoOpen>]
[<SqlHydraFunction>]
module SqlFunctions =

    /// A stub used to define SQL function wrappers: the wrapper's name and arguments are
    /// translated directly to SQL, and its body never runs.
    /// Example:
    ///   [<SqlHydraFunction>]
    ///   let LEN (s: string) : int = sqlFn
    ///   [<SqlHydraFunction>]
    ///   let SUBSTRING (s: string, start: int, length: int) : string = sqlFn
    ///
    /// Raises if it is ever actually executed, which means the call was not rendered. Handing
    /// back `Unchecked.defaultof` instead is what let `where (col = SOUNDEX "Smith")` compile,
    /// run, and quietly mean `col IS NULL`.
    let sqlFn<'Return> : 'Return =
        raise (SqlFunctionNotRenderedException(
            "A SqlHydra `sqlFn` function was executed instead of being rendered as SQL. \
             In a `where` or `on'` predicate, a wrapper must be marked `[<SqlHydraFunction>]`, \
             on the function or on the module or type holding it. Outside a query expression a \
             `sqlFn` wrapper has no runtime meaning and cannot be called."))

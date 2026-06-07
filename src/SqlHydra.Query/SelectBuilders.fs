/// Linq select query builders
[<AutoOpen>]
module SqlHydra.Query.SelectBuilders

open System
open System.Linq.Expressions
open System.Data.Common
open System.Threading
open System.Threading.Tasks

/// The context type that determines how the query context is created and disposed.
/// Can be implicitly converted from a QueryContext, a function that creates a QueryContext, a Task that creates a QueryContext, or an Async that creates a QueryContext.
[<NoComparison; NoEquality>]
type ContextType =
    /// A new QueryContext will be created and disposed within the select builder.
    | Create of create: (unit -> QueryContext)
    /// A new QueryContext will be created and disposed within the select builder.
    | CreateTask of create: (unit -> Task<QueryContext>)
    /// A new QueryContext will be created and disposed within the select builder.
    | CreateAsync of create: (unit -> Async<QueryContext>)
    /// A shared QueryContext will be used and not disposed within the select builder.
    | Shared of QueryContext
    static member op_Implicit(ctx: QueryContext) = Shared ctx
    static member op_Implicit(createFn: unit -> QueryContext) = Create createFn
    static member op_Implicit(createFn: unit -> Task<QueryContext>) = CreateTask createFn
    static member op_Implicit(createFn: unit -> Async<QueryContext>) = CreateAsync createFn

/// SRTP-based context type resolution for selectTask/selectAsync
[<RequireQualifiedAccess>]
module ContextTypeResolver =

    /// Helper type for SRTP overload resolution using the $ operator pattern
    type Resolver =
        | Resolver

        // Direct ContextType - pass through
        static member inline ($) (Resolver, ct: ContextType) = ct

        // QueryContext - wrap in Shared
        static member inline ($) (Resolver, ctx: QueryContext) = Shared ctx

        // unit -> QueryContext - wrap in Create
        static member inline ($) (Resolver, createFn: unit -> QueryContext) = Create createFn

        // unit -> Task<QueryContext> - wrap in CreateTask
        static member inline ($) (Resolver, createFn: unit -> Task<QueryContext>) = CreateTask createFn

        // unit -> Async<QueryContext> - wrap in CreateAsync
        static member inline ($) (Resolver, createFn: unit -> Async<QueryContext>) = CreateAsync createFn

        // Explicit overload for IQueryContextFactory
        static member inline ($) (Resolver, factory: IQueryContextFactory) =
            CreateTask factory.OpenContextAsync

    /// Inline function that resolves any supported type to ContextType
    let inline resolve< ^T when (Resolver or ^T) : (static member ($) : Resolver * ^T -> ContextType)> (input: ^T) : ContextType =
        Resolver $ input

module ContextUtils =
    let private tryOpen (ctx: QueryContext) =
        if ctx.Connection.State <> Data.ConnectionState.Open
        then ctx.Connection.Open()
        ctx

    let getContext ct : Task<QueryContext> =
        match ct with
        | Create create ->
            create() |> tryOpen |> Task.FromResult
        | CreateTask create ->
            task {
                let! ctx = create()
                return ctx |> tryOpen
            }
        | CreateAsync create ->
            task {
                let! ctx = create()
                return ctx |> tryOpen
            }
        | Shared ctx ->
            ctx |> tryOpen |> Task.FromResult

    let disposeIfNotShared ct (ctx: QueryContext) =
        match ct with
        | Create _ -> (ctx :> IDisposable).Dispose()
        | CreateTask _ -> (ctx :> IDisposable).Dispose()
        | CreateAsync _ -> (ctx :> IDisposable).Dispose()
        | Shared _ -> () // Do not dispose if shared


[<RequireQualifiedAccess>]
module ResultModifier =
    type ModifierBase<'T>(qs: QuerySource<'T, SelectQueryIR>) =
        member this.Query = qs.Query

    type Count<'T>(qs) = inherit ModifierBase<'T>(qs)

    type Head<'T>(qs) = inherit ModifierBase<'T>(qs)

/// The base select builder that contains all common operations
type SelectBuilder<'Selected, 'Mapped> () =

    let getQueryOrDefault (state: QuerySource<'T>) =
        match state with
        | :? QuerySource<'T, SelectQueryIR> as qs -> qs.Query
        | _ -> SelectQueryIR.empty

    let mergeTableMappings (a: Map<TableMappingKey, TableMapping>, b: Map<TableMappingKey, TableMapping>) =
        Map (Seq.concat [ (Map.toSeq a); (Map.toSeq b) ])

    let qualifyColumnWithAlias (alias: string) (col: Reflection.MemberInfo) =
        $"%s{alias}.%s{col.Name}"

    /// Combines outer + inner CTEs, deduplicating by alias (last write wins). Used when
    /// a `cteFrom` source is the inner side of a `leftJoin'`/`join'` — the inner's
    /// WithCtes must propagate, but the same source could be used in multiple joins.
    let mergeCtes (outerIr: SelectQueryIR) (inner: QuerySource<'T>) =
        let innerIr = inner |> getQueryOrDefault
        if innerIr.WithCtes.IsEmpty then outerIr
        else
            let seen = System.Collections.Generic.HashSet<string>()
            let merged =
                outerIr.WithCtes @ innerIr.WithCtes
                |> List.filter (fun (alias, _) -> seen.Add alias)
            { outerIr with WithCtes = merged }

    member val MapFn = Option<Func<'Selected, 'Mapped>>.None with get, set
    member val CancellationToken = CancellationToken.None with get, set
    member val private PendingJoinInfo = Option<PendingJoin>.None with get, set

    member this.For (state: QuerySource<'T>, [<ReflectedDefinition>] forExpr: FSharp.Quotations.Expr<'T -> QuerySource<'T>>) =
        let tableAlias = QuotationVisitor.visitFor forExpr
        let ir = state |> getQueryOrDefault
        let tblMaybe, tableMappings = TableMappings.tryGetByRootOrAlias tableAlias state.TableMappings

        match tblMaybe with
        | Some tbl ->
            let fromSpec = $"{FQ.qualifiedTable tbl} as {tableAlias}"
            QuerySource<'T, SelectQueryIR>({ ir with From = Some fromSpec }, tableMappings)
        | None ->
            // Handles this scenario: `select (p.FirstName, p.LastName) into (fname, lname)`
            state :?> QuerySource<'T, SelectQueryIR>

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    // Prevents errors while typing join statement if rest of query is not filled in yet.
    member this.Zero _ =
        QuerySource<'T>(Map.empty)

    /// Sets the WHERE condition
    [<CustomOperation("where", MaintainsVariableSpace = true)>]
    member this.Where (state: QuerySource<'T, SelectQueryIR>, [<ProjectionParameter>] whereExpression) =
        let ir = state.Query
        let tableMappings = state.TableMappings |> Map.values
        let newClause = LinqExpressionVisitors.visitWhere<'T> tableMappings whereExpression qualifyColumnWithAlias
        QuerySource<'T, SelectQueryIR>({ ir with Where = WhereClause.combineAnd ir.Where newClause }, state.TableMappings)

    /// WHERE EXISTS (subquery)
    [<CustomOperation("whereExists", MaintainsVariableSpace = true)>]
    member this.WhereExists (state: QuerySource<'T, SelectQueryIR>, subquery: SelectQuery) =
        let ir = state.Query
        let newClause = WhereClause.Exists subquery.SelectIR
        QuerySource<'T, SelectQueryIR>({ ir with Where = WhereClause.combineAnd ir.Where newClause }, state.TableMappings)

    /// WHERE NOT EXISTS (subquery)
    [<CustomOperation("whereNotExists", MaintainsVariableSpace = true)>]
    member this.WhereNotExists (state: QuerySource<'T, SelectQueryIR>, subquery: SelectQuery) =
        let ir = state.Query
        let newClause = WhereClause.NotExists subquery.SelectIR
        QuerySource<'T, SelectQueryIR>({ ir with Where = WhereClause.combineAnd ir.Where newClause }, state.TableMappings)

    /// HAVING with raw SQL fragment. Use ? for parameter placeholders.
    [<CustomOperation("havingRaw", MaintainsVariableSpace = true)>]
    member this.HavingRaw (state: QuerySource<'T, SelectQueryIR>, fragment: string) =
        let ir = state.Query
        QuerySource<'T, SelectQueryIR>({ ir with Having = WhereClause.combineAnd ir.Having (RawWhere(fragment, [||])) }, state.TableMappings)

    /// HAVING with raw SQL fragment + parameters. Use ? as placeholders.
    [<CustomOperation("havingRaw", MaintainsVariableSpace = true)>]
    member this.HavingRaw (state: QuerySource<'T, SelectQueryIR>, fragment: string, parameters: obj[]) =
        let ir = state.Query
        QuerySource<'T, SelectQueryIR>({ ir with Having = WhereClause.combineAnd ir.Having (RawWhere(fragment, parameters)) }, state.TableMappings)

    /// Sets the SELECT statement and filters the query to include only the selected tables
    [<CustomOperation("select", MaintainsVariableSpace = true, AllowIntoPattern = true)>]
    member this.Select (state: QuerySource<'T, SelectQueryIR>, [<ProjectionParameter>] selectExpression: Expression<Func<'T, 'Selected>>) =
        let selections = LinqExpressionVisitors.visitSelect<'T,'Selected> selectExpression

        let irWithSelectedColumns =
            selections
            |> List.fold (fun (ir: SelectQueryIR) -> function
                | LinqExpressionVisitors.SelectedTable (tableAlias, tableType) ->
                    // Bug fix: temporarily revert to * until option types are properly implemented.
                    // `tableType` was not properly unwrapping option types, causing a runtime error.
                    // For example, left joining a table creates an option type, which should be unwrapped.
                    { ir with Select = ir.Select @ [AllColumns tableAlias] }

                | LinqExpressionVisitors.SelectedColumn (tableAlias, column, _, _, _) ->
                    // Select a single column
                    { ir with Select = ir.Select @ [SpecificColumn $"%s{tableAlias}.%s{column}"] }
                | LinqExpressionVisitors.SelectedExpression sqlFragment ->
                    { ir with Select = ir.Select @ [RawColumn (sqlFragment, [||])] }
                | LinqExpressionVisitors.SelectedExpressionWithParams (sqlFragment, parms) ->
                    { ir with Select = ir.Select @ [RawColumn (sqlFragment, parms)] }
                | LinqExpressionVisitors.SelectedAs (LinqExpressionVisitors.SelectedColumn (tableAlias, column, _, _, _), alias) ->
                    { ir with Select = ir.Select @ [RawColumn ($"\"%s{tableAlias}\".\"%s{column}\" AS \"%s{alias}\"", [||])] }
                | LinqExpressionVisitors.SelectedAs (LinqExpressionVisitors.SelectedExpression frag, alias) ->
                    { ir with Select = ir.Select @ [RawColumn ($"%s{frag} AS \"%s{alias}\"", [||])] }
                | LinqExpressionVisitors.SelectedAs (LinqExpressionVisitors.SelectedExpressionWithParams (frag, parms), alias) ->
                    { ir with Select = ir.Select @ [RawColumn ($"%s{frag} AS \"%s{alias}\"", parms)] }
                | LinqExpressionVisitors.SelectedAs _ ->
                    failwith "SelectedAs may only wrap SelectedColumn / SelectedExpression / SelectedExpressionWithParams"
            ) state.Query

        QuerySource<'Selected, SelectQueryIR>(irWithSelectedColumns, state.TableMappings)

    /// Sets the ORDER BY for single column
    [<CustomOperation("orderBy", MaintainsVariableSpace = true)>]
    member this.OrderBy (state: QuerySource<'T, SelectQueryIR>, [<ProjectionParameter>] propertySelector) =
        let ir = state.Query
        let newOrderBy =
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function
                | LinqExpressionVisitors.OrderByColumn (tableAlias, p) ->
                    let fqCol = $"%s{tableAlias}.%s{p.Name}"
                    [OrderByColumn (fqCol, Asc)]
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, tableAlias, p) ->
                    let fqCol = $"{{%s{tableAlias}}}.{{%s{p.Name}}}"
                    [OrderByRaw (LinqExpressionVisitors.renderAggregate aggType fqCol, [||])]
                | LinqExpressionVisitors.OrderByExpression (frag, parms) ->
                    [OrderByRaw (frag, parms)]
                | LinqExpressionVisitors.OrderByIgnored ->
                    []
        QuerySource<'T, SelectQueryIR>({ ir with OrderBy = ir.OrderBy @ newOrderBy }, state.TableMappings)

    /// Sets the ORDER BY for single column
    [<CustomOperation("thenBy", MaintainsVariableSpace = true)>]
    member this.ThenBy (state: QuerySource<'T, SelectQueryIR>, [<ProjectionParameter>] propertySelector) =
        this.OrderBy(state, propertySelector)

    /// Sets the ORDER BY DESC for single column
    [<CustomOperation("orderByDescending", MaintainsVariableSpace = true)>]
    member this.OrderByDescending (state: QuerySource<'T, SelectQueryIR>, [<ProjectionParameter>] propertySelector) =
        let ir = state.Query
        let newOrderBy =
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function
                | LinqExpressionVisitors.OrderByColumn (tableAlias, p) ->
                    let fqCol = $"%s{tableAlias}.%s{p.Name}"
                    [OrderByColumn (fqCol, Desc)]
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, tableAlias, p) ->
                    let fqCol = $"{{%s{tableAlias}}}.{{%s{p.Name}}}"
                    [OrderByRaw ($"{LinqExpressionVisitors.renderAggregate aggType fqCol} DESC", [||])]
                | LinqExpressionVisitors.OrderByExpression (frag, parms) ->
                    [OrderByRaw ($"{frag} DESC", parms)]
                | LinqExpressionVisitors.OrderByIgnored ->
                    []
        QuerySource<'T, SelectQueryIR>({ ir with OrderBy = ir.OrderBy @ newOrderBy }, state.TableMappings)

    /// Sets the ORDER BY DESC for single column
    [<CustomOperation("thenByDescending", MaintainsVariableSpace = true)>]
    member this.ThenByDescending (state: QuerySource<'T, SelectQueryIR>, [<ProjectionParameter>] propertySelector) =
        this.OrderByDescending(state, propertySelector)

    /// ORDER BY a raw SQL fragment (e.g. an aggregate or computed expression).
    [<CustomOperation("orderByRaw", MaintainsVariableSpace = true)>]
    member this.OrderByRaw (state: QuerySource<'T, SelectQueryIR>, fragment: string) =
        let ir = state.Query
        QuerySource<'T, SelectQueryIR>({ ir with OrderBy = ir.OrderBy @ [OrderByRaw (fragment, [||])] }, state.TableMappings)

    /// ORDER BY a column alias (raw, quoted). Use for computed/aliased columns produced in select.
    [<CustomOperation("orderByAlias", MaintainsVariableSpace = true)>]
    member this.OrderByAlias (state: QuerySource<'T, SelectQueryIR>, alias: string) =
        let ir = state.Query
        QuerySource<'T, SelectQueryIR>({ ir with OrderBy = ir.OrderBy @ [OrderByRaw ($"\"{alias}\"", [||])] }, state.TableMappings)

    /// ORDER BY a column alias DESC (raw, quoted).
    [<CustomOperation("orderByAliasDesc", MaintainsVariableSpace = true)>]
    member this.OrderByAliasDesc (state: QuerySource<'T, SelectQueryIR>, alias: string) =
        let ir = state.Query
        QuerySource<'T, SelectQueryIR>({ ir with OrderBy = ir.OrderBy @ [OrderByRaw ($"\"{alias}\" DESC", [||])] }, state.TableMappings)

    /// Applies a NULLS ordering to the most recent ORDER BY clause.
    member private this.ApplyNulls (state: QuerySource<'T, SelectQueryIR>, nulls: NullsOrdering, rawLiteral: string) =
        let ir = state.Query
        let newOrderBy =
            match List.tryLast ir.OrderBy with
            | Some last ->
                let init = ir.OrderBy.[0..ir.OrderBy.Length - 2]
                let updated =
                    match last with
                    | OrderByColumn (col, dir) -> OrderByColumnNulls (col, dir, nulls)
                    | OrderByColumnNulls (col, dir, _) -> OrderByColumnNulls (col, dir, nulls)
                    | OrderByRaw (frag, parms) -> OrderByRaw ($"{frag} {rawLiteral}", parms)
                init @ [updated]
            | None -> ir.OrderBy
        QuerySource<'T, SelectQueryIR>({ ir with OrderBy = newOrderBy }, state.TableMappings)

    /// Adds NULLS LAST to the most recent ORDER BY clause.
    [<CustomOperation("nullsLast", MaintainsVariableSpace = true)>]
    member this.NullsLast (state: QuerySource<'T, SelectQueryIR>) =
        this.ApplyNulls(state, NullsLast, "NULLS LAST")

    /// Adds NULLS FIRST to the most recent ORDER BY clause.
    [<CustomOperation("nullsFirst", MaintainsVariableSpace = true)>]
    member this.NullsFirst (state: QuerySource<'T, SelectQueryIR>) =
        this.ApplyNulls(state, NullsFirst, "NULLS FIRST")

    /// Sets the SKIP value for query
    [<CustomOperation("skip", MaintainsVariableSpace = true)>]
    member this.Skip (state: QuerySource<'T, SelectQueryIR>, skip) =
        QuerySource<'T, SelectQueryIR>({ state.Query with Skip = Some skip }, state.TableMappings)

    /// Sets the TAKE value for query
    [<CustomOperation("take", MaintainsVariableSpace = true)>]
    member this.Take (state: QuerySource<'T, SelectQueryIR>, take) =
        QuerySource<'T, SelectQueryIR>({ state.Query with Take = Some take }, state.TableMappings)

    /// INNER JOIN table on one or more columns
    [<CustomOperation("join", MaintainsVariableSpace = true, IsLikeJoin = true, JoinConditionWord = "on")>]
    member this.Join (outerSource: QuerySource<'Outer>,
                      innerSource: QuerySource<'Inner>,
                      outerKeySelector: Expression<Func<'Outer,'Key>>,
                      innerKeySelector: Expression<Func<'Inner,'Key>>,
                      resultSelector: Expression<Func<'Outer,'Inner,'JoinResult>> ) =

        let outerProperties = LinqExpressionVisitors.visitJoin<'Outer, 'Key> outerKeySelector // left
        let innerProperties = LinqExpressionVisitors.visitJoin<'Inner, 'Key> innerKeySelector // right

        let mergedTables =
            // Update outer table mappings with join aliases (accumulated outer/left mappings)
            let outerTableMappings =
                outerProperties
                |> List.fold (fun (mappings: Map<TableMappingKey, TableMapping>) joinPI ->
                    let _, updatedMappings = TableMappings.tryGetByRootOrAlias joinPI.Alias mappings
                    updatedMappings
                ) outerSource.TableMappings

            // Update inner table mapping with join aliases (this will always be 1 mapping being joined)
            let innerTableMappings =
                innerProperties
                |> List.fold (fun (mappings: Map<TableMappingKey, TableMapping>) joinPI ->
                    let _, updatedMappings = TableMappings.tryGetByRootOrAlias joinPI.Alias mappings
                    updatedMappings
                ) innerSource.TableMappings

            mergeTableMappings (outerTableMappings, innerTableMappings)

        let ir = outerSource |> getQueryOrDefault
        let innerTableNameAsAlias =
            innerProperties
            |> Seq.map (fun p -> p, mergedTables[TableAliasKey p.Alias])
            |> Seq.map (fun (p, tbl) -> $"%s{FQ.qualifiedTable tbl} AS %s{p.Alias}")
            |> Seq.head

        let joinCondition =
            List.zip outerProperties innerProperties
            |> List.fold (fun (acc: WhereClause) (outerProp, innerProp) ->
                let cond = CompareColumns($"%s{outerProp.Alias}.%s{outerProp.Member.Name}", Eq, $"%s{innerProp.Alias}.%s{innerProp.Member.Name}")
                WhereClause.combineAndFlat acc cond
            ) WhereClause.Empty

        let joinClause = { Kind = InnerJoin; Table = innerTableNameAsAlias; Subquery = None; Condition = joinCondition }
        QuerySource<'JoinResult, SelectQueryIR>({ ir with Joins = ir.Joins @ [joinClause] }, mergedTables)

    /// LEFT JOIN table on one or more columns
    [<CustomOperation("leftJoin", MaintainsVariableSpace = true, IsLikeJoin = true, JoinConditionWord = "on")>]
    member this.LeftJoin (outerSource: QuerySource<'Outer>,
                          innerSource: QuerySource<'Inner>,
                          outerKeySelector: Expression<Func<'Outer,'Key>>,
                          innerKeySelector: Expression<Func<'Inner option,'Key>>,
                          resultSelector: Expression<Func<'Outer,'Inner option,'JoinResult>> ) =

        let outerProperties = LinqExpressionVisitors.visitJoin<'Outer, 'Key> outerKeySelector
        let innerProperties = LinqExpressionVisitors.visitJoin<'Inner option, 'Key> innerKeySelector

        let mergedTables =
            // Update outer table mappings with join aliases
            let outerTableMappings =
                outerProperties
                |> List.fold (fun (mappings: Map<TableMappingKey, TableMapping>) joinPI ->
                    let _, updatedMappings = TableMappings.tryGetByRootOrAlias joinPI.Alias mappings
                    updatedMappings
                ) outerSource.TableMappings

            // Update inner table mappings with join aliases
            let innerTableMappings =
                innerProperties
                |> List.fold (fun (mappings: Map<TableMappingKey, TableMapping>) joinPI ->
                    let _, updatedMappings = TableMappings.tryGetByRootOrAlias joinPI.Alias mappings
                    updatedMappings
                ) innerSource.TableMappings

            mergeTableMappings (outerTableMappings, innerTableMappings)

        let ir = outerSource |> getQueryOrDefault
        let innerTableNameAsAlias =
            innerProperties
            |> Seq.map (fun p -> p, mergedTables[TableAliasKey p.Alias])
            |> Seq.map (fun (p, tbl) -> $"%s{FQ.qualifiedTable tbl} AS %s{p.Alias}")
            |> Seq.head

        let joinCondition =
            List.zip outerProperties innerProperties
            |> List.fold (fun (acc: WhereClause) (outerProp, innerProp) ->
                let cond = CompareColumns($"%s{outerProp.Alias}.%s{outerProp.Member.Name}", Eq, $"%s{innerProp.Alias}.%s{innerProp.Member.Name}")
                WhereClause.combineAndFlat acc cond
            ) WhereClause.Empty

        let joinClause = { Kind = LeftJoin; Table = innerTableNameAsAlias; Subquery = None; Condition = joinCondition }
        QuerySource<'JoinResult, SelectQueryIR>({ ir with Joins = ir.Joins @ [joinClause] }, mergedTables)

    /// References a table variable from a correlated parent query from within a subquery.
    [<CustomOperation("correlate", MaintainsVariableSpace = true, IsLikeZip = true)>]
    member this.Correlate (outerSource: QuerySource<'Outer>,
                      innerSource: QuerySource<'Inner>,
                      resultSelector: Expression<Func<'Outer,'Inner,'JoinResult>> ) =

        // F#'s `IsLikeZip = true` semantics call Correlate BEFORE the enclosing `For`,
        // so both outerSource.TableMappings and innerSource.TableMappings still have their
        // tables under the `Root` key. A naive merge collapses both Roots and the later
        // `For` lookup picks the wrong table. Convert each side's Root → TableAliasKey
        // first using the parameter names from the resultSelector.
        let outerAlias, innerAlias =
            match resultSelector.Parameters |> Seq.toList with
            | [outer; inner] -> outer.Name, inner.Name
            | _ -> failwith "Expected two parameters in correlate result selector"
        let _, outerMappings = TableMappings.tryGetByRootOrAlias outerAlias outerSource.TableMappings
        let _, innerMappings = TableMappings.tryGetByRootOrAlias innerAlias innerSource.TableMappings
        let mergedTables = mergeTableMappings (outerMappings, innerMappings)
        let ir = outerSource |> getQueryOrDefault
        QuerySource<'JoinResult, SelectQueryIR>(ir, mergedTables)

    /// Introduces an INNER JOIN table binding (use with on' to complete the join).
    /// Unlike the standard `join ... on`, this allows predicate-style join conditions.
    /// Example: `join' d in Sales.Detail; on' (o.Id = d.Id && d.Type = "X")`
    [<CustomOperation("join'", MaintainsVariableSpace = true, IsLikeZip = true)>]
    member this.Join' (outerSource: QuerySource<'Outer>,
                        innerSource: QuerySource<'Inner>,
                        resultSelector: Expression<Func<'Outer, 'Inner, 'JoinResult>>) =
        // Extract alias from the resultSelector's second parameter (the inner table alias)
        let innerAlias =
            match resultSelector.Parameters |> Seq.toList with
            | [_; inner] -> inner.Name
            | _ -> failwith "Expected two parameters in join result selector"

        // Merge table mappings
        let _, innerTableMappings = TableMappings.tryGetByRootOrAlias innerAlias innerSource.TableMappings
        let mergedTables = mergeTableMappings (outerSource.TableMappings, innerTableMappings)

        // Get inner table info
        let innerTable = mergedTables[TableAliasKey innerAlias]
        let tableName = FQ.qualifiedTable innerTable

        let pendingJoin = {
            JoinType = JoinType.Inner
            TableName = tableName
            TableAlias = innerAlias
        }

        let ir = mergeCtes (outerSource |> getQueryOrDefault) innerSource
        this.PendingJoinInfo <- Some pendingJoin
        QuerySource<'JoinResult, SelectQueryIR>(ir, mergedTables)

    /// Introduces a LEFT JOIN table binding (use with on' to complete the join).
    /// Unlike the standard `leftJoin ... on`, this allows predicate-style join conditions.
    /// Example: `leftJoin' d in Sales.Detail; on' (o.Id = d.Value.Id && d.Value.Type = "X")`
    [<CustomOperation("leftJoin'", MaintainsVariableSpace = true, IsLikeZip = true)>]
    member this.LeftJoin' (outerSource: QuerySource<'Outer>,
                            innerSource: QuerySource<'Inner>,
                            resultSelector: Expression<Func<'Outer, 'Inner option, 'JoinResult>>) =
        // Extract alias from the resultSelector's second parameter (the inner table alias)
        let innerAlias =
            match resultSelector.Parameters |> Seq.toList with
            | [_; inner] -> inner.Name
            | _ -> failwith "Expected two parameters in leftJoin result selector"

        // Merge table mappings
        let _, innerTableMappings = TableMappings.tryGetByRootOrAlias innerAlias innerSource.TableMappings
        let mergedTables = mergeTableMappings (outerSource.TableMappings, innerTableMappings)

        // Get inner table info
        let innerTable = mergedTables[TableAliasKey innerAlias]
        let tableName = FQ.qualifiedTable innerTable

        let pendingJoin = {
            JoinType = JoinType.Left
            TableName = tableName
            TableAlias = innerAlias
        }

        let ir = mergeCtes (outerSource |> getQueryOrDefault) innerSource
        this.PendingJoinInfo <- Some pendingJoin
        QuerySource<'JoinResult, SelectQueryIR>(ir, mergedTables)

    /// Completes a pending join with a predicate expression.
    /// Used after `join'` or `leftJoin'` to specify the join condition.
    /// Example: `on' (o.Id = d.Id && d.Type = "X")`
    [<CustomOperation("on'", MaintainsVariableSpace = true)>]
    member this.OnPredicate (state: QuerySource<'T, SelectQueryIR>,
                             [<ProjectionParameter>] joinPredicate: Expression<Func<'T, bool>>) =
        let ir = state.Query
        let pendingJoin =
            match this.PendingJoinInfo with
            | Some pj -> this.PendingJoinInfo <- None; pj
            | None -> failwith "on' must be used after join' or leftJoin'"

        let tableMappings = state.TableMappings |> Map.values

        // Build the join predicate as a WhereClause
        let joinCondition = LinqExpressionVisitors.visitJoinPredicate<'T> tableMappings joinPredicate qualifyColumnWithAlias

        // Create the table name with alias
        let tableNameAsAlias = $"{pendingJoin.TableName} AS {pendingJoin.TableAlias}"

        // Build the join clause
        let joinKind =
            match pendingJoin.JoinType with
            | JoinType.Inner -> InnerJoin
            | JoinType.Left -> LeftJoin

        let joinClause = { Kind = joinKind; Table = tableNameAsAlias; Subquery = None; Condition = joinCondition }

        QuerySource<'T, SelectQueryIR>({ ir with Joins = ir.Joins @ [joinClause] }, state.TableMappings)

    /// Sets the GROUP BY for one or more columns.
    [<CustomOperation("groupBy", MaintainsVariableSpace = true)>]
    member this.GroupBy (state: QuerySource<'T, SelectQueryIR>, [<ProjectionParameter>] propertySelector) =
        let properties = LinqExpressionVisitors.visitPropertiesSelector<'T, 'Prop> propertySelector qualifyColumnWithAlias
        QuerySource<'T, SelectQueryIR>({ state.Query with GroupBy = state.Query.GroupBy @ (properties |> List.ofSeq) }, state.TableMappings)

    /// Sets the HAVING condition.
    [<CustomOperation("having", MaintainsVariableSpace = true)>]
    member this.Having (state: QuerySource<'T, SelectQueryIR>, [<ProjectionParameter>] havingExpression) =
        let ir = state.Query
        let tableMappings = state.TableMappings |> Map.values
        let newClause = LinqExpressionVisitors.visitHaving<'T> tableMappings havingExpression qualifyColumnWithAlias
        QuerySource<'T, SelectQueryIR>({ ir with Having = WhereClause.combineAnd ir.Having newClause }, state.TableMappings)

    /// Sets query to return DISTINCT values
    [<CustomOperation("distinct", MaintainsVariableSpace = true)>]
    member this.Distinct (state: QuerySource<'T, SelectQueryIR>) =
        QuerySource<'T, SelectQueryIR>({ state.Query with Distinct = true }, state.TableMappings)

    /// Sets a CancellationToken for the query execution.
    [<CustomOperation("cancel", MaintainsVariableSpace = true)>]
    member this.Cancel (state: QuerySource<'T, SelectQueryIR>, cancellationToken: CancellationToken) =
        this.CancellationToken <- cancellationToken
        state

    /// Maps the query results into a seq.
    [<CustomOperation("mapSeq", MaintainsVariableSpace = true)>]
    member this.MapSeq (state: QuerySource<'Selected, SelectQueryIR>, [<ProjectionParameter>] map: Func<'Selected, 'Mapped>) =
        this.MapFn <- Some map
        QuerySource<'Mapped seq, SelectQueryIR>(state.Query, state.TableMappings)

    /// Maps the query results into an array.
    [<CustomOperation("mapArray", MaintainsVariableSpace = true)>]
    member this.MapArray (state: QuerySource<'Selected, SelectQueryIR>, [<ProjectionParameter>] map: Func<'Selected, 'Mapped>) =
        this.MapFn <- Some map
        QuerySource<'Mapped array, SelectQueryIR>(state.Query, state.TableMappings)

    /// Maps the query results into a list.
    [<CustomOperation("mapList", MaintainsVariableSpace = true)>]
    member this.MapList (state: QuerySource<'Selected, SelectQueryIR>, [<ProjectionParameter>] map: Func<'Selected, 'Mapped>) =
        this.MapFn <- Some map
        QuerySource<'Mapped list, SelectQueryIR>(state.Query, state.TableMappings)

    /// Returns the query results as an array.
    [<CustomOperation("toArray", MaintainsVariableSpace = true)>]
    member this.ToArray (state: QuerySource<'Selected, SelectQueryIR>) =
        QuerySource<'Selected array, SelectQueryIR>(state.Query, state.TableMappings)

    /// Returns the query results as a list.
    [<CustomOperation("toList", MaintainsVariableSpace = true)>]
    member this.ToList (state: QuerySource<'Selected, SelectQueryIR>) =
        QuerySource<'Selected list, SelectQueryIR>(state.Query, state.TableMappings)

    /// COUNT aggregate function
    [<CustomOperation("count", MaintainsVariableSpace = true)>]
    member this.Count (state: QuerySource<'T, SelectQueryIR>) =
        QuerySource<ResultModifier.Count<int>, SelectQueryIR>({ state.Query with IsCount = true }, state.TableMappings)

    /// Applies Seq.tryHead to the 'Selected query results.
    [<CustomOperation("tryHead", MaintainsVariableSpace = true)>]
    member this.TryHead (state: QuerySource<'Selected, SelectQueryIR>) =
        QuerySource<'Selected option, SelectQueryIR>(state.Query, state.TableMappings)

    /// Applies Seq.tryHead to the 'Mapped query results.
    [<CustomOperation("tryHead", MaintainsVariableSpace = true)>]
    member this.TryHead (state: QuerySource<'Mapped seq, SelectQueryIR>) =
        QuerySource<'Mapped option, SelectQueryIR>(state.Query, state.TableMappings)

    /// Applies Seq.tryHead to the 'Mapped query results.
    [<CustomOperation("tryHead", MaintainsVariableSpace = true)>]
    member this.TryHead (state: QuerySource<'Mapped array, SelectQueryIR>) =
        QuerySource<'Mapped option, SelectQueryIR>(state.Query, state.TableMappings)

    /// Applies Seq.tryHead to the 'Mapped query results.
    [<CustomOperation("tryHead", MaintainsVariableSpace = true)>]
    member this.TryHead (state: QuerySource<'Mapped list, SelectQueryIR>) =
        QuerySource<'Mapped option, SelectQueryIR>(state.Query, state.TableMappings)

    /// Applies Seq.head to the 'Selected query results.
    [<CustomOperation("head", MaintainsVariableSpace = true)>]
    member this.Head (state: QuerySource<'Selected, SelectQueryIR>) =
        QuerySource<ResultModifier.Head<'Selected>, SelectQueryIR>(state.Query, state.TableMappings)

    /// Applies Seq.head to the 'Mapped query results.
    [<CustomOperation("head", MaintainsVariableSpace = true)>]
    member this.Head (state: QuerySource<'Mapped seq, SelectQueryIR>) =
        QuerySource<ResultModifier.Head<'Mapped>, SelectQueryIR>(state.Query, state.TableMappings)

    /// Applies Seq.head to the 'Selected query results.
    [<CustomOperation("head", MaintainsVariableSpace = true)>]
    member this.Head (state: QuerySource<'Mapped array, SelectQueryIR>) =
        QuerySource<ResultModifier.Head<'Mapped>, SelectQueryIR>(state.Query, state.TableMappings)

    /// Applies Seq.head to the 'Selected query results.
    [<CustomOperation("head", MaintainsVariableSpace = true)>]
    member this.Head (state: QuerySource<'Mapped list, SelectQueryIR>) =
        QuerySource<ResultModifier.Head<'Mapped>, SelectQueryIR>(state.Query, state.TableMappings)

/// A select builder that returns a select query.
type SelectQueryBuilder<'Selected, 'Mapped> () =
    inherit SelectBuilder<'Selected, 'Mapped>()

    member this.Run (state: QuerySource<ResultModifier.Count<int>, SelectQueryIR>) =
        SelectQuery<int>(state.Query)

    member this.Run (state: QuerySource<'Selected, SelectQueryIR>) =
        SelectQuery<'Selected>(state.Query)


/// A select builder that returns a Task result.
type SelectTaskBuilder<'Selected, 'Mapped> (ct: ContextType) =
    inherit SelectBuilder<'Selected, 'Mapped>()

    member this.RunSelected(query: SelectQueryIR, resultModifier) =
        task {
            let! ctx = ContextUtils.getContext ct
            try
                use cmd = ctx.BuildCommand(query)
                use! reader = cmd.ExecuteReaderAsync(this.CancellationToken)
                let readEntity = Hydration.buildRowReader<'Selected> ctx.Provider reader
                let results = ResizeArray<'Selected>()

                let! hasMore = reader.ReadAsync(this.CancellationToken)
                let mutable hasMore = hasMore
                while hasMore && not this.CancellationToken.IsCancellationRequested do
                    results.Add(readEntity())
                    let! hasMore' = reader.ReadAsync(this.CancellationToken)
                    hasMore <- hasMore'

                return results :> seq<'Selected> |> resultModifier
            finally
                ContextUtils.disposeIfNotShared ct ctx
        }

    member this.RunMapped(query: SelectQueryIR, resultModifier) =
        task {
            let! ctx = ContextUtils.getContext ct
            try
                use cmd = ctx.BuildCommand(query)
                use! reader = cmd.ExecuteReaderAsync(this.CancellationToken)
                let readEntity = Hydration.buildRowReader<'Selected> ctx.Provider reader
                let results = ResizeArray<'Mapped>()

                let! hasMore = reader.ReadAsync(this.CancellationToken)
                let mutable hasMore = hasMore
                while hasMore && not this.CancellationToken.IsCancellationRequested do
                    results.Add(this.MapFn.Value.Invoke(readEntity()))
                    let! hasMore' = reader.ReadAsync(this.CancellationToken)
                    hasMore <- hasMore'

                return results :> seq<'Mapped> |> resultModifier
            finally
                ContextUtils.disposeIfNotShared ct ctx
        }

    /// Run: default
    member this.Run(state: QuerySource<'Selected, SelectQueryIR>) =
        this.RunSelected(state.Query, id)

    /// Run: toList
    member this.Run(state: QuerySource<'Selected list, SelectQueryIR>) =
        this.RunSelected(state.Query, Seq.toList)

    /// Run: toArray
    member this.Run(state: QuerySource<'Selected array, SelectQueryIR>) =
        this.RunSelected(state.Query, Seq.toArray)

    /// Run: mapList
    member this.Run(state: QuerySource<'Mapped list, SelectQueryIR>) =
        this.RunMapped(state.Query, Seq.toList)

    // Run: mapArray
    member this.Run(state: QuerySource<'Mapped array, SelectQueryIR>) =
        this.RunMapped(state.Query, Seq.toArray)

    // Run: mapSeq
    member this.Run(state: QuerySource<'Mapped seq, SelectQueryIR>) =
        this.RunMapped(state.Query, id)

    // Run: tryHead - 'Selected
    member this.Run(state: QuerySource<'Selected option, SelectQueryIR>) =
        this.RunSelected(state.Query, Seq.tryHead)

    // Run: tryHead - 'Mapped
    member this.Run(state: QuerySource<'Mapped option, SelectQueryIR>) =
        this.RunMapped(state.Query, Seq.tryHead)

    // Run: head - 'Selected
    member this.Run(state: QuerySource<ResultModifier.Head<'Selected>, SelectQueryIR>) =
        this.RunSelected(state.Query, Seq.head)

    // Run: head - 'Mapped
    member this.Run(state: QuerySource<ResultModifier.Head<'Mapped>, SelectQueryIR>) =
        this.RunMapped(state.Query, Seq.head)

    // Run: count
    member this.Run(state: QuerySource<ResultModifier.Count<int>, SelectQueryIR>) =
        task {
            let! ctx = ContextUtils.getContext ct
            try return! ctx.CountAsyncWithOptions (SelectQuery<int>(state.Query), this.CancellationToken) |> Async.AwaitTask
            finally ContextUtils.disposeIfNotShared ct ctx
        }


/// A select builder that returns an Async result.
type SelectAsyncBuilder<'Selected, 'Mapped> (ct: ContextType) =
    inherit SelectBuilder<'Selected, 'Mapped>()

    member this.RunSelected(query: SelectQueryIR, resultModifier) =
        async {
            let! ctx = ContextUtils.getContext ct |> Async.AwaitTask
            try
                use cmd = ctx.BuildCommand(query)
                let! asyncCancel = Async.CancellationToken
                let cancel = if this.CancellationToken <> CancellationToken.None then this.CancellationToken else asyncCancel
                use! reader = cmd.ExecuteReaderAsync(cancel) |> Async.AwaitTask
                let readEntity = Hydration.buildRowReader<'Selected> ctx.Provider (reader : DbDataReader)
                let results = ResizeArray<'Selected>()

                let! hasMore = reader.ReadAsync(cancel) |> Async.AwaitTask
                let mutable hasMore = hasMore
                while hasMore && not cancel.IsCancellationRequested do
                    results.Add(readEntity())
                    let! hasMore' = reader.ReadAsync(cancel) |> Async.AwaitTask
                    hasMore <- hasMore'

                return results :> seq<'Selected> |> resultModifier
            finally
                ContextUtils.disposeIfNotShared ct ctx
        }

    member this.RunMapped(query: SelectQueryIR, resultModifier) =
        async {
            let! ctx = ContextUtils.getContext ct |> Async.AwaitTask
            try
                use cmd = ctx.BuildCommand(query)
                let! asyncCancel = Async.CancellationToken
                let cancel = if this.CancellationToken <> CancellationToken.None then this.CancellationToken else asyncCancel
                use! reader = cmd.ExecuteReaderAsync(cancel) |> Async.AwaitTask
                let readEntity = Hydration.buildRowReader<'Selected> ctx.Provider (reader : DbDataReader)
                let results = ResizeArray<'Mapped>()

                let! hasMore = reader.ReadAsync(cancel) |> Async.AwaitTask
                let mutable hasMore = hasMore
                while hasMore && not cancel.IsCancellationRequested do
                    results.Add(this.MapFn.Value.Invoke(readEntity()))
                    let! hasMore' = reader.ReadAsync(cancel) |> Async.AwaitTask
                    hasMore <- hasMore'

                return results :> seq<'Mapped> |> resultModifier
            finally
                ContextUtils.disposeIfNotShared ct ctx
        }

    /// Run: default
    member this.Run(state: QuerySource<'Selected, SelectQueryIR>) =
        this.RunSelected(state.Query, id)

    /// Run: toList
    member this.Run(state: QuerySource<'Selected list, SelectQueryIR>) =
        this.RunSelected(state.Query, Seq.toList)

    /// Run: toArray
    member this.Run(state: QuerySource<'Selected array, SelectQueryIR>) =
        this.RunSelected(state.Query, Seq.toArray)

    /// Run: mapList
    member this.Run(state: QuerySource<'Mapped list, SelectQueryIR>) =
        this.RunMapped(state.Query, Seq.toList)

    // Run: mapArray
    member this.Run(state: QuerySource<'Mapped array, SelectQueryIR>) =
        this.RunMapped(state.Query, Seq.toArray)

    // Run: mapSeq
    member this.Run(state: QuerySource<'Mapped seq, SelectQueryIR>) =
        this.RunMapped(state.Query, id)

    // Run: tryHead - 'Selected
    member this.Run(state: QuerySource<'Selected option, SelectQueryIR>) =
        this.RunSelected(state.Query, Seq.tryHead)

    // Run: tryHead - 'Mapped
    member this.Run(state: QuerySource<'Mapped option, SelectQueryIR>) =
        this.RunMapped(state.Query, Seq.tryHead)

    // Run: head - 'Selected
    member this.Run(state: QuerySource<ResultModifier.Head<'Selected>, SelectQueryIR>) =
        this.RunSelected(state.Query, Seq.head)

    // Run: head - 'Mapped
    member this.Run(state: QuerySource<ResultModifier.Head<'Mapped>, SelectQueryIR>) =
        this.RunMapped(state.Query, Seq.head)

    // Run: count
    member this.Run(state: QuerySource<ResultModifier.Count<int>, SelectQueryIR>) =
        async {
            let! ctx = ContextUtils.getContext ct |> Async.AwaitTask
            let! asyncCancel = Async.CancellationToken
            let cancel = if this.CancellationToken <> CancellationToken.None then this.CancellationToken else asyncCancel
            try return! ctx.CountAsyncWithOptions (SelectQuery<int>(state.Query), cancel) |> Async.AwaitTask
            finally ContextUtils.disposeIfNotShared ct ctx
        }


/// Builds and returns a select query that can be manually run by piping into QueryContext read methods
let select<'Selected, 'Mapped> =
    SelectQueryBuilder<'Selected, 'Mapped>()

/// Alias for `select` — use inside `whereExists`/`whereNotExists` (or other contexts that
/// already use `select` as a custom op) to disambiguate.
let subquery<'Selected, 'Mapped> =
    SelectQueryBuilder<'Selected, 'Mapped>()

/// Builds a select query with a context source - returns an Async query result
let inline selectAsync< ^Selected, ^Mapped, ^Context
    when (ContextTypeResolver.Resolver or ^Context) : (static member ($) : ContextTypeResolver.Resolver * ^Context -> ContextType)>
    (ctSource: ^Context) =
    let ct = ContextTypeResolver.resolve ctSource
    SelectAsyncBuilder< ^Selected, ^Mapped>(ct)

/// Builds a select query with a context source - returns a Task query result
let inline selectTask< ^Selected, ^Mapped, ^Context
    when (ContextTypeResolver.Resolver or ^Context) : (static member ($) : ContextTypeResolver.Resolver * ^Context -> ContextType)>
    (ctSource: ^Context) =
    let ct = ContextTypeResolver.resolve ctSource
    SelectTaskBuilder< ^Selected, ^Mapped>(ct)

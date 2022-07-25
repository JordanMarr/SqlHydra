/// Linq select query builders
[<AutoOpen>]
module SqlHydra.Query.SelectBuilders

open System
open System.Linq.Expressions
open System.Data.Common
open System.Threading.Tasks
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open SqlKata

type ContextType = 
    | Create of create: (unit -> QueryContext)
    | Shared of QueryContext

module ContextUtils = 
    let tryOpen (ctx: QueryContext) = 
        if ctx.Connection.State <> Data.ConnectionState.Open 
        then ctx.Connection.Open()
        ctx

    let getContext ct =
        match ct with 
        | Create create -> create() |> tryOpen
        | Shared ctx -> ctx

    let disposeIfNotShared ct (ctx: QueryContext) =
        match ct with
        | Create _ -> (ctx :> IDisposable).Dispose()
        | Shared _ -> () // Do not dispose if shared


[<RequireQualifiedAccess>]
module ResultModifier =
    type ModifierBase<'T>(qs: QuerySource<'T, Query>) = 
        member this.Query = qs.Query

    type Count<'T>(qs) = inherit ModifierBase<'T>(qs)

/// The base select builder that contains all common operations
type SelectBuilder<'Selected, 'Mapped> () =

    let getQueryOrDefault (state: QuerySource<'T>) =
        match state with
        | :? QuerySource<'T, Query> as qs -> qs.Query
        | _ -> Query()            

    let mergeTableMappings (a: Map<FQ.FQName, TableMapping>, b: Map<FQ.FQName, TableMapping>) =
        Map (Seq.concat [ (Map.toSeq a); (Map.toSeq b) ])

    member val MapFn = Option<Func<'Selected, 'Mapped>>.None with get, set
    
//    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T> ) =
//    member this.For (state: QuerySource<'T>, f: Expression<Func<'T, QuerySource<'T>>>) =
    member this.For (state: QuerySource<'T>, [<ReflectedDefinition>] f: FSharp.Quotations.Expr<'T -> QuerySource<'T>>) =        
        match state.TryGetOuterTableMapping() with
        | Some tbl ->
            printfn "For:\n%A" f
            let tblName = match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name
            let tblAlias: string = QuotationVisitor.visitFor f
            let query = state |> getQueryOrDefault
            QuerySource<'T, Query>(
                query.From(sprintf "%s AS %s" tblName tblAlias),
                state.TableMappings)
        | None -> 
            state :?> QuerySource<'T, Query>

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    // Prevents errors while typing join statement if rest of query is not filled in yet.
    member this.Zero _ = 
        QuerySource<'T>(Map.empty)

    /// Sets the WHERE condition
    [<CustomOperation("where", MaintainsVariableSpace = true)>]
    member this.Where (state: QuerySource<'T, Query>, [<ProjectionParameter>] whereExpression) = 
        let query = state.Query
        let where = LinqExpressionVisitors.visitWhere<'T> whereExpression (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(query.Where(fun w -> where), state.TableMappings)

    /// Sets the SELECT statement and filters the query to include only the selected tables
    [<CustomOperation("select", MaintainsVariableSpace = true, AllowIntoPattern = true)>]
    member this.Select (state: QuerySource<'T, Query>, [<ProjectionParameter>] selectExpression: Expression<Func<'T, 'Selected>>) =
        let selections = LinqExpressionVisitors.visitSelect<'T,'Selected> selectExpression

        let queryWithSelectedColumns =
            selections
            |> List.fold (fun (q: Query) -> function
                | LinqExpressionVisitors.SelectedTable tbl -> 
                    // Select all columns in table
                    q.Select($"{tbl}.*")
                | LinqExpressionVisitors.SelectedColumn (tblAlias, col) -> 
                    // Select a single column
                    q.Select($"{tblAlias}.{col}")
                | LinqExpressionVisitors.SelectedAggregateColumn (aggFn, tblAlias, col) -> 
                    // Currently in v2.3.7, SqlKata doesn't support multiple inline aggregate functions.
                    // Use SelectRaw as a workaround until SqlKata supports multiple aggregates.
                    // https://github.com/sqlkata/querybuilder/pull/504

                    // SqlKata will translate curly braces to dialect-specific characters (ex: [] for mssql, "" for postgres)
                    let fqColWithCurlyBraces = sprintf "{%s}.{%s}" tblAlias col.Name

                    q.SelectRaw($"{aggFn}({fqColWithCurlyBraces})")
            ) state.Query
                  
        QuerySource<'Selected, Query>(queryWithSelectedColumns, state.TableMappings)

    /// Sets the ORDER BY for single column
    [<CustomOperation("orderBy", MaintainsVariableSpace = true)>]
    member this.OrderBy (state: QuerySource<'T, Query>, [<ProjectionParameter>] propertySelector) = 
        let orderedQuery = 
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function 
                | LinqExpressionVisitors.OrderByColumn p -> 
                    state.Query.OrderBy(FQ.fullyQualifyColumn state.TableMappings p)
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, p) -> 
                    state.Query.OrderByRaw($"{aggType}({FQ.fullyQualifyColumn state.TableMappings p})")        
        QuerySource<'T, Query>(orderedQuery, state.TableMappings)

    /// Sets the ORDER BY for single column
    [<CustomOperation("thenBy", MaintainsVariableSpace = true)>]
    member this.ThenBy (state: QuerySource<'T, Query>, [<ProjectionParameter>] propertySelector) = 
        let orderedQuery = 
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function 
                | LinqExpressionVisitors.OrderByColumn p -> 
                    state.Query.OrderBy(FQ.fullyQualifyColumn state.TableMappings p)
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, p) -> 
                    state.Query.OrderByRaw($"{aggType}({FQ.fullyQualifyColumn state.TableMappings p})")        
        QuerySource<'T, Query>(orderedQuery, state.TableMappings)

    /// Sets the ORDER BY DESC for single column
    [<CustomOperation("orderByDescending", MaintainsVariableSpace = true)>]
    member this.OrderByDescending (state: QuerySource<'T, Query>, [<ProjectionParameter>] propertySelector) = 
        let orderedQuery = 
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function 
                | LinqExpressionVisitors.OrderByColumn p -> 
                    state.Query.OrderByDesc(FQ.fullyQualifyColumn state.TableMappings p)
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, p) -> 
                    state.Query.OrderByRaw($"{aggType}({FQ.fullyQualifyColumn state.TableMappings p}) DESC")        
        QuerySource<'T, Query>(orderedQuery, state.TableMappings)

    /// Sets the ORDER BY DESC for single column
    [<CustomOperation("thenByDescending", MaintainsVariableSpace = true)>]
    member this.ThenByDescending (state: QuerySource<'T, Query>, [<ProjectionParameter>] propertySelector) = 
        let orderedQuery = 
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function 
                | LinqExpressionVisitors.OrderByColumn p -> 
                    state.Query.OrderByDesc(FQ.fullyQualifyColumn state.TableMappings p)
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, p) -> 
                    state.Query.OrderByRaw($"{aggType}({FQ.fullyQualifyColumn state.TableMappings p}) DESC")        
        QuerySource<'T, Query>(orderedQuery, state.TableMappings)

    /// Sets the SKIP value for query
    [<CustomOperation("skip", MaintainsVariableSpace = true)>]
    member this.Skip (state: QuerySource<'T, Query>, skip) = 
        QuerySource<'T, Query>(state.Query.Skip(skip), state.TableMappings)
    
    /// Sets the TAKE value for query
    [<CustomOperation("take", MaintainsVariableSpace = true)>]
    member this.Take (state: QuerySource<'T, Query>, take) =
        QuerySource<'T, Query>(state.Query.Take(take), state.TableMappings)

    /// INNER JOIN table on one or more columns
    [<CustomOperation("join", MaintainsVariableSpace = true, IsLikeJoin = true, JoinConditionWord = "on")>]
    member this.Join (outerSource: QuerySource<'Outer>, 
                      innerSource: QuerySource<'Inner>, 
                      outerKeySelector: Expression<Func<'Outer,'Key>>, 
                      innerKeySelector: Expression<Func<'Inner,'Key>>, 
                      resultSelector: Expression<Func<'Outer,'Inner,'JoinResult>> ) = 
        
        let outerProperties = LinqExpressionVisitors.visitJoin<'Outer, 'Key> outerKeySelector
        let innerProperties = LinqExpressionVisitors.visitJoin<'Inner, 'Key> innerKeySelector
        let mergedTables = mergeTableMappings (outerSource.TableMappings, innerSource.TableMappings)
        
        let outerQuery = outerSource |> getQueryOrDefault
        let innerTableAlias = innerProperties |> Seq.map fst |> Seq.head
        let innerTableName = 
            innerProperties 
            |> Seq.map (fun (pTableAlias, p) -> pTableAlias, mergedTables.[FQ.fqName p.DeclaringType])
            |> Seq.map (fun (tblAlias, tbl) -> 
                match tbl.Schema with
                | Some schema -> sprintf "%s.%s" schema tbl.Name
                | None -> tbl.Name
            )
            |> Seq.head
        
        let joinOn = 
            let fq = FQ.fullyQualifyColumn mergedTables
            List.zip outerProperties innerProperties
            |> List.fold (fun (j: Join) ((outerTableAlias, outerProp), (innerTableAlias, innerProp)) ->
                let o = sprintf "%s.%s" outerTableAlias outerProp.Name
                let i = sprintf "%s.%s" innerTableAlias innerProp.Name
                j.On(o, i)) (Join())
        
        let innerTable = Query(innerTableName).As(innerTableAlias)
            
        QuerySource<'JoinResult, Query>(outerQuery.Join(innerTable, fun j -> joinOn), mergedTables)

//    /// LEFT JOIN table on one or more columns
//    [<CustomOperation("leftJoin", MaintainsVariableSpace = true, IsLikeJoin = true, JoinConditionWord = "on")>]
//    member this.LeftJoin (outerSource: QuerySource<'Outer>, 
//                          innerSource: QuerySource<'Inner>, 
//                          outerKeySelector: Expression<Func<'Outer,'Key>>, 
//                          innerKeySelector: Expression<Func<'Inner option,'Key>>, 
//                          resultSelector: Expression<Func<'Outer,'Inner option,'JoinResult>> ) = 
//
//        let mergedTables = mergeTableMappings (outerSource.TableMappings, innerSource.TableMappings)
//        let outerProperties = LinqExpressionVisitors.visitJoin<'Outer, 'Key> outerKeySelector
//        let innerProperties = LinqExpressionVisitors.visitJoin<'Inner option, 'Key> innerKeySelector
//
//        let outerQuery = outerSource |> getQueryOrDefault
//        let innerTableName = 
//            innerProperties 
//            |> Seq.map (fun p -> mergedTables.[FQ.fqName p.DeclaringType])
//            |> Seq.map (fun tbl -> 
//                match tbl.Schema with
//                | Some schema -> sprintf "%s.%s" schema tbl.Name
//                | None -> tbl.Name
//            )
//            |> Seq.head
//
//        let joinOn = 
//            let fq = FQ.fullyQualifyColumn mergedTables
//            List.zip outerProperties innerProperties
//            |> List.fold (fun (j: Join) (outerProp, innerProp) -> j.On(fq outerProp, fq innerProp)) (Join())
//            
//        QuerySource<'JoinResult, Query>(outerQuery.LeftJoin(innerTableName, fun j -> joinOn), mergedTables)

    /// Sets the GROUP BY for one or more columns.
    [<CustomOperation("groupBy", MaintainsVariableSpace = true)>]
    member this.GroupBy (state: QuerySource<'T, Query>, [<ProjectionParameter>] propertySelector) = 
        let properties = LinqExpressionVisitors.visitPropertiesSelector<'T, 'Prop> propertySelector (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(state.Query.GroupBy(properties |> List.toArray), state.TableMappings)

    /// Sets the HAVING condition.
    [<CustomOperation("having", MaintainsVariableSpace = true)>]
    member this.Having (state: QuerySource<'T, Query>, [<ProjectionParameter>] havingExpression) = 
        let having = LinqExpressionVisitors.visitHaving<'T> havingExpression (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(state.Query.Having(fun w -> having), state.TableMappings)

    /// Sets query to return DISTINCT values
    [<CustomOperation("distinct", MaintainsVariableSpace = true)>]
    member this.Distinct (state: QuerySource<'T, Query>) = 
        QuerySource<'T, Query>(state.Query.Distinct(), state.TableMappings)

    /// Maps the query results into a seq.
    [<CustomOperation("mapSeq", MaintainsVariableSpace = true)>]
    member this.MapSeq (state: QuerySource<'Selected, Query>, [<ProjectionParameter>] map: Func<'Selected, 'Mapped>) =
        this.MapFn <- Some map
        QuerySource<'Mapped seq, Query>(state.Query, state.TableMappings)
    
    /// Maps the query results into an array.
    [<CustomOperation("mapArray", MaintainsVariableSpace = true)>]
    member this.MapArray (state: QuerySource<'Selected, Query>, [<ProjectionParameter>] map: Func<'Selected, 'Mapped>) =
        this.MapFn <- Some map
        QuerySource<'Mapped array, Query>(state.Query, state.TableMappings)
        
    /// Maps the query results into a list.
    [<CustomOperation("mapList", MaintainsVariableSpace = true)>]
    member this.MapList (state: QuerySource<'Selected, Query>, [<ProjectionParameter>] map: Func<'Selected, 'Mapped>) =
        this.MapFn <- Some map
        QuerySource<'Mapped list, Query>(state.Query, state.TableMappings)
    
    /// Returns the query results as an array.
    [<CustomOperation("toArray", MaintainsVariableSpace = true)>]
    member this.ToArray (state: QuerySource<'Selected, Query>) =
        QuerySource<'Selected array, Query>(state.Query, state.TableMappings)

    /// Returns the query results as a list.
    [<CustomOperation("toList", MaintainsVariableSpace = true)>]
    member this.ToList (state: QuerySource<'Selected, Query>) =
        QuerySource<'Selected list, Query>(state.Query, state.TableMappings)

    /// COUNT aggregate function
    [<CustomOperation("count", MaintainsVariableSpace = true)>]
    member this.Count (state: QuerySource<'T, Query>) = 
        QuerySource<ResultModifier.Count<int>, Query>(state.Query.AsCount(), state.TableMappings)

    /// Applies Seq.tryHead to the 'Selected query results.
    [<CustomOperation("tryHead", MaintainsVariableSpace = true)>]
    member this.TryHead (state: QuerySource<'Selected, Query>) = 
        QuerySource<'Selected option, Query>(state.Query, state.TableMappings)

    /// Applies Seq.tryHead to the 'Mapped query results.
    [<CustomOperation("tryHead", MaintainsVariableSpace = true)>]
    member this.TryHead (state: QuerySource<'Mapped seq, Query>) = 
        QuerySource<'Mapped option, Query>(state.Query, state.TableMappings)
    
    /// Applies Seq.tryHead to the 'Mapped query results.
    [<CustomOperation("tryHead", MaintainsVariableSpace = true)>]
    member this.TryHead (state: QuerySource<'Mapped array, Query>) = 
        QuerySource<'Mapped option, Query>(state.Query, state.TableMappings)
        
    /// Applies Seq.tryHead to the 'Mapped query results.
    [<CustomOperation("tryHead", MaintainsVariableSpace = true)>]
    member this.TryHead (state: QuerySource<'Mapped list, Query>) = 
        QuerySource<'Mapped option, Query>(state.Query, state.TableMappings)

/// A select builder that returns a select query.
type SelectQueryBuilder<'Selected, 'Mapped> () = 
    inherit SelectBuilder<'Selected, 'Mapped>()
    
    member this.Run (state: QuerySource<ResultModifier.Count<int>, Query>) = 
        SelectQuery<int>(state.Query)

    member this.Run (state: QuerySource<'Selected, Query>) =
        SelectQuery<'Selected>(state.Query)


/// A select builder that returns a Task result.
type SelectTaskBuilder<'Selected, 'Mapped, 'Reader when 'Reader :> DbDataReader> (
    readEntityBuilder: 'Reader -> (unit -> 'Selected), ct: ContextType) =
    inherit SelectBuilder<'Selected, 'Mapped>()
    
    member this.RunSelected(query: Query, resultModifier) =
        async {
            let ctx = ContextUtils.getContext ct
            try 
                let selectQuery = SelectQuery<'Selected>(query)
                let! results = selectQuery |> ctx.ReadAsync readEntityBuilder |> Async.AwaitTask
                return results |> resultModifier
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }
        |> Async.StartImmediateAsTask

    member this.RunMapped(query: Query, resultModifier) =
        async {
            let ctx = ContextUtils.getContext ct
            try 
                let selectQuery = SelectQuery<'Selected>(query)
                let! results = selectQuery |> ctx.ReadAsync readEntityBuilder |> Async.AwaitTask
                return results |> Seq.map this.MapFn.Value.Invoke |> resultModifier
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }
        |> Async.StartImmediateAsTask

    /// Run: default
    /// Called when no mapSeq, mapArray or mapList is present; 
    /// this input will always be 'Selected -- even if select is not present.
    member this.Run(state: QuerySource<'Selected, Query>) =
        this.RunSelected(state.Query, id)
    
    /// Run: toList
    member this.Run(state: QuerySource<'Selected list, Query>) =
        this.RunSelected(state.Query, Seq.toList)
    
    /// Run: toArray
    member this.Run(state: QuerySource<'Selected array, Query>) =
        this.RunSelected(state.Query, Seq.toArray)
    
    /// Run: mapList
    member this.Run(state: QuerySource<'Mapped list, Query>) =
        this.RunMapped(state.Query, Seq.toList)
  
    // Run: mapArray
    member this.Run(state: QuerySource<'Mapped array, Query>) =
        this.RunMapped(state.Query, Seq.toArray)

    // Run: mapSeq
    member this.Run(state: QuerySource<'Mapped seq, Query>) =
        this.RunMapped(state.Query, id)
        
    // Run: tryHead - 'Selected
    member this.Run(state: QuerySource<'Selected option, Query>) =
        this.RunSelected(state.Query, Seq.tryHead)

    // Run: tryHead - 'Mapped
    member this.Run(state: QuerySource<'Mapped option, Query>) =
        this.RunMapped(state.Query, Seq.tryHead)

    // Run: count
    member this.Run(state: QuerySource<ResultModifier.Count<int>, Query>) =
        async {
            let ctx = ContextUtils.getContext ct
            try return! ctx.CountAsync (SelectQuery<int>(state.Query)) |> Async.AwaitTask
            finally ContextUtils.disposeIfNotShared ct ctx
        }
        |> Async.StartImmediateAsTask


/// A select builder that returns an Async result.
type SelectAsyncBuilder<'Selected, 'Mapped, 'Reader when 'Reader :> DbDataReader> (
    readEntityBuilder: 'Reader -> (unit -> 'Selected), ct: ContextType) =
    inherit SelectBuilder<'Selected, 'Mapped>()
    
    member this.RunSelected(query: Query, resultModifier) =
        async {
            let ctx = ContextUtils.getContext ct
            try 
                let selectQuery = SelectQuery<'Selected>(query)
                let! results = selectQuery |> ctx.ReadAsync readEntityBuilder |> Async.AwaitTask
                return results |> resultModifier
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }

    member this.RunMapped(query: Query, resultModifier) =
        async {
            let ctx = ContextUtils.getContext ct
            try 
                let selectQuery = SelectQuery<'Selected>(query)
                let! results = selectQuery |> ctx.ReadAsync readEntityBuilder |> Async.AwaitTask
                return results |> Seq.map this.MapFn.Value.Invoke |> resultModifier
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }

    /// Run: default
    /// Called when no mapSeq, mapArray or mapList is present; 
    /// this input will always be 'Selected -- even if select is not present.
    member this.Run(state: QuerySource<'Selected, Query>) =
        this.RunSelected(state.Query, id)
    
    /// Run: toList
    member this.Run(state: QuerySource<'Selected list, Query>) =
        this.RunSelected(state.Query, Seq.toList)
    
    /// Run: toArray
    member this.Run(state: QuerySource<'Selected array, Query>) =
        this.RunSelected(state.Query, Seq.toArray)

    /// Run: mapList
    member this.Run(state: QuerySource<'Mapped list, Query>) =
        this.RunMapped(state.Query, Seq.toList)
    
    // Run: mapArray
    member this.Run(state: QuerySource<'Mapped array, Query>) =
        this.RunMapped(state.Query, Seq.toArray)

    // Run: mapSeq
    member this.Run(state: QuerySource<'Mapped seq, Query>) =
        this.RunMapped(state.Query, id)
    
    // Run: tryHead - 'Selected
    member this.Run(state: QuerySource<'Selected option, Query>) =
        this.RunSelected(state.Query, Seq.tryHead)

    // Run: tryHead - 'Mapped
    member this.Run(state: QuerySource<'Mapped option, Query>) =
        this.RunMapped(state.Query, Seq.tryHead)

    // Run: count
    member this.Run(state: QuerySource<ResultModifier.Count<int>, Query>) =
        async {
            let ctx = ContextUtils.getContext ct
            try return! ctx.CountAsync (SelectQuery<int>(state.Query)) |> Async.AwaitTask
            finally ContextUtils.disposeIfNotShared ct ctx
        }


/// Builds and returns a select query that can be manually run by piping into QueryContext read methods
let select<'Selected, 'Mapped> = 
    SelectQueryBuilder<'Selected, 'Mapped>()

/// Builds a select query with a HydraReader.Read function and QueryContext - returns an Async query result
let selectAsync<'Selected, 'Mapped, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Selected)) ct = 
    SelectAsyncBuilder<'Selected, 'Mapped, 'Reader>(readEntityBuilder, ct)

/// Builds a select query with a HydraReader.Read function and QueryContext - returns a Task query result
let selectTask<'Selected, 'Mapped, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Selected)) ct = 
    SelectTaskBuilder<'Selected, 'Mapped, 'Reader>(readEntityBuilder, ct)


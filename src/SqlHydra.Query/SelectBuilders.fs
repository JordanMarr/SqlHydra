/// Linq select query builders
[<AutoOpen>]
module SqlHydra.Query.SelectBuilders

open System
open System.Linq.Expressions
open System.Data.Common
open System.Threading.Tasks
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

    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T>) =
        match state.TryGetOuterTableMapping() with
        | Some tbl -> 
            let query = state |> getQueryOrDefault
            QuerySource<'T, Query>(
                query.From(match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name), 
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
    member this.Where (state:QuerySource<'T>, [<ProjectionParameter>] whereExpression) = 
        let query = state |> getQueryOrDefault
        let where = LinqExpressionVisitors.visitWhere<'T> whereExpression (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(query.Where(fun w -> where), state.TableMappings)

    /// Sets the SELECT statement and filters the query to include only the selected tables
    [<CustomOperation("select", MaintainsVariableSpace = true, AllowIntoPattern = true)>]
    member this.Select (state: QuerySource<'T>, [<ProjectionParameter>] selectExpression: Expression<Func<'T, 'Selected>>) =
        let query = state |> getQueryOrDefault

        let selections = LinqExpressionVisitors.visitSelect<'T,'Selected> selectExpression

        let queryWithSelectedColumns =
            selections
            |> List.fold (fun (q: Query) -> function
                | LinqExpressionVisitors.SelectedTable tbl -> 
                    // Select all columns in table
                    q.Select($"%s{FQ.fullyQualifyTable state.TableMappings tbl}.*")
                | LinqExpressionVisitors.SelectedColumn col -> 
                    // Select a single column
                    q.Select(FQ.fullyQualifyColumn state.TableMappings col)
                | LinqExpressionVisitors.SelectedAggregateColumn (aggFn, col) -> 
                    // Currently in v2.3.7, SqlKata doesn't support multiple inline aggregate functions.
                    // Use SelectRaw as a workaround until SqlKata supports multiple aggregates.
                    // https://github.com/sqlkata/querybuilder/pull/504
                    let fqCol = FQ.fullyQualifyColumn state.TableMappings col

                    // SqlKata will translate curly braces to dialect-specific characters (ex: [] for mssql, "" for postgres)
                    let fqColWithCurlyBraces = 
                        fqCol.Split([|'.'|], StringSplitOptions.RemoveEmptyEntries)
                        |> Array.map (sprintf "{%s}")
                        |> fun parts -> String.Join(".", parts)

                    q.SelectRaw($"{aggFn}({fqColWithCurlyBraces})")
            ) query
                  
        QuerySource<'Selected, Query>(queryWithSelectedColumns, state.TableMappings)

    /// Sets the ORDER BY for single column
    [<CustomOperation("orderBy", MaintainsVariableSpace = true)>]
    member this.OrderBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let orderedQuery = 
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function 
                | LinqExpressionVisitors.OrderByColumn p -> 
                    query.OrderBy(FQ.fullyQualifyColumn state.TableMappings p)
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, p) -> 
                    query.OrderByRaw($"{aggType}({FQ.fullyQualifyColumn state.TableMappings p})")        
        QuerySource<'T, Query>(orderedQuery, state.TableMappings)

    /// Sets the ORDER BY for single column
    [<CustomOperation("thenBy", MaintainsVariableSpace = true)>]
    member this.ThenBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let orderedQuery = 
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function 
                | LinqExpressionVisitors.OrderByColumn p -> 
                    query.OrderBy(FQ.fullyQualifyColumn state.TableMappings p)
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, p) -> 
                    query.OrderByRaw($"{aggType}({FQ.fullyQualifyColumn state.TableMappings p})")        
        QuerySource<'T, Query>(orderedQuery, state.TableMappings)

    /// Sets the ORDER BY DESC for single column
    [<CustomOperation("orderByDescending", MaintainsVariableSpace = true)>]
    member this.OrderByDescending (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let orderedQuery = 
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function 
                | LinqExpressionVisitors.OrderByColumn p -> 
                    query.OrderByDesc(FQ.fullyQualifyColumn state.TableMappings p)
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, p) -> 
                    query.OrderByRaw($"{aggType}({FQ.fullyQualifyColumn state.TableMappings p}) DESC")        
        QuerySource<'T, Query>(orderedQuery, state.TableMappings)

    /// Sets the ORDER BY DESC for single column
    [<CustomOperation("thenByDescending", MaintainsVariableSpace = true)>]
    member this.ThenByDescending (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let orderedQuery = 
            LinqExpressionVisitors.visitOrderByPropertySelector<'T, 'Prop> propertySelector
            |> function 
                | LinqExpressionVisitors.OrderByColumn p -> 
                    query.OrderByDesc(FQ.fullyQualifyColumn state.TableMappings p)
                | LinqExpressionVisitors.OrderByAggregateColumn (aggType, p) -> 
                    query.OrderByRaw($"{aggType}({FQ.fullyQualifyColumn state.TableMappings p}) DESC")        
        QuerySource<'T, Query>(orderedQuery, state.TableMappings)

    /// Sets the SKIP value for query
    [<CustomOperation("skip", MaintainsVariableSpace = true)>]
    member this.Skip (state:QuerySource<'T>, skip) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, Query>(query.Skip(skip), state.TableMappings)
    
    /// Sets the TAKE value for query
    [<CustomOperation("take", MaintainsVariableSpace = true)>]
    member this.Take (state:QuerySource<'T>, take) = 
        let query = state |> getQueryOrDefault
        QuerySource<'T, Query>(query.Take(take), state.TableMappings)

    /// INNER JOIN table on one or more columns
    [<CustomOperation("join", MaintainsVariableSpace = true, IsLikeJoin = true, JoinConditionWord = "on")>]
    member this.Join (outerSource: QuerySource<'Outer>, 
                      innerSource: QuerySource<'Inner>, 
                      outerKeySelector: Expression<Func<'Outer,'Key>>, 
                      innerKeySelector: Expression<Func<'Inner,'Key>>, 
                      resultSelector: Expression<Func<'Outer,'Inner,'JoinResult>> ) = 

        let mergedTables = mergeTableMappings (outerSource.TableMappings, innerSource.TableMappings)
        let outerProperties = LinqExpressionVisitors.visitJoin<'Outer, 'Key> outerKeySelector
        let innerProperties = LinqExpressionVisitors.visitJoin<'Inner, 'Key> innerKeySelector

        let outerQuery = outerSource |> getQueryOrDefault
        let innerTableName = 
            innerProperties 
            |> Seq.map (fun p -> mergedTables.[FQ.fqName p.DeclaringType])
            |> Seq.map (fun tbl -> 
                match tbl.Schema with
                | Some schema -> sprintf "%s.%s" schema tbl.Name
                | None -> tbl.Name
            )
            |> Seq.head
        
        let joinOn = 
            let fq = FQ.fullyQualifyColumn mergedTables
            List.zip outerProperties innerProperties
            |> List.fold (fun (j: Join) (outerProp, innerProp) -> j.On(fq outerProp, fq innerProp)) (Join())
            
        QuerySource<'JoinResult, Query>(outerQuery.Join(innerTableName, fun j -> joinOn), mergedTables)

    /// LEFT JOIN table on one or more columns
    [<CustomOperation("leftJoin", MaintainsVariableSpace = true, IsLikeJoin = true, JoinConditionWord = "on")>]
    member this.LeftJoin (outerSource: QuerySource<'Outer>, 
                          innerSource: QuerySource<'Inner>, 
                          outerKeySelector: Expression<Func<'Outer,'Key>>, 
                          innerKeySelector: Expression<Func<'Inner option,'Key>>, 
                          resultSelector: Expression<Func<'Outer,'Inner option,'JoinResult>> ) = 

        let mergedTables = mergeTableMappings (outerSource.TableMappings, innerSource.TableMappings)
        let outerProperties = LinqExpressionVisitors.visitJoin<'Outer, 'Key> outerKeySelector
        let innerProperties = LinqExpressionVisitors.visitJoin<'Inner option, 'Key> innerKeySelector

        let outerQuery = outerSource |> getQueryOrDefault
        let innerTableName = 
            innerProperties 
            |> Seq.map (fun p -> mergedTables.[FQ.fqName p.DeclaringType])
            |> Seq.map (fun tbl -> 
                match tbl.Schema with
                | Some schema -> sprintf "%s.%s" schema tbl.Name
                | None -> tbl.Name
            )
            |> Seq.head

        let joinOn = 
            let fq = FQ.fullyQualifyColumn mergedTables
            List.zip outerProperties innerProperties
            |> List.fold (fun (j: Join) (outerProp, innerProp) -> j.On(fq outerProp, fq innerProp)) (Join())
            
        QuerySource<'JoinResult, Query>(outerQuery.LeftJoin(innerTableName, fun j -> joinOn), mergedTables)

    /// Sets the GROUP BY for one or more columns.
    [<CustomOperation("groupBy", MaintainsVariableSpace = true)>]
    member this.GroupBy (state:QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let query = state |> getQueryOrDefault
        let properties = LinqExpressionVisitors.visitGroupBy<'T, 'Prop> propertySelector (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(query.GroupBy(properties |> List.toArray), state.TableMappings)

    /// Sets the HAVING condition.
    [<CustomOperation("having", MaintainsVariableSpace = true)>]
    member this.Having (state:QuerySource<'T>, [<ProjectionParameter>] havingExpression) = 
        let query = state |> getQueryOrDefault
        let having = LinqExpressionVisitors.visitHaving<'T> havingExpression (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(query.Having(fun w -> having), state.TableMappings)

    /// Sets query to return DISTINCT values
    [<CustomOperation("distinct", MaintainsVariableSpace = true)>]
    member this.Distinct (state:QuerySource<'T>) = 
        let query = state |> getQueryOrDefault        
        QuerySource<'T, Query>(query.Distinct(), state.TableMappings)

    /// Maps the query results into a seq.
    [<CustomOperation("mapSeq", MaintainsVariableSpace = true)>]
    member this.MapSeq (state: QuerySource<'Selected>, [<ProjectionParameter>] map: Func<'Selected, 'Mapped>) =
        let query = state |> getQueryOrDefault
        this.MapFn <- Some map
        QuerySource<'Mapped seq, Query>(query, state.TableMappings)
    
    /// Maps the query results into an array.
    [<CustomOperation("mapArray", MaintainsVariableSpace = true)>]
    member this.MapArray (state: QuerySource<'Selected>, [<ProjectionParameter>] map: Func<'Selected, 'Mapped>) =
        let query = state |> getQueryOrDefault
        this.MapFn <- Some map
        QuerySource<'Mapped array, Query>(query, state.TableMappings)
        
    /// Maps the query results into a list.
    [<CustomOperation("mapList", MaintainsVariableSpace = true)>]
    member this.MapList (state: QuerySource<'Selected>, [<ProjectionParameter>] map: Func<'Selected, 'Mapped>) =
        let query = state |> getQueryOrDefault
        this.MapFn <- Some map
        QuerySource<'Mapped list, Query>(query, state.TableMappings)
    
    /// Returns the query results as an array.
    [<CustomOperation("toArray", MaintainsVariableSpace = true)>]
    member this.ToArray (state: QuerySource<'Selected>) =
        let query = state |> getQueryOrDefault
        QuerySource<'Selected array, Query>(query, state.TableMappings)

    /// Returns the query results as a list.
    [<CustomOperation("toList", MaintainsVariableSpace = true)>]
    member this.ToList (state: QuerySource<'Selected>) =
        let query = state |> getQueryOrDefault
        QuerySource<'Selected list, Query>(query, state.TableMappings)

    /// COUNT aggregate function
    [<CustomOperation("count", MaintainsVariableSpace = true)>]
    member this.Count (state:QuerySource<'T>) = 
        let query = state |> getQueryOrDefault
        QuerySource<ResultModifier.Count<int>, Query>(query.AsCount(), state.TableMappings)

    /// Applies Seq.tryHead to the query results.
    [<CustomOperation("tryHead", MaintainsVariableSpace = true)>]
    member this.TryHead (state: QuerySource<'Selected, Query>) = 
        QuerySource<'Mapped option, Query>(state.Query, state.TableMappings)


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
        
    // Run: tryHead
    member this.Run(state: QuerySource<'Selected option, Query>) =
        this.RunSelected(state.Query, Seq.tryHead)

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
    
    // Run: tryHead
    member this.Run(state: QuerySource<'Selected option, Query>) =
        this.RunSelected(state.Query, Seq.tryHead)

    // Run: count
    member this.Run(state: QuerySource<ResultModifier.Count<int>, Query>) =
        async {
            let ctx = ContextUtils.getContext ct
            try return! ctx.CountAsync (SelectQuery<int>(state.Query)) |> Async.AwaitTask
            finally ContextUtils.disposeIfNotShared ct ctx
        }


/// Builds and returns a select query.
let select<'Selected, 'Mapped> = 
    SelectBuilder<'Selected, 'Mapped>()

/// Builds a select query with a HydraReader.Read function and QueryContext - returns a Task query result
let selectTask<'Selected, 'Mapped, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Selected)) ct = 
    SelectTaskBuilder<'Selected, 'Mapped, 'Reader>(readEntityBuilder, ct)

/// Builds a select query with a HydraReader.Read function and QueryContext - returns an Async query result
let selectAsync<'Selected, 'Mapped, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Selected)) ct = 
    SelectAsyncBuilder<'Selected, 'Mapped, 'Reader>(readEntityBuilder, ct)


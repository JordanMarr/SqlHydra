/// LINQ builders for SqlKata.Query
[<AutoOpen>]
module SqlHydra.Query.Builders

open System
open System.Linq.Expressions
open SqlKata
open System.Data.Common

let getQueryOrDefault (state: QuerySource<'T>) =
    match state with
    | :? QuerySource<'T, Query> as qs -> qs.Query
    | _ -> Query()            

type SelectBuilder<'Selected> () =

    let mergeTableMappings (a: Map<FQ.FQName, TableMapping>, b: Map<FQ.FQName, TableMapping>) =
        Map (Seq.concat [ (Map.toSeq a); (Map.toSeq b) ])

    //let mutable _mapper = Unchecked.defaultof<'Selected -> 'Transform>

    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T>) =
        let tbl = state.GetOuterTableMapping()
        let query = state |> getQueryOrDefault
        QuerySource<'T, Query>(
            query.From(match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name), 
            state.TableMappings)

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
    [<CustomOperation("select", MaintainsVariableSpace = true)>]
    member this.Select (state: QuerySource<'T>, [<ProjectionParameter>] selectExpression: Expression<Func<'T, 'U>>) =
        let query = state |> getQueryOrDefault

        let selections = LinqExpressionVisitors.visitSelect<'T,'U> selectExpression

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
                  
        QuerySource<'U, Query>(queryWithSelectedColumns, state.TableMappings)

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

    /// COUNT aggregate function
    [<CustomOperation("count", MaintainsVariableSpace = true)>]
    member this.Count (state:QuerySource<'T>) = 
        let query = state |> getQueryOrDefault
        QuerySource<int, Query>(query.AsCount(), state.TableMappings)

    /// Sets query to return DISTINCT values
    [<CustomOperation("distinct", MaintainsVariableSpace = true)>]
    member this.Distinct (state:QuerySource<'T>) = 
        let query = state |> getQueryOrDefault        
        QuerySource<'T, Query>(query.Distinct(), state.TableMappings)

    [<CustomOperation("map", MaintainsVariableSpace = true)>]
    member this.Map (state: QuerySource<'T>, [<ProjectionParameter>] selectExpression: Expression<Func<'T, 'U>>) =
        let query = state |> getQueryOrDefault
        QuerySource<'U, Query>(query, state.TableMappings)

type SelectTaskBuilder<'Selected, 'Reader when 'Reader :> DbDataReader> (
    readEntityBuilder: 'Reader -> (unit -> 'Selected), 
    conn: System.Data.Common.DbConnection) =
    inherit SelectBuilder<'Selected>()
    
    member this.Run(state: QuerySource<'Selected>) =
        async {
            let query = state |> getQueryOrDefault
            let selectQuery = SelectQuery<'Selected>(query)
            use ctx = new QueryContext(conn, SqlKata.Compilers.SqlServerCompiler())
            let! result = selectQuery |> ctx.ReadAsync readEntityBuilder |> Async.AwaitTask
            return result
        }
        |> Async.StartImmediateAsTask

type SelectAsyncBuilder<'Selected, 'Reader when 'Reader :> DbDataReader> (
    readEntityBuilder: 'Reader -> (unit -> 'Selected), 
    conn: System.Data.Common.DbConnection) =
    inherit SelectBuilder<'Selected>()
    
    member this.Run(state: QuerySource<'Selected>) =
        async {
            let query = state |> getQueryOrDefault
            let selectQuery = SelectQuery<'Selected>(query)
            use ctx = new QueryContext(conn, SqlKata.Compilers.SqlServerCompiler())
            let! result = selectQuery |> ctx.ReadAsync readEntityBuilder |> Async.AwaitTask
            return result
        }

let selectTask<'Selected, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Selected)) conn = 
    SelectTaskBuilder(readEntityBuilder, conn)

let selectAsync<'Selected, 'Reader when 'Reader :> DbDataReader> (readEntityBuilder: 'Reader -> (unit -> 'Selected)) conn = 
    SelectAsyncBuilder(readEntityBuilder, conn)
/// Linq delete query builders
[<AutoOpen>]
module SqlHydra.Query.DeleteBuilders

open System.Threading
open SqlKata

let private prepareDeleteQuery<'Deleted> (query: Query) = 
    DeleteQuery<'Deleted>(query.AsDelete())

/// The base delete builder that contains all common operations
type DeleteBuilder<'Deleted>() =

    let getQueryOrDefault (state: QuerySource<'T>) =
        match state with
        | :? QuerySource<'T, Query> as qs -> qs.Query
        | _ -> Query()            

    member this.For (state: QuerySource<'T>, [<ReflectedDefinition>] forExpr: FSharp.Quotations.Expr<'T -> QuerySource<'T>>) =
        let query = state |> getQueryOrDefault
        let tableAlias = QuotationVisitor.visitFor forExpr |> QuotationVisitor.allowUnderscore true
        let tblMaybe, tableMappings = TableMappings.tryGetByRootOrAlias tableAlias state.TableMappings
        let tbl = tblMaybe |> Option.get

        QuerySource<'T, Query>(
            query.From($"{tbl.Schema}.{tbl.Name}"), 
            tableMappings)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    /// Sets the WHERE condition
    [<CustomOperation("where", MaintainsVariableSpace = true)>]
    member this.Where (state:QuerySource<'T>, [<ProjectionParameter>] whereExpression) = 
        let query = state |> getQueryOrDefault
        let tableMappings = state.TableMappings |> Map.values
        let where = LinqExpressionVisitors.visitWhere<'T> tableMappings whereExpression (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, Query>(query.Where(fun w -> where), state.TableMappings)

    /// Deletes all records in the table (only when there are is no where clause)
    [<CustomOperation("deleteAll", MaintainsVariableSpace = true)>]
    member this.DeleteAll (state:QuerySource<'T>) = 
        state :?> QuerySource<'T, Query>

    /// Unwraps the query
    member this.Run (state: QuerySource<'Deleted>) =
        state 
        |> getQueryOrDefault 
        |> prepareDeleteQuery


/// A delete builder that returns an Async result.
type DeleteAsyncBuilder<'Deleted>(ct: ContextType) =
    inherit DeleteBuilder<'Deleted>()

    member this.Run (state: QuerySource<'Deleted, Query>) = 
        async {
            let deleteQuery = state.Query |> prepareDeleteQuery
            let! ctx = ContextUtils.getContext ct |> Async.AwaitTask
            try
                let! cancel = Async.CancellationToken
                let! result = ctx.DeleteAsyncWithOptions (deleteQuery, cancel) |> Async.AwaitTask
                return result
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }


/// A delete builder that returns a Task result.
type DeleteTaskBuilder<'Deleted>(ct: ContextType, cancellationToken: CancellationToken) =
    inherit DeleteBuilder<'Deleted>()
    
    new(ct) = DeleteTaskBuilder(ct, CancellationToken.None)

    member this.Run (state: QuerySource<'Deleted, Query>) = 
        task {
            let deleteQuery = state.Query |> prepareDeleteQuery
            let! ctx = ContextUtils.getContext ct
            try
                let! result = ctx.DeleteAsyncWithOptions (deleteQuery, cancellationToken) |> Async.AwaitTask
                return result
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }
    
/// Builds and returns a delete query that can be manually run by piping into QueryContext delete methods
let delete<'Deleted> = 
    DeleteBuilder<'Deleted>()

/// Builds and returns a delete query that returns an Async result
let deleteAsync<'Deleted> ct = 
    DeleteAsyncBuilder<'Deleted>(ct)

/// Builds and returns a delete query that returns a Task result
let deleteTask<'Deleted> ct = 
    DeleteTaskBuilder<'Deleted>(ct)
    
/// Builds and returns a delete query with a QueryContext and a CancellationToken - returns a Task result
let deleteTaskCancellable<'Deleted> ct cancellationToken = 
    DeleteTaskBuilder<'Deleted>(ct, cancellationToken)
    
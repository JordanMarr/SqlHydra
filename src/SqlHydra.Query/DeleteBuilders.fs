/// Linq delete query builders
[<AutoOpen>]
module SqlHydra.Query.DeleteBuilders

open System
open System.Linq.Expressions
open System.Data.Common
open System.Threading.Tasks
open SqlKata

let private prepareDeleteQuery<'Deleted> (query: Query) = 
    DeleteQuery<'Deleted>(query.AsDelete())

/// The base delete builder that contains all common operations
type DeleteBuilder<'Deleted>() =

    let getQueryOrDefault (state: QuerySource<'T>) =
        match state with
        | :? QuerySource<'T, Query> as qs -> qs.Query
        | _ -> Query()            

    member this.For (state: QuerySource<'T>, f: 'T -> QuerySource<'T>) =
        let tbl = state.GetOuterTableMapping()
        let query = state |> getQueryOrDefault
        QuerySource<'T, Query>(
            query.From(match tbl.Schema with Some schema -> $"{schema}.{tbl.Name}" | None -> tbl.Name), 
            state.TableMappings)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    /// Sets the WHERE condition
    [<CustomOperation("where", MaintainsVariableSpace = true)>]
    member this.Where (state:QuerySource<'T>, [<ProjectionParameter>] whereExpression) = 
        let query = state |> getQueryOrDefault
        let where = LinqExpressionVisitors.visitWhere<'T> whereExpression (FQ.fullyQualifyColumn state.TableMappings)
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
            let ctx = ContextUtils.getContext ct
            try 
                let! result = deleteQuery |> ctx.DeleteAsync |> Async.AwaitTask
                return result
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }


/// A delete builder that returns a Task result.
type DeleteTaskBuilder<'Deleted>(ct: ContextType) =
    inherit DeleteBuilder<'Deleted>()

    member this.Run (state: QuerySource<'Deleted, Query>) = 
        async {
            let deleteQuery = state.Query |> prepareDeleteQuery
            let ctx = ContextUtils.getContext ct
            try 
                let! result = deleteQuery |> ctx.DeleteAsync |> Async.AwaitTask
                return result
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }
        |> Async.StartImmediateAsTask
    
/// Builds and returns a delete query.
let delete<'Deleted> = 
    DeleteBuilder<'Deleted>()

/// Builds and returns a delete query that returns an Async result.
let deleteAsync<'Deleted> ct = 
    DeleteAsyncBuilder<'Deleted>(ct)

/// Builds and returns a delete query that returns a Task result.
let deleteTask<'Deleted> ct = 
    DeleteTaskBuilder<'Deleted>(ct)
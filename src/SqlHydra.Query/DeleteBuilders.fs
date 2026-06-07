/// Linq delete query builders
[<AutoOpen>]
module SqlHydra.Query.DeleteBuilders

open System
open System.Linq.Expressions
open System.Threading

let private prepareDeleteQuery<'Deleted> (spec: DeleteQuerySpec<'Deleted>) =
    if spec.Where = WhereClause.Empty && not spec.DeleteAll then
        invalidOp "A `delete` expression must either contain a `where` clause or `deleteAll`."
    DeleteQuery<'Deleted>({ Table = spec.Table; Where = spec.Where; Returning = spec.Returning })

/// The base delete builder that contains all common operations
type DeleteBuilder<'Deleted>() =

    let getQueryOrDefault (state: QuerySource<'T>) =
        match state with
        | :? QuerySource<'T, DeleteQuerySpec<'T>> as qs -> qs.Query
        | _ -> DeleteQuerySpec.Default

    member val CancellationToken = CancellationToken.None with get, set

    member this.For (state: QuerySource<'T>, [<ReflectedDefinition>] forExpr: FSharp.Quotations.Expr<'T -> QuerySource<'T>>) =
        let spec = state |> getQueryOrDefault
        let tableAlias = QuotationVisitor.visitFor forExpr |> QuotationVisitor.allowUnderscore true
        let tblMaybe, tableMappings = TableMappings.tryGetByRootOrAlias tableAlias state.TableMappings
        let tbl = tblMaybe |> Option.get
        QuerySource<'T, DeleteQuerySpec<'T>>(
            { spec with Table = $"{tbl.Schema}.{tbl.Name}" },
            tableMappings)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    /// Sets the WHERE condition
    [<CustomOperation("where", MaintainsVariableSpace = true)>]
    member this.Where (state: QuerySource<'T>, [<ProjectionParameter>] whereExpression) =
        let spec = state |> getQueryOrDefault
        let tableMappings = state.TableMappings |> Map.values
        let newClause = LinqExpressionVisitors.visitWhere<'T> tableMappings whereExpression (FQ.fullyQualifyColumn state.TableMappings)
        QuerySource<'T, DeleteQuerySpec<'T>>(
            { spec with Where = WhereClause.combineAnd spec.Where newClause; DeleteAll = false },
            state.TableMappings)

    /// Adds one or more columns to the DELETE ... RETURNING clause (PostgreSQL).
    /// Pass a single property or a tuple of properties.
    [<CustomOperation("returning", MaintainsVariableSpace = true)>]
    member this.Returning (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector: Expression<Func<'T, 'Prop>>) =
        let spec = state |> getQueryOrDefault
        let cols = LinqExpressionVisitors.visitPropertiesSelector<'T, 'Prop> propertySelector (fun _ p -> p.Name)
        QuerySource<'T, DeleteQuerySpec<'T>>(
            { spec with Returning = spec.Returning @ cols },
            state.TableMappings)

    /// Safeguard verifying that all rows in the table should be deleted (no `where` clause).
    [<CustomOperation("deleteAll", MaintainsVariableSpace = true)>]
    member this.DeleteAll (state: QuerySource<'T>) =
        let spec = state |> getQueryOrDefault
        if spec.Where <> WhereClause.Empty then
            invalidOp "Cannot have `deleteAll` clause in a query where `where` has been used."
        QuerySource<'T, DeleteQuerySpec<'T>>({ spec with DeleteAll = true }, state.TableMappings)

    /// Sets a CancellationToken for the query execution.
    [<CustomOperation("cancel", MaintainsVariableSpace = true)>]
    member this.Cancel (state: QuerySource<'T, DeleteQuerySpec<'T>>, cancellationToken: CancellationToken) =
        this.CancellationToken <- cancellationToken
        state

    /// Unwraps the query
    member this.Run (state: QuerySource<'Deleted>) =
        state |> getQueryOrDefault |> prepareDeleteQuery


/// A delete builder that returns an Async result.
type DeleteAsyncBuilder<'Deleted>(ct: ContextType) =
    inherit DeleteBuilder<'Deleted>()

    member this.Run (state: QuerySource<'Deleted, DeleteQuerySpec<'Deleted>>) =
        async {
            let deleteQuery = state.Query |> prepareDeleteQuery
            let! ctx = ContextUtils.getContext ct |> Async.AwaitTask
            try
                let! asyncCancel = Async.CancellationToken
                let cancel = if this.CancellationToken <> CancellationToken.None then this.CancellationToken else asyncCancel
                let! result = ctx.DeleteAsyncWithOptions (deleteQuery, cancel) |> Async.AwaitTask
                return result
            finally
                ContextUtils.disposeIfNotShared ct ctx
        }


/// A delete builder that returns a Task result.
type DeleteTaskBuilder<'Deleted>(ct: ContextType) =
    inherit DeleteBuilder<'Deleted>()

    member this.Run (state: QuerySource<'Deleted, DeleteQuerySpec<'Deleted>>) =
        task {
            let deleteQuery = state.Query |> prepareDeleteQuery
            let! ctx = ContextUtils.getContext ct
            try
                let! result = ctx.DeleteAsyncWithOptions (deleteQuery, this.CancellationToken) |> Async.AwaitTask
                return result
            finally
                ContextUtils.disposeIfNotShared ct ctx
        }

/// Builds and returns a delete query that can be manually run by piping into QueryContext delete methods
let delete<'Deleted> =
    DeleteBuilder<'Deleted>()

/// Builds and returns a delete query that returns an Async result
let inline deleteAsync< ^Deleted, ^Context
    when (ContextTypeResolver.Resolver or ^Context) : (static member ($) : ContextTypeResolver.Resolver * ^Context -> ContextType)>
    (ctSource: ^Context) =
    let ct = ContextTypeResolver.resolve ctSource
    DeleteAsyncBuilder< ^Deleted>(ct)

/// Builds and returns a delete query that returns a Task result
let inline deleteTask< ^Deleted, ^Context
    when (ContextTypeResolver.Resolver or ^Context) : (static member ($) : ContextTypeResolver.Resolver * ^Context -> ContextType)>
    (ctSource: ^Context) =
    let ct = ContextTypeResolver.resolve ctSource
    DeleteTaskBuilder< ^Deleted>(ct)

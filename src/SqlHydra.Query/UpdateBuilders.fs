/// Linq update query builders
[<AutoOpen>]
module SqlHydra.Query.UpdateBuilders

open System
open System.Linq.Expressions
open System.Threading

let private prepareUpdateQuery<'Updated, 'UpdateReturn> (spec: UpdateQuerySpec<'Updated, 'UpdateReturn>) =
    if spec.Where = WhereClause.Empty && spec.UpdateAll = false
    then invalidOp "An `update` expression must either contain a `where` clause or `updateAll`."
    UpdateQuery<'Updated, 'UpdateReturn>(spec)

/// The base update builder that contains all common operations
type UpdateBuilder<'Updated, 'UpdateReturn>() =

    let getQueryOrDefault (state: QuerySource<'T>) =
        match state with
        | :? QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>> as qs -> qs.Query
        | _ -> UpdateQuerySpec.Default

    member val CancellationToken = CancellationToken.None with get, set

    member this.For (state: QuerySource<'T>, [<ReflectedDefinition>] forExpr: FSharp.Quotations.Expr<'T -> QuerySource<'T>>) =
        let query = state |> getQueryOrDefault
        let tableAlias = QuotationVisitor.visitFor forExpr |> QuotationVisitor.allowUnderscore false
        let tblMaybe, tableMappings = TableMappings.tryGetByRootOrAlias tableAlias state.TableMappings
        let tbl = tblMaybe |> Option.get

        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>({ query with Table = $"{tbl.Schema}.{tbl.Name}" }, tableMappings)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    /// Sets the emtore entity ('T) to be updated
    [<CustomOperation("entity", MaintainsVariableSpace = true)>]
    member this.Entity (state: QuerySource<'T>, value: 'T) =
        let query = state |> getQueryOrDefault
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>(
            { query with Entity = Some (box value) }
            , state.TableMappings)

    /// Sets the entity to be updated from the table's write record, which has no field for a read-only column.
    [<CustomOperation("writeEntity", MaintainsVariableSpace = true)>]
    member this.WriteEntity<'T, 'Write when 'Write :> SqlHydra.IWriteOf<'T>> (state: QuerySource<'T>, value: 'Write) =
        let query = state |> getQueryOrDefault
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>(
            { query with Entity = Some (box value) }
            , state.TableMappings)

    /// Sets a property of the entity ('T) to be updated
    [<CustomOperation("set", MaintainsVariableSpace = true)>]
    member this.Set (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector: Expression<Func<'T, 'Prop>>, value: 'Prop) =
        let query = state |> getQueryOrDefault
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector :?> Reflection.PropertyInfo

        let value = QueryUtils.getQueryParameterForValue prop value
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>(
            { query with SetValues = query.SetValues @ [ prop.Name, value ] }
            , state.TableMappings)

    /// Sets a column to a raw SQL fragment with parameters. Use ? as parameter placeholders.
    /// Example: setRaw c.col "COALESCE(?, col)" [| box value |]
    [<CustomOperation("setRaw", MaintainsVariableSpace = true)>]
    member this.SetRaw (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector: Expression<Func<'T, 'Prop>>, fragment: string, parameters: obj[]) =
        let query = state |> getQueryOrDefault
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector :?> Reflection.PropertyInfo
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>(
            { query with RawSetValues = query.RawSetValues @ [ prop.Name, fragment, parameters ] }
            , state.TableMappings)

    /// Convenience overload: setRaw with no parameters.
    [<CustomOperation("setRaw", MaintainsVariableSpace = true)>]
    member this.SetRaw (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector: Expression<Func<'T, 'Prop>>, fragment: string) =
        this.SetRaw(state, propertySelector, fragment, [||])

    /// Adds one or more columns to the RETURNING clause (PostgreSQL).
    /// Pass a single property `e.id` or a tuple `(e.id, e.email, e.updated_at)`.
    [<CustomOperation("returning", MaintainsVariableSpace = true)>]
    member this.Returning (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector: Expression<Func<'T, 'Prop>>) =
        let query = state |> getQueryOrDefault
        let cols = LinqExpressionVisitors.visitPropertiesSelector<'T, 'Prop> propertySelector (fun _ p -> p.Name)
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>(
            { query with Returning = query.Returning @ cols }
            , state.TableMappings)

    /// Includes a column in the update query.
    [<CustomOperation("includeColumn", MaintainsVariableSpace = true)>]
    member this.IncludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) =
        let query = state |> getQueryOrDefault
        let prop = (propertySelector |> LinqExpressionVisitors.visitPropertySelector<'T, 'Prop>).Name
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>({ query with Fields = query.Fields @ [ prop ] }, state.TableMappings)

    /// Excludes a column from the update query.
    [<CustomOperation("excludeColumn", MaintainsVariableSpace = true)>]
    member this.ExcludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) =
        let query = state |> getQueryOrDefault
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector
        let newQuery =
            query.Fields
            |> function
                | [] -> FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) |> Array.map (fun x -> x.Name) |> Array.toList
                | fields -> fields
            |> List.filter (fun f -> f <> prop.Name)
            |> (fun x -> { query with Fields = x })
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>(newQuery, state.TableMappings)

    /// Sets the WHERE condition
    [<CustomOperation("where", MaintainsVariableSpace = true)>]
    member this.Where (state: QuerySource<'T>, [<ProjectionParameter>] whereExpression) =
        let query = state |> getQueryOrDefault
        let tableMappings = state.TableMappings |> Map.values
        let newClause = LinqExpressionVisitors.visitWhere<'T> tableMappings whereExpression (FQ.fullyQualifyColumn state.TableMappings)
        if query.UpdateAll then
            invalidOp "Cannot have `where` clause in a query where `updateAll` has been used."
        let where' = WhereClause.combineAnd query.Where newClause
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>({ query with Where = where'; UpdateAll = false }, state.TableMappings)

    /// A safeguard that verifies that all records in the table should be updated.
    [<CustomOperation("updateAll", MaintainsVariableSpace = true)>]
    member this.UpdateAll (state: QuerySource<'T>) =
        let query = state |> getQueryOrDefault
        if query.Where <> WhereClause.Empty then
            invalidOp "Cannot have `updateAll` clause in a query where `where` has been used."
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>({ query with UpdateAll = true; Where = WhereClause.Empty }, state.TableMappings)

    /// Sets a CancellationToken for the query execution.
    [<CustomOperation("cancel", MaintainsVariableSpace = true)>]
    member this.Cancel (state: QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>, cancellationToken: CancellationToken) =
        this.CancellationToken <- cancellationToken
        state

    /// Sets the command execution timeout for this query.
    /// Sub-second positive values are rounded up to one second. 
    /// Passing `TimeSpan.Zero` is interpreted as "wait indefinitely".
    /// Omitting `timeout` leaves the provider's default in place.
    [<CustomOperation("timeout", MaintainsVariableSpace = true)>]
    member this.Timeout (state: QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>, timeout: TimeSpan) =
        let query = state |> getQueryOrDefault
        QuerySource<'T, UpdateQuerySpec<'T, 'UpdateReturn>>({ query with CommandOptions = { query.CommandOptions with CommandTimeout = Some timeout } }, state.TableMappings)

    /// Unwraps the query
    member this.Run (state: QuerySource<'Updated>) =
        state |> getQueryOrDefault |> prepareUpdateQuery


/// An update builder that returns an Async result.
type UpdateAsyncBuilder<'Updated, 'UpdateReturn>(ct: ContextType) =
    inherit UpdateBuilder<'Updated, 'UpdateReturn>()

    member this.Run (state: QuerySource<'Updated, UpdateQuerySpec<'Updated, 'UpdateReturn>>) =
        async {
            let updateQuery = state.Query |> prepareUpdateQuery
            let! ctx = ContextUtils.getContext ct |> Async.AwaitTask
            try
                let! asyncCancel = Async.CancellationToken
                let cancel = if this.CancellationToken <> CancellationToken.None then this.CancellationToken else asyncCancel
                let! result = ctx.UpdateAsyncWithOptions (updateQuery, cancel) |> Async.AwaitTask
                return result
            finally
                ContextUtils.disposeIfNotShared ct ctx
        }


/// An update builder that returns a Task result.
type UpdateTaskBuilder<'Updated, 'UpdateReturn>(ct: ContextType) =
    inherit UpdateBuilder<'Updated, 'UpdateReturn>()

    member this.Run (state: QuerySource<'Updated, UpdateQuerySpec<'Updated, 'UpdateReturn>>) =
        task {
            let updateQuery = state.Query |> prepareUpdateQuery
            let! ctx = ContextUtils.getContext ct
            try
                let! result = ctx.UpdateAsyncWithOptions (updateQuery, this.CancellationToken) |> Async.AwaitTask
                return result
            finally
                ContextUtils.disposeIfNotShared ct ctx
        }


/// Builds and returns an update query that can be manually run by piping into QueryContext update methods
let update<'Updated, 'UpdateReturn> =
    UpdateBuilder<'Updated, 'UpdateReturn>()

/// Builds an update query that returns an Async result
let inline updateAsync< ^Updated, ^UpdateReturn, ^Context
    when (ContextTypeResolver.Resolver or ^Context) : (static member ($) : ContextTypeResolver.Resolver * ^Context -> ContextType)>
    (ctSource: ^Context) =
    let ct = ContextTypeResolver.resolve ctSource
    UpdateAsyncBuilder< ^Updated, ^UpdateReturn>(ct)

/// Builds an update query that returns a Task result
let inline updateTask< ^Updated, ^UpdateReturn, ^Context
    when (ContextTypeResolver.Resolver or ^Context) : (static member ($) : ContextTypeResolver.Resolver * ^Context -> ContextType)>
    (ctSource: ^Context) =
    let ct = ContextTypeResolver.resolve ctSource
    UpdateTaskBuilder< ^Updated, ^UpdateReturn>(ct)


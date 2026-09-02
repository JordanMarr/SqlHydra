/// Linq insert query builders
[<AutoOpen>]
module SqlHydra.Query.InsertBuilders

open System
open System.Threading

/// The base insert builder that contains all common operations
type InsertBuilder<'Inserted, 'InsertReturn>() =

    let getQueryOrDefault (state: QuerySource<'T>) =
        match state with
        | :? QuerySource<'T, InsertQuerySpec<'T, 'IdentityReturn>> as qs -> qs.Query
        | _ -> InsertQuerySpec.Default

    member val CancellationToken = CancellationToken.None with get, set

    member this.For (state: QuerySource<'T>, [<ReflectedDefinition>] forExpr: FSharp.Quotations.Expr<'T -> QuerySource<'T>>) =        
        let query = state |> getQueryOrDefault
        let tableAlias = QuotationVisitor.visitFor forExpr |> QuotationVisitor.allowUnderscore false
        let tblMaybe, tableMappings = TableMappings.tryGetByRootOrAlias tableAlias state.TableMappings
        let tbl = tblMaybe |> Option.get

        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { query with Table = $"{tbl.Schema}.{tbl.Name}" }
            , tableMappings)

    /// Sets the TABLE name for query.
    [<CustomOperation("into")>]
    member this.Into (state: QuerySource<'T>, table: QuerySource<'T>) =
        let tbl = TableMappings.getFirst table.TableMappings
        let query = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { query with Table = $"{tbl.Schema}.{tbl.Name}" }
            , state.TableMappings)

    member this.Yield _ =
        QuerySource<'T>(Map.empty)

    /// Sets a single value for INSERT
    [<CustomOperation("entity", MaintainsVariableSpace = true)>]
    member this.Entity (state:QuerySource<'T>, value: 'T) = 
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Entities = [ box value ] }
            , state.TableMappings)

    /// Sets a single value for INSERT from the table's write record, which has no field for a read-only column.
    [<CustomOperation("writeEntity", MaintainsVariableSpace = true)>]
    member this.WriteEntity<'T, 'Write when 'Write :> SqlHydra.IWriteOf<'T>> (state:QuerySource<'T>, value: 'Write) =
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Entities = [ box value ] }
            , state.TableMappings)

    /// Sets multiple values for INSERT. (Must have at least one value.)
    [<CustomOperation("entities", MaintainsVariableSpace = true)>]
    member this.Entities (state:QuerySource<'T>, entities: AtLeastOne.AtLeastOne<'T>) = 
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Entities = entities |> AtLeastOne.getSeq |> Seq.map box |> Seq.toList }
            , state.TableMappings)

    /// Sets multiple values for INSERT. (Should have at least one value.)
    [<CustomOperation("entities", MaintainsVariableSpace = true)>]
    member this.Entities (state:QuerySource<'T>, entities: 'T seq) = 
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Entities = entities |> Seq.map box |> Seq.toList }
            , state.TableMappings)

    /// Sets multiple values for INSERT from the table's write record. (Should have at least one value.)
    [<CustomOperation("writeEntities", MaintainsVariableSpace = true)>]
    member this.WriteEntities<'T, 'Write when 'Write :> SqlHydra.IWriteOf<'T>> (state:QuerySource<'T>, entities: 'Write seq) =
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Entities = entities |> Seq.map box |> Seq.toList }
            , state.TableMappings)

    /// Includes a column in the insert query.
    [<CustomOperation("includeColumn", MaintainsVariableSpace = true)>]
    member this.IncludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let spec = state |> getQueryOrDefault
        let prop = (propertySelector |> LinqExpressionVisitors.visitPropertySelector<'T, 'Prop>).Name
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>({ spec with Fields = spec.Fields @ [ prop ] }, state.TableMappings)

    /// Excludes a column from the insert query.
    [<CustomOperation("excludeColumn", MaintainsVariableSpace = true)>]
    member this.ExcludeColumn (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector) = 
        let spec = state |> getQueryOrDefault
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'Prop> propertySelector
        let newSpec =
            spec.Fields
            |> function
                | [] -> FSharp.Reflection.FSharpType.GetRecordFields(typeof<'T>) |> Array.map (fun x -> x.Name) |> Array.toList
                | fields -> fields
            |> List.filter (fun f -> f <> prop.Name)
            |> (fun x -> { spec with Fields = x })
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(newSpec, state.TableMappings)
    
    /// Inserts the result of a SELECT query: INSERT INTO ... (cols) <select-subquery>
    [<CustomOperation("fromSelect", MaintainsVariableSpace = true)>]
    member this.FromSelect (state: QuerySource<'T>, subquery: SelectQuery) =
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with FromSelect = Some subquery.SelectIR }
            , state.TableMappings)

    /// Adds one or more columns to the RETURNING clause (PostgreSQL/SQLite).
    /// Pass a single property `e.id` or a tuple `(e.id, e.email, e.created_at)`.
    [<CustomOperation("returning", MaintainsVariableSpace = true)>]
    member this.Returning (state: QuerySource<'T>, [<ProjectionParameter>] propertySelector: System.Linq.Expressions.Expression<Func<'T, 'Prop>>) =
        let spec = state |> getQueryOrDefault
        let cols = LinqExpressionVisitors.visitPropertiesSelector<'T, 'Prop> propertySelector (fun _ p -> p.Name)
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Returning = spec.Returning @ cols }
            , state.TableMappings)

    /// Sets the identity field that should be returned from the insert and excludes it from the insert columns.
    [<CustomOperation("getId", MaintainsVariableSpace = true)>]
    member this.GetId (state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, [<ProjectionParameter>] idProperty) = 
        // Exclude the identity column
        let spec = this.ExcludeColumn(state, idProperty).Query
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'InsertReturn> idProperty :?> Reflection.PropertyInfo
        
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>({ spec with IdentityField = Some prop.Name }, state.TableMappings)

    /// Sets a CancellationToken for the query execution.
    [<CustomOperation("cancel", MaintainsVariableSpace = true)>]
    member this.Cancel (state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, cancellationToken: CancellationToken) =
        this.CancellationToken <- cancellationToken
        state

    /// Sets the command execution timeout for this query.
    /// Sub-second positive values are rounded up to one second. 
    /// Passing `TimeSpan.Zero` is interpreted as "wait indefinitely".
    /// Omitting `timeout` leaves the provider's default in place.
    [<CustomOperation("timeout", MaintainsVariableSpace = true)>]
    member this.Timeout (state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, timeout: TimeSpan) =
        let query = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>({ query with CommandOptions = { query.CommandOptions with CommandTimeout = Some timeout } }, state.TableMappings)

    member this.Run (state: QuerySource<'Inserted>) =
        let spec = getQueryOrDefault state
        InsertQuery<'Inserted, 'InsertReturn>(spec)


/// An insert builder that returns a Task result.
type InsertAsyncBuilder<'Inserted, 'InsertReturn>(ct: ContextType) =
    inherit InsertBuilder<'Inserted, 'InsertReturn>()

    member this.Run (state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>) = 
        async {
            let! ctx = ContextUtils.getContext ct |> Async.AwaitTask 
            try 
                let insertQuery = InsertQuery<'Inserted, 'InsertReturn>(state.Query)
                let! asyncCancel = Async.CancellationToken
                let cancel = if this.CancellationToken <> CancellationToken.None then this.CancellationToken else asyncCancel
                if state.Query.Entities |> Seq.isEmpty && state.Query.FromSelect.IsNone then
                    return Unchecked.defaultof<'InsertReturn>
                else
                    let! insertReturn = ctx.InsertAsyncWithOptions (insertQuery, cancel) |> Async.AwaitTask
                    return insertReturn
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }


/// An insert builder that returns an Async result.
type InsertTaskBuilder<'Inserted, 'InsertReturn>(ct: ContextType) =
    inherit InsertBuilder<'Inserted, 'InsertReturn>()

    member this.Run (state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>) =
        task {
            let! ctx = ContextUtils.getContext ct
            try
                let insertQuery = InsertQuery<'Inserted, 'InsertReturn>(state.Query)
                if state.Query.Entities |> Seq.isEmpty && state.Query.FromSelect.IsNone then
                    return Unchecked.defaultof<'InsertReturn>
                else
                    let! insertReturn = ctx.InsertAsyncWithOptions (insertQuery, this.CancellationToken)
                    return insertReturn
            finally
                ContextUtils.disposeIfNotShared ct ctx
        }


/// Builds an insert query that can be manually run by piping into QueryContext insert methods
let insert<'Inserted, 'InsertReturn> = 
    InsertBuilder<'Inserted, 'InsertReturn>()

/// Builds an insert query that returns an Async result
let inline insertAsync< ^Inserted, ^InsertReturn, ^Context
    when (ContextTypeResolver.Resolver or ^Context) : (static member ($) : ContextTypeResolver.Resolver * ^Context -> ContextType)>
    (ctSource: ^Context) =
    let ct = ContextTypeResolver.resolve ctSource
    InsertAsyncBuilder< ^Inserted, ^InsertReturn>(ct)

/// Builds an insert query that returns a Task result
let inline insertTask< ^Inserted, ^InsertReturn, ^Context
    when (ContextTypeResolver.Resolver or ^Context) : (static member ($) : ContextTypeResolver.Resolver * ^Context -> ContextType)>
    (ctSource: ^Context) =
    let ct = ContextTypeResolver.resolve ctSource
    InsertTaskBuilder< ^Inserted, ^InsertReturn>(ct)
    

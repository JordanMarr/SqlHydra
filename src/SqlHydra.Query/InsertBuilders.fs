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
            { spec with Entities = [ value ] }
            , state.TableMappings)

    /// Sets multiple values for INSERT. (Must have at least one value.)
    [<CustomOperation("entities", MaintainsVariableSpace = true)>]
    member this.Entities (state:QuerySource<'T>, entities: AtLeastOne.AtLeastOne<'T>) = 
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Entities = entities |> AtLeastOne.getSeq |> Seq.toList }
            , state.TableMappings)

    /// Sets multiple values for INSERT. (Should have at least one value.)
    [<CustomOperation("entities", MaintainsVariableSpace = true)>]
    member this.Entities (state:QuerySource<'T>, entities: 'T seq) = 
        let spec = state |> getQueryOrDefault
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>(
            { spec with Entities = entities |> Seq.toList }
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
    
    /// Sets the identity field that should be returned from the insert and excludes it from the insert columns.
    [<CustomOperation("getId", MaintainsVariableSpace = true)>]
    member this.GetId (state: QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>, [<ProjectionParameter>] idProperty) = 
        // Exclude the identity column
        let spec = this.ExcludeColumn(state, idProperty).Query
        let prop = LinqExpressionVisitors.visitPropertySelector<'T, 'InsertReturn> idProperty :?> Reflection.PropertyInfo
        
        QuerySource<'T, InsertQuerySpec<'T, 'InsertReturn>>({ spec with IdentityField = Some prop.Name }, state.TableMappings)

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
                let! cancel = Async.CancellationToken
                if state.Query.Entities |> Seq.isEmpty then
                    return Unchecked.defaultof<'InsertReturn>
                else
                    let! insertReturn = ctx.InsertAsyncWithOptions (insertQuery, cancel) |> Async.AwaitTask
                    return insertReturn
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }


/// An insert builder that returns an Async result.
type InsertTaskBuilder<'Inserted, 'InsertReturn>(ct: ContextType, cancellationToken: CancellationToken) =
    inherit InsertBuilder<'Inserted, 'InsertReturn>()
    
    new(ct) = InsertTaskBuilder(ct, CancellationToken.None)

    member this.Run (state: QuerySource<'Inserted, InsertQuerySpec<'Inserted, 'InsertReturn>>) = 
        task {
            let! ctx = ContextUtils.getContext ct
            try 
                let insertQuery = InsertQuery<'Inserted, 'InsertReturn>(state.Query)
                if state.Query.Entities |> Seq.isEmpty then
                    return Unchecked.defaultof<'InsertReturn>
                else
                    let! insertReturn = ctx.InsertAsyncWithOptions (insertQuery, cancellationToken)
                    return insertReturn
            finally 
                ContextUtils.disposeIfNotShared ct ctx
        }


/// Builds an insert query that can be manually run by piping into QueryContext insert methods
let insert<'Inserted, 'InsertReturn> = 
    InsertBuilder<'Inserted, 'InsertReturn>()

/// Builds an insert query that returns an Async result
let insertAsync<'Inserted, 'InsertReturn> ct = 
    InsertAsyncBuilder<'Inserted, 'InsertReturn>(ct)

/// Builds an insert query that returns a Task result
let insertTask<'Inserted, 'InsertReturn> ct = 
    InsertTaskBuilder<'Inserted, 'InsertReturn>(ct)
    
/// Builds an insert query with a CancellationToken - returns a Task result
let insertTaskCancellable<'Inserted, 'InsertReturn> ct cancellationToken =
    InsertTaskBuilder<'Inserted, 'InsertReturn>(ct, cancellationToken)
